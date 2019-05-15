using System;
using System.IO;

namespace VirtualDrive.Internal
{
    internal static class BufferHelper
    {
        public static byte[] GetBuffer(Stream stream)
        {
            var bufferSize = (int)Math.Min(1024 * 1024, stream.Length); //1 MB
            return new byte[bufferSize];
        }
    }
}