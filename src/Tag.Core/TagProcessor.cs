using Tag.Core.IO;
using Tag.Core.Model;
using Tag.Core.Ordering;
using Tag.Core.Parsing;
using Tag.Core.Rendering;

namespace Tag.Core;

/// <summary>
/// Façade orchestrating the Tag pipeline: load &amp; parse a file, render a reordered
/// preview, and save the result. The GUI typically calls <see cref="Load"/> once, then
/// <see cref="Render"/> on every toggle change (for the live preview), and <see cref="Save"/>
/// when the user commits.
/// </summary>
public sealed class TagProcessor
{
    private readonly TagParser _parser = new();

    /// <summary>
    /// Reads, encoding-detects, and parses the file. Throws
    /// <see cref="EncodingDetectionException"/> if the file is not decodable as text.
    /// </summary>
    public ParsedDocument Load(string path)
    {
        var (encoding, text) = FileReader.Read(path);
        return _parser.Parse(text, encoding);
    }

    /// <summary>Reorders the document (sort tags within slots) and renders to text.</summary>
    public string Render(ParsedDocument doc, bool removeTags)
    {
        var reordered = TagReorderer.Reorder(doc);
        return DocumentRenderer.Render(reordered, removeTags);
    }

    /// <summary>
    /// Writes <paramref name="renderedText"/> beside <paramref name="inputPath"/> using the
    /// "ordered" naming rule (never overwriting), round-tripping the document's encoding.
    /// Returns the actual output path written.
    /// </summary>
    public string Save(string inputPath, string renderedText, ParsedDocument doc)
    {
        string outputPath = OutputNamer.ResolvePath(inputPath);
        FileWriter.Write(outputPath, renderedText, doc.Encoding);
        return outputPath;
    }

    /// <summary>Convenience: load → render → save in one call. Returns the output path.</summary>
    public string Process(string inputPath, bool removeTags)
    {
        var doc = Load(inputPath);
        string rendered = Render(doc, removeTags);
        return Save(inputPath, rendered, doc);
    }
}
