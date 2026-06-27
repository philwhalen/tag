namespace Tag.Core.IO;

/// <summary>
/// Builds the output path for an input file by inserting the literal "ordered" before the
/// final extension (e.g. <c>cow.md</c> → <c>cowordered.md</c>, <c>my.notes.txt</c> →
/// <c>my.notesordered.txt</c>, extensionless <c>README</c> → <c>READMEordered</c>). The
/// output is placed beside the input. On a name collision the file is never overwritten;
/// instead an incrementing suffix is added: <c>...ordered(1).ext</c>, <c>(2)</c>, …
/// </summary>
public static class OutputNamer
{
    /// <summary>Returns the base output name (no collision handling), filename only.</summary>
    public static string BaseName(string inputFileName)
    {
        string stem = Path.GetFileNameWithoutExtension(inputFileName);
        string ext = Path.GetExtension(inputFileName); // includes leading dot, or "" if none
        return stem + "ordered" + ext;
    }

    /// <summary>
    /// Returns a full output path beside <paramref name="inputPath"/> that does not yet
    /// exist, applying the "ordered" naming rule and collision suffixing.
    /// </summary>
    public static string ResolvePath(string inputPath)
    {
        string dir = Path.GetDirectoryName(Path.GetFullPath(inputPath)) ?? ".";
        string fileName = Path.GetFileName(inputPath);
        string stem = Path.GetFileNameWithoutExtension(fileName);
        string ext = Path.GetExtension(fileName);

        string candidate = Path.Combine(dir, stem + "ordered" + ext);
        if (!File.Exists(candidate))
            return candidate;

        for (int n = 1; ; n++)
        {
            candidate = Path.Combine(dir, $"{stem}ordered({n}){ext}");
            if (!File.Exists(candidate))
                return candidate;
        }
    }
}
