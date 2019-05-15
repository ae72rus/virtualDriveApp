using System.IO;

namespace VirtualDrive.Internal
{
    internal static class StreamExtensions
    {
        public static int GetProgress(this Stream stream)
        {
            return (int)((float)stream.Position / stream.Length * 100);
        }
    }
}