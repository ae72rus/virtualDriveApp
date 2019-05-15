using DemoApp.Abstractions.Viewmodels.TreeView;
using VirtualDrive;

namespace DemoApp.Abstractions.Viewmodels
{
    public interface IFileSystemViewModel : IViewModel
    {
        string Filename { get; }
        string PathString { get; set; }
        IVirtualFileSystem FileSystem { get; }
        ITreeViewViewModel TreeViewModel { get; }
        IDirectoryViewModel CurrentViewModel { get; }
        void SelectItem(string path);
    }
}