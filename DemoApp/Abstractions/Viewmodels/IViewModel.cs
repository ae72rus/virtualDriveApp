using System;
using System.ComponentModel;

namespace DemoApp.Abstractions.Viewmodels
{
    public interface IViewModel : INotifyPropertyChanged, IDisposable
    {
        bool IsInitializing { get; }
        void Initialize();
    }
}