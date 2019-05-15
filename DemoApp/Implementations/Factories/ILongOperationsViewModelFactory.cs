using System;
using System.Threading.Tasks;
using DemoApp.Abstractions.Viewmodels;
using OrlemSoftware.Basics.Core;
using VirtualDrive;

namespace DemoApp.Implementations.Factories
{
    public interface ILongOperationsViewModelFactory : IFactory
    {
        ILongOperationViewModel Create(IVirtualFileSystem fileSystem, Func<ILongOperationViewModel, Task> operationAction);
    }
}