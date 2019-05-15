namespace VirtualDrive.Internal.RawData.Writers
{
    internal class OptimizedOperation : OperationHint
    {
        public bool IsExistingBlockUsed { get; set; }
        public DriveBlock GeneratedAvailableBlock { get; set; }
    }
}