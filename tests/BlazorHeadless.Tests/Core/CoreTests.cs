namespace BlazorHeadless.Tests.Core;

public class FilterTests
{
    [Theory]
    [InlineData("Banana", "nan", true)]
    [InlineData("Banana", "NAN", true)]
    [InlineData("Banana", "x", false)]
    [InlineData("Banana", "", true)]
    [InlineData(null, "a", false)]
    public void Contains_ignores_case(string? text, string query, bool expected)
        => Assert.Equal(expected, BhFilter.Contains(text, query));

    [Fact]
    public void StartsWith_and_AllWords()
    {
        Assert.True(BhFilter.StartsWith("Apple pie", "app"));
        Assert.False(BhFilter.StartsWith("Apple pie", "pie"));
        Assert.True(BhFilter.AllWords("Apple pie with cream", "cream apple"));
        Assert.False(BhFilter.AllWords("Apple pie", "apple tart"));
    }

    [Fact]
    public void Score_ranks_exact_then_prefix_then_fuzzy()
    {
        var exact = BhFilter.Score("Settings", "settings");
        var prefix = BhFilter.Score("Settings", "set");
        var inside = BhFilter.Score("Reset settings", "set");
        var fuzzy = BhFilter.Score("Open settings", "ost");
        var none = BhFilter.Score("Profile", "xyz");

        Assert.Equal(1, exact);
        Assert.True(prefix > inside, $"{prefix} > {inside}");
        Assert.True(inside > 0);
        Assert.True(fuzzy > 0);
        Assert.Equal(0, none);
        Assert.Equal(1, BhFilter.Score("anything", " "));
    }

    [Fact]
    public void Keywords_match_with_a_small_penalty()
    {
        var byKeyword = BhFilter.Score("Search emoji", "smiley", ["smiley"]);
        Assert.True(byKeyword is > 0 and < 1);
    }

    [Fact]
    public void Fuzzy_filters_and_sorts()
    {
        var result = BhFilter.Fuzzy(["Profile", "Billing", "Billing history", "Bill Murray"], "bill", s => s).ToArray();
        Assert.Equal("Billing", result[0]);
        Assert.DoesNotContain("Profile", result);
    }
}

public class TypeaheadTests
{
    private static readonly string[] Items = ["Apple", "Apricot", "Banana", "Blueberry", "Cherry"];

    [Fact]
    public void Single_characters_cycle_through_matches()
    {
        var t = new BhTypeahead();
        var disabled = (int _) => false;
        Assert.Equal(2, t.SearchIndex(Items.Length, "b", 0, i => Items[i], disabled));
        t.Reset();
        Assert.Equal(3, t.SearchIndex(Items.Length, "b", 2, i => Items[i], disabled));
        // "bb" cycles like pressing "b" twice.
        Assert.Equal(2, t.SearchIndex(Items.Length, "b", 3, i => Items[i], disabled));
    }

    [Fact]
    public void Multiple_characters_match_a_prefix_and_skip_disabled_items()
    {
        var t = new BhTypeahead();
        // Apple is disabled: "a" lands on Apricot.
        Assert.Equal(1, t.SearchIndex(Items.Length, "a", -1, i => Items[i], i => i == 0));
        t.Reset();
        Assert.Equal(0, t.SearchIndex(Items.Length, "a", -1, i => Items[i], _ => false));
        // A longer search may stay on the current item, then moves on as it narrows.
        Assert.Equal(0, t.SearchIndex(Items.Length, "p", 0, i => Items[i], _ => false));
        Assert.Equal(1, t.SearchIndex(Items.Length, "r", 0, i => Items[i], _ => false));
        Assert.Equal("apr", t.Buffer);
        Assert.True(t.IsActive);
    }

    [Fact]
    public void Buffer_expires()
    {
        var t = new BhTypeahead { Timeout = TimeSpan.Zero };
        t.SearchIndex(Items.Length, "c", -1, i => Items[i], _ => false);
        Thread.Sleep(5);
        Assert.Equal(string.Empty, t.Buffer);
        Assert.False(t.IsActive);
    }

    [Theory]
    [InlineData("a", false, false, true)]
    [InlineData(" ", false, false, false)] // Space only extends an active search (handled by the caller)
    [InlineData("ArrowDown", false, false, false)]
    [InlineData("a", true, false, false)]
    [InlineData("a", false, true, false)]
    public void Typeahead_keys(string key, bool ctrl, bool alt, bool expected)
        => Assert.Equal(expected, BhTypeahead.IsTypeaheadKey(key, ctrl, alt));
}
