using DemoApp.Abstractions.Common;
using DemoApp.Enums;
using VirtualDrive;

namespace DemoApp.Implementations.Common
{
    public class VirtualDirectoryInfo : BaseEntityInfo, IDirectoryInfo
    {
        public VirtualDirectoryInfo(VirtualDirectory entity, IVirtualFileSystem fileSystem)
            : base(entity, fileSystem)
        {
        }

        public override EntityType Type => EntityType.Directory;
    }
}