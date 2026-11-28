using StudentFleaMarket.Application.Common.Helpers;
using Xunit;

namespace StudentFleaMarket.Tests;

public sealed class ItemCodeHelperTests
{
    [Fact]
    public void CleanRaw_keeps_legacy_letters_that_normalize_rewrites()
    {
        // Legacy seed code: Normalize() turns 'O' into '0', so lookups must also try the raw form.
        Assert.Equal("QDP0WMBD", ItemCodeHelper.Normalize("QDPOWMBD"));
        Assert.Equal("QDPOWMBD", ItemCodeHelper.CleanRaw(" qd-powmbd "));
    }

    [Theory]
    [InlineData("QDPOWMBD", "QDPOWMBD")]
    [InlineData("qdp0wmbd", "QDPOWMBD")]
    [InlineData("QD-POWMBD", "QDPOWMBD")]
    [InlineData("BK-7K9A2P", "BK7K9A2P")]
    public void Matches_compares_normalized_codes(string entered, string stored) =>
        Assert.True(ItemCodeHelper.Matches(entered, stored));

    [Theory]
    [InlineData("QDPOWMBX", "QDPOWMBD")]
    [InlineData("", "QDPOWMBD")]
    [InlineData(null, "QDPOWMBD")]
    public void Matches_rejects_different_or_empty_codes(string? entered, string stored) =>
        Assert.False(ItemCodeHelper.Matches(entered, stored));
}
