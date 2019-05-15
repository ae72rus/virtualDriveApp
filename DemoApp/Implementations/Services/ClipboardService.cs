using System;
using System.Collections.Generic;
using System.Linq;
using DemoApp.Abstractions.Services;
using DemoApp.Abstractions.Viewmodels;
using DemoApp.Enums;
using OrlemSoftware.Basics.Core.Attributes;

namespace DemoApp.Implementations.Services
{
    [Singletone]
    public class ClipboardService : IClipboardService
    {
        private readonly ILongOperationsManager _longOperationsManager;
        private readonly IWindowsManager _windowsManager;
        private readonly List<IEntityViewModel> _clipboardData = new List<IEntityViewModel>();
        private bool _isCutOperation;
        public event EventHandler HasItemsChanged;
        public bool HasItems => _clipboardData.Any();

        public ClipboardService(ILongOperationsManager longOperationsManager, IWindowsManager windowsManager)
        {
            _longOperationsManager = longOperationsManager;
            _windowsManager = windowsManager;
        }

        public void Cut(IEnumerable<IEntityViewModel> entities)
        {
            clearClipboard();
            _isCutOperation = true;
            foreach (var entity in entities)
            {
                _clipboardData.Add(entity);
                entity.IsCut = true;
            }

            HasItemsChanged?.Invoke(this, EventArgs.Empty);
        }

        public void Copy(IEnumerable<IEntityViewModel> entities)
        {
            clearClipboard();
            _clipboardData.AddRange(entities);
            HasItemsChanged?.Invoke(this, EventArgs.Empty);
        }

        public void Paste(IDirectoryViewModel directory)
        {
            var entities = _clipboardData.ToList();
            var isCutOperation = _isCutOperation;
            if (_isCutOperation)
                clearClipboard();//allow continue copying as many times as user wants
            _isCutOperation = false;
            HasItemsChanged?.Invoke(this, EventArgs.Empty);

            try
            {
                foreach (var entityInfo in entities)
                    performOperation(entityInfo, directory, isCutOperation);
            }
            catch (Exception e)
            {
                _windowsManager.ReportError("Paste error", e);
            }
        }

        private void clearClipboard()
        {
            foreach (var entity in _clipboardData)
                entity.IsCut = false;

            _clipboardData.Clear();
        }

        private void performOperation(IEntityViewModel entity, IDirectoryViewModel targetDirectory, bool isCutOperation)
        {
            if (entity.VirtualFileSystem == targetDirectory.VirtualFileSystem)
            {
                performPaste(entity, targetDirectory, isCutOperation);
                return;
            }

            performImport(entity, targetDirectory);
        }

        private void performPaste(IEntityViewModel entity, IDirectoryViewModel targetDirectory, bool isCutOperation)
        {
            if (isCutOperation)
            {
                performMove(entity, targetDirectory);
                return;
            }

            performCopy(entity, targetDirectory);
        }

        private async void performCopy(IEntityViewModel entity, IDirectoryViewModel targetDirectory)
        {
            var targetVirtualDirectory = targetDirectory.VirtualFileSystem.GetDirectory(targetDirectory.DirectoryPath);
            switch (entity.Type)
            {
                case EntityType.Directory:
                    var virtualDirectory = entity.VirtualFileSystem.GetDirectory(entity.FullName);
                    try
                    {
                        await _longOperationsManager.StartLongOperation(targetDirectory.VirtualFileSystem,
                              operation => virtualDirectory.CopyTo(targetVirtualDirectory, operation.SetState,
                                  operation.CancellationTokenSource.Token));
                    }
                    catch (Exception e)
                    {
                        _windowsManager.ReportError("Directory copy error", e);
                    }
                    break;
                case EntityType.File:
                    var virtualFile = entity.VirtualFileSystem.GetFile(entity.FullName);
                    try
                    {
                        await _longOperationsManager.StartLongOperation(targetDirectory.VirtualFileSystem,
                            operation => virtualFile.CopyTo(targetVirtualDirectory, operation.SetState,
                                operation.CancellationTokenSource.Token));
                    }
                    catch (Exception e)
                    {
                        _windowsManager.ReportError("File copy error", e);
                    }
                    break;
            }
        }

        private async void performMove(IEntityViewModel entity, IDirectoryViewModel targetDirectory)
        {
            var targetVirtualDirectory = targetDirectory.VirtualFileSystem.GetDirectory(targetDirectory.DirectoryPath);
            switch (entity.Type)
            {
                case EntityType.Directory:
                    var virtualDirectory = entity.VirtualFileSystem.GetDirectory(entity.FullName);
                    try
                    {
                        await _longOperationsManager.StartLongOperation(targetDirectory.VirtualFileSystem,
                            operation => virtualDirectory.MoveTo(targetVirtualDirectory, operation.SetState,
                                operation.CancellationTokenSource.Token));
                    }
                    catch (Exception e)
                    {
                        _windowsManager.ReportError("Directory move error", e);
                    }
                    break;
                case EntityType.File:
                    var virtualFile = entity.VirtualFileSystem.GetFile(entity.FullName);
                    try
                    {
                        await _longOperationsManager.StartLongOperation(targetDirectory.VirtualFileSystem,
                            operation => virtualFile.MoveTo(targetVirtualDirectory, operation.SetState,
                                operation.CancellationTokenSource.Token));
                    }
                    catch (Exception e)
                    {
                        _windowsManager.ReportError("File move error", e);
                    }
                    break;
            }
        }

        private async void performImport(IEntityViewModel entity, IDirectoryViewModel targetDirectory)
        {
            var targetFs = targetDirectory.VirtualFileSystem;

            var targetVirtualDirectory = targetDirectory.VirtualFileSystem.GetDirectory(targetDirectory.DirectoryPath);
            switch (entity.Type)
            {
                case EntityType.Directory:
                    var virtualDirectory = entity.VirtualFileSystem.GetDirectory(entity.FullName);
                    try
                    {
                        await _longOperationsManager.StartLongOperation(targetDirectory.VirtualFileSystem,
                            operation => targetFs.ImportDirectory(virtualDirectory, targetVirtualDirectory.Name, operation.SetState,
                                operation.CancellationTokenSource.Token));
                    }
                    catch (Exception e)
                    {
                        _windowsManager.ReportError("Directory import error", e);
                    }
                    break;

                case EntityType.File:
                    var virtualFile = entity.VirtualFileSystem.GetFile(entity.FullName);
                    try
                    {
                        await _longOperationsManager.StartLongOperation(targetDirectory.VirtualFileSystem,
                            operation => targetFs.ImportFile(virtualFile, targetVirtualDirectory.Name, operation.SetState,
                                operation.CancellationTokenSource.Token));
                    }
                    catch (Exception e)
                    {
                        _windowsManager.ReportError("File import error", e);
                    }
                    break;
            }
        }

        public void Dispose()
        {
            clearClipboard();
            HasItemsChanged?.Invoke(this, EventArgs.Empty);
        }
    }
}