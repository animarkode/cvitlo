using Cvitlo.Auth.Application.Features.Login;
using Shouldly;

namespace Cvitlo.Auth.Application.UnitTests.Features.Login;

public class LocalReturnUrlTests
{
    [Theory]
    [InlineData("/")]
    [InlineData("/account")]
    [InlineData("/a?b=c#d")]
    public void From_LocalPath_ShouldReturnIt(string path)
    {
        LocalReturnUrl.From(path).ShouldBe(path);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("https://evil.example")]
    [InlineData("//evil.example")]
    [InlineData("/\\evil.example")]
    [InlineData("javascript:alert(1)")]
    [InlineData("account")]
    [InlineData("/a\nb")]
    [InlineData("/a\tb")]
    [InlineData("/a\u0000b")]
    public void From_ForeignOrMalformed_ShouldReturnRoot(string? returnUrl)
    {
        LocalReturnUrl.From(returnUrl).ShouldBe("/");
    }
}
