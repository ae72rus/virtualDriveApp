﻿using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using VirtualDrive.Internal.Drive.Operations;

namespace VirtualDrive.Internal.RawData.Threading
{
    internal class DriveAccessSynchronizer : IDisposable
    {
        private bool _isDisposing;
        private volatile object _disposeLock = new object();
        private volatile object _eventLock = new object();
        private volatile object _driveAccessLock = new object();
        private readonly ConcurrentQueue<BaseDriveOperation> _highPriorityOperations = new ConcurrentQueue<BaseDriveOperation>();
        private readonly ConcurrentQueue<BaseDriveOperation> _lowPriorityOperations = new ConcurrentQueue<BaseDriveOperation>();

        private readonly ManualResetEvent _waitForOperationsEvent = new ManualResetEvent(false);
        private readonly Thread _driveAccessThread;

        private readonly Drive.VirtualDrive _drive;
        public Task DriveAccess { get; private set; } = Task.CompletedTask;

        public bool CanRead => _drive.CanRead;

        public DriveAccessSynchronizer(Drive.VirtualDrive drive)
        {
            _drive = drive ?? throw new ArgumentNullException(nameof(drive));

            _driveAccessThread = new Thread(runDriveOperations);
            _driveAccessThread.Start();
        }

        private async void runDriveOperations()
        {
            while (true)
            {
                var shouldWait = !await runHighPriorityOperations() && !await runLowPriorityOperations();

                if (shouldWait)
                    lock (_eventLock)
                        _waitForOperationsEvent.Reset();//if no operations have been run so we will wait until an operation added or for dispose

                if (_isDisposing)
                    break;

                if (_highPriorityOperations.Any() || _lowPriorityOperations.Any())//if an operation has been added to querries go on without waiting
                    continue;

                _waitForOperationsEvent.WaitOne();
            }
        }

        public long GetDriveLength() => _drive.Length;

        public void EnqueueOperation(BaseDriveOperation operation)
        {
            lock (_driveAccessLock)
                DriveAccess = DriveAccess.ContinueWith(t => operation.Task);

            switch (operation.Type)
            {
                case OperationType.FileTable:
                    _highPriorityOperations.Enqueue(operation);
                    break;
                case OperationType.Read:
                case OperationType.Write:
                    _lowPriorityOperations.Enqueue(operation);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            lock (_eventLock)
                _waitForOperationsEvent.Set();
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
            lock (_disposeLock)
                _isDisposing = true;

            lock (_eventLock)
                _waitForOperationsEvent.Set(); //in case if drive thread is waiting for operations

            _driveAccessThread.Join();//wait for all of operations to be completed
            _drive?.Dispose();
        }
    }
}