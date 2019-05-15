using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using DemoApp.Abstractions.Services;
using DemoApp.Abstractions.Viewmodels.TreeView;
using DemoApp.Implementations.Factories;
using VirtualDrive;

namespace DemoApp.Implementations.Viewmodels.TreeView
{
    public class TreeViewViewModel : BaseViewmodel, ITreeViewViewModel
    {
        private readonly ITreeViewItemViewModelFactory _treeViewItemViewModelFactory;
        private readonly VirtualDirectory _virtualDirectory;
        private ITreeViewItemViewModel _selectedItem;

        public event EventHandler SelectedItemChanged;

        public ITreeViewItemViewModel SelectedItem
        {
            get => _selectedItem;
            set
            {
                _selectedItem = value;
                RaisePropertyChanged();
                SelectedItemChanged?.Invoke(this, EventArgs.Empty);
            }
        }

        public ObservableCollection<ITreeViewItemViewModel> Directories { get; set; } = new ObservableCollection<ITreeViewItemViewModel>();

        public TreeViewViewModel(IWindowsManager windowsManager,
            ITreeViewItemViewModelFactory treeViewItemViewModelFactory,
            VirtualDirectory virtualDirectory)
            : base(windowsManager)
        {
            _treeViewItemViewModelFactory = treeViewItemViewModelFactory;
            _virtualDirectory = virtualDirectory;
        }

        protected override async Task InitializeInternal()
        {
            var rootItem = _treeViewItemViewModelFactory.Create(_virtualDirectory);
            Directories.Add(rootItem);
            rootItem.Initialize();
            rootItem.IsExpanded = true;
        }
    }
}