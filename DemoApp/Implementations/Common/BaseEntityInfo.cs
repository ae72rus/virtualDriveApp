using System;
using DemoApp.Abstractions.Common;
using DemoApp.Enums;
using VirtualDrive;

namespace DemoApp.Implementations.Common
{
    public abstract class BaseEntityInfo : IEntityInfo
    {
        private readonly BaseVirtualEntity _entity;
        public virtual string Name
        {
            get => VirtualPath.GetFileName(FullName);
            set
            {
                if (_entity != null)
                    _entity.Name = value;
            }
        }

        public string FullName => _entity?.Name;
        public abstract EntityType Type { get; }
        public DateTime CreatedDateTime => _entity?.CreatedAt ?? DateTime.MinValue;
        public DateTime ModifiedDateTime => _entity?.LastEditAt ?? DateTime.MinValue;
        public IVirtualFileSystem VirtualFileSystem { get; }

        protected BaseEntityInfo(BaseVirtualEntity entity, IVirtualFileSystem fileSystem)
        {
            _entity = entity;
            VirtualFileSystem = fileSystem;
        }
    }
}