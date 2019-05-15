using System;
using System.Collections.ObjectModel;

namespace DemoApp.Abstractions.Viewmodels.TreeView
{
    public interface ITreeViewViewModel : IViewModel
    {
        event EventHandler SelectedItemChanged;
        ITreeViewItemViewModel SelectedItem { get; set; }
        ObservableCollection<ITreeViewItemViewModel> Directories { get; }
    }
}