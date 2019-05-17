using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using VirtualDrive.Internal.Drive.Operations;

namespace VirtualDrive.Internal.RawData.Threading
{
    internal class DriveAccessSynchronizer : IDisposable
    {
        private bool _isDisposing;
        private volatile object _driveAccessLock = new object();
        private readonly ConcurrentQueue<BaseDriveOperation> _highPriorityOperations = new ConcurrentQueue<BaseDriveOperation>();
        private readonly ConcurrentQueue<BaseDriveOperation> _lowPriorityOperations = new ConcurrentQueue<BaseDriveOperation>();

        private readonly Drive.VirtualDrive _drive;
        public Task DriveAccess { get; private set; } = Task.CompletedTask;

        public bool CanRead => _drive.CanRead;

        public DriveAccessSynchronizer(Drive.VirtualDrive drive)
        {
            _drive = drive ?? throw new ArgumentNullException(nameof(drive));
        }

        private async Task runOperations()
        {
            await runHighPriorityOperations();
            await runLowPriorityOperations();
        }

        public long GetDriveLength() => _drive.Length;

        public void EnqueueOperation(BaseDriveOperation operation)
        {

            switch (operation.Type)
            {
                case OperationType.FileTable:
                    _highPriorityOperations.Enqueue(operation);
                    break;
                case OperationType.Read:
                case OperationType.Write:
                    _lowPriorityOperations.Enqueue(operation);
                    break;
            }

            lock (_driveAccessLock)
                DriveAccess = DriveAccess.ContinueWith(t => runOperations().Wait());
        }

        public DriveOperation EnqueueOperation(Action<Drive.VirtualDrive> driveAction, OperationType type)
        {
            var operation = new DriveOperation(driveAction, type);
            EnqueueOperation(operation);
            return operation;
        }

        public DriveOperation<TResult> EnqueueOperation<TResult>(Func<Drive.VirtualDrive, TResult> driveFunc, OperationType type)
        {
            var operation = new DriveOperation<TResult>(driveFunc, type);
            EnqueueOperation(operation);
            return operation;
        }

        private async Task<bool> runHighPriorityOperations()
        {
            var success = false;
            while (_highPriorityOperations.TryDequeue(out var op))
            {
                success = true;
                await runOperation(op);
            }

            return success;
        }

        private async Task<bool> runLowPriorityOperations()
        {
            var success = await runHighPriorityOperations();
            while (_lowPriorityOperations.TryDequeue(out var op))
            {
                await runHighPriorityOperations();
                success = true;
                await runOperation(op);
            }

            return success;
        }

        private async Task runOperation(BaseDriveOperation operation)
        {
            var task = operation.Run(_drive, TaskScheduler.Current);
            await task;
        }

        public void Dispose()
        {
            if (_isDisposing)
                return;

            _isDisposing = true;
            DriveAccess.Wait();
            _drive?.Dispose();
        }
    }
}