using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using DemoApp.Abstractions.Common;
using DemoApp.Abstractions.Services;
using DemoApp.Abstractions.Viewmodels;
using DemoApp.Abstractions.Viewmodels.TreeView;
using DemoApp.Implementations.Common;
using DemoApp.Implementations.Factories;
using VirtualDrive;

namespace DemoApp.Implementations.Viewmodels
{
    public class FileSystemViewModel : BaseViewmodel, IFileSystemViewModel
    {
        private readonly ITreeViewViewModelFactory _treeViewViewModelFactory;
        private readonly IDirectoryViewModelFactory _directoryViewModelFactory;
        private readonly ISearchResultViewModelFactory _searchResultViewModelFactory;
        private string _pathString;
        private IDirectoryViewModel _currentViewModel;
        private ITreeViewViewModel _treeViewModel;
        private Task _searchTask = Task.CompletedTask;
        private bool _shouldBreakSearch;

        public string Title => string.IsNullOrWhiteSpace(PathString)
                                    ? Filename
                                    : $"{Filename}{PathString}";

        public string Filename => Path.GetFileName(FileSystem.File);
        public IVirtualFileSystem FileSystem { get; }
        public ISearchViewModel SearchViewModel { get; }

        public string PathString
        {
            get => _pathString;
            set
            {
                if (_pathString?.Equals(value, StringComparison.InvariantCultureIgnoreCase) == true)
                    return;

                _pathString = value;
                if (FileSystem != null)
                    setCurrentDirectory();

                RaisePropertyChanged();
                RaisePropertyChanged(() => Title);
            }
        }

        public ITreeViewViewModel TreeViewModel => _treeViewModel;
        public IDirectoryViewModel CurrentViewModel => _currentViewModel;

        public FileSystemViewModel(IWindowsManager windowsManager,
            ITreeViewViewModelFactory treeViewViewModelFactory,
            IDirectoryViewModelFactory directoryViewModelFactory,
            ISearchViewModelFactory searchViewModelFactory,
            ISearchResultViewModelFactory searchResultViewModelFactory,
            IVirtualFileSystem fileSystem)
            : base(windowsManager)
        {
            FileSystem = fileSystem;
            _treeViewViewModelFactory = treeViewViewModelFactory;
            _directoryViewModelFactory = directoryViewModelFactory;
            _searchResultViewModelFactory = searchResultViewModelFactory;
            SearchViewModel = searchViewModelFactory.Create();
        }

        public void SelectItem(string path)
        {
            var dirPath = VirtualPath.GetDirectoryName(path);
            var itemName = VirtualPath.GetFileName(path);
            Dispatcher.Invoke(() =>
            {
                PathString = dirPath;
                CurrentViewModel.SelectItem(itemName);
            });
        }

        protected override async Task InitializeInternal()
        {
            SearchViewModel.SearchRequest += onSearchRequest;
            var treeViewModel = _treeViewViewModelFactory.Create(FileSystem.GetRootDirectory());
            SetProperty(() => TreeViewModel, ref _treeViewModel, treeViewModel);
            treeViewModel.Initialize();
            PathString = string.Empty;
            _treeViewModel.SelectedItemChanged += onTreeViewSelectedItemChanged;
        }

        private async void onSearchRequest(object sender, EventArgs e)
        {
            _shouldBreakSearch = true;
            await _searchTask; // wait until running search stops
            _shouldBreakSearch = false;

            var dir = PathString;
            var files = FileSystem.FindFiles(dir, SearchViewModel.IsRecursive, SearchViewModel.SearchPattern)
                    .Select(x => new VirtualFileInfo(x, FileSystem) as IEntityInfo);
            var directories =
                FileSystem.FindDirectories(dir, SearchViewModel.IsRecursive, SearchViewModel.SearchPattern)
                    .Select(x => new VirtualDirectoryInfo(x, FileSystem) as IEntityInfo);

            var foundItems = files.Concat(directories);
            var searchResultsModel = _searchResultViewModelFactory.Create(this, SearchViewModel.SearchPattern);
            WindowsManager.OpenSearchResultsWindow(this, searchResultsModel);
            _searchTask = Task.Factory.StartNew(() =>
            {
                foreach (var foundItem in foundItems)
                    if (!_shouldBreakSearch)
                        searchResultsModel.AddResult(foundItem);
            });

            try
            {
                await _searchTask;
            }
            catch (Exception ex)
            {
                WindowsManager.ReportError("Search error", ex);
            }
        }

        private void setCurrentDirectory()
        {
            try
            {
                var vm = _directoryViewModelFactory.Create(this, FileSystem, PathString);
                CurrentViewModel?.Dispose();
                SetProperty(() => CurrentViewModel, ref _currentViewModel, vm);
                vm.Initialize();
            }
            catch (Exception e)
            {
                WindowsManager.ReportError("Unable to open directory", e);
            }
        }


        private void onTreeViewSelectedItemChanged(object sender, EventArgs e)
        {
            PathString = _treeViewModel.SelectedItem?.Path ?? string.Empty;
        }

        protected override void DisposeInternal()
        {
            _shouldBreakSearch = true;
            if (SearchViewModel != null)
                SearchViewModel.SearchRequest -= onSearchRequest;
            _treeViewModel.SelectedItemChanged -= onTreeViewSelectedItemChanged;
            CurrentViewModel?.Dispose();
            TreeViewModel?.Dispose();
            SetProperty(() => CurrentViewModel, ref _currentViewModel, null);
            SetProperty(() => TreeViewModel, ref _treeViewModel, null);
            FileSystem?.Dispose();
        }
    }
}