namespace VirtualDrive.Internal
{
    internal class RemoveProgressArgs : ProgressArgs
    {
        public RemoveProgressArgs(int progress, string message)
            : base(progress, message, Operation.Removing | Operation.Initializing)
        {

        }
    }
}