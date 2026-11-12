using Cvitlo.Common;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;

namespace Cvitlo.Auth.Application.Options;

public class KeycloakOptionsSetup(IConfiguration configuration) : IConfigureOptions<KeycloakOptions>
{
    public void Configure(KeycloakOptions options)
    {
        configuration.GetSection(ConfigurationConstants.Sections.Keycloak).Bind(options);
        options.Issuer = options.Issuer.TrimEnd('/');
    }
}
