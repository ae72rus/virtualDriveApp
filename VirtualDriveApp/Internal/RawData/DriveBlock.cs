namespace VirtualDrive.Internal.RawData
{
    internal class DriveBlock
    {
        public long Position { get; set; }
        public int Length { get; set; }

        public DriveBlock()
        {

        }

        public DriveBlock(DriveBlock block)
        {
            Position = block.Position;
            Length = block.Length;
        }

        public DriveBlock(FileChunkEntry chunk)
        {
            Position = chunk.Position;
            Length = chunk.Length;
        }
    }
}