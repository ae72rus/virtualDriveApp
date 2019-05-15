using VirtualDrive;

namespace DemoApp.Implementations.Common
{
    public class ParentDirectoryInfo : VirtualDirectoryInfo
    {
        private string _name = "[...]";

        public override string Name
        {
            get => _name;
            set {  }
        }

        public ParentDirectoryInfo(VirtualDirectory entity, IVirtualFileSystem fileSystem) 
            : base(entity, fileSystem)
        {
        }
    }
}