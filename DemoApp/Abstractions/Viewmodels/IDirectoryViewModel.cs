using VirtualDrive;

namespace DemoApp.Abstractions.Viewmodels
{
    public interface IDirectoryViewModel : IViewModel
    {
        string DirectoryPath { get; }
        IVirtualFileSystem VirtualFileSystem { get; }
        void SelectItem(string itemName);
    }
}