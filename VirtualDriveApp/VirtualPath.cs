using System;
using System.Linq;
using System.Text;

namespace VirtualDrive
{
    public static class VirtualPath
    {
        public static char Separator => '/';
        public static char[] RestrictedSymbols { get; } = { Separator, '*', '?' };

        internal static void CheckRestrictedSymbols(string input)
        {
            if (RestrictedSymbols.Any(restrictedSymbol => input.IndexOf(restrictedSymbol) > 0))
                throw new ArgumentException("Path contains restricted symbols");
        }

        public static string GetFileName(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
                throw new ArgumentNullException(path);

            path = path.Trim(Separator);
            var pathLength = path.Length;
            var currentIndex = pathLength - 1;
            while (currentIndex > 0 && path[currentIndex] != Separator)
            {
                currentIndex--;
            }

            var nameStartPosition = currentIndex != 0 ? currentIndex + 1 : 0;
            return path.Substring(nameStartPosition);
        }

        public static string GetDirectoryName(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
                throw new ArgumentNullException(path);

            var pathLength = path.Length;
            var currentIndex = pathLength - 1;
            while (currentIndex > 0 && path[currentIndex] != Separator)
            {
                currentIndex--;
            }

            var directoryNameEndPosition = currentIndex;
            return path.Substring(0, directoryNameEndPosition);
        }

        public static string GetFileNameWithoutExtension(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
                throw new ArgumentNullException(path);

            var filename = GetFileName(path);
            var nameLength = filename.Length;
            var dotIndex = nameLength;
            var currentIndex = nameLength - 1;
            while (currentIndex > 0)
            {
                if (filename[currentIndex] == '.')
                    dotIndex = currentIndex;
                currentIndex--;
            }

            var nameEndPosition = dotIndex;
            return filename.Substring(0, nameEndPosition);
        }

        public static string GetFileExtension(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
                throw new ArgumentNullException(path);

            var filename = GetFileName(path);
            var nameLength = filename.Length;
            var dotIndex = nameLength - 1;
            var currentIndex = dotIndex;
            while (currentIndex > 0)
            {
                if (filename[currentIndex] == '.')
                    dotIndex = currentIndex;
                currentIndex--;
            }

            var extStartPosition = dotIndex + 1;
            return filename.Substring(extStartPosition);
        }

        public static string Combine(params string[] subPaths)
        {
            var builder = new StringBuilder();
            for (var i = 0; i < subPaths.Length; i++)
            {
                var subPath = subPaths[i];
                if (string.IsNullOrWhiteSpace(subPath))
                    continue;

                var startIndex = 0;
                var length = subPath.Length;
                if (subPath.Length == 1 && subPath.IndexOf(Separator) >= 0)
                    continue;

                if (subPath.IndexOf(Separator) == 0)
                {
                    startIndex++;
                    length--;
                }

                if (subPath.LastIndexOf(Separator) == subPath.Length - 1)
                    length--;

                builder.Append(subPath.Substring(startIndex, length));
                if (i != subPaths.Length - 1)//do not add separator at the end of path
                    builder.Append(Separator);
            }

            return builder.ToString();
        }
    }
}