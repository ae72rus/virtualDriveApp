using System.Diagnostics;

namespace VirtualDrive
{
    [DebuggerDisplay("Operation: {Operation} Progress: {Progress} Message: {Message}")]
    public abstract class ProgressArgs
    {
        /// <summary>
        /// Operation hint
        /// </summary>
        public Operation Operation { get; internal set; }
        /// <summary>
        /// Progress in percents
        /// </summary>
        public int Progress { get; }

        /// <summary>
        /// Any message: comment, filename etc.
        /// </summary>
        public string Message { get; }

        protected ProgressArgs(int progress, string message, Operation operation)
        {
            Progress = progress;
            Message = message;
            Operation = operation;
        }
    }
}