using DemoApp.Abstractions.Common;
using DemoApp.Enums;
using VirtualDrive;

namespace DemoApp.Implementations.Common
{
    public class VirtualFileInfo : BaseEntityInfo, IFileInfo
    {
        private readonly VirtualFile _entity;
        public long Length => _entity.Length;
        public VirtualFileInfo(VirtualFile entity, IVirtualFileSystem fileSystem) : base(entity, fileSystem)
        {
            _entity = entity;
        }

        public override EntityType Type => EntityType.File;
    }
}