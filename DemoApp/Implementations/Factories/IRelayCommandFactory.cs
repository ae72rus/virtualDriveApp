﻿using System;
using DemoApp.Abstractions.Common;
using OrlemSoftware.Basics.Core;

namespace DemoApp.Implementations.Factories
{
    /// <summary>
    /// Implementation for this interface will be generated by Container automatically in runtime
    /// </summary>
    public interface IRelayCommandFactory : IFactory
    {
        IRelayCommand Create(Action execute);
        IRelayCommand Create(Action<object> execute);
        IRelayCommand Create(Action execute, Func<bool> canExecute);
        IRelayCommand Create(Action execute, Func<object, bool> canExecute);
        IRelayCommand Create(Action<object> execute, Func<bool> canExecute);
        IRelayCommand Create(Action<object> execute, Func<object, bool> canExecute);
    }
}