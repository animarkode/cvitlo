using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using ErrorOr;
using Cvitlo.Auth.Application.Features.BackchannelLogout;
using Cvitlo.Auth.Application.Options;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using Shouldly;

namespace Cvitlo.Auth.Application.UnitTests.Features.BackchannelLogout;

public sealed class LogoutTokenValidatorTests : IDisposable
{
    private const string Issuer = "https://kc.test/realms/cvitlo";
    private const string ClientId = "cvitlo-web";
    private const string LogoutEvent = "http://schemas.openid.net/event/backchannel-logout";

    private readonly RSA _rsa = RSA.Create(2048);
    private readonly RSA _otherRsa = RSA.Create(2048);
    private readonly LogoutTokenValidator _validator;

    public LogoutTokenValidatorTests()
    {
        var configuration = new OpenIdConnectConfiguration();
        configuration.SigningKeys.Add(new RsaSecurityKey(_rsa));
        var options = new OpenIdConnectOptions
        {
            ConfigurationManager = new StaticConfigurationManager<OpenIdConnectConfiguration>(configuration)
        };

        _validator = new LogoutTokenValidator(
            new FixedOptionsMonitor(options),
            Microsoft.Extensions.Options.Options.Create(new KeycloakOptions { Issuer = Issuer, ClientId = ClientId }));
    }

    public void Dispose()
    {
        _rsa.Dispose();
        _otherRsa.Dispose();
    }

    [Fact]
    public async Task Validate_TokenWithSessionId_ShouldTargetThatSession()
    {
        var result = await ValidateAsync(Mint());

        result.IsError.ShouldBeFalse();
        result.Value.ShouldBe(new LogoutTarget("s1", "u1"));
    }

    [Fact]
    public async Task Validate_TokenWithSubjectOnly_ShouldTargetTheUser()
    {
        var result = await ValidateAsync(Mint(claims => claims.Remove("sid")));

        result.IsError.ShouldBeFalse();
        result.Value.KeycloakSessionId.ShouldBeNullOrEmpty();
        result.Value.Subject.ShouldBe("u1");
    }

    [Fact]
    public async Task Validate_NeitherSessionNorSubject_ShouldReturnValidationError()
    {
        var result = await ValidateAsync(Mint(claims =>
        {
            claims.Remove("sid");
            claims.Remove("sub");
        }));

        result.IsError.ShouldBeTrue();
        result.FirstError.Code.ShouldBe("Auth.BackchannelLogout");
    }

    [Fact]
    public async Task Validate_TokenWithNonce_ShouldReturnValidationError()
    {
        (await ValidateAsync(Mint(claims => claims["nonce"] = "n"))).IsError.ShouldBeTrue();
    }

    [Theory]
    [InlineData("missing")]
    [InlineData("string")]
    [InlineData("otherEvent")]
    public async Task Validate_EventsClaimWrong_ShouldReturnValidationError(string shape)
    {
        var token = Mint(claims =>
        {
            switch (shape)
            {
                case "missing":
                    claims.Remove("events");
                    break;
                case "string":
                    claims["events"] = LogoutEvent;
                    break;
                default:
                    claims["events"] = new Dictionary<string, object>(StringComparer.Ordinal) { ["http://schemas.openid.net/event/other"] = new Dictionary<string, object>(StringComparer.Ordinal) };
                    break;
            }
        });

        (await ValidateAsync(token)).IsError.ShouldBeTrue();
    }

    [Fact]
    public async Task Validate_WrongAudience_ShouldReturnValidationError()
    {
        (await ValidateAsync(Mint(audience: "other-client"))).IsError.ShouldBeTrue();
    }

    [Fact]
    public async Task Validate_WrongIssuer_ShouldReturnValidationError()
    {
        (await ValidateAsync(Mint(issuer: "https://evil.test/realms/cvitlo"))).IsError.ShouldBeTrue();
    }

    [Fact]
    public async Task Validate_ExpiredBeyondTheSkew_ShouldReturnValidationError()
    {
        (await ValidateAsync(Mint(expires: DateTime.UtcNow.AddMinutes(-3)))).IsError.ShouldBeTrue();
    }

    [Fact]
    public async Task Validate_ExpiredWithinTheSkew_ShouldSucceed()
    {
        (await ValidateAsync(Mint(expires: DateTime.UtcNow.AddMinutes(-1)))).IsError.ShouldBeFalse();
    }

    [Fact]
    public async Task Validate_NoExpiry_ShouldReturnValidationError()
    {
        var token = Mint(noExpiry: true);
        (await ValidateAsync(token)).IsError.ShouldBeTrue();
    }

    [Fact]
    public async Task Validate_SignedWithAnUnknownKey_ShouldReturnValidationError()
    {
        var credentials = new SigningCredentials(new RsaSecurityKey(_otherRsa), SecurityAlgorithms.RsaSha256);

        (await ValidateAsync(Mint(credentials: credentials))).IsError.ShouldBeTrue();
    }

    [Fact]
    public async Task Validate_HmacSignedWithThePublicKeyBytes_ShouldReturnValidationError()
    {
        var secret = _rsa.ExportSubjectPublicKeyInfo();
        var credentials = new SigningCredentials(new SymmetricSecurityKey(secret), SecurityAlgorithms.HmacSha256);

        (await ValidateAsync(Mint(credentials: credentials))).IsError.ShouldBeTrue();
    }

    [Fact]
    public async Task Validate_UnsignedToken_ShouldReturnValidationError()
    {
        var exp = DateTimeOffset.UtcNow.AddMinutes(5).ToUnixTimeSeconds();
        var payload = "{\"iss\":\"" + Issuer + "\",\"aud\":\"" + ClientId + "\",\"sub\":\"u1\",\"sid\":\"s1\",\"exp\":" + exp
                      + ",\"events\":{\"" + LogoutEvent + "\":{}}}";
        var token = $"{Base64Url("""{"alg":"none"}""")}.{Base64Url(payload)}.";

        (await ValidateAsync(token)).IsError.ShouldBeTrue();
    }

    [Fact]
    public async Task Validate_NotAToken_ShouldReturnValidationError()
    {
        (await ValidateAsync("garbage")).IsError.ShouldBeTrue();
    }

    private Task<ErrorOr<LogoutTarget>> ValidateAsync(string token)
    {
        return _validator.ValidateAsync(token, TestContext.Current.CancellationToken);
    }

    private string Mint(
        Action<Dictionary<string, object>>? tweak = null,
        string issuer = Issuer,
        string audience = ClientId,
        DateTime? expires = null,
        bool noExpiry = false,
        SigningCredentials? credentials = null)
    {
        var claims = new Dictionary<string, object>(StringComparer.Ordinal)
        {
            ["iss"] = issuer,
            ["aud"] = audience,
            ["iat"] = DateTimeOffset.UtcNow.AddHours(-2).ToUnixTimeSeconds(),
            ["sub"] = "u1",
            ["sid"] = "s1",
            ["events"] = new Dictionary<string, object>(StringComparer.Ordinal) { [LogoutEvent] = new Dictionary<string, object>(StringComparer.Ordinal) }
        };
        if (!noExpiry)
        {
            claims["exp"] = new DateTimeOffset(expires ?? DateTime.UtcNow.AddMinutes(5)).ToUnixTimeSeconds();
        }

        tweak?.Invoke(claims);

        // The handler adds a default exp to a descriptor without one, so the payload is written by hand.
        return new JsonWebTokenHandler().CreateToken(
            JsonSerializer.Serialize(claims),
            credentials ?? new SigningCredentials(new RsaSecurityKey(_rsa), SecurityAlgorithms.RsaSha256));
    }

    private static string Base64Url(string value)
    {
        return Convert.ToBase64String(Encoding.UTF8.GetBytes(value)).TrimEnd('=').Replace('+', '-').Replace('/', '_');
    }

    private sealed class FixedOptionsMonitor(OpenIdConnectOptions options) : IOptionsMonitor<OpenIdConnectOptions>
    {
        public OpenIdConnectOptions CurrentValue => options;

        public OpenIdConnectOptions Get(string? name) => options;

        public IDisposable? OnChange(Action<OpenIdConnectOptions, string?> listener) => null;
    }
}
