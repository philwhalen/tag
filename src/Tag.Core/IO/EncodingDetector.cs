using System.Text;

namespace Tag.Core.IO;

/// <summary>
/// Detects a file's text encoding and decodes it. Detection order: BOM → no-BOM UTF-16
/// heuristic → strict UTF-8 → Latin-1 fallback for byte streams that still look like
/// text. Anything that looks binary (NUL bytes, many control characters) is rejected
/// with <see cref="EncodingDetectionException"/>. The returned encoding round-trips the
/// input, including BOM presence, so writing it back reproduces the original framing.
/// </summary>
public static class EncodingDetector
{
    public static (Encoding Encoding, string Text) Detect(byte[] bytes)
    {
        // 1) BOM-based detection (most reliable).
        if (HasPrefix(bytes, 0xEF, 0xBB, 0xBF))
            return (Encoding.UTF8, DecodeAfterBom(bytes, 3, Encoding.UTF8));
        if (HasPrefix(bytes, 0xFF, 0xFE))
            return (Encoding.Unicode, DecodeAfterBom(bytes, 2, Encoding.Unicode));
        if (HasPrefix(bytes, 0xFE, 0xFF))
            return (Encoding.BigEndianUnicode, DecodeAfterBom(bytes, 2, Encoding.BigEndianUnicode));

        if (bytes.Length == 0)
            return (new UTF8Encoding(false), string.Empty);

        // 2) BOM-less UTF-16 heuristic: ASCII-ish text has a NUL in every other byte.
        if (TryDetectUtf16NoBom(bytes, out var utf16Encoding, out var utf16Text))
            return (utf16Encoding!, utf16Text!);

        // 3) Strict UTF-8 (no BOM). Reject if it carries NUL bytes (binary signal).
        try
        {
            var strictUtf8 = new UTF8Encoding(false, true);
            string text = strictUtf8.GetString(bytes);
            if (!ContainsNul(bytes))
                return (new UTF8Encoding(false), text);
        }
        catch (DecoderFallbackException)
        {
            // Not valid UTF-8; fall through.
        }

        // 4) Single-byte fallback (Latin-1) only if the stream still looks like text.
        if (LooksLikeText(bytes))
            return (Encoding.Latin1, Encoding.Latin1.GetString(bytes));

        throw new EncodingDetectionException(
            "Could not detect a text encoding for this file. It appears to be binary or " +
            "uses an unsupported encoding, so it was not processed.");
    }

    private static bool TryDetectUtf16NoBom(byte[] bytes, out Encoding? encoding, out string? text)
    {
        encoding = null;
        text = null;
        if (bytes.Length < 2 || bytes.Length % 2 != 0)
            return false;

        int zerosAtEven = 0, zerosAtOdd = 0, pairs = bytes.Length / 2;
        for (int i = 0; i < bytes.Length; i++)
        {
            if (bytes[i] != 0) continue;
            if ((i & 1) == 0) zerosAtEven++; else zerosAtOdd++;
        }

        // Strong, lopsided NUL pattern indicates ASCII-range UTF-16. Use BOM-less encoding
        // instances so the round-trip does not introduce a BOM the source lacked. Confirm
        // the decoded characters actually look like text, so binary data whose NULs happen
        // to align is not mistaken for UTF-16.
        const double threshold = 0.30;
        if (zerosAtOdd >= pairs * threshold && zerosAtEven == 0)
        {
            var candidate = new UnicodeEncoding(bigEndian: false, byteOrderMark: false); // LE: high byte (odd) is NUL
            string decoded = candidate.GetString(bytes);
            if (DecodedLooksLikeText(decoded)) { encoding = candidate; text = decoded; return true; }
        }
        if (zerosAtEven >= pairs * threshold && zerosAtOdd == 0)
        {
            var candidate = new UnicodeEncoding(bigEndian: true, byteOrderMark: false);  // BE: high byte (even) is NUL
            string decoded = candidate.GetString(bytes);
            if (DecodedLooksLikeText(decoded)) { encoding = candidate; text = decoded; return true; }
        }
        return false;
    }

    private static bool DecodedLooksLikeText(string text)
    {
        if (text.Length == 0) return false;
        int bad = 0;
        foreach (char c in text)
        {
            if (c == '�') { bad++; continue; }            // replacement char = decode failure
            if (c < 0x20 && c != '\t' && c != '\n' && c != '\r') bad++;
        }
        return bad <= text.Length * 0.01; // <=1% control/replacement characters
    }

    private static bool LooksLikeText(byte[] bytes)
    {
        if (ContainsNul(bytes)) return false;
        int control = 0;
        foreach (byte b in bytes)
        {
            // Allow tab, LF, CR; everything else below space is a control character.
            if (b < 0x20 && b != 0x09 && b != 0x0A && b != 0x0D)
                control++;
        }
        return control <= bytes.Length * 0.01; // <=1% stray control bytes
    }

    private static bool ContainsNul(byte[] bytes)
    {
        foreach (byte b in bytes)
            if (b == 0) return true;
        return false;
    }

    private static string DecodeAfterBom(byte[] bytes, int bomLength, Encoding encoding) =>
        encoding.GetString(bytes, bomLength, bytes.Length - bomLength);

    private static bool HasPrefix(byte[] bytes, params byte[] prefix)
    {
        if (bytes.Length < prefix.Length) return false;
        for (int i = 0; i < prefix.Length; i++)
            if (bytes[i] != prefix[i]) return false;
        return true;
    }
}
