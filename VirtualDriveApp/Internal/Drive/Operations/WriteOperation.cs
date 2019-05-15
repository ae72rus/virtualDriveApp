using System;

namespace VirtualDrive.Internal.Drive.Operations
{
    internal class WriteOperation : DriveOperation<long>
    {
        public static WriteOperation Empty => new WriteOperation();
        public long Position { get; }

        private WriteOperation() : base(d => 0, OperationType.Write)
        {
            Task = System.Threading.Tasks.Task.FromResult(0L);
        }

        public WriteOperation(Func<VirtualDrive, long> operationFunc, long position)
            : base(operationFunc, OperationType.Write)
        {
            Position = position;
        }

    }
}