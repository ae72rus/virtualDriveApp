﻿using System;
using System.Threading;
using System.Threading.Tasks;

namespace VirtualDrive.Internal.Drive.Operations
{
    internal class DriveOperation<TResult> : DriveOperation
    {

        public new Task<TResult> Task
        {
            get => base.Task as Task<TResult>;
            protected set => base.Task = value;
        }

        public DriveOperation(Func<VirtualDrive, TResult> operationFunc, OperationType type)
            : base(drive => { }, type)//task assigned from current ctor
        {
            var t = operationFunc ?? throw new ArgumentNullException(nameof(operationFunc));
            Task = new Task<TResult>(() => t.Invoke(GetDrive.Invoke()));
        }

        public DriveOperation(Func<VirtualDrive, CancellationToken, TResult> operationFunc, OperationType type, CancellationToken cancellationToken)
            : base(drive => { }, type)//task assigned from current ctor
        {
            var t = operationFunc ?? throw new ArgumentNullException(nameof(operationFunc));
            Task = new Task<TResult>(() => t.Invoke(GetDrive.Invoke(), cancellationToken), cancellationToken);
        }
    }

    internal class DriveOperation : BaseDriveOperation
    {
        protected Func<VirtualDrive> GetDrive { get; set; }

        public DriveOperation(Action<VirtualDrive> operationFunc, OperationType type)
        {
            Type = type;
            var operationAction = operationFunc ?? throw new ArgumentNullException(nameof(operationFunc));
            Task = new Task(() => operationAction.Invoke(GetDrive?.Invoke()));
        }

        public DriveOperation(Action<VirtualDrive, CancellationToken> operationFunc, OperationType type, CancellationToken cancellationToken)
        {
            Type = type;
            var operationAction = operationFunc ?? throw new ArgumentNullException(nameof(operationFunc));
            Task = new Task(() => operationAction.Invoke(GetDrive?.Invoke(), cancellationToken), cancellationToken);
        }

        public override Task Run(VirtualDrive drive, TaskScheduler scheduler)
        {
            GetDrive = () => drive;
            if (Task.Status == TaskStatus.Created)
                Task.Start(scheduler);
            return Task;
        }
    }

    internal abstract class BaseDriveOperation
    {
        public Task Task { get; protected set; }
        public OperationType Type { get; protected set; }
        public abstract Task Run(VirtualDrive drive, TaskScheduler scheduler);
    }
}