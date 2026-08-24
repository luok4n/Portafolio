using Portfolio.Application;
using Portfolio.Domain.ValueObjects;

namespace Portfolio.Tests.Application;

public sealed class LanguageNegotiatorTests
{
    [Fact]
    public void An_explicit_parameter_wins_over_the_header()
    {
        var result = LanguageNegotiator.Negotiate("es", "en-US,en;q=0.9");

        Assert.Equal(LanguageCode.Spanish, result.Language);
        Assert.Equal(LanguageSource.Explicit, result.Source);
    }

    [Fact]
    public void The_header_is_used_when_no_parameter_is_given()
    {
        var result = LanguageNegotiator.Negotiate(null, "es-CO,es;q=0.9,en;q=0.8");

        Assert.Equal(LanguageCode.Spanish, result.Language);
        Assert.Equal(LanguageSource.AcceptHeader, result.Source);
    }

    [Fact]
    public void Quality_values_decide_the_order()
    {
        // English is listed first but ranked lower, so Spanish wins.
        var result = LanguageNegotiator.Negotiate(null, "en;q=0.3,es;q=0.9");

        Assert.Equal(LanguageCode.Spanish, result.Language);
    }

    [Fact]
    public void An_unsupported_language_falls_through_to_the_next_candidate()
    {
        var result = LanguageNegotiator.Negotiate(null, "de,fr;q=0.9,es;q=0.5");

        Assert.Equal(LanguageCode.Spanish, result.Language);
        Assert.Equal(LanguageSource.AcceptHeader, result.Source);
    }

    [Theory]
    [InlineData(null, null)]
    [InlineData("", "")]
    [InlineData("de", "de,fr")]
    [InlineData("klingon", "*")]
    public void Nothing_usable_falls_back_to_the_default(string? explicitLanguage, string? header)
    {
        var result = LanguageNegotiator.Negotiate(explicitLanguage, header);

        Assert.Equal(LanguageCode.Default, result.Language);
        Assert.Equal(LanguageSource.Fallback, result.Source);
    }

    [Theory]
    [InlineData(";;;")]
    [InlineData("es;q=notanumber")]
    [InlineData("   ")]
    [InlineData(",,,")]
    public void A_malformed_header_never_throws(string header)
    {
        // A broken header from some client is not a reason to fail a request for a public page.
        var result = LanguageNegotiator.Negotiate(null, header);

        Assert.Contains(result.Language, LanguageCode.Supported);
    }

    [Fact]
    public void An_unparseable_quality_value_does_not_promote_the_entry()
    {
        // "es;q=bogus" must not outrank a well-formed English entry by defaulting to q=1.0.
        var result = LanguageNegotiator.Negotiate(null, "es;q=bogus,en;q=0.4");

        Assert.Equal(LanguageCode.English, result.Language);
    }

    [Theory]
    [InlineData("ES")]
    [InlineData("es-CO")]
    [InlineData("es-419")]
    [InlineData(" es ")]
    public void Regional_and_cased_tags_resolve_to_the_base_language(string value)
    {
        Assert.True(LanguageCode.TryParse(value, out var code));
        Assert.Equal(LanguageCode.Spanish, code);
    }
}
