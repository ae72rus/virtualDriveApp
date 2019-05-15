using System.Collections.ObjectModel;

namespace DemoApp.Abstractions.Viewmodels
{
    public interface IProgressWindowViewModel : IViewModel
    {
        ObservableCollection<ILongOperationViewModel> Operations { get; }
    }
}