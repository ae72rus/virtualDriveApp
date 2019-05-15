using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using VirtualDrive.Internal.RawData;

namespace VirtualDrive.Internal
{
    internal class FileEntry : BaseEntry
    {
        public string Extension { get; set; }
        public long FileLength => Blocks.Sum(x => (long)x.Length);
        public List<DriveBlock> Blocks { get; } = new List<DriveBlock>();

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
            var chunksCountBytes = BitConverter.GetBytes(Blocks.Count);
            var chunkEntriesBytes = Blocks.Select(x =>
            {
                var positionBytes = BitConverter.GetBytes(x.Position);
                var lengthBytes = BitConverter.GetBytes(x.Length);
                return positionBytes.Concat(lengthBytes).ToArray();
            });

            var block = new List<byte> { (byte)Mark };
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
        
        public override int GetEntryLength()
        {
            return GetBytes().Length - 4;
        }

        protected override ServiceMarks GetMark() => ServiceMarks.FileEntry;

        public static FileEntry Read(byte[] bytes)
        {
            var position = 0;
            var mark = bytes.First();
            position = ByteHelper.GetLength(mark);
            
            if (mark != (byte)ServiceMarks.FileEntry)
                throw new DataException("Unable to read File entry");

            try
            {
                var id = BitConverter.ToInt64(bytes, 1);
                position += ByteHelper.GetLength(id);

                var nameLength = BitConverter.ToInt32(bytes, position);
                position += ByteHelper.GetLength(nameLength);

                var nameBytes = bytes.Skip(position).Take(nameLength).ToArray();
                var name = Encoding.UTF8.GetString(nameBytes);
                position += nameLength;

                var createdDateTime = DateTime.FromBinary(BitConverter.ToInt64(bytes, position));
                position += ByteHelper.GetLength(createdDateTime);
                var modifiedDateTime = DateTime.FromBinary(BitConverter.ToInt64(bytes, position));
                position += ByteHelper.GetLength(modifiedDateTime);

                var extLength = BitConverter.ToInt32(bytes, position);
                position += ByteHelper.GetLength(extLength);

                var extBytes = bytes.Skip(position).Take(extLength).ToArray();
                var ext = Encoding.UTF8.GetString(extBytes);
                position += extLength;

                var directoryId = BitConverter.ToInt64(bytes, position);
                position += ByteHelper.GetLength(directoryId);

                var chunkCount = BitConverter.ToInt32(bytes, position);
                position += ByteHelper.GetLength(chunkCount);

                var retv = new FileEntry
                {
                    Id = id,
                    CreationTime = createdDateTime,
                    DirectoryId = directoryId,
                    ModificationTime = modifiedDateTime,
                    Name = name,
                    Extension = ext
                };

                for (var i = 0; i < chunkCount; i++)
                {
                    var chunkPosition = BitConverter.ToInt64(bytes, position);
                    position += ByteHelper.GetLength(chunkPosition);
                    var chunkLength = BitConverter.ToInt32(bytes, position);
                    position += ByteHelper.GetLength(chunkLength);
                    retv.Blocks.Add(new DriveBlock
                    {
                        Length = chunkLength,
                        Position = chunkPosition
                    });
                }

                return retv;
            }
            catch (Exception e)
            {
                throw new DataException("Unable to read File entry", e);
            }
        }
    }
}
