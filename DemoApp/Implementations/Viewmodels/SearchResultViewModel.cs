using System.Collections.ObjectModel;
using DemoApp.Abstractions.Common;
using DemoApp.Abstractions.Services;
using DemoApp.Abstractions.Viewmodels;

namespace DemoApp.Implementations.Viewmodels
{
    public class SearchResultViewModel : BaseViewmodel, ISearchResultViewModel
    {
        private readonly IFileSystemViewModel _fileSystemViewModel;
        private IEntityInfo _selectedResult;
        public ObservableCollection<IEntityInfo> Results { get; } = new ObservableCollection<IEntityInfo>();

        public IEntityInfo SelectedResult
        {
            get => _selectedResult;
            set
            {
                _selectedResult = value;
                RaisePropertyChanged();
                synchronizeSelectedResult();
            }
        }
        public string SearchPattern { get; }

        public SearchResultViewModel(IWindowsManager windowsManager,
                                    IFileSystemViewModel fileSystemViewModel,
                                    string searchPattern)
            : base(windowsManager)
        {
            SearchPattern = searchPattern;
            _fileSystemViewModel = fileSystemViewModel;
        }


        public void AddResult(IEntityInfo result)
        {
            Dispatcher.Invoke(() => Results.Add(result));
        }

        private void synchronizeSelectedResult()
        {
            if (SelectedResult != null)
                _fileSystemViewModel.SelectItem(SelectedResult.FullName);
        }
    }
}