using System.Buffers;

namespace System.IO;

#if NETSTANDARD2_0
internal static class StreamExtensions
{
    internal static int Read(this Stream stream, Span<byte> buffer)
    {
        var array = ArrayPool<byte>.Shared.Rent(buffer.Length);
        int result;
        try
        {
            var num = stream.Read(array, 0, buffer.Length);
            if (num > buffer.Length)
            {
                throw new IOException("Stream was too long.");
            }
            new Span<byte>(array, 0, num).CopyTo(buffer);
            result = num;
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(array, false);
        }
        return result;
    }
}
#endif
