using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using VirtualDrive;

namespace VirtualFileSystem
{
    public class FileChunkEntry
    {
        public long Position { get; set; }
        public int Length { get; set; }
    }

    public abstract class BaseEntry
    {
        protected const int NameMaxLength = 1024;
        private string _name;
        public long Id { get; set; }
        public int Length => GetBytes().Length;

        public byte Mark => GetMark();

        public string Name
        {
            get => _name;
            set
            {
                if (string.IsNullOrWhiteSpace(value))
                    throw new InvalidOperationException("Name could not be empty");

                if (value.Length > NameMaxLength)
                    throw new InvalidOperationException($"Max name length is {NameMaxLength} symbols");

                _name = value;
            }
        }

        public DateTime CreationTime { get; set; } = DateTime.Now;
        public DateTime ModificationTime { get; set; } = DateTime.Now;


        public abstract byte[] GetBytes();
        protected abstract byte GetMark();
    }

    public class FileEntry : BaseEntry
    {
        protected const int ExtensionMaxLength = 1024;
        private string _extension;

        public string Extension
        {
            get => _extension;
            set
            {
                if (value != null && value.Length > ExtensionMaxLength)
                    throw new InvalidOperationException($"Max extension length is {ExtensionMaxLength} symbols");

                _extension = value;
            }
        }

        public long DirectoryId { get; set; }
        public long FileLength => ChunkEntries.Sum(x => x.Length);
        public List<FileChunkEntry> ChunkEntries { get; } = new List<FileChunkEntry>();

        public override byte[] GetBytes()
        {
            var idBytes = BitConverter.GetBytes(Id);
            var nameBytes = Encoding.UTF8.GetBytes(Name);
            var nameBlockLengthBytes = BitConverter.GetBytes(nameBytes.Length);
            var createTimeBytes = BitConverter.GetBytes(CreationTime.ToBinary());
            var modifyTimeBytes = BitConverter.GetBytes(ModificationTime.ToBinary());
            var extBytes = Encoding.UTF8.GetBytes(Extension);
            var directoryIdBytes = BitConverter.GetBytes(DirectoryId);
            var extBlockLengthBytes = BitConverter.GetBytes(extBytes.Length);
            var chunksCountBytes = BitConverter.GetBytes(ChunkEntries.Count);
            var chunkEntriesBytes = ChunkEntries.Select(x =>
            {
                var positionBytes = BitConverter.GetBytes(x.Position);
                var lengthBytes = BitConverter.GetBytes(x.Length);
                return positionBytes.Union(lengthBytes).ToArray();
            });

            var block = new List<byte> { Mark };
            block.AddRange(idBytes);
            block.AddRange(nameBlockLengthBytes);
            block.AddRange(nameBytes);
            block.AddRange(createTimeBytes);
            block.AddRange(modifyTimeBytes);

            block.AddRange(extBlockLengthBytes);
            block.AddRange(extBytes);
            block.AddRange(directoryIdBytes);
            block.AddRange(chunksCountBytes);
            foreach (var chunkEntrysBytes in chunkEntriesBytes)
                block.AddRange(chunkEntrysBytes);

            var blockLength = block.Count;
            var blockLengthBytes = BitConverter.GetBytes(blockLength);

            var retv = new List<byte>();
            retv.AddRange(blockLengthBytes);
            retv.AddRange(block);

            return retv.ToArray();
        }

        protected override byte GetMark() => ServiceMarks.FileEntry;

        public static FileEntry Read(byte[] bytes)
        {
            var position = 0;
            var mark = bytes.First();
            position = 1;

            if (mark != ServiceMarks.FileEntry)
                throw new DataException("Unable to read File entry");

            try
            {
                var id = BitConverter.ToInt64(bytes, 1);
                position += 4;
                var nameLength = BitConverter.ToInt32(bytes, position);
                position += 4;
                var nameBytes = bytes.Skip(position).Take(nameLength).ToArray();
                var name = Encoding.UTF8.GetString(nameBytes);
                position += nameLength;
                var createdDateTime = DateTime.FromBinary(BitConverter.ToInt64(bytes, position));
                position += 8;
                var modifiedDateTime = DateTime.FromBinary(BitConverter.ToInt64(bytes, position));
                position += 8;
                var directoryId = BitConverter.ToInt64(bytes, position);
                position += 8;
                var chunkCount = BitConverter.ToInt32(bytes, position);
                position += 4;

                var retv = new FileEntry
                {
                    Id = id,
                    CreationTime = createdDateTime,
                    DirectoryId = directoryId,
                    ModificationTime = modifiedDateTime,
                    Name = name
                };

                for (var i = 0; i < chunkCount; i++)
                {
                    var chunkPosition = BitConverter.ToInt64(bytes, position);
                    position += 8;
                    var chunkLength = BitConverter.ToInt32(bytes, position);
                    position += 4;
                    retv.ChunkEntries.Add(new FileChunkEntry
                    {
                        Length = chunkLength,
                        Position = chunkPosition
                    });
                }

                return retv;
            }
            catch (Exception e)
            {
                throw new DataException("Unable to read Directory entry", e);
            }
        }
    }

    public class DirectoryEntry : BaseEntry
    {
        public long DirectoryId { get; set; }

        public override byte[] GetBytes()
        {
            var idBytes = BitConverter.GetBytes(Id);
            var nameBytes = Encoding.UTF8.GetBytes(Name);
            var nameBlockLengthBytes = BitConverter.GetBytes(nameBytes.Length);
            var createTimeBytes = BitConverter.GetBytes(CreationTime.ToBinary());
            var modifyTimeBytes = BitConverter.GetBytes(ModificationTime.ToBinary());
            var directoryIdBytes = BitConverter.GetBytes(DirectoryId);

            var block = new List<byte> { Mark };
            block.AddRange(idBytes);
            block.AddRange(nameBlockLengthBytes);
            block.AddRange(nameBytes);
            block.AddRange(createTimeBytes);
            block.AddRange(modifyTimeBytes);

            block.AddRange(directoryIdBytes);

            var blockLength = block.Count;
            var blockLengthBytes = BitConverter.GetBytes(blockLength);

            var retv = new List<byte>();
            retv.AddRange(blockLengthBytes);
            retv.AddRange(block);

            return retv.ToArray();
        }

        protected override byte GetMark() => ServiceMarks.DirectoryEntry;
        public static DirectoryEntry Read(byte[] bytes)
        {
            var position = 0;
            var mark = bytes.First();
            position = 1;

            if (mark != ServiceMarks.DirectoryEntry)
                throw new DataException("Unable to read Directory entry");

            try
            {
                var id = BitConverter.ToInt64(bytes, 1);
                position += 4;
                var nameLength = BitConverter.ToInt32(bytes, position);
                position += 4;
                var nameBytes = bytes.Skip(position).Take(nameLength).ToArray();
                var name = Encoding.UTF8.GetString(nameBytes);
                position += nameLength;
                var createdDateTime = DateTime.FromBinary(BitConverter.ToInt64(bytes, position));
                position += 8;
                var modifiedDateTime = DateTime.FromBinary(BitConverter.ToInt64(bytes, position));
                position += 8;
                var directoryId = BitConverter.ToInt64(bytes, position);
                var retv = new DirectoryEntry
                {
                    Id = id,
                    CreationTime = createdDateTime,
                    DirectoryId = directoryId,
                    ModificationTime = modifiedDateTime,
                    Name = name
                };

                return retv;
            }
            catch (Exception e)
            {
                throw new DataException("Unable to read Directory entry", e);
            }
        }
    }

    public class Sector
    {
        public int Id { get; set; }
        public long StartPosition { get; set; }
        public int Length { get; set; }
        public byte Mark { get; set; }

        public byte[] GetBytes()
        {
            var block = new List<byte>();
            block.AddRange(BitConverter.GetBytes(Id));
            block.AddRange(BitConverter.GetBytes(StartPosition));
            block.AddRange(BitConverter.GetBytes(Length));
            block.Add(Mark);

            var blockLengthBytes = BitConverter.GetBytes(block.Count);

            var retv = new List<byte>();
            retv.AddRange(blockLengthBytes);
            retv.AddRange(block);

            return retv.ToArray();
        }

        public static Sector Read(byte[] bytes)
        {
            if (bytes.Length != 17)//Block length should be 17 bytes (int + long + int + byte)
                throw new DataException("Unable to read sector data");

            return new Sector
            {
                Id = BitConverter.ToInt32(bytes, 0),
                StartPosition = BitConverter.ToInt32(bytes, 4),
                Length = BitConverter.ToInt32(bytes, 12),
                Mark = bytes.Last()
            };
        }
    }

    public static class ServiceMarks
    {
        public static byte FileEntriesSector = 0;
        public static byte ContentSector = 1;
        public static byte FileEntry = 2;
        public static byte DirectoryEntry = 3;
    }

    public static class ServiceBytes
    {
        public static byte[] EndBytes = { 0x65, 0x6e, 0x64, 0x2e };
    }

    public class VirtualFile
    {
        public string Name { get; set; }
        public DateTime CreatedAt { get; }
        public DateTime LastEditAt { get; }
        public long Length { get; }
    }

    public class VirtualDirectory
    {
        public string Name { get; set; }
        public DateTime CreatedAt { get; }
        public DateTime LastEditAt { get; }
    }

    public class VirtualFileStream : Stream
    {
        private readonly FileSystem _fileSystem;
        private readonly VirtualFile _file;

        public override bool CanRead { get; }
        public override bool CanSeek { get; }
        public override bool CanWrite { get; }
        public override long Length => _file.Length;
        public override long Position { get; set; }

        public VirtualFileStream(FileSystem fileSystem, VirtualFile file)
        {
            _fileSystem = fileSystem;
            _file = file;
        }

        public override void Flush()
        {

        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            switch (origin)
            {
                case SeekOrigin.Begin:
                    Position = offset;
                    break;
                case SeekOrigin.Current:
                    Position += offset;
                    break;
                case SeekOrigin.End:
                    Position = Length - offset;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(origin), origin, null);
            }

            if (Position < 0 || Position > Length)
                throw new ArgumentOutOfRangeException(nameof(offset));

            return Position;
        }

        public override void SetLength(long value)
        {
            throw new NotImplementedException();
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            throw new NotImplementedException();
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            throw new NotImplementedException();
        }
    }

    public class FileSystem : IDisposable
    {
        protected readonly int FileTableSectorLength = 4 * 1024 * 1024; // 4mb
        protected readonly int SectorsRegionLength = 1 * 1024 * 1024; // 1mb
        private readonly IVirtualDriveFactory _virtualDriveFactory;
        private readonly List<FileEntry> _fileEntries = new List<FileEntry>();
        private readonly List<DirectoryEntry> _directoryEntries = new List<DirectoryEntry>();

        private long _newFileEntryId = 0;
        private long _newDirectoryEntryId = 0;

        private readonly Dictionary<FileEntry, DirectoryEntry> _filesParentDirectoryIndex = new Dictionary<FileEntry, DirectoryEntry>();

        private readonly Dictionary<DirectoryEntry, List<FileEntry>> _directoryFilesIndex = new Dictionary<DirectoryEntry, List<FileEntry>>();
        private readonly Dictionary<DirectoryEntry, List<DirectoryEntry>> _directoryDirectoriesIndex = new Dictionary<DirectoryEntry, List<DirectoryEntry>>();

        private readonly Dictionary<long, FileEntry> _filesIndex = new Dictionary<long, FileEntry>();
        private readonly Dictionary<long, DirectoryEntry> _directoriesIndex = new Dictionary<long, DirectoryEntry>();

        protected IVirtualDrive Drive { get; private set; }
        public bool IsInitilized { get; protected set; }

        public FileSystem(IVirtualDriveFactory virtualDriveFactory)
        {
            _virtualDriveFactory = virtualDriveFactory;
        }

        public void Initialize(string fileName)
        {
            Drive = _virtualDriveFactory.Create(fileName);
            if (Drive.Size == 0)
                createNewFileSystem();

            var sectors = readSectors();
            foreach (var sector in sectors)
                readEntries(sector);

            populateIndexes();
        }

        private void populateIndexes()
        {
            foreach (var directoryEntry in _directoryEntries)
                _directoriesIndex[directoryEntry.Id] = directoryEntry;

            foreach (var directoryEntry in _directoriesIndex.Values)
            {
                if (directoryEntry.Id == -1)//if root dir
                    continue;

                var parentDirectory = _directoriesIndex[directoryEntry.DirectoryId];
                if (!_directoryDirectoriesIndex.ContainsKey(parentDirectory))
                    _directoryDirectoriesIndex[parentDirectory] = new List<DirectoryEntry>();

                _directoryDirectoriesIndex[parentDirectory].Add(directoryEntry);
            }

            foreach (var fileEntry in _fileEntries)
            {
                _filesIndex[fileEntry.Id] = fileEntry;
                var parentDirectory = _directoriesIndex[fileEntry.DirectoryId];
                _filesParentDirectoryIndex[fileEntry] = parentDirectory;

                if (!_directoryFilesIndex.ContainsKey(parentDirectory))
                    _directoryFilesIndex[parentDirectory] = new List<FileEntry>();

                _directoryFilesIndex[parentDirectory].Add(fileEntry);
            }
        }

        private List<Sector> readSectors()
        {
            var retv = new List<Sector>();

            var sectorsRegionBytes = Drive.ReadData(0, SectorsRegionLength);
            var canRead = sectorsRegionBytes.Any();
            var position = 0;
            while (canRead)
            {
                var blockLengthBytes = sectorsRegionBytes.Skip(position).Take(4).ToArray();
                if (blockLengthBytes.SequenceEqual(ServiceBytes.EndBytes))
                    break;

                position += 4;
                var blockLength = BitConverter.ToInt32(blockLengthBytes, 0);
                var block = sectorsRegionBytes.Skip(position).Take(blockLength).ToArray();
                position += blockLength;
                var sector = Sector.Read(block);
                retv.Add(sector);
                canRead = position < SectorsRegionLength;
            }

            return retv;
        }

        private void readEntries(Sector sector)
        {
            if (sector.Mark != ServiceMarks.FileEntriesSector)
                return;

            var endPosition = sector.StartPosition + sector.Length;
            var position = sector.StartPosition;
            var canRead = true;
            while (canRead)
            {
                var blockLengthBytes = Drive.ReadData(position, 4);
                if (blockLengthBytes.SequenceEqual(ServiceBytes.EndBytes))
                    break;

                position += 4;
                var blockLength = BitConverter.ToInt32(blockLengthBytes, 0);
                var block = Drive.ReadData(position, blockLength);
                position += blockLength;

                if (block.FirstOrDefault() == ServiceMarks.DirectoryEntry)
                {
                    var entry = DirectoryEntry.Read(block);
                    _directoryEntries.Add(entry);
                }

                if (block.FirstOrDefault() == ServiceMarks.FileEntry)
                {
                    var entry = FileEntry.Read(block);
                    _fileEntries.Add(entry);
                }

                canRead = position < endPosition;
            }
        }

        private void createNewFileSystem()
        {
            var fileTableSector = new Sector
            {
                Id = 0,
                Length = FileTableSectorLength,
                Mark = ServiceMarks.FileEntriesSector,
                StartPosition = SectorsRegionLength
            };

            var fileContentSector = new Sector
            {
                Id = 1,
                Mark = ServiceMarks.ContentSector,
                Length = -1,//read to the end
                StartPosition = fileTableSector.StartPosition + fileTableSector.Length
            };

            var fileTableSectorBytes = fileTableSector.GetBytes();
            var fileContentSectorBytes = fileContentSector.GetBytes();
            var position = 0L;

            position += Drive.WriteData(position, fileTableSectorBytes);
            position += Drive.WriteData(position, fileContentSectorBytes);
            position += Drive.WriteData(position, ServiceBytes.EndBytes);

            position = SectorsRegionLength + 1;
            var rootDirectory = new DirectoryEntry
            {
                Id = getDirectoryEntryId(),
                DirectoryId = -1,//has no parent
                Name = "root"
            };

            var rootDirectoryBytes = rootDirectory.GetBytes();
            position = fileTableSector.StartPosition;
            position += Drive.WriteData(position, rootDirectoryBytes);
            position += Drive.WriteData(position, ServiceBytes.EndBytes);
        }

        private long getFileEntryId()
        {
            return _newFileEntryId++;
        }

        private long getDirectoryEntryId()
        {
            return _newDirectoryEntryId++;
        }

        public void Dispose()
        {
            Drive?.Dispose();
        }
    }
}
