namespace Lanflix.Application.Common.Exceptions;

public class TranscodingException : ApplicationException
{
    public string? FFmpegOutput { get; }

    public TranscodingException(string message, string? ffmpegOutput = null)
        : base(message)
    {
        FFmpegOutput = ffmpegOutput;
    }

    public TranscodingException(string message, Exception innerException, string? ffmpegOutput = null)
        : base(message, innerException)
    {
        FFmpegOutput = ffmpegOutput;
    }
}
