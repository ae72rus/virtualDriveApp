using System;
using System.Collections.Generic;
using DemoApp.Abstractions.Common;
using DemoApp.Abstractions.Viewmodels;

namespace DemoApp.Abstractions.Services
{
    public interface IClipboardService : IDisposable
    {
        event EventHandler HasItemsChanged;
        bool HasItems { get; }
        void Cut(IEnumerable<IEntityViewModel> entities);
        void Copy(IEnumerable<IEntityViewModel> entities);
        void Paste(IDirectoryViewModel directory);
    }
}