using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows.Threading;
using DemoApp.Abstractions.Common;
using DemoApp.Abstractions.Services;
using DemoApp.Abstractions.Viewmodels;

namespace DemoApp.Implementations.Viewmodels
{
    public abstract class BaseViewmodel : IViewModel
    {
        private bool _isInitialized;
        private bool _isDisposing;
        private bool _isInitializing;
        private readonly Dictionary<string, List<Func<IRelayCommand>>> _commandsWiring = new Dictionary<string, List<Func<IRelayCommand>>>();
        public event PropertyChangedEventHandler PropertyChanged;

        public bool IsInitializing => _isInitializing;

        protected IWindowsManager WindowsManager { get; }
        protected Dispatcher Dispatcher { get; set; }
        protected bool IsInitialized => _isInitialized;

        protected BaseViewmodel(IWindowsManager windowsManager)
        {
            WindowsManager = windowsManager;
            Dispatcher = Dispatcher.CurrentDispatcher;
        }

        protected void RaisePropertyChanged<T>(Expression<Func<T>> propertyAccessor)
        {
            RaisePropertyChanged(getMemberNameFromExpression(propertyAccessor));
        }

        protected void RaisePropertyChanged([CallerMemberName]string propertyName = null)
        {
            if (!string.IsNullOrWhiteSpace(propertyName))
                Dispatcher.InvokeAsync(() =>
                    {
                        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
                        raiseWiredCOmmandsCanExecuteChanged(propertyName);
                    });
        }

        protected void SetProperty<T>(Expression<Func<T>> propertyAccessor, ref T backingField, T value)
        {
            if (backingField?.Equals(value) == true)
                return;

            backingField = value;
            RaisePropertyChanged(propertyAccessor);
        }

        /// <summary>
        /// Wire up property and commands. After raising PropertyChanged event commands RaiseCanExecuteChanged will be called.
        /// </summary>
        /// <typeparam name="T">Property type</typeparam>
        /// <param name="propertyAccessor">Property accessor</param>
        /// <param name="commandAccessors">Command accessors</param>
        protected void WireUpPropertyAndCommand<T>(Expression<Func<T>> propertyAccessor,
            params Func<IRelayCommand>[] commandAccessors)
        {
            var propertyName = getMemberNameFromExpression(propertyAccessor);
            WireUpPropertyAndCommand(propertyName, commandAccessors);
        }

        /// <summary>
        /// Wire up property and commands. After raising PropertyChanged event commands RaiseCanExecuteChanged will be called.
        /// </summary>
        /// <param name="propertyName">Property name</param>
        /// <param name="commandAccessors">Command accessors</param>
        protected void WireUpPropertyAndCommand(string propertyName,
           params Func<IRelayCommand>[] commandAccessors)
        {
            if (!_commandsWiring.ContainsKey(propertyName))
                _commandsWiring[propertyName] = new List<Func<IRelayCommand>>();

            _commandsWiring[propertyName].AddRange(commandAccessors);
        }

        public async void Initialize()
        {
            if(_isInitialized)
                return;

            _isInitialized = true;

            var hasNoErrors = true;
            SetProperty(() => IsInitializing, ref _isInitializing, true);
            try
            {
                await InitializeInternal();
            }
            catch (Exception e)
            {
                hasNoErrors = false;
                WindowsManager.ReportError("Initialization error", e);
            }
            finally
            {
                SetProperty(() => IsInitializing, ref _isInitializing, false);
                if (hasNoErrors)
                    OnInitialized();
            }
        }

        protected virtual Task InitializeInternal()
        {
            return Task.CompletedTask;
        }

        protected virtual void OnInitialized()
        {

        }

        private string getMemberNameFromExpression<T>(Expression<Func<T>> expression)
        {
            if (!(expression.Body is MemberExpression memberExpression))
                return string.Empty;

            return memberExpression.Member.Name;
        }

        private void raiseWiredCOmmandsCanExecuteChanged(string propertyName)
        {
            if (!_commandsWiring.ContainsKey(propertyName))
                return;

            foreach (var commandAccessor in _commandsWiring[propertyName])
                commandAccessor?.Invoke()?.RaiseCanExecuteChanged();
        }

        public void Dispose()
        {
            if (_isDisposing)
                return;
            _isDisposing = true;
            DisposeInternal();
        }

        protected virtual void DisposeInternal()
        {

        }
    }
}