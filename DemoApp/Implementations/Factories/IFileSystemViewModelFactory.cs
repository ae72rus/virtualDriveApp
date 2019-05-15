using DemoApp.Abstractions.Viewmodels;
using OrlemSoftware.Basics.Core;
using VirtualDrive;

namespace DemoApp.Implementations.Factories
{
    public interface IFileSystemViewModelFactory : IFactory
    {
        IFileSystemViewModel Create(IVirtualFileSystem fileSystem);
    }
}