using System;

namespace VirtualDrive.Internal.Drive.Operations
{
    internal class FileTableOperation : WriteOperation
    {
        public FileTableOperation(Func<VirtualDrive, long> operationFunc, long position)
            : base(operationFunc, position)
        {
            Type = OperationType.FileTable;
        }
    }
}