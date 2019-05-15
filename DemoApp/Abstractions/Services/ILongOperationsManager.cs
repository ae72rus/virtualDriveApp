using System;
using System.Threading.Tasks;
using DemoApp.Abstractions.Viewmodels;
using VirtualDrive;

namespace DemoApp.Abstractions.Services
{
    public interface ILongOperationsManager : IDisposable
    {
        event EventHandler HasOperationsChanged;
        bool HasOperations { get; }
        Task StartLongOperation(IVirtualFileSystem fileSystem, Func<ILongOperationViewModel, Task> callback);
        void StopLongOperation(ILongOperationViewModel operation);
    }
}