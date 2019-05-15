using System.IO;
using System.Text;

namespace VirtualDrive.Extensions
{
    public static class VirtualFileExtensions
    {
        public static void WriteAllText(this VirtualFile file, string text, Encoding encoding)
        {
            using (var stream = file.Open(FileMode.Truncate, FileAccess.Write))
            {
                var bytes = encoding.GetBytes(text);
                stream.Write(bytes, 0, bytes.Length);
            }
        }

        public static void WriteAllText(this VirtualFile file, string text)
        {
            WriteAllText(file, text, Encoding.UTF8);
        }

        public static string ReadAllText(this VirtualFile file, Encoding encoding)
        {
            using (var stream = file.Open(FileMode.Open, FileAccess.Read))
            {
                var buffer = new byte[stream.Length];
                stream.Read(buffer, 0, buffer.Length);
                var retv = encoding.GetString(buffer);
                return retv;
            }
        }

        public static string ReadAllText(this VirtualFile file)
        {
            return ReadAllText(file, Encoding.UTF8);
        }
    }
}