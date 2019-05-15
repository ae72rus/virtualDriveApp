using VirtualDrive.Internal;

namespace VirtualDrive
{
    public static class VirtualFileSystemApi
    {
        /// <summary>
        /// Instantiate API
        /// </summary>
        /// <param name="filename">Storage file</param>
        /// <param name="parameters">File system parameters</param>
        /// <returns>IVirtualFileSystem</returns>
        public static IVirtualFileSystem Create(string filename, VirtualDriveParameters parameters)
        {
            return new VirtualFileSystem(filename, parameters);
        }

        /// <summary>
        /// Instantiate API
        /// </summary>
        /// <param name="filename">Storage file</param>
        /// <returns>IVirtualFileSystem</returns>
        public static IVirtualFileSystem Create(string filename)
        {
            return new VirtualFileSystem(filename);
        }
    }
}