using System;
using System.Threading;
using System.Threading.Tasks;
using DemoApp.Abstractions.Common;
using DemoApp.Abstractions.Services;
using DemoApp.Abstractions.Viewmodels;
using DemoApp.Implementations.Factories;
using VirtualDrive;
using Exception = System.Exception;

namespace DemoApp.Implementations.Viewmodels
{
    public class LongOperationViewModel : BaseViewmodel, ILongOperationViewModel
    {
        private readonly ILongOperationsManager _longOperationsManager;
        private readonly Func<ILongOperationViewModel, Task> _operationAction;
        private bool _isCanceled;
        private int _progress;
        private string _message = "Initializing";
        public Task Task { get; private set; } = Task.CompletedTask;
        public IVirtualFileSystem FileSystem { get; }
        public bool IsCanceled => _isCanceled;

        public int Progress
        {
            get => _progress;
            set
            {
                _progress = value;
                RaisePropertyChanged();
            }
        }

        public string Message
        {
            get => _message;
            set
            {
                _message = value;
                RaisePropertyChanged();
            }
        }

        public CancellationTokenSource CancellationTokenSource { get; } = new CancellationTokenSource();

        public IRelayCommand CancelCommand { get; }

        public LongOperationViewModel(IWindowsManager windowsManager,
            ILongOperationsManager longOperationsManager,
            IRelayCommandFactory commandFactory,
            IVirtualFileSystem fileSystem,
            Func<ILongOperationViewModel, Task> operationAction)
            : base(windowsManager)
        {
            _longOperationsManager = longOperationsManager;
            _operationAction = operationAction;
            FileSystem = fileSystem;
            CancelCommand = commandFactory.Create(cancelExec, cancelCanExec);

            WireUpPropertyAndCommand(() => IsCanceled, () => CancelCommand);
        }

        public void SetState(ProgressArgs progressArgs)
        {
            Progress = progressArgs.Progress;
            Message = $"[{progressArgs.Operation.ToString()}] {progressArgs.Message}";
        }

        public void Cancel()
        {
            cancelExec();
        }

        protected override async Task InitializeInternal()
        {
            WindowsManager.AddProgress(this);
            Task = _operationAction?.Invoke(this) ?? Task.CompletedTask;

            try
            {
                await Task;
            }
            catch (Exception)
            {
                //do not handle exception here. 
                //It should be handled inside action or around external await
            }

            Dispose();
        }

        #region Cancel
        private bool cancelCanExec()
        {
            return !_isCanceled;
        }

        private void cancelExec()
        {
            if (!cancelCanExec())
                return;

            SetProperty(() => IsCanceled, ref _isCanceled, true);
            CancellationTokenSource.Cancel();
        }
        #endregion

        protected override void DisposeInternal()
        {
            Dispatcher.Invoke(() =>
            {
                _longOperationsManager.StopLongOperation(this);
                WindowsManager.RemoveProgress(this);
            });

            CancellationTokenSource?.Dispose();
        }
    }
}