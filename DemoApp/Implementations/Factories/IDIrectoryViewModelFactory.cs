using DemoApp.Abstractions.Viewmodels;
using OrlemSoftware.Basics.Core;
using VirtualDrive;

namespace DemoApp.Implementations.Factories
{
    public interface IDirectoryViewModelFactory : IFactory
    {
        IDirectoryViewModel Create(IFileSystemViewModel fileSystem, IVirtualFileSystem virtualFileSystem, string directoryPath);
    }
}