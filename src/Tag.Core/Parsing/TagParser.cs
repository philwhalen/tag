using System.Text;
using System.Text.RegularExpressions;
using Tag.Core.Model;

namespace Tag.Core.Parsing;

/// <summary>
/// Parses raw document text into an ordered list of <see cref="Segment"/>s plus
/// advisory warnings. A valid tag is <c>~&lt;digits&gt;%&lt;content&gt;%~</c> where the number
/// is an integer ≥ 1 and the first <c>%~</c> closes the tag (content may span lines).
///
/// Malformed-tag detection is deliberately conservative to avoid false positives on
/// arbitrary text files: it warns only on (1) a complete tag-shaped construct whose
/// number is not a valid ≥ 1 integer, (2) a <c>~&lt;digits&gt;%</c> opening with no closing
/// <c>%~</c>, and (3) a stray <c>%~</c> closing with no matching opening. Untagged text
/// (and any malformed construct) is preserved verbatim.
/// </summary>
public sealed class TagParser
{
    private const int SnippetMaxLength = 40;

    // A valid, complete tag: ~<digits>%<content>%~ (non-greedy: first %~ closes; content may span lines).
    private static readonly Regex ValidTag =
        new(@"\G~(\d+)%(.*?)%~", RegexOptions.Singleline | RegexOptions.Compiled);

    // A complete tag-shaped construct whose number part is present but not a valid integer.
    // The number class excludes %, ~ and newlines so it cannot swallow delimiters or span lines.
    private static readonly Regex TagShaped =
        new(@"\G~([^%~\r\n]*)%(.*?)%~", RegexOptions.Singleline | RegexOptions.Compiled);

    // An opening delimiter with a digit number: ~<digits>% (used to flag unterminated tags).
    private static readonly Regex Opening =
        new(@"\G~\d+%", RegexOptions.Compiled);

    /// <summary>Parses text using UTF-8 as the round-trip encoding (convenience for tests).</summary>
    public ParsedDocument Parse(string text) => Parse(text, new UTF8Encoding(false));

    public ParsedDocument Parse(string text, Encoding encoding)
    {
        var segments = new List<Segment>();
        var warnings = new List<MalformedWarning>();
        int readingIndex = 0;
        int textStart = 0;
        int i = 0;
        int n = text.Length;

        void FlushText(int upTo)
        {
            if (upTo > textStart)
                segments.Add(new TextSegment(text.Substring(textStart, upTo - textStart)));
        }

        while (i < n)
        {
            if (text[i] != '~')
            {
                i++;
                continue;
            }

            // 1) Valid, complete tag.
            var m = ValidTag.Match(text, i);
            if (m.Success)
            {
                FlushText(i);
                if (int.TryParse(m.Groups[1].Value, out int number) && number >= 1)
                {
                    segments.Add(new TagSegment(number, m.Groups[2].Value, readingIndex++));
                }
                else
                {
                    // Well-formed delimiters but number is 0 or overflows: keep literal, warn.
                    warnings.Add(MakeWarning(text, i, m.Value,
                        "Tag number must be a whole number ≥ 1; left as literal text."));
                    segments.Add(new TextSegment(m.Value));
                }
                textStart = i + m.Length;
                i = textStart;
                continue;
            }

            // 2) Complete tag-shaped construct with an invalid (non-digit / empty) number.
            var ms = TagShaped.Match(text, i);
            if (ms.Success)
            {
                FlushText(i);
                warnings.Add(MakeWarning(text, i, ms.Value,
                    "Tag number must be a whole number ≥ 1; left as literal text."));
                segments.Add(new TextSegment(ms.Value));
                textStart = i + ms.Length;
                i = textStart;
                continue;
            }

            // 3) Unterminated opening delimiter (~<digits>% with no matching %~).
            var mo = Opening.Match(text, i);
            if (mo.Success)
            {
                warnings.Add(MakeWarning(text, i, mo.Value,
                    "Unterminated tag: opening delimiter has no matching '%~'."));
                i += mo.Length; // leave characters in place as literal text
                continue;
            }

            // 4) Stray closing delimiter '%~' (e.g. a nested tag's leftover remainder).
            if (i > 0 && text[i - 1] == '%')
            {
                warnings.Add(MakeWarning(text, i - 1, "%~",
                    "Stray closing delimiter '%~' with no matching opening tag."));
                i++;
                continue;
            }

            // 5) A lone '~' that is not part of any tag construct.
            i++;
        }

        FlushText(n);

        string lineEnding = text.Contains("\r\n") ? "\r\n" : "\n";
        bool hadTrailingNewline = text.EndsWith("\n");

        return new ParsedDocument(segments, warnings, lineEnding, encoding, hadTrailingNewline);
    }

    private static MalformedWarning MakeWarning(string text, int offset, string raw, string reason)
    {
        int line = 1, col = 1;
        for (int k = 0; k < offset && k < text.Length; k++)
        {
            if (text[k] == '\n') { line++; col = 1; }
            else col++;
        }

        string snippet = raw.Replace("\r", " ").Replace("\n", " ");
        if (snippet.Length > SnippetMaxLength)
            snippet = snippet.Substring(0, SnippetMaxLength) + "…";

        return new MalformedWarning(line, col, snippet, reason);
    }
}
