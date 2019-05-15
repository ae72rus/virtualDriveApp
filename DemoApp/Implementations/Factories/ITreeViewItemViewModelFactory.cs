using DemoApp.Abstractions.Common;
using DemoApp.Abstractions.Viewmodels.TreeView;
using OrlemSoftware.Basics.Core;
using VirtualDrive;

namespace DemoApp.Implementations.Factories
{
    public interface ITreeViewItemViewModelFactory : IFactory
    {
        ITreeViewItemViewModel Create(VirtualDirectory virtualDirectory);
    }
}