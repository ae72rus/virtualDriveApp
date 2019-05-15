using DemoApp.Abstractions.Viewmodels.TreeView;
using OrlemSoftware.Basics.Core;
using VirtualDrive;

namespace DemoApp.Implementations.Factories
{
    public interface ITreeViewViewModelFactory : IFactory
    {
        ITreeViewViewModel Create(VirtualDirectory rootDirectory);
    }
}