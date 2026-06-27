using System.Text;

namespace Tag.Core.IO;

/// <summary>Reads a file as bytes and decodes it via <see cref="EncodingDetector"/>.</summary>
public static class FileReader
{
    public static (Encoding Encoding, string Text) Read(string path)
    {
        byte[] bytes = File.ReadAllBytes(path);
        return EncodingDetector.Detect(bytes);
    }
}
