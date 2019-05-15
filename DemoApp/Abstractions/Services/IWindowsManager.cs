using System;
using DemoApp.Abstractions.Viewmodels;
using DemoApp.Implementations.Viewmodels;
using VirtualDrive;

namespace DemoApp.Abstractions.Services
{
    public interface IWindowsManager : IDisposable
    {
        void ShowFileSystemWindow(IFileSystemViewModel fileSystemViewModel);
        bool GetConfirmation(string message);
        void AddProgress(ILongOperationViewModel longOperationViewModel);
        void RemoveProgress(ILongOperationViewModel longOperationViewModel);
        void OpenSearchResultsWindow(IFileSystemViewModel filesystem, ISearchResultViewModel searchResultViewModel);
        void ToggleProgressWindow();
        void ReportError(string title, Exception e);
        bool ShowOpenDriveFileDialog(out string filename);
        bool ShowCreateDriveFileDialog(out VirtualDriveParameters parameters, out string filename);
        void OpenStartWindow(StartWindowViewModel startWindowViewModel);
        void CloseStartWindow();
    }
}