using System.Collections.ObjectModel;

namespace DemoApp.Abstractions.Viewmodels.TreeView
{
    public interface ITreeViewItemViewModel : IViewModel
    {
        bool IsExpanded { get; set; }
        string Path { get; }
        string Name { get; }
        ObservableCollection<ITreeViewItemViewModel> NestedDirectories { get; }
        void RaiseUpdated();
    }
}