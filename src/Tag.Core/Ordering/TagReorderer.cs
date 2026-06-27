using Tag.Core.Model;

namespace Tag.Core.Ordering;

/// <summary>
/// Reorders a document by the "sort tags within their slots" rule: untagged text is
/// fixed scaffolding; the tagged pieces are collected, stably sorted by their number
/// (ascending), and dropped back into the original tag positions (slots) in reading
/// order. Slot 0 receives the lowest-numbered tag, and so on.
/// </summary>
public static class TagReorderer
{
    public static ParsedDocument Reorder(ParsedDocument doc)
    {
        // Stable sort of the tags by number; LINQ OrderBy is stable, so equal numbers
        // keep their original reading order.
        var sortedTags = doc.Segments
            .OfType<TagSegment>()
            .OrderBy(t => t.Number)
            .ToList();

        var result = new List<Segment>(doc.Segments.Count);
        int slot = 0;
        foreach (var segment in doc.Segments)
        {
            if (segment is TagSegment)
                result.Add(sortedTags[slot++]); // refill slots in reading order
            else
                result.Add(segment); // untagged text stays exactly where it is
        }

        return doc with { Segments = result };
    }
}
