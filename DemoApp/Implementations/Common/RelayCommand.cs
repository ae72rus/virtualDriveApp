using System;
using System.Windows.Threading;
using DemoApp.Abstractions.Common;

namespace DemoApp.Implementations.Common
{
    public class RelayCommand : IRelayCommand
    {
        private readonly Dispatcher _dispatcher = Dispatcher.CurrentDispatcher;
        private readonly Action<object> _action;
        private readonly Func<object, bool> _canExecuteFunc;
        public event EventHandler CanExecuteChanged;

        public RelayCommand(Action<object> execute, Func<object, bool> canExecute)
        {
            _action = execute;
            _canExecuteFunc = canExecute ?? throw new ArgumentNullException(nameof(canExecute));
        }

        public RelayCommand(Action<object> execute, Func<bool> canExecute)
            : this(execute, o => canExecute?.Invoke() ?? throw new ArgumentNullException(nameof(canExecute)))
        {

        }

        public RelayCommand(Action execute, Func<object, bool> canExecute)
            : this(o => execute?.Invoke(), canExecute)
        {

        }

        public RelayCommand(Action execute, Func<bool> canExecute)
            : this(o => execute?.Invoke(), canExecute)
        {

        }

        public RelayCommand(Action<object> execute)
            : this(execute, o => true)
        {

        }

        public RelayCommand(Action execute)
            : this(execute, o => true)
        {

        }

        public bool CanExecute(object parameter)
        {
            return _canExecuteFunc.Invoke(parameter);
        }

        public void Execute(object parameter)
        {
            _action?.Invoke(parameter);
        }

        public void RaiseCanExecuteChanged()
        {
            _dispatcher.InvokeAsync(() =>
            {
                CanExecuteChanged?.Invoke(this, EventArgs.Empty);
            });
        }
    }
}