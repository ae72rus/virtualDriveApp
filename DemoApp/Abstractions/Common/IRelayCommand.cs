using System.Windows.Input;

namespace DemoApp.Abstractions.Common
{
    public interface IRelayCommand : ICommand
    {
        void RaiseCanExecuteChanged();
    }
}