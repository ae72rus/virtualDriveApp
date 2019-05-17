using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;

namespace VirtualDrive.Internal.RawData
{
    internal class SectorInfo : IByteSource
    {
        public int Id { get; set; }
        public long StartPosition { get; set; }
        public long Length { get; set; }
        public ServiceMarks Mark { get; set; }

        public byte[] GetBytes()
        {
            var block = new List<byte>();
            block.AddRange(BitConverter.GetBytes(Id));
            block.AddRange(BitConverter.GetBytes(StartPosition));
            block.AddRange(BitConverter.GetBytes(Length));
            block.Add((byte)Mark);

            var blockLengthBytes = BitConverter.GetBytes(block.Count);

            var retv = new List<byte>();
            retv.AddRange(blockLengthBytes);
            retv.AddRange(block);

            return retv.ToArray();
        }

        public static SectorInfo Read(byte[] bytes)
        {
            if (bytes.Length != 21)//Block length should be 21 bytes (int + long + long + byte)
                throw new DataException($"Unable to read sector data: data length {bytes.Length}, expected: 21");

            return new SectorInfo
            {
                Id = BitConverter.ToInt32(bytes, 0),
                StartPosition = BitConverter.ToInt32(bytes, 4),
                Length = BitConverter.ToInt64(bytes, 12),
                Mark = (ServiceMarks)bytes.Last()
            };
        }
    }
}
