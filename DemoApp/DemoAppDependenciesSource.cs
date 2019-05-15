using DemoApp.Abstractions.Common;
using DemoApp.Abstractions.Services;
using DemoApp.Abstractions.Viewmodels;
using DemoApp.Abstractions.Viewmodels.TreeView;
using DemoApp.Implementations.Common;
using DemoApp.Implementations.Factories;
using DemoApp.Implementations.Services;
using DemoApp.Implementations.Viewmodels;
using DemoApp.Implementations.Viewmodels.TreeView;
using OrlemSoftware.Basics.Core;

namespace DemoApp
{
    public class DemoAppDependenciesSource : IDependenciesSource
    {
        public void SetDependencies(IContainer container)
        {
            container.Register<IWindowsManager, WindowsManager>();
            container.Register<IClipboardService, ClipboardService>();
            container.Register<ILongOperationsManager, LongOperationsManager>();

            //Implementations will be generated in runtime
            container.RegisterFactory<ILongOperationsViewModelFactory, ILongOperationViewModel, LongOperationViewModel>();
            container.RegisterFactory<IDirectoryViewModelFactory, IDirectoryViewModel, DirectoryViewModel>();
            container.RegisterFactory<IRelayCommandFactory, IRelayCommand, RelayCommand>();
            container.RegisterFactory<IFileSystemViewModelFactory, IFileSystemViewModel, FileSystemViewModel>();
            container.RegisterFactory<IProgressWindowViewModelFactory, IProgressWindowViewModel, ProgressWindowViewModel>();
            container.RegisterFactory<ITreeViewItemViewModelFactory, ITreeViewItemViewModel, TreeViewItemViewModel>();
            container.RegisterFactory<ITreeViewViewModelFactory, ITreeViewViewModel, TreeViewViewModel>();
            container.RegisterFactory<IEntityViewModelFactory, IEntityViewModel, EntityViewModel>();
            container.RegisterFactory<ISearchViewModelFactory, ISearchViewModel, SearchViewModel>();
            container.RegisterFactory<ISearchResultViewModelFactory, ISearchResultViewModel, SearchResultViewModel>();
        }
    }
}