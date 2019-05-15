using System.Collections.ObjectModel;
using DemoApp.Abstractions.Services;
using DemoApp.Abstractions.Viewmodels;

namespace DemoApp.Implementations.Viewmodels
{
    public class ProgressWindowViewModel : BaseViewmodel, IProgressWindowViewModel
    {
        public ObservableCollection<ILongOperationViewModel> Operations { get; } = new ObservableCollection<ILongOperationViewModel>();

        public ProgressWindowViewModel(IWindowsManager windowsManager) 
            : base(windowsManager)
        {

        }
    }
}