using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using DemoApp.Abstractions.Services;
using DemoApp.Abstractions.Viewmodels;
using DemoApp.Implementations.Factories;
using DemoApp.Implementations.Viewmodels;
using DemoApp.Windows;
using Microsoft.Win32;
using OrlemSoftware.Basics.Core.Attributes;
using VirtualDrive;

namespace DemoApp.Implementations.Services
{
    [Singletone]
    public class WindowsManager : IWindowsManager
    {
        private bool _isShuttingDown;
        private readonly Window _startWindow;
        private const string _fileDialogFilter = "Virtual Disk File|*.vdd";
        private readonly IProgressWindowViewModelFactory _progressWindowViewModelFactory;
        private readonly Dictionary<IFileSystemViewModel, FileSystemWindow> _fileSystemWindows = new Dictionary<IFileSystemViewModel, FileSystemWindow>();

        private Task _finalizationTask = Task.CompletedTask;

        private ProgressWindow _progressWindow;
        private IProgressWindowViewModel _progressWindowViewModel;

        public WindowsManager(IProgressWindowViewModelFactory progressWindowViewModelFactory)
        {
            _progressWindowViewModelFactory = progressWindowViewModelFactory;
            _startWindow = new StartWindow();
        }

        public void ShowFileSystemWindow(IFileSystemViewModel fileSystemViewModel)
        {
            if (_fileSystemWindows.ContainsKey(fileSystemViewModel))
            {
                _fileSystemWindows[fileSystemViewModel].Focus();
                return;
            }

            var window = new FileSystemWindow
            {
                DataContext = fileSystemViewModel
            };

            window.Closed += onFileSystemWindowClosed;
            window.Closing += onFileSystemClosing;
            _fileSystemWindows[fileSystemViewModel] = window;
            window.Show();
        }

        public bool GetConfirmation(string message)
        {
            return MessageBox.Show(message,
                       "Confirmation",
                       MessageBoxButton.YesNo,
                       MessageBoxImage.Question,
                       MessageBoxResult.No) == MessageBoxResult.Yes;
        }

        public void AddProgress(ILongOperationViewModel longOperationViewModel)
        {
            showProgressWindow();
            _progressWindow.Focus();
            var progressVM = getProgressWindowViewModel();
            if (progressVM.Operations.Contains(longOperationViewModel))
                return;

            progressVM.Operations.Add(longOperationViewModel);
        }

        public void RemoveProgress(ILongOperationViewModel longOperationViewModel)
        {
            var progressVM = getProgressWindowViewModel();
            progressVM.Operations.Remove(longOperationViewModel);
        }

        public void OpenSearchResultsWindow(IFileSystemViewModel filesystem, ISearchResultViewModel searchResultViewModel)
        {
            var fsWnd = _fileSystemWindows[filesystem];
            var wnd = new Window
            {
                DataContext = searchResultViewModel,
                Width = 200,
                Height = 450,
                Left = fsWnd.Left + fsWnd.ActualWidth,
                Top = fsWnd.Top,
                WindowStyle = WindowStyle.ToolWindow,
                Title = Path.GetFileName(filesystem.Filename) + ": " + searchResultViewModel.SearchPattern
            };

            var presenter = new ContentPresenter
            {
                Content = wnd.DataContext
            };

            wnd.Content = presenter;
            wnd.Owner = fsWnd;
            wnd.Show();
        }

        public void ToggleProgressWindow()
        {
            if (_progressWindow != null)
                closeProgressWindow();
            else
                showProgressWindow();
        }

        public void ReportError(string title, Exception e)
        {
            MessageBox.Show(e.Message, title);
        }

        public bool ShowOpenDriveFileDialog(out string filename)
        {
            filename = null;
            var ofd = new OpenFileDialog
            {
                Filter = _fileDialogFilter
            };

            var result = ofd.ShowDialog(_startWindow);
            var retv = result == true;
            if (retv)
                filename = ofd.FileName;

            return retv;
        }

        public bool ShowCreateDriveFileDialog(out VirtualDriveParameters parameters, out string filename)
        {
            parameters = VirtualDriveParameters.Default;
            filename = string.Empty;
            var sfd = new SaveFileDialog
            {
                Filter = _fileDialogFilter
            };

            var result = sfd.ShowDialog(_startWindow);
            var retv = result == true;
            if (!retv)
                return false;

            filename = sfd.FileName;

            try
            {
                if (File.Exists(filename))
                    using (var t = File.Open(filename, FileMode.Open))
                        t.SetLength(0);
            }
            catch (Exception e)
            {
                ReportError("Unable to create file", e);
                return false;
            }

            return true;
        }

        public void OpenStartWindow(StartWindowViewModel startWindowViewModel)
        {
            if (_startWindow.IsVisible)
                return;

            _startWindow.DataContext = startWindowViewModel;
            _startWindow.Show();
            _startWindow.Closing += onStartWindowClosing;
            _startWindow.Closed += onStartWindowClosed;
        }

        public void CloseStartWindow()
        {
            _startWindow?.Close();
        }

        private async void onStartWindowClosed(object sender, EventArgs e)
        {
            _isShuttingDown = true;

            foreach (var fileSystemWindow in _fileSystemWindows.Values.ToList())
                fileSystemWindow.Close();

            try
            {
                await _finalizationTask;
            }
            catch (Exception)
            {
                //it does not matter if something's wrong.
            }

            Application.Current.Shutdown(0);
        }

        private void onStartWindowClosing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            e.Cancel = !GetConfirmation("Are you sure you want to close program?");
        }

        private async void onFileSystemWindowClosed(object sender, EventArgs e)
        {
            if (!(sender is FileSystemWindow window))
                return;

            window.Closing -= onFileSystemClosing;
            window.Closed -= onFileSystemWindowClosed;


            if (!(window.DataContext is IFileSystemViewModel vm))
                return;

            //use while instead of foreach to prevent iteration through changed ienumerable
            while (getProgressWindowViewModel().Operations.Any(x => x.FileSystem == vm.FileSystem && !x.IsCanceled))
            {
                var longOperationViewModel = getProgressWindowViewModel().Operations
                    .First(x => x.FileSystem == vm.FileSystem && !x.IsCanceled);
                longOperationViewModel.Cancel();
                _finalizationTask = _finalizationTask.ContinueWith(t => longOperationViewModel.Task);//in case if app is going to shutdown
                try
                {
                    await longOperationViewModel.Task; //wait until all tasks are canceled before disposing file system
                }
                catch (Exception)
                {
                    //it does not matter if something's wrong.
                }
            }

            handleClosedFileSystemWindow(vm);
        }

        private void onFileSystemClosing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (_isShuttingDown)
                return;

            if (!(sender is FileSystemWindow window))
                return;
            if (!(window.DataContext is IFileSystemViewModel vm))
                return;

            if (getProgressWindowViewModel().Operations.Any(x => x.FileSystem == vm.FileSystem && !x.IsCanceled))
                e.Cancel = !GetConfirmation("Closing of this window will cancel running operations.\r\n" +
                                            "Continue?");
        }

        private void handleClosedFileSystemWindow(IFileSystemViewModel fileSystemViewModel)
        {
            if (fileSystemViewModel == null)
                return;

            if (_fileSystemWindows.ContainsKey(fileSystemViewModel))
                _fileSystemWindows.Remove(fileSystemViewModel);

            fileSystemViewModel.Dispose();
        }

        private IProgressWindowViewModel getProgressWindowViewModel()
        {
            return _progressWindowViewModel ?? (_progressWindowViewModel = _progressWindowViewModelFactory.Create());
        }

        private void closeProgressWindow()
        {
            _progressWindow?.Close();
        }

        private void showProgressWindow()
        {
            if (_progressWindow != null)
                return;

            _progressWindow = new ProgressWindow
            {
                DataContext = getProgressWindowViewModel()
            };

            _progressWindow.Show();
            _progressWindow.Closed += onProgressWindowClosed;
        }

        private void onProgressWindowClosed(object sender, EventArgs e)
        {
            _progressWindow.Closed -= onProgressWindowClosed;
            _progressWindow = null;
        }

        public void Dispose()
        {
            if (_startWindow?.IsVisible == true)
                _startWindow.Close();
        }
    }
}