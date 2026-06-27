using System.Text;

namespace Tag.Core.Model;

/// <summary>
/// The result of parsing a document: an ordered list of segments plus the metadata
/// needed to losslessly re-render and round-trip the file.
/// </summary>
/// <param name="Segments">Segments in document (reading) order.</param>
/// <param name="Warnings">Advisory warnings about malformed tag-like constructs.</param>
/// <param name="LineEnding">Detected dominant line ending ("\r\n" or "\n"); informational only.</param>
/// <param name="Encoding">Encoding to use when writing output (round-trips the input).</param>
/// <param name="HadTrailingNewline">Whether the source ended with a newline.</param>
public sealed record ParsedDocument(
    IReadOnlyList<Segment> Segments,
    IReadOnlyList<MalformedWarning> Warnings,
    string LineEnding,
    Encoding Encoding,
    bool HadTrailingNewline);
