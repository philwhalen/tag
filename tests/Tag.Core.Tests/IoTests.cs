using System.Text;
using Tag.Core.IO;
using Xunit;

namespace Tag.Core.Tests;

public class IoTests
{
    [Theory]
    [InlineData("cows.txt", "cowsordered.txt")]
    [InlineData("cow.md", "cowordered.md")]
    [InlineData("my.notes.txt", "my.notesordered.txt")]
    [InlineData("README", "READMEordered")]
    public void BaseName_InsertsOrderedBeforeFinalExtension(string input, string expected)
    {
        Assert.Equal(expected, OutputNamer.BaseName(input));
    }

    [Fact]
    public void ResolvePath_SuffixesOnCollision_NeverOverwrites()
    {
        string dir = NewTempDir();
        try
        {
            string input = Path.Combine(dir, "cows.txt");
            File.WriteAllText(input, "data");

            string first = OutputNamer.ResolvePath(input);
            Assert.Equal(Path.Combine(dir, "cowsordered.txt"), first);

            File.WriteAllText(first, "x"); // simulate existing output
            string second = OutputNamer.ResolvePath(input);
            Assert.Equal(Path.Combine(dir, "cowsordered(1).txt"), second);

            File.WriteAllText(second, "x");
            string third = OutputNamer.ResolvePath(input);
            Assert.Equal(Path.Combine(dir, "cowsordered(2).txt"), third);
        }
        finally { Directory.Delete(dir, true); }
    }

    [Fact]
    public void Utf8Bom_RoundTrips()
    {
        byte[] bom = { 0xEF, 0xBB, 0xBF };
        byte[] body = Encoding.UTF8.GetBytes("héllo");
        var (encoding, text) = EncodingDetector.Detect(Concat(bom, body));
        Assert.Equal("héllo", text);
        Assert.Equal(bom, encoding.GetPreamble());
    }

    [Fact]
    public void Utf16Le_WithBom_RoundTrips()
    {
        byte[] bytes = Encoding.Unicode.GetPreamble()
            .Concat(Encoding.Unicode.GetBytes("héllo\r\nworld")).ToArray();
        var (encoding, text) = EncodingDetector.Detect(bytes);
        Assert.Equal("héllo\r\nworld", text);
        Assert.Equal(Encoding.Unicode.GetPreamble(), encoding.GetPreamble());
    }

    [Fact]
    public void PlainAscii_DetectsAsUtf8NoBom()
    {
        var (encoding, text) = EncodingDetector.Detect(Encoding.ASCII.GetBytes("hello"));
        Assert.Equal("hello", text);
        Assert.Empty(encoding.GetPreamble());
    }

    [Fact]
    public void BinaryFile_IsRejected()
    {
        byte[] binary = { 0x00, 0x01, 0x02, 0xFF, 0x00, 0x10, 0x80, 0x03 };
        Assert.Throws<EncodingDetectionException>(() => EncodingDetector.Detect(binary));
    }

    [Fact]
    public void FileWriter_WithUtf8Bom_WritesPreamble()
    {
        string dir = NewTempDir();
        try
        {
            string path = Path.Combine(dir, "out.txt");
            FileWriter.Write(path, "abc", Encoding.UTF8);
            byte[] bytes = File.ReadAllBytes(path);
            Assert.Equal(new byte[] { 0xEF, 0xBB, 0xBF }, bytes.Take(3).ToArray());
        }
        finally { Directory.Delete(dir, true); }
    }

    [Fact]
    public void FileWriter_NeverOverwritesExisting()
    {
        string dir = NewTempDir();
        try
        {
            string path = Path.Combine(dir, "out.txt");
            File.WriteAllText(path, "existing");
            Assert.Throws<IOException>(() => FileWriter.Write(path, "new", new UTF8Encoding(false)));
        }
        finally { Directory.Delete(dir, true); }
    }

    private static string NewTempDir()
    {
        string dir = Path.Combine(Path.GetTempPath(), "tagtests_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        return dir;
    }

    private static byte[] Concat(byte[] a, byte[] b) => a.Concat(b).ToArray();
}
