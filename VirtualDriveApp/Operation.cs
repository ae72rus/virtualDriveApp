using System;

namespace VirtualDrive
{
    [Flags]
    public enum Operation
    {//let's break S from SOLID
        Copying = 1 << 0,
        Moving = 1 << 1,
        Removing = 1 << 2,
        Importing = 1 << 3,

        Initializing = 1 << 4,
        Progress = 1 << 5,
        Completed = 1 << 6,

        ProgressUnknown = 1 << 7
    }
}