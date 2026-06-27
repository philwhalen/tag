using Tag.Core.Model;
using Tag.Core.Parsing;
using Xunit;

namespace Tag.Core.Tests;

public class ParserTests
{
    private static ParsedDocument Parse(string text) => new TagParser().Parse(text);

    [Fact]
    public void SingleTag_IsParsed()
    {
        var doc = Parse("~3%Cows are animals.%~");
        var tag = Assert.IsType<TagSegment>(Assert.Single(doc.Segments));
        Assert.Equal(3, tag.Number);
        Assert.Equal("Cows are animals.", tag.Content);
        Assert.Empty(doc.Warnings);
    }

    [Fact]
    public void MultipleTags_OnePerLine_GetSequentialReadingIndex()
    {
        var doc = Parse("~3%B%~\n~1%A%~");
        var tags = doc.Segments.OfType<TagSegment>().ToList();
        Assert.Equal(2, tags.Count);
        Assert.Equal(0, tags[0].ReadingIndex);
        Assert.Equal(1, tags[1].ReadingIndex);
    }

    [Fact]
    public void MultipleTags_OnOneLine_AreReadLeftToRight()
    {
        var doc = Parse("~3%B%~ and ~1%A%~");
        var tags = doc.Segments.OfType<TagSegment>().ToList();
        Assert.Equal(3, tags[0].Number);
        Assert.Equal(1, tags[1].Number);
        // The untagged " and " between them is preserved as its own segment.
        Assert.Contains(doc.Segments, s => s is TextSegment t && t.Text == " and ");
    }

    [Fact]
    public void MultiLineContent_IsCapturedAsOneBlock()
    {
        var doc = Parse("~1%line one\nline two%~");
        var tag = Assert.IsType<TagSegment>(Assert.Single(doc.Segments));
        Assert.Equal("line one\nline two", tag.Content);
    }

    [Fact]
    public void UntaggedOnly_ProducesSingleTextSegment()
    {
        var doc = Parse("just plain text");
        var text = Assert.IsType<TextSegment>(Assert.Single(doc.Segments));
        Assert.Equal("just plain text", text.Text);
        Assert.Empty(doc.Warnings);
    }

    [Fact]
    public void EmptyFile_ProducesNoSegments()
    {
        var doc = Parse("");
        Assert.Empty(doc.Segments);
        Assert.Empty(doc.Warnings);
    }

    [Fact]
    public void LeadingZeros_ParseAsInteger()
    {
        var doc = Parse("~01%x%~");
        var tag = Assert.IsType<TagSegment>(Assert.Single(doc.Segments));
        Assert.Equal(1, tag.Number);
        Assert.Empty(doc.Warnings);
    }

    [Fact]
    public void ZeroNumber_IsMalformed_AndLeftLiteral()
    {
        var doc = Parse("~0%x%~");
        Assert.DoesNotContain(doc.Segments, s => s is TagSegment);
        var text = Assert.IsType<TextSegment>(Assert.Single(doc.Segments));
        Assert.Equal("~0%x%~", text.Text);
        Assert.Single(doc.Warnings);
    }

    [Fact]
    public void NegativeNumber_IsMalformed_AndLeftLiteral()
    {
        var doc = Parse("~-3%x%~");
        Assert.DoesNotContain(doc.Segments, s => s is TagSegment);
        Assert.Contains(doc.Segments, s => s is TextSegment t && t.Text == "~-3%x%~");
        Assert.Single(doc.Warnings);
    }

    [Fact]
    public void NonIntegerNumber_IsMalformed_AndLeftLiteral()
    {
        var doc = Parse("~1.5%x%~");
        Assert.DoesNotContain(doc.Segments, s => s is TagSegment);
        Assert.Single(doc.Warnings);
    }

    [Fact]
    public void EmptyNumber_IsMalformed()
    {
        var doc = Parse("~%x%~");
        Assert.DoesNotContain(doc.Segments, s => s is TagSegment);
        Assert.Single(doc.Warnings);
    }

    [Fact]
    public void Overflow_IsMalformed()
    {
        var doc = Parse("~99999999999999999999%x%~");
        Assert.DoesNotContain(doc.Segments, s => s is TagSegment);
        Assert.Single(doc.Warnings);
    }

    [Fact]
    public void UnterminatedTag_IsWarned_AndTextPreserved()
    {
        var doc = Parse("~3%hello with no closing");
        Assert.DoesNotContain(doc.Segments, s => s is TagSegment);
        Assert.Single(doc.Warnings);
        // The full original text survives verbatim.
        string joined = string.Concat(doc.Segments.OfType<TextSegment>().Select(t => t.Text));
        Assert.Equal("~3%hello with no closing", joined);
    }

    [Fact]
    public void Nested_InnerCloses_OuterRemainderWarned()
    {
        var doc = Parse("~1%a ~2%b%~ c%~");
        var tag = Assert.IsType<TagSegment>(doc.Segments.OfType<TagSegment>().Single());
        Assert.Equal(1, tag.Number);
        Assert.Equal("a ~2%b", tag.Content);
        // The leftover " c%~" stray closing produces a warning.
        Assert.Single(doc.Warnings);
        Assert.Contains(doc.Warnings, w => w.Reason.Contains("Stray closing"));
    }

    [Fact]
    public void TildeInProse_DoesNotFalseWarn()
    {
        var doc = Parse("about ~5 items and 100% done");
        Assert.Empty(doc.Warnings);
        Assert.DoesNotContain(doc.Segments, s => s is TagSegment);
    }

    [Fact]
    public void Warning_HasLineAndColumn()
    {
        var doc = Parse("ok line\nthen ~0%bad%~ here");
        var w = Assert.Single(doc.Warnings);
        Assert.Equal(2, w.Line);
        Assert.Equal(6, w.Column); // 1-based column of '~' on line 2: "then " = 5 chars
    }
}
