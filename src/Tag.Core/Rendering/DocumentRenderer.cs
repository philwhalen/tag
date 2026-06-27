using System.Text;
using Tag.Core.Model;

namespace Tag.Core.Rendering;

/// <summary>
/// Renders a (reordered) document back to text. With <paramref name="removeTags"/> on,
/// only the tag delimiters are stripped, leaving the content; with it off, the full
/// <c>~N%content%~</c> marker is emitted. Untagged text and all whitespace are preserved
/// exactly — no normalization.
/// </summary>
public static class DocumentRenderer
{
    public static string Render(ParsedDocument doc, bool removeTags)
    {
        var sb = new StringBuilder();
        foreach (var segment in doc.Segments)
        {
            switch (segment)
            {
                case TextSegment t:
                    sb.Append(t.Text);
                    break;
                case TagSegment tag:
                    sb.Append(removeTags ? tag.Content : $"~{tag.Number}%{tag.Content}%~");
                    break;
            }
        }
        return sb.ToString();
    }
}
