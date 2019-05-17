using System.Threading.Tasks;

namespace VirtualDrive.Internal.Drive.Operations
{
    internal abstract class BaseDriveOperation
    {
        public Task Task { get; protected set; }
        public OperationType Type { get; protected set; }
        public abstract Task Run(VirtualDrive drive, TaskScheduler scheduler);
    }
}