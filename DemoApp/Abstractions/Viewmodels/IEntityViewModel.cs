using System;
using DemoApp.Abstractions.Common;
using DemoApp.Enums;
using VirtualDrive;

namespace DemoApp.Abstractions.Viewmodels
{
    public interface IEntityViewModel
    {
        string Name { get; }
        string FullName { get; }
        EntityType Type { get; }
        DateTime CreatedDateTime { get; }
        DateTime ModifiedDateTime { get; }
        IVirtualFileSystem VirtualFileSystem { get; }
        bool IsRenaming { get; set; }
        bool IsCut { get; set; }
        void RaiseUpdated();
        IEntityInfo GetEntityInfo();
    }
}