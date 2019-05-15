using System;
using DemoApp.Enums;
using VirtualDrive;

namespace DemoApp.Abstractions.Common
{
    public interface IEntityInfo
    {
        string Name { get; set; }
        string FullName { get; }
        EntityType Type { get; }
        DateTime CreatedDateTime { get; }
        DateTime ModifiedDateTime { get; }
        IVirtualFileSystem VirtualFileSystem { get; }
    }
}