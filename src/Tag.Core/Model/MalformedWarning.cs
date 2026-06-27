namespace Tag.Core.Model;

/// <summary>
/// An advisory warning about a tag-like construct that is not a valid tag. The
/// offending text is left in the document verbatim (as untagged text); warnings
/// never block producing output.
/// </summary>
/// <param name="Line">1-based line number of the construct.</param>
/// <param name="Column">1-based column number of the construct.</param>
/// <param name="Snippet">The offending text (possibly truncated).</param>
/// <param name="Reason">Human-readable explanation.</param>
public sealed record MalformedWarning(int Line, int Column, string Snippet, string Reason);
