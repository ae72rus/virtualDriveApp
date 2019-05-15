using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using DemoApp.Abstractions.Services;
using DemoApp.Abstractions.Viewmodels.TreeView;
using DemoApp.Extensions;
using DemoApp.Implementations.Factories;
using VirtualDrive;

namespace DemoApp.Implementations.Viewmodels.TreeView
{
    public class TreeViewItemViewModel : BaseViewmodel, ITreeViewItemViewModel
    {
        private readonly Dictionary<VirtualDirectory, ITreeViewItemViewModel> _nestedDirectoriesDictionary
            = new Dictionary<VirtualDirectory, ITreeViewItemViewModel>();

        private readonly ITreeViewItemViewModelFactory _treeViewItemViewModelFactory;
        private readonly VirtualDirectory _virtualDirectory;
        private readonly VirtualDirectoryWatcher _watcher;
        private ObservableCollection<ITreeViewItemViewModel> _nestedDirectories = new ObservableCollection<ITreeViewItemViewModel>();
        private bool _isExpanded;

        public string Path => _virtualDirectory.Name;
        public string Name => string.IsNullOrWhiteSpace(Path) 
            ? "/" 
            : VirtualPath.GetFileName(Path);

        public bool IsExpanded
        {
            get => _isExpanded;
            set
            {
                _isExpanded = value;
                RaisePropertyChanged();
            }
        }

        public ObservableCollection<ITreeViewItemViewModel> NestedDirectories => _nestedDirectories;

        public TreeViewItemViewModel(IWindowsManager windowsManager,
            ITreeViewItemViewModelFactory treeViewItemViewModelFactory,
            VirtualDirectory virtualDirectory)
            : base(windowsManager)
        {
            _treeViewItemViewModelFactory = treeViewItemViewModelFactory;
            _virtualDirectory = virtualDirectory;

            _watcher = _virtualDirectory.GetWatcher();
            _watcher.DirectoryEvent += onDirectoryEvent;
            _watcher.NameChanged += onNameChanged;
        }

        public void RaiseUpdated()
        {
            RaisePropertyChanged(() => Name);
        }

        protected override async Task InitializeInternal()
        {
            SetProperty(() => NestedDirectories, ref _nestedDirectories, getNestedDirectories());
        }

        private void onDirectoryEvent(object sender, DirectoryEventArgs e)
        {
            if (!IsInitialized)
                return;

            var dir = e.Directory;
            switch (e.Event)
            {
                case WatcherEvent.Created:
                    addNewNestedDirectory(dir);
                    break;
                case WatcherEvent.Updated:
                    if (!_nestedDirectoriesDictionary.ContainsKey(dir))
                        addNewNestedDirectory(dir);
                    else
                        _nestedDirectoriesDictionary[dir].RaiseUpdated();
                    break;
                case WatcherEvent.Deleted:
                    if (!_nestedDirectoriesDictionary.ContainsKey(dir))
                        break;

                    NestedDirectories.Remove(_nestedDirectoriesDictionary[dir]);
                    _nestedDirectoriesDictionary.Remove(dir);
                    break;
            }
        }

        private void addNewNestedDirectory(VirtualDirectory dir)
        {
            var newItem = _treeViewItemViewModelFactory.Create(dir);
            _nestedDirectoriesDictionary[dir] = newItem;
            NestedDirectories.InsertAuto(newItem, x => x.Name);
            newItem.Initialize();
        }

        private ObservableCollection<ITreeViewItemViewModel> getNestedDirectories()
        {
            var nestedItems = _virtualDirectory
                .GetDirectories()
                .OrderBy(x => VirtualPath.GetFileName(x.Name))
                .Select(x =>
            {
                var retv = _treeViewItemViewModelFactory.Create(x);
                _nestedDirectoriesDictionary[x] = retv;
                retv.Initialize();
                return retv;
            });

            return new ObservableCollection<ITreeViewItemViewModel>(nestedItems);
        }

        private void onNameChanged(object sender, EventArgs e)
        {
            RaisePropertyChanged(() => Name);
        }

        protected override void DisposeInternal()
        {
            if (_watcher != null)
            {
                _watcher.DirectoryEvent -= onDirectoryEvent;
                _watcher.NameChanged -= onNameChanged;
                _watcher.Dispose();
            }
        }
    }
}