namespace VirtualDrive.Internal.RawData.Writers
{
    internal class DataChunk
    {
        public long Position { get; set; }
        public byte[] Bytes { get; set; }
    }
}