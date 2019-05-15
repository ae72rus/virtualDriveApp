using DemoApp.Abstractions.Viewmodels;
using OrlemSoftware.Basics.Core;
using VirtualDrive;

namespace DemoApp.Implementations.Factories
{
    public interface IEntityViewModelFactory : IFactory
    {
        IEntityViewModel Create(IVirtualFileSystem fs, VirtualDirectory directory, bool isParentDirectory);
        IEntityViewModel Create(IVirtualFileSystem fs, VirtualFile file);
    }
}