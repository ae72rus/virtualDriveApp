namespace VirtualDrive.Internal
{
    internal class CopyProgressArgs : ProgressArgs
    {
        public CopyProgressArgs(int progress, string message)
            : base(progress, message, Operation.Copying | Operation.Initializing)
        {

        }
    }
}