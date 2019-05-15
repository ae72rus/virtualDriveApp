using System;
using DemoApp.Abstractions.Common;
using DemoApp.Abstractions.Services;
using DemoApp.Abstractions.Viewmodels;
using DemoApp.Implementations.Factories;

namespace DemoApp.Implementations.Viewmodels
{
    public class SearchViewModel : BaseViewmodel, ISearchViewModel
    {
        private string _searchPattern;
        private bool _isRecursive;

        public event EventHandler SearchRequest;

        public string SearchPattern
        {
            get => _searchPattern;
            set
            {
                _searchPattern = value;
                RaisePropertyChanged();
            }
        }

        public bool IsRecursive
        {
            get => _isRecursive;
            set
            {
                _isRecursive = value;
                RaisePropertyChanged();
            }
        }

        public IRelayCommand StartSearchCommand { get; }

        public SearchViewModel(IWindowsManager windowsManager,
                               IRelayCommandFactory commandFactory)
            : base(windowsManager)
        {
            StartSearchCommand = commandFactory.Create(startSearch, startSearchCanExec);
            WireUpPropertyAndCommand(() => SearchPattern, () => StartSearchCommand);
        }

        private bool startSearchCanExec()
        {
            return !string.IsNullOrWhiteSpace(SearchPattern);
        }

        private void startSearch()
        {
            SearchRequest?.Invoke(this, EventArgs.Empty);
        }
    }
}