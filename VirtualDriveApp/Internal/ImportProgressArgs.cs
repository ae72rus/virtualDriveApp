namespace VirtualDrive.Internal
{
    internal class ImportProgressArgs : ProgressArgs
    {
        public ImportProgressArgs(int progress, string message)
            : base(progress, message, Operation.Importing | Operation.Initializing)
        {

        }
    }
}