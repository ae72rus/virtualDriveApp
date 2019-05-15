using System;
using System.Collections.Generic;
using VirtualDrive.Internal.RawData;

namespace VirtualDrive.Internal
{
    internal class InternalVirtualDriveParameters : VirtualDriveParameters, IByteSource
    {
        public InternalVirtualDriveParameters(int entriesTableSectorLength, int sectorsInformationRegionLength)
            : base(entriesTableSectorLength, sectorsInformationRegionLength)
        {

        }

        public InternalVirtualDriveParameters(VirtualDriveParameters virtualDriveParameters)
            : base(virtualDriveParameters.EntriesTableSectorLength, virtualDriveParameters.SectorsInformationRegionLength)
        {

        }


        public static VirtualDriveParameters Read(byte[] block)
        {
            if (block.Length != 8)
                throw new InvalidOperationException("Unable to read virtual drive parameters");

            var sectorsInformationRegionLength = BitConverter.ToInt32(block, 0);
            var entriesTableSectorLength = BitConverter.ToInt32(block, ByteHelper.GetLength<int>());

            return new InternalVirtualDriveParameters(entriesTableSectorLength, sectorsInformationRegionLength);
        }

        public VirtualDriveParameters ToVirtualDriveParameters() => this;


        public byte[] GetBytes()
        {
            var retv = new List<byte>();
            retv.AddRange(BitConverter.GetBytes(SectorsInformationRegionLength));
            retv.AddRange(BitConverter.GetBytes(EntriesTableSectorLength));
            return retv.ToArray();
        }
    }
}