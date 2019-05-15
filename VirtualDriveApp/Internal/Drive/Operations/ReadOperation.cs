using System;
using System.Threading;

namespace VirtualDrive.Internal.Drive.Operations
{
    internal class ReadOperation : DriveOperation<int>
    {
        public static ReadOperation Empty => new ReadOperation();

        private ReadOperation() : base(d => 0, OperationType.Read)
        {
            Task = System.Threading.Tasks.Task.FromResult(0);
        }

        public ReadOperation(Func<VirtualDrive, int> operationFunc)
            : base(operationFunc, OperationType.Read)
        {

        }
        public ReadOperation(Func<VirtualDrive, CancellationToken, int> operationFunc, CancellationToken cancellationToken)
            : base(operationFunc, OperationType.Read, cancellationToken)
        {

        }
    }
}