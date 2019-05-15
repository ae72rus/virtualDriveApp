using System;
using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Text;

namespace VirtualDrive.Internal
{
    internal class NamesCache : IDisposable
    {
        private readonly ConcurrentDictionary<long, string> _cachedData = new ConcurrentDictionary<long, string>();
        private readonly ConcurrentDictionary<string, long> _swappedCachedData = new ConcurrentDictionary<string, long>();

        public bool TryGetName(long id, out string name)
        {
            return _cachedData.TryGetValue(id, out name);
        }

        public bool TryGetId(string name, out long id)
        {
            if (name == null)
                throw new ArgumentNullException(nameof(name));

            var hashed = getHashedPath(name);
            return _swappedCachedData.TryGetValue(hashed, out id);
        }

        public void Update(long id, string name)
        {
            if (name == null)
                throw new ArgumentNullException(nameof(name));

            Add(id, name);
        }

        public void Add(long id, string name)
        {
            _cachedData[id] = name ?? throw new ArgumentNullException(nameof(name));
            var hashed = getHashedPath(name);//it should help on long names, but on short names it'll slow down process
            _swappedCachedData[hashed] = id;
        }

        public void Remove(long id)
        {
            if (!_cachedData.TryRemove(id, out var name))
                return;

            if (string.IsNullOrWhiteSpace(name))
                return;

            var hashed = getHashedPath(name);

            _swappedCachedData.TryRemove(hashed, out var i);
        }

        private string getHashedPath(string path)
        {
            using (var sha256 = SHA256.Create())
            {
                var strBytes = Encoding.UTF8.GetBytes(path.ToUpper().Trim(VirtualPath.Separator, ' '));
                var hashedBytes = sha256.ComputeHash(strBytes);
                var builder = new StringBuilder();
                foreach (var hashedByte in hashedBytes)
                    builder.Append(hashedByte.ToString("x2"));

                return builder.ToString();
            }
        }

        public void Dispose()
        {
            _cachedData.Clear();
            _swappedCachedData.Clear();
        }
    }
}