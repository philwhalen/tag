namespace Tag.Core.IO;

/// <summary>
/// Thrown when a file's text encoding cannot be confidently detected/decoded (e.g. a
/// binary file). Tag refuses to process such files rather than producing lossy output.
/// </summary>
public sealed class EncodingDetectionException : Exception
{
    public EncodingDetectionException(string message) : base(message) { }
}
