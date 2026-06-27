using Tag.Core.Ordering;
using Tag.Core.Parsing;
using Tag.Core.Rendering;
using Xunit;

namespace Tag.Core.Tests;

public class ReorderRenderTests
{
    private static string Run(string input, bool removeTags)
    {
        var doc = new TagParser().Parse(input);
        var reordered = TagReorderer.Reorder(doc);
        return DocumentRenderer.Render(reordered, removeTags);
    }

    // AC-1: cows.txt with ~3 then ~1, Remove ON -> ~1 content first, then ~3, no delimiters.
    [Fact]
    public void AC1_RemoveOn_SortsContentByNumber()
    {
        string input =
            "~3%Cows are animals that, contrary to popular belief, are not always black and white.%~\n" +
            "~1%Cows are interesting creatures.%~";
        string expected =
            "Cows are interesting creatures.\n" +
            "Cows are animals that, contrary to popular belief, are not always black and white.";
        Assert.Equal(expected, Run(input, removeTags: true));
    }

    // AC-2: same input, Keep ON -> full tags preserved, ~1 line before ~3 line.
    [Fact]
    public void AC2_KeepOn_PreservesTagsInSortedOrder()
    {
        string input =
            "~3%Cows are animals.%~\n" +
            "~1%Cows are interesting creatures.%~";
        string expected =
            "~1%Cows are interesting creatures.%~\n" +
            "~3%Cows are animals.%~";
        Assert.Equal(expected, Run(input, removeTags: false));
    }

    // AC-4: mixed content keeps untagged text in place while tags swap into sorted slots.
    [Fact]
    public void AC4_Mixed_RemoveOn_KeepsUntaggedTextInPlace()
    {
        string input =
            "Intro line.\n" +
            "~3%Third piece%~ and ~1%First piece%~\n" +
            "Middle text.\n" +
            "~2%Second piece%~";
        string expected =
            "Intro line.\n" +
            "First piece and Second piece\n" +
            "Middle text.\n" +
            "Third piece";
        Assert.Equal(expected, Run(input, removeTags: true));
    }

    [Fact]
    public void AC4_Mixed_KeepOn_RelocatesFullTags()
    {
        string input =
            "Intro line.\n" +
            "~3%Third piece%~ and ~1%First piece%~\n" +
            "Middle text.\n" +
            "~2%Second piece%~";
        string expected =
            "Intro line.\n" +
            "~1%First piece%~ and ~2%Second piece%~\n" +
            "Middle text.\n" +
            "~3%Third piece%~";
        Assert.Equal(expected, Run(input, removeTags: false));
    }

    [Fact]
    public void Gaps_AreSortedWithoutError()
    {
        Assert.Equal("AB", Run("~3%B%~~1%A%~", removeTags: true));
    }

    [Fact]
    public void DuplicateNumbers_KeepOriginalOrder_StableSort()
    {
        // Two tags numbered 1 should retain reading order (A before B); 2 last.
        string input = "~1%A%~~2%C%~~1%B%~";
        Assert.Equal("ABC", Run(input, removeTags: true));
    }

    [Fact]
    public void StrictWhitespace_NoCollapseWhenRemovingInlineTags()
    {
        // A~1%x%~B -> AxB (no spaces invented or removed).
        Assert.Equal("AxB", Run("A~1%x%~B", removeTags: true));
    }

    [Fact]
    public void RoundTrip_KeepOn_ReproducesInputWhenAlreadySorted()
    {
        string input = "x ~1%a%~ y ~2%b%~ z";
        Assert.Equal(input, Run(input, removeTags: false));
    }
}
