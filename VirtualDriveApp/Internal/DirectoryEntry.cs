using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using VirtualDrive.Internal.RawData;

namespace VirtualDrive.Internal
{
    internal class DirectoryEntry : BaseEntry
    {
        public override byte[] GetBytes()
        {
            var idBytes = BitConverter.GetBytes(Id);
            var nameBytes = Encoding.UTF8.GetBytes(Name);
            var nameBlockLengthBytes = BitConverter.GetBytes(nameBytes.Length);
            var createTimeBytes = BitConverter.GetBytes(CreationTime.ToBinary());
            var modifyTimeBytes = BitConverter.GetBytes(ModificationTime.ToBinary());
            var directoryIdBytes = BitConverter.GetBytes(DirectoryId);

            var block = new List<byte> { (byte)Mark };
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

        public override int GetEntryLength()
        {
            return GetBytes().Length - 4;
        }

        protected override ServiceMarks GetMark() => ServiceMarks.DirectoryEntry;
        public static DirectoryEntry Read(byte[] bytes)
        {
            var position = 0;
            var mark = bytes.First();
            position = 1;

            if (mark != (byte)ServiceMarks.DirectoryEntry)
                throw new DataException("Unable to read Directory entry");

            try
            {
                var id = BitConverter.ToInt64(bytes, position);
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
}