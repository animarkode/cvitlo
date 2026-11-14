using System.Collections;
using System.Globalization;
using Cvitlo.Documents.Application.Resources;
using Shouldly;

namespace Cvitlo.Documents.Application.UnitTests.Resources;

public class TemplateStringsTests
{
    [Theory]
    [InlineData("en-us")]
    [InlineData("uk")]
    [InlineData("uk-ua")]
    [InlineData("pl")]
    [InlineData("pl-pl")]
    public void Culture_ShouldTranslateEveryKey(string name)
    {
        var invariantKeys = ReadStrings(CultureInfo.InvariantCulture).Keys;

        var translated = ReadStrings(CultureInfo.GetCultureInfo(name));

        invariantKeys.Count.ShouldBe(11);
        translated.Keys.ShouldBe(invariantKeys, ignoreOrder: true);
        translated.Values.ShouldAllBe(value => !string.IsNullOrWhiteSpace(value));
    }

    [Theory(Skip = "test-audit 03-NEW1: uk-ua and pl-pl wording differs from uk and pl (e.g. uk-ua Hobbies 'Захоплення' vs uk 'Хобі'); decide which is intended")]
    [InlineData("uk-ua", "uk")]
    [InlineData("pl-pl", "pl")]
    public void RegionalCulture_ShouldMatchItsNeutralCulture(string regional, string neutral)
    {
        var regionalStrings = ReadStrings(CultureInfo.GetCultureInfo(regional));
        var neutralStrings = ReadStrings(CultureInfo.GetCultureInfo(neutral));

        regionalStrings.ShouldBe(neutralStrings, ignoreOrder: true);
    }

    private static Dictionary<string, string?> ReadStrings(CultureInfo culture)
    {
        var set = TemplateStrings.ResourceManager.GetResourceSet(culture, true, false);
        set.ShouldNotBeNull();

        return set.Cast<DictionaryEntry>()
            .ToDictionary(e => (string)e.Key, e => e.Value as string, StringComparer.Ordinal);
    }
}
