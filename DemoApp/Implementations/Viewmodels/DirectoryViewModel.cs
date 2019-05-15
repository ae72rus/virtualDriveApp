using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using DemoApp.Abstractions.Common;
using DemoApp.Abstractions.Services;
using DemoApp.Abstractions.Viewmodels;
using DemoApp.Enums;
using DemoApp.Extensions;
using DemoApp.Implementations.Common;
using DemoApp.Implementations.Factories;
using VirtualDrive;
using DirectoryInfo = System.IO.DirectoryInfo;
using FileInfo = System.IO.FileInfo;

namespace DemoApp.Implementations.Viewmodels
{
    public class DirectoryViewModel : BaseViewmodel, IDirectoryViewModel
    {
        private readonly Dictionary<BaseVirtualEntity, IEntityViewModel> _entitiesMatchDict = new Dictionary<BaseVirtualEntity, IEntityViewModel>();
        private readonly ILongOperationsManager _longOperationsManager;
        private readonly IClipboardService _clipboardService;
        private readonly IFileSystemViewModel _fileSystem;
        private readonly IEntityViewModelFactory _entityViewModelFactory;
        private ObservableCollection<IEntityViewModel> _nestedObjects = new ObservableCollection<IEntityViewModel>();
        private VirtualDirectory _directory;
        private VirtualDirectoryWatcher _watcher;
        private string _autoRenameEntityname = string.Empty;
        private IEntityViewModel _selectedObject;
        private IList _selectedObjects = new ObservableCollection<IEntityViewModel>();

        #region Properties

        public IVirtualFileSystem VirtualFileSystem { get; }

        public string DirectoryPath { get; }


        public ObservableCollection<IEntityViewModel> NestedObjects => _nestedObjects;

        public IList SelectedObjects
        {
            get => _selectedObjects;
            set
            {
                _selectedObjects = value;
                RaisePropertyChanged();
            }
        }

        public IEntityViewModel SelectedObject
        {
            get => _selectedObject;
            set
            {
                if (_selectedObject != null && _selectedObject.IsRenaming)
                    _selectedObject.IsRenaming = false;

                _selectedObject = value;
                RaisePropertyChanged();
            }
        }

        #region Commands
        public IRelayCommand StartRenamingCommand { get; }
        public IRelayCommand PrimaryActionCommand { get; }
        public IRelayCommand CreateFileCommand { get; }
        public IRelayCommand CreateDirectoryCommand { get; }
        public IRelayCommand CopyCommand { get; }
        public IRelayCommand CutCommand { get; }
        public IRelayCommand PasteCommand { get; }
        public IRelayCommand DeleteCommand { get; }
        public IRelayCommand MoveUpCommand { get; }
        public IRelayCommand DropFilesCommand { get; }
        public IRelayCommand DropItemsCommand { get; }

        #endregion Commands

        #endregion Properties

        public DirectoryViewModel(IWindowsManager windowsManager,
            ILongOperationsManager longOperationsManager,
            IClipboardService clipboardService,
            IRelayCommandFactory commandFactory,
            IFileSystemViewModel fileSystem,
            IVirtualFileSystem virtualFileSystem,
            IEntityViewModelFactory entityViewModelFactory,
            string directoryPath)
            : base(windowsManager)
        {
            _longOperationsManager = longOperationsManager;
            _clipboardService = clipboardService;
            _clipboardService.HasItemsChanged += onClipboardHasItemsChanged;
            _fileSystem = fileSystem;
            _entityViewModelFactory = entityViewModelFactory;
            VirtualFileSystem = virtualFileSystem;
            DirectoryPath = directoryPath;

            //commands init
            StartRenamingCommand = commandFactory.Create(startRenamingExec, startRenamingCanExec);
            wireUpRenamingCommands();

            PrimaryActionCommand = commandFactory.Create(primaryActionExec, primaryActionCanExec);
            wireUpPrimaryAction();

            CreateFileCommand = commandFactory.Create(createFileExec, createFileCanExec);
            CreateDirectoryCommand = commandFactory.Create(createDirectoryExec, createDirectoryCanExec);
            CopyCommand = commandFactory.Create(copyExec, copyCanExec);
            CutCommand = commandFactory.Create(cutExec, cutCanExec);
            wireUpCopyAndCut();

            PasteCommand = commandFactory.Create(pasteExec, pasteCanExec);

            DeleteCommand = commandFactory.Create(deleteExec, deleteCanExec);
            wireUpDeleteCommand();

            MoveUpCommand = commandFactory.Create(moveUpExec, moveUpCanExec);
            DropFilesCommand = commandFactory.Create(dropFilesExec, dropFilesCanExec);
            DropItemsCommand = commandFactory.Create(dropItemsExec, dropItemsCanExec);
        }
        public void SelectItem(string itemName)
        {
            var item = NestedObjects.FirstOrDefault(x =>
                x.Name.Equals(itemName, StringComparison.InvariantCultureIgnoreCase));
            SelectedObject = item;
        }

        #region Initialization
        protected override async Task InitializeInternal()
        {
            _directory = VirtualFileSystem.GetDirectory(DirectoryPath);
            MoveUpCommand.RaiseCanExecuteChanged();
            readNestedObjects();
            addLevelUpLink();
            _watcher = VirtualFileSystem.Watch(_directory);
            _watcher.DirectoryEvent += onDirectoryEvent;
            _watcher.FileEvent += onFileEvent;
        }

        private void addLevelUpLink()
        {
            if (_directory == VirtualFileSystem.GetRootDirectory())
                return;

            var parentDirectory = VirtualFileSystem.GetDirectory(VirtualPath.GetDirectoryName(_directory.Name));
            NestedObjects.Insert(0, _entityViewModelFactory.Create(VirtualFileSystem, parentDirectory, true));
        }

        private void readNestedObjects()
        {
            var nestedDirectories = _directory.GetDirectories().Select(x =>
            {
                var retv = _entityViewModelFactory.Create(VirtualFileSystem, x, false);
                _entitiesMatchDict[x] = retv;
                return retv;
            });

            var nestedFiles = _directory.GetFiles().Select(x =>
            {
                var retv = _entityViewModelFactory.Create(VirtualFileSystem, x);
                _entitiesMatchDict[x] = retv;
                return retv;
            });

            var nestedObjects = nestedDirectories.Concat(nestedFiles);
            SetProperty(() => NestedObjects, ref _nestedObjects, new ObservableCollection<IEntityViewModel>(nestedObjects.OrderBy(x => x.Name)));
        }

        private void onDirectoryEvent(object sender, DirectoryEventArgs e)
        {
            onWatcherEvent(e.Event, e.Directory, x => _entityViewModelFactory.Create(VirtualFileSystem, (VirtualDirectory)x, false));
        }

        private void onFileEvent(object sender, FileEventArgs e)
        {
            onWatcherEvent(e.Event, e.File, x => _entityViewModelFactory.Create(VirtualFileSystem, (VirtualFile)x));
        }

        private void onWatcherEvent(WatcherEvent e,
            BaseVirtualEntity virtualEntity,
            Func<BaseVirtualEntity, IEntityViewModel> createEntityInfoFunc)
        {
            switch (e)
            {
                case WatcherEvent.Created:

                    var entityInfo = createEntityInfoFunc(virtualEntity);
                    _entitiesMatchDict[virtualEntity] = entityInfo;

                    NestedObjects.InsertAuto(entityInfo, x => x.Name);

                    handleAddedEntityAutoRename(entityInfo);
                    break;
                case WatcherEvent.Updated:
                    if (_entitiesMatchDict.ContainsKey(virtualEntity))
                        _entitiesMatchDict[virtualEntity].RaiseUpdated();
                    break;
                case WatcherEvent.Deleted:
                    if (!_entitiesMatchDict.ContainsKey(virtualEntity))
                        break;

                    var item = _entitiesMatchDict[virtualEntity];
                    NestedObjects.Remove(item);
                    break;
            }
        }

        private void handleAddedEntityAutoRename(IEntityViewModel entityInfo)
        {
            if (string.IsNullOrWhiteSpace(_autoRenameEntityname))
                return;

            if (!_autoRenameEntityname.Equals(entityInfo.Name, StringComparison.InvariantCultureIgnoreCase))
                return;

            SelectedObject = entityInfo;

            StartRenamingCommand?.Execute(null);
        }
        #endregion

        #region Renaming

        private void wireUpRenamingCommands()
        {
            WireUpPropertyAndCommand(() => SelectedObject, () => StartRenamingCommand);
        }

        private void startRenamingExec()
        {
            if (startRenamingCanExec())
                SelectedObject.IsRenaming = true;
        }

        private bool startRenamingCanExec()
        {
            return SelectedObject != null;
        }
        #endregion

        #region Primary action

        private void wireUpPrimaryAction()
        {
            WireUpPropertyAndCommand(() => SelectedObject, () => PrimaryActionCommand);
        }

        private void primaryActionExec()
        {
            if (!primaryActionCanExec())
                return;

            switch (SelectedObject.Type)
            {
                case EntityType.Directory:
                    openDirectoryExec();
                    break;
                case EntityType.File:
                    openFileExec();
                    break;
            }
        }

        private void openDirectoryExec()
        {
            _fileSystem.PathString = SelectedObject.FullName;
        }

        private async void openFileExec()
        {
            var tmpDir = Path.GetTempPath();
            var fileName = SelectedObject.Name;
            var tmpFilePath = Path.Combine(tmpDir, fileName);
            var currentObject = SelectedObject;
            var operationTask = _longOperationsManager.StartLongOperation(VirtualFileSystem, async operation =>
            {
                operation.Message = $"Opening {currentObject.Name}";
                var hasErrors = false;
                using (var virtualFile = VirtualFileSystem.OpenFile(currentObject.FullName, FileMode.Open, FileAccess.Read))
                using (var fStream = new FileStream(tmpFilePath, FileMode.Create, FileAccess.Write))
                {
                    var buffer = new byte[1024 * 1024 * 4];
                    var totalReadCount = 0L;
                    var currentProgress = 0;
                    if (virtualFile.Length > 0)
                        await Task.Factory.StartNew(() =>
                        {
                            fStream.SetLength(virtualFile.Length);
                            while (totalReadCount < virtualFile.Length && !operation.CancellationTokenSource.IsCancellationRequested)
                            {
                                var readCount = virtualFile.Read(buffer, 0, buffer.Length);
                                totalReadCount += readCount;

                                try
                                {
                                    fStream.Write(buffer, 0, readCount);
                                }
                                catch (Exception e)
                                {
                                    hasErrors = true;
                                    WindowsManager.ReportError("Write file error", e);
                                    break;
                                }

                                var progress = (int)((float)totalReadCount / virtualFile.Length * 100);

                                if (currentProgress == progress)
                                    continue;

                                currentProgress = progress;
                                operation.Progress = progress;
                            }
                        });
                }

                if (!operation.IsCanceled && !hasErrors)
                    Process.Start(tmpFilePath);
                else
                    try
                    {
                        File.Delete(tmpFilePath);
                    }
                    catch (Exception e)
                    {
                        WindowsManager.ReportError("Cleanup error", e);
                    }
            });


            try
            {
                await operationTask;
            }
            catch (Exception e)
            {
                WindowsManager.ReportError("Open file error", e);
            }
        }

        private bool primaryActionCanExec()
        {
            return SelectedObject != null;
        }
        #endregion

        #region CreateFile

        private bool createFileCanExec()
        {
            return true;
        }

        private void createFileExec()
        {
            if (!createFileCanExec())
                return;

            var fileName = "New file.txt";
            fileName = ensureFileNameIsUnique(fileName);

            _autoRenameEntityname = fileName;
            VirtualFileSystem.CreateFile(VirtualPath.Combine(DirectoryPath, fileName));
        }

        #endregion

        #region CreateDirectory

        private bool createDirectoryCanExec()
        {
            return true;
        }

        private void createDirectoryExec()
        {
            if (!createDirectoryCanExec())
                return;

            var directoryName = "New directory";
            directoryName = ensureDirectoryNameIsUnique(directoryName);

            _autoRenameEntityname = directoryName;
            VirtualFileSystem.CreateDirectory(VirtualPath.Combine(DirectoryPath, directoryName));
        }

        #endregion

        #region Copy

        private bool copyCanExec()
        {
            return SelectedObjects?.Cast<IEntityViewModel>().Any() == true;
        }

        private void copyExec()
        {
            if (!copyCanExec())
                return;

            _clipboardService.Copy(SelectedObjects.Cast<IEntityViewModel>());
        }

        #endregion

        #region Cut

        private bool cutCanExec() => copyCanExec();

        private void cutExec()
        {
            if (!cutCanExec())
                return;

            _clipboardService.Cut(SelectedObjects.Cast<IEntityViewModel>());
        }

        #endregion

        private void wireUpCopyAndCut()
        {
            WireUpPropertyAndCommand(() => SelectedObjects, () => CopyCommand, () => CutCommand);
        }

        #region Paste

        private bool pasteCanExec()
        {
            return _clipboardService.HasItems;
        }

        private void pasteExec()
        {
            if (!pasteCanExec())
                return;

            _clipboardService.Paste(this);
        }

        #endregion

        #region Delete

        public bool deleteCanExec()
        {
            return SelectedObjects?.Count > 0;
        }

        public void deleteExec()
        {
            if (!deleteCanExec())
                return;

            if(!WindowsManager.GetConfirmation("Are you sure you want to delete selected item(s)?"))
                return;

            foreach (var entityInfo in SelectedObjects.Cast<IEntityViewModel>().Select(x => x.GetEntityInfo()).ToList())
            {
                if (entityInfo is ParentDirectoryInfo)
                    continue;

                switch (entityInfo.Type)
                {
                    case EntityType.Directory:
                        try
                        {
                            VirtualFileSystem.DeleteDirectory(entityInfo.FullName);
                        }
                        catch (Exception e)
                        {
                            WindowsManager.ReportError("Directory delete error", e);
                        }
                        break;
                    case EntityType.File:
                        try
                        {
                            VirtualFileSystem.DeleteFile(entityInfo.FullName);
                        }
                        catch (Exception e)
                        {
                            WindowsManager.ReportError("File delete error", e);
                        }
                        break;
                }
            }
        }

        private void wireUpDeleteCommand()
        {
            WireUpPropertyAndCommand(() => SelectedObjects, () => DeleteCommand);
        }

        #endregion
        
        #region MoveUp
        private bool moveUpCanExec()
        {
            return _directory != VirtualFileSystem.GetRootDirectory();
        }

        private void moveUpExec()
        {
            if (!moveUpCanExec())
                return;

            var parentDirName = VirtualPath.GetDirectoryName(_directory.Name);
            _fileSystem.PathString = parentDirName;
        }
        #endregion

        #region DropFiles

        private bool dropFilesCanExec(object parameters)
        {
            return parameters is string[];
        }

        private void dropFilesExec(object parameters)
        {
            if (!dropFilesCanExec(parameters))
                return;

            var paths = (string[])parameters;
            var files = paths.Where(File.Exists).Select(x => new FileInfo(x));
            var directories = paths.Where(Directory.Exists).Select(x => new DirectoryInfo(x));

            foreach (var file in files)
            {
                var internalFile = file;//avoid closure
                _longOperationsManager.StartLongOperation(VirtualFileSystem, operation =>
                   VirtualFileSystem.ImportFile(internalFile, DirectoryPath, operation.SetState, operation.CancellationTokenSource.Token));
            }

            foreach (var directory in directories)
            {
                var internalDirectory = directory;//avoid closure
                _longOperationsManager.StartLongOperation(VirtualFileSystem, operation =>
                     VirtualFileSystem.ImportDirectory(internalDirectory, DirectoryPath, operation.SetState, operation.CancellationTokenSource.Token));
            }
        }

        #endregion

        #region DropItems

        private bool dropItemsCanExec(object parameters)
        {
            if (parameters is IList items)
                return items.Cast<IEntityViewModel>().All(x => x.VirtualFileSystem != VirtualFileSystem);

            return false;
        }

        private void dropItemsExec(object parameters)
        {
            if (!dropItemsCanExec(parameters))
                return;

            var items = ((IList)parameters);

            var files = items.OfType<IEntityViewModel>()
                .Select(x => x.GetEntityInfo())
                .OfType<IFileInfo>()
                .Select(x => x.VirtualFileSystem.GetFile(x.FullName));

            var directories = items.OfType<IEntityViewModel>()
                .Select(x => x.GetEntityInfo())
                .OfType<IDirectoryInfo>()
                .Where(x => !(x is ParentDirectoryInfo))
                .Select(x => x.VirtualFileSystem.GetDirectory(x.FullName));

            foreach (var file in files)
            {
                var internalFile = file;//avoid closure
                _longOperationsManager.StartLongOperation(VirtualFileSystem,
                    operation => VirtualFileSystem.ImportFile(internalFile, DirectoryPath,
                        operation.SetState, operation.CancellationTokenSource.Token));
            }

            foreach (var directory in directories)
            {
                var internalDirectory = directory;//avoid closure
                _longOperationsManager.StartLongOperation(VirtualFileSystem,
                    operation => VirtualFileSystem.ImportDirectory(internalDirectory, DirectoryPath,
                        operation.SetState, operation.CancellationTokenSource.Token));
            }
        }

        #endregion

        private void onClipboardHasItemsChanged(object sender, EventArgs e)
        {
            PasteCommand.RaiseCanExecuteChanged();
        }

        private string ensureFileNameIsUnique(string candidate)
        {
            var retv = candidate;
            var namePart = VirtualPath.GetFileNameWithoutExtension(candidate);
            var extension = VirtualPath.GetFileExtension(candidate) ?? string.Empty;
            var idx = 0;
            while (NestedObjects.Any(x => x.Type == EntityType.File && x.Name.Equals(retv, StringComparison.InvariantCultureIgnoreCase)))
            {
                retv = $"{namePart} ({++idx})";
                if (!string.IsNullOrWhiteSpace(extension))
                    retv += $".{extension}";
            }

            return retv;
        }

        private string ensureDirectoryNameIsUnique(string candidate)
        {
            var retv = candidate;
            var idx = 0;
            while (NestedObjects.Any(x => x.Type == EntityType.Directory && x.Name.Equals(retv, StringComparison.InvariantCultureIgnoreCase)))
                retv = $"{candidate} ({++idx})";

            return retv;
        }

        protected override void DisposeInternal()
        {
            if (_watcher != null)
            {
                _watcher.DirectoryEvent -= onDirectoryEvent;
                _watcher.FileEvent -= onFileEvent;
                _watcher.Dispose();
            }

            _clipboardService.HasItemsChanged -= onClipboardHasItemsChanged;
            _entitiesMatchDict.Clear();
            NestedObjects?.Clear();
        }
    }
}