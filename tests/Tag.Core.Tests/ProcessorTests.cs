using System.Text;
using Tag.Core;
using Tag.Core.IO;
using Xunit;

namespace Tag.Core.Tests;

public class ProcessorTests
{
    // AC-3: cow.md -> cowordered.md in the same folder.
    [Fact]
    public void Process_NamesOutputWithOrderedRule()
    {
        string dir = NewTempDir();
        try
        {
            string input = Path.Combine(dir, "cow.md");
            File.WriteAllText(input, "~2%b%~\n~1%a%~", new UTF8Encoding(false));

            var processor = new TagProcessor();
            string output = processor.Process(input, removeTags: true);

            Assert.Equal(Path.Combine(dir, "cowordered.md"), output);
            Assert.Equal("a\nb", File.ReadAllText(output));
        }
        finally { Directory.Delete(dir, true); }
    }

    // AC-6: re-running produces ...ordered(1).ext without overwriting.
    [Fact]
    public void Process_DoesNotOverwrite_OnRerun()
    {
        string dir = NewTempDir();
        try
        {
            string input = Path.Combine(dir, "cows.txt");
            File.WriteAllText(input, "~1%a%~", new UTF8Encoding(false));

            var processor = new TagProcessor();
            string first = processor.Process(input, removeTags: true);
            string second = processor.Process(input, removeTags: true);

            Assert.Equal(Path.Combine(dir, "cowsordered.txt"), first);
            Assert.Equal(Path.Combine(dir, "cowsordered(1).txt"), second);
            Assert.True(File.Exists(first) && File.Exists(second));
        }
        finally { Directory.Delete(dir, true); }
    }

    // AC-7: UTF-16 file round-trips to UTF-16 output with CRLF preserved.
    [Fact]
    public void Process_RoundTripsUtf16_AndPreservesCrlf()
    {
        string dir = NewTempDir();
        try
        {
            string input = Path.Combine(dir, "u.txt");
            // Encoding.Unicode emits a BOM; content uses CRLF line endings.
            File.WriteAllText(input, "~2%b%~\r\n~1%a%~", Encoding.Unicode);

            var processor = new TagProcessor();
            string output = processor.Process(input, removeTags: false);

            byte[] outBytes = File.ReadAllBytes(output);
            Assert.Equal(Encoding.Unicode.GetPreamble(), outBytes.Take(2).ToArray()); // UTF-16 LE BOM
            string outText = Encoding.Unicode.GetString(outBytes, 2, outBytes.Length - 2);
            Assert.Equal("~1%a%~\r\n~2%b%~", outText);
        }
        finally { Directory.Delete(dir, true); }
    }

    // Warnings are advisory: malformed input still produces savable output with literal text.
    [Fact]
    public void Process_WithMalformedTag_StillSaves_LiteralPreserved()
    {
        string dir = NewTempDir();
        try
        {
            string input = Path.Combine(dir, "m.txt");
            File.WriteAllText(input, "~0%bad%~ ~1%good%~", new UTF8Encoding(false));

            var processor = new TagProcessor();
            var doc = processor.Load(input);
            Assert.NotEmpty(doc.Warnings);

            string rendered = processor.Render(doc, removeTags: true);
            // "~0%bad%~" stays literal; "good" is the sole real tag.
            Assert.Contains("~0%bad%~", rendered);
            Assert.Contains("good", rendered);
        }
        finally { Directory.Delete(dir, true); }
    }

    [Fact]
    public void Load_BinaryFile_Throws()
    {
        string dir = NewTempDir();
        try
        {
            string input = Path.Combine(dir, "bin.dat");
            File.WriteAllBytes(input, new byte[] { 0x00, 0x01, 0x02, 0xFF, 0x00, 0x80 });
            var processor = new TagProcessor();
            Assert.Throws<EncodingDetectionException>(() => processor.Load(input));
        }
        finally { Directory.Delete(dir, true); }
    }

    private static string NewTempDir()
    {
        string dir = Path.Combine(Path.GetTempPath(), "tagtests_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        return dir;
    }
}
