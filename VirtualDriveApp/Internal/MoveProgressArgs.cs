namespace VirtualDrive.Internal
{
    internal class MoveProgressArgs : ProgressArgs
    {
        public MoveProgressArgs(int progress, string message)
            : base(progress, message, Operation.Moving | Operation.Initializing)
        {

        }
    }
}