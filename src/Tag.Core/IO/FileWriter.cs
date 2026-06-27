using System.Text;

namespace Tag.Core.IO;

/// <summary>
/// Writes text to a new file using the given encoding, reproducing the encoding's BOM
/// (preamble) exactly. Uses <see cref="FileMode.CreateNew"/> so an existing file is never
/// overwritten — callers obtain a unique path from <see cref="OutputNamer"/> first.
/// </summary>
public static class FileWriter
{
    public static void Write(string path, string text, Encoding encoding)
    {
        using var fs = new FileStream(path, FileMode.CreateNew, FileAccess.Write, FileShare.None);
        byte[] preamble = encoding.GetPreamble();
        if (preamble.Length > 0)
            fs.Write(preamble, 0, preamble.Length);
        byte[] body = encoding.GetBytes(text);
        fs.Write(body, 0, body.Length);
    }
}
