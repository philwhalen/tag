namespace Tag.Core.Model;

/// <summary>
/// A single piece of a parsed document, in reading order. A document is a flat,
/// ordered list of segments; tag segments mark reorderable "slots", text segments
/// are fixed scaffolding.
/// </summary>
public abstract record Segment;

/// <summary>Untagged text. Preserved verbatim and never moved.</summary>
public sealed record TextSegment(string Text) : Segment;

/// <summary>
/// A valid tag occupying a slot. <see cref="ReadingIndex"/> is the tag's position
/// among all valid tags in left-to-right, top-to-bottom reading order.
/// </summary>
public sealed record TagSegment(int Number, string Content, int ReadingIndex) : Segment;
