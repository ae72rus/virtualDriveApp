using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DemoApp.Abstractions.Services;
using DemoApp.Abstractions.Viewmodels;
using DemoApp.Implementations.Factories;
using OrlemSoftware.Basics.Core.Attributes;
using VirtualDrive;

namespace DemoApp.Implementations.Services
{
    [Singletone]
    public class LongOperationsManager : ILongOperationsManager
    {
        private readonly ILongOperationsViewModelFactory _longOperationsViewModelFactory;
        private readonly List<ILongOperationViewModel> _longOperations = new List<ILongOperationViewModel>();

        public event EventHandler HasOperationsChanged;
        public bool HasOperations => _longOperations.Any();

        public LongOperationsManager(ILongOperationsViewModelFactory longOperationsViewModelFactory)
        {
            _longOperationsViewModelFactory = longOperationsViewModelFactory;
        }

        public Task StartLongOperation(IVirtualFileSystem fileSystem, Func<ILongOperationViewModel, Task> callback)
        {
            var retv = _longOperationsViewModelFactory.Create(fileSystem, callback);
            retv.Initialize();
            _longOperations.Add(retv);
            HasOperationsChanged?.Invoke(this, EventArgs.Empty);
            return retv.Task;
        }

        public void StopLongOperation(ILongOperationViewModel operation)
        {
            _longOperations.Remove(operation);
            HasOperationsChanged?.Invoke(this, EventArgs.Empty);
        }

        public void Dispose()
        {
            _longOperations.Clear();
            HasOperationsChanged?.Invoke(this, EventArgs.Empty);
        }
    }
}