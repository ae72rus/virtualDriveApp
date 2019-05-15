using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace VirtualDrive
{
    public interface IVirtualFileSystem : IDisposable
    {
        /// <summary>
        /// Virtual drive file name
        /// </summary>
        string File { get; }

        /// <summary>
        /// Returns an entity that watches the directory
        /// </summary>
        /// <param name="directory">Directory to watch</param>
        /// <returns>VirtualDirectoryWatcher</returns>
        VirtualDirectoryWatcher Watch(VirtualDirectory directory);

        /// <summary>
        /// Get root directory of current file system
        /// </summary>
        /// <returns></returns>
        VirtualDirectory GetRootDirectory();

        /// <summary>
        /// Get directory by path
        /// </summary>
        /// <param name="directoryName">Directory full name (path)</param>
        /// <returns>VirtualDirectory</returns>
        VirtualDirectory GetDirectory(string directoryName);

        /// <summary>
        /// Get file by path
        /// </summary>
        /// <param name="filename">File full name (path)</param>
        /// <returns>VirtualFile</returns>
        VirtualFile GetFile(string filename);

        /// <summary>
        /// Find directories in Root directory
        /// </summary>
        /// <param name="recursive">Should check nested directoris</param>
        /// <param name="pattern">Search pattern</param>
        /// <returns>IEnumerable&lt;VirtualDirectory&gt;</returns>
        IEnumerable<VirtualDirectory> FindDirectories(bool recursive, string pattern);

        /// <summary>
        /// Find directories in specified directory
        /// </summary>
        /// <param name="directoryPath">Directory full name (path)</param>
        /// <param name="recursive">Should check nested directoris</param>
        /// <param name="pattern">Search pattern</param>
        /// <returns>IEnumerable&lt;VirtualDirectory&gt;</returns>
        IEnumerable<VirtualDirectory> FindDirectories(string directoryPath, bool recursive, string pattern);

        /// <summary>
        /// Find files in Root directory
        /// </summary>
        /// <param name="recursive">Should check nested directoris</param>
        /// <param name="pattern">Search pattern</param>
        /// <returns>IEnumerable&lt;VirtualFile&gt;</returns>
        IEnumerable<VirtualFile> FindFiles(bool recursive, string pattern);

        /// <summary>
        /// Find files in specified directory
        /// </summary>
        /// <param name="directoryPath">Directory full name (path)</param>
        /// <param name="recursive">Should check nested directoris</param>
        /// <param name="pattern">Search pattern</param>
        /// <returns>IEnumerable&lt;VirtualFile&gt;</returns>
        IEnumerable<VirtualFile> FindFiles(string directoryPath, bool recursive, string pattern);

        /// <summary>
        /// Create directory using specified path.
        /// Directories will be created recursively.
        /// </summary>
        /// <param name="directoryName">Full name (path)</param>
        /// <returns>VirtualDirectory</returns>
        VirtualDirectory CreateDirectory(string directoryName);

        /// <summary>
        /// Create file using specified path.
        /// Directories will NOT be created recursively.
        /// </summary>
        /// <param name="filename">Full name (path)</param>
        /// <returns>VirtualFile</returns>
        VirtualFile CreateFile(string filename);

        /// <summary>
        /// Open specified file and return stream
        /// </summary>
        /// <param name="filename">Full name (path)</param>
        /// <param name="mode">Open mode</param>
        /// <param name="access">File access</param>
        /// <returns>Stream</returns>
        Stream OpenFile(string filename, FileMode mode, FileAccess access);

        /// <summary>
        /// Delete specified file
        /// </summary>
        /// <param name="sourcePath">Full name (path)</param>
        void DeleteFile(string sourcePath);

        /// <summary>
        /// Delete specified directory including nested directories and files
        /// </summary>
        /// <param name="sourcePath">Full name (path)</param>
        void DeleteDirectory(string sourcePath);

        /// <summary>
        /// Rename specified file
        /// </summary>
        /// <param name="filename">Full name (path)</param>
        /// <param name="newName">New file name (short or full, path part will be ignored)</param>
        void RenameFile(string filename, string newName);

        /// <summary>
        /// Rename specified directory
        /// </summary>
        /// <param name="directoryName">Full name (path)</param>
        /// <param name="newName">New file name (short or full, path part will be ignored)</param>
        void RenameDirectory(string directoryName, string newName);

        /// <summary>
        /// Copy file
        /// </summary>
        /// <param name="sourcePath">Initial full name (path)</param>
        /// <param name="targetPath">Target full name (path)</param>
        /// <param name="progressCallback">Call back that will be called during operation using same thread that was used to instatiate File system API</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Task</returns>
        Task CopyFile(string sourcePath, string targetPath, Action<ProgressArgs> progressCallback, CancellationToken cancellationToken);

        /// <summary>
        /// Copy directory
        /// </summary>
        /// <param name="sourcePath">Initial full name (path)</param>
        /// <param name="targetPath">Target full name (path)</param>
        /// <param name="progressCallback">Call back that will be called during operation using same thread that was used to instatiate File system API</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Task</returns>
        Task CopyDirectory(string sourcePath, string targetPath, Action<ProgressArgs> progressCallback, CancellationToken cancellationToken);

        /// <summary>
        /// Move file
        /// </summary>
        /// <param name="sourcePath">Initial full name (path)</param>
        /// <param name="targetPath">Target full name (path)</param>
        /// <param name="progressCallback">Call back that will be called during operation using same thread that was used to instatiate File system API</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Task</returns>
        Task MoveFile(string sourcePath, string targetPath, Action<ProgressArgs> progressCallback, CancellationToken cancellationToken);

        /// <summary>
        /// Move directory
        /// </summary>
        /// <param name="sourcePath">Initial full name (path)</param>
        /// <param name="targetPath">Target full name (path)</param>
        /// <param name="progressCallback">Call back that will be called during operation using same thread that was used to instatiate File system API</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Task</returns>
        Task MoveDirectory(string sourcePath, string targetPath, Action<ProgressArgs> progressCallback, CancellationToken cancellationToken);

        /// <summary>
        /// Import directory from real filesystem
        /// </summary>
        /// <param name="source">Directory info</param>
        /// <param name="targetDirectoryPath">Target directory full name (path)</param>
        /// <param name="progressCallback">Call back that will be called during operation using same thread that was used to instatiate File system API</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Task</returns>
        Task ImportDirectory(DirectoryInfo source, string targetDirectoryPath, Action<ProgressArgs> progressCallback, CancellationToken cancellationToken);

        /// <summary>
        /// Import directory from another virtual file system
        /// </summary>
        /// <param name="source">Directory info</param>
        /// <param name="targetDirectoryPath">Target directory full name (path)</param>
        /// <param name="progressCallback">Call back that will be called during operation using same thread that was used to instatiate File system API</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Task</returns>
        Task ImportDirectory(VirtualDirectory source, string targetDirectoryPath, Action<ProgressArgs> progressCallback, CancellationToken cancellationToken);

        /// <summary>
        /// Import file from real filesystem
        /// </summary>
        /// <param name="source">File info</param>
        /// <param name="targetDirectoryPath">Target directory full name (path)</param>
        /// <param name="progressCallback">Call back that will be called during operation using same thread that was used to instatiate File system API</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Task</returns>
        Task ImportFile(FileInfo source, string targetDirectoryPath, Action<ProgressArgs> progressCallback, CancellationToken cancellationToken);

        /// <summary>
        /// Import file from another virtual file system
        /// </summary>
        /// <param name="source">Directory info</param>
        /// <param name="targetDirectoryPath">Target directory full name (path)</param>
        /// <param name="progressCallback">Call back that will be called during operation using same thread that was used to instatiate File system API</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Task</returns>
        Task ImportFile(VirtualFile source, string targetDirectoryPath, Action<ProgressArgs> progressCallback, CancellationToken cancellationToken);
    }
}