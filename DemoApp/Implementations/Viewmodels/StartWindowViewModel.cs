using System;
using DemoApp.Abstractions.Common;
using DemoApp.Abstractions.Services;
using DemoApp.Abstractions.Viewmodels;
using DemoApp.Implementations.Factories;
using VirtualDrive;

namespace DemoApp.Implementations.Viewmodels
{
    public class StartWindowViewModel : BaseViewmodel
    {
        private readonly IFileSystemViewModelFactory _fileSystemViewModelFactory;
        public IRelayCommand CreateDriveCommand { get; }
        public IRelayCommand OpenDriveCommand { get; }
        public IRelayCommand ExitCommand { get; }
        public StartWindowViewModel(IWindowsManager windowsManager,
             IFileSystemViewModelFactory fileSystemViewModelFactory,
             IRelayCommandFactory commandFactory)
            : base(windowsManager)
        {
            _fileSystemViewModelFactory = fileSystemViewModelFactory;
            CreateDriveCommand = commandFactory.Create(createDriveExec);
            OpenDriveCommand = commandFactory.Create(openDriveExec);
            ExitCommand = commandFactory.Create(exitExec);
        }

        private void openDriveExec()
        {
            if (!WindowsManager.ShowOpenDriveFileDialog(out var filename))
                return;
            try
            {
                var fs = VirtualFileSystemApi.Create(filename);

                var fileSystem = _fileSystemViewModelFactory.Create(fs);
                handleFileSystemViewModel(fileSystem);
            }
            catch (Exception e)
            {
                WindowsManager.ReportError("Unable to open file", e);
            }
        }

        private void createDriveExec()
        {
            if (!WindowsManager.ShowCreateDriveFileDialog(out var parameters, out var filename))
                return;

            try
            {
                var fs = VirtualFileSystemApi.Create(filename, parameters);

                var fileSystem = _fileSystemViewModelFactory.Create(fs);
                handleFileSystemViewModel(fileSystem);
            }
            catch (Exception e)
            {
                WindowsManager.ReportError("Unable to create file", e);
            }
        }

        private void handleFileSystemViewModel(IFileSystemViewModel fileSystem)
        {
            WindowsManager.ShowFileSystemWindow(fileSystem);
            fileSystem.Initialize();
        }

        private void exitExec()
        {
            WindowsManager.CloseStartWindow();
        }
    }
}