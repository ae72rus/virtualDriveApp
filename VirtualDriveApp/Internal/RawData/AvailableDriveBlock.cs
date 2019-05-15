using System;
using System.Collections.Generic;

namespace VirtualDrive.Internal.RawData
{
    internal class AvailableDriveBlock : DriveBlock, IByteSource
    {

        private AvailableDriveBlock(long position, int length)
        {
            Position = position;
            Length = length;
        }

        public AvailableDriveBlock(DriveBlock block)
            : this(block?.Position ?? throw new ArgumentNullException(nameof(block)), block.Length)
        {

        }

        public byte[] GetBytes()
        {
            var retv = new List<byte>();

            var positionBytes = BitConverter.GetBytes(Position);
            var lengthBytes = BitConverter.GetBytes(Length);

            retv.AddRange(positionBytes);
            retv.AddRange(lengthBytes);

            return retv.ToArray();
        }
        
        public static int BytesBlockLength => ByteHelper.GetLength<long>() + ByteHelper.GetLength<int>();
        public static AvailableDriveBlock Read(byte[] block)
        {
            if(block.Length!= BytesBlockLength)
                throw new InvalidOperationException("Unable to read block");

            var position = 0;
            var blockPosition = BitConverter.ToInt64(block, position);
            position += ByteHelper.GetLength<long>();
            var blockLength = BitConverter.ToInt32(block, position);
            var retv = new AvailableDriveBlock(blockPosition, blockLength);
            return retv;
        }
    }
}