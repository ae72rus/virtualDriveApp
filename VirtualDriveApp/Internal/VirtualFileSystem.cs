using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace VirtualDrive.Internal
{
    [DebuggerDisplay("File: {" + nameof(File) + "}")]
    internal class VirtualFileSystem : IVirtualFileSystem
    {
        public string File { get; }

#if !DEBUG
        [DebuggerBrowsable(DebuggerBrowsableState.Never)] //no one should see my secrets :)
#endif
        private readonly InternalFileSystem _fileSystem;

#if !DEBUG
        [DebuggerBrowsable(DebuggerBrowsableState.Never)] //no one should see my secrets :)
#endif
        private readonly SynchronizationContext _synchronizationContext = SynchronizationContext.Current;//for callbacks and exceptions

        public VirtualFileSystem(string filename) : this(filename, VirtualDriveParameters.Default)
        {

        }

        public VirtualFileSystem(string filename, VirtualDriveParameters parameters)
        {
            File = filename;
            _fileSystem = new InternalFileSystem(_synchronizationContext, filename, parameters);
            if (_synchronizationContext == null)
                throw new Exception("Syncronization context is missing");
        }

        public VirtualDirectoryWatcher Watch(VirtualDirectory directory)
        {
            return _fileSystem.Watch(directory);
        }

        public VirtualDirectory GetRootDirectory()
        {
            return _fileSystem.GetRoot();
        }

        public IEnumerable<VirtualDirectory> FindDirectories(bool recursive, string pattern)
        {
            return FindDirectories("/", recursive, pattern);
        }

        public IEnumerable<VirtualDirectory> FindDirectories(string directoryPath, bool recursive, string pattern)
        {
            var directory = getDirectoryFromPath(directoryPath);
            return directory.GetDirectories(recursive, pattern);
        }

        public IEnumerable<VirtualFile> FindFiles(bool recursive, string pattern)
        {
            return FindFiles("/", recursive, pattern);
        }

        public IEnumerable<VirtualFile> FindFiles(string directoryPath, bool recursive, string pattern)
        {
            var directory = getDirectoryFromPath(directoryPath);
            return directory.GetFiles(recursive, pattern);
        }

        public Stream OpenFile(string filename, FileMode mode, FileAccess access)
        {
            var file = getFileFromPath(filename);
            return file.Open(mode, access);
        }

        public VirtualDirectory GetDirectory(string directoryName)
        {
            return getDirectoryFromPath(directoryName);
        }

        public VirtualFile GetFile(string filename)
        {
            return getFileFromPath(filename);
        }

        public VirtualDirectory CreateDirectory(string directoryName)
        {
            var dirs = directoryName.Split(new[] { VirtualPath.Separator }, StringSplitOptions.RemoveEmptyEntries);
            var currentDir = GetRootDirectory();

            foreach (var dir in dirs)
                currentDir = currentDir.GetDirectories(false, dir).FirstOrDefault() ?? currentDir.CreateDirectory(dir);

            return currentDir;
        }

        public VirtualFile CreateFile(string filename)
        {
            var directoryName = VirtualPath.GetDirectoryName(filename);
            var directory = getDirectoryFromPath(directoryName);
            return directory.CreateFile(filename);
        }

        public void DeleteFile(string filename)
        {
            var file = getFileFromPath(filename);
            file.Remove();
        }

        public void DeleteDirectory(string directoryName)
        {
            var directory = getDirectoryFromPath(directoryName);
            directory.Remove();
        }

        public void RenameFile(string filename, string newName)
        {
            var file = getFileFromPath(filename);
            file.Name = newName;
        }

        public void RenameDirectory(string directoryName, string newName)
        {
            var dir = getDirectoryFromPath(directoryName);
            dir.Name = newName;
        }

        public async Task CopyFile(string sourcePath, string targetPath, Action<ProgressArgs> progressCallback, CancellationToken cancellationToken)
        {
            var file = getFileFromPath(sourcePath);
            var targetDirPath = VirtualPath.GetDirectoryName(targetPath);
            var targetFilename = VirtualPath.GetFileName(targetPath);
            var targetDir = getDirectoryFromPath(targetDirPath);
            var copiedFile = await file.CopyTo(targetDir, progressCallback, cancellationToken);
            if (!cancellationToken.IsCancellationRequested)
                copiedFile.Name = targetFilename;
        }

        public async Task CopyDirectory(string sourcePath, string targetPath, Action<ProgressArgs> progressCallback, CancellationToken cancellationToken)
        {
            var directory = getDirectoryFromPath(sourcePath);
            var targetDirectoryPath = VirtualPath.GetDirectoryName(targetPath);
            var targetDirectoryName = VirtualPath.GetFileName(targetPath);
            var targetDirectory = getDirectoryFromPath(targetDirectoryPath);
            var createdDirectory = await directory.CopyTo(targetDirectory, progressCallback, cancellationToken);
            if (!cancellationToken.IsCancellationRequested)
                createdDirectory.Name = targetDirectoryName;
        }

        public async Task MoveFile(string sourcePath, string targetPath, Action<ProgressArgs> progressCallback, CancellationToken cancellationToken)
        {
            var file = getFileFromPath(sourcePath);
            var targetDirectoryName = VirtualPath.GetDirectoryName(targetPath);
            var targetFileName = VirtualPath.GetFileName(targetPath);
            var targetDirectory = getDirectoryFromPath(targetDirectoryName);
            var movedFile = await file.MoveTo(targetDirectory, progressCallback, cancellationToken);
            if (!cancellationToken.IsCancellationRequested)
                movedFile.Name = targetFileName;
        }

        public async Task MoveDirectory(string sourcePath, string targetPath, Action<ProgressArgs> progressCallback, CancellationToken cancellationToken)
        {
            var directory = getDirectoryFromPath(sourcePath);
            var targetParentDirectoryName = VirtualPath.GetDirectoryName(targetPath);
            var targetDirectoryName = VirtualPath.GetFileName(targetPath);
            var targetDirectory = getDirectoryFromPath(targetParentDirectoryName);
            var movedDir = await directory.MoveTo(targetDirectory, progressCallback, cancellationToken);
            if (!cancellationToken.IsCancellationRequested)
                movedDir.Name = targetDirectoryName;
        }

        public async Task ImportDirectory(DirectoryInfo source, string targetDirectoryPath, Action<ProgressArgs> progressCallback, CancellationToken cancellationToken)
        {
            if(source == null)
                throw new ArgumentNullException(nameof(source));

            var targetDirectory = getDirectoryFromPath(targetDirectoryPath);
            var directories = source.GetDirectories("*", SearchOption.AllDirectories).Concat(new[] { source });
            var idx = 0;
            foreach (var directory in directories)
            {
                if (idx++ == 0)
                    _synchronizationContext.Post(x => progressCallback?.Invoke((ProgressArgs)x), new ImportProgressArgs(0, directory.Name));

                if (cancellationToken.IsCancellationRequested)
                    break;

                var relativePath = directory.FullName.Replace(Path.GetDirectoryName(source.FullName), string.Empty).Replace(Path.DirectorySeparatorChar, VirtualPath.Separator);
                var targetDir = targetDirectory;

                if (!string.IsNullOrWhiteSpace(relativePath))
                {
                    var importedDirectoryPath = targetDirectory.Id != 0
                        ? VirtualPath.Combine(targetDirectory.Name, relativePath)
                        : relativePath;
                    targetDir = CreateDirectory(importedDirectoryPath);
                }

                var files = directory.GetFiles();

                var completeFiles = 0;
                foreach (var file in files)
                {
                    if (cancellationToken.IsCancellationRequested)
                        break;

                    var complete = completeFiles;
                    await ImportFile(file, targetDir.Name, x => progressCallback?.Invoke(new ImportProgressArgs((int)((float)complete / files.Length * 100 + (float)x.Progress / files.Length), x.Message)), cancellationToken);
                    completeFiles++;
                }
            }
        }

        public async Task ImportDirectory(VirtualDirectory source, string targetDirectoryPath, Action<ProgressArgs> progressCallback, CancellationToken cancellationToken)
        {
            var targetDirectory = getDirectoryFromPath(targetDirectoryPath);
            await source.CopyTo(targetDirectory, progressCallback, cancellationToken);
        }

        public async Task ImportFile(FileInfo source, string targetDirectoryPath, Action<ProgressArgs> progressCallback, CancellationToken cancellationToken)
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));

            var targetDirectory = getDirectoryFromPath(targetDirectoryPath);
            var newFile = targetDirectory.CreateFile(Path.GetFileName(source.FullName));
            using (var sourceFileStream = source.Open(FileMode.Open, FileAccess.Read, FileShare.Read))
            using (var newFileStream = newFile.Open(FileMode.Open, FileAccess.Write))
            {
                newFileStream.SetLength(sourceFileStream.Length);
                var restBytes = sourceFileStream.Length;
                var buffer = BufferHelper.GetBuffer(sourceFileStream);
                _synchronizationContext.Post(x => progressCallback?.Invoke((ProgressArgs)x), new ImportProgressArgs(0, source.Name));
                var currentProgress = -1;
                while (restBytes > 0 && !cancellationToken.IsCancellationRequested)
                {
                    try
                    {
                        var count = (int)(restBytes > buffer.Length ? buffer.Length : restBytes);
                        var read = await sourceFileStream.ReadAsync(buffer, 0, count, cancellationToken);
                        if (read == 0)
                            continue;//hdd problems?

                        await newFileStream.WriteAsync(buffer, 0, read, cancellationToken);
                        restBytes -= read;
                    }
                    catch (TaskCanceledException)
                    {

                    }

                    //report progress
                    var progress = sourceFileStream.GetProgress();
                    if (progress == currentProgress)
                        continue;

                    currentProgress = progress;
                    var message = Path.GetFileName(source.FullName);
                    var progressArgs = new ImportProgressArgs(currentProgress, message) { Operation = Operation.Importing };
                    _synchronizationContext.Post(x => progressCallback?.Invoke((ProgressArgs)x), progressArgs);
                }
            }

            if (cancellationToken.IsCancellationRequested)
                _fileSystem.Remove(newFile);
        }

        public Task ImportFile(VirtualFile source, string targetDirectoryPath, Action<ProgressArgs> progressCallback, CancellationToken cancellationToken)
        {
            var targetDirectory = getDirectoryFromPath(targetDirectoryPath);
            return source.CopyTo(targetDirectory, progressCallback, cancellationToken);
        }

        private VirtualFile getFileFromPath(string filename)
        {
            if (!_fileSystem.Cache.FileNames.TryGetId(filename, out var id))
                id = getFileIdFromPath(filename);

            if (id == -1)
                throw new FileNotFoundException();

            var retv = _fileSystem.GetFile(id);
            _fileSystem.Cache.FileNames.Add(id, filename);
            return retv;
        }

        private VirtualDirectory getDirectoryFromPath(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
                return GetRootDirectory();

            if (!_fileSystem.Cache.DirectoryNames.TryGetId(path, out var id))
                id = getDirectoryIdFromPath(path);

            if (id == -1)
                throw new FileNotFoundException();

            var retv = _fileSystem.GetDirectory(id);
            _fileSystem.Cache.DirectoryNames.Add(id, path);
            return retv;
        }

        private long getFileIdFromPath(string filename)
        {
            var fn = VirtualPath.GetFileName(filename);
            var dirPath = filename.Replace(fn, string.Empty);

            var currentDirectoryId = getDirectoryIdFromPath(dirPath);
            var currentDirectory = _fileSystem.GetDirectory(currentDirectoryId);

            var file = currentDirectory?.GetFiles(false, fn).FirstOrDefault();

            return file?.Id ?? throw new FileNotFoundException(filename);
        }

        private long getDirectoryIdFromPath(string path)
        {
            var splittedPath = path.Split(new[] { VirtualPath.Separator }, StringSplitOptions.RemoveEmptyEntries);
            var currentDirectory = GetRootDirectory();
            foreach (var p in splittedPath)
            {
                currentDirectory = currentDirectory.GetDirectories(false, p).FirstOrDefault();
                if (currentDirectory == null)
                    throw new DirectoryNotFoundException(path);
            }

            return currentDirectory?.Id ?? throw new DirectoryNotFoundException(path);
        }

        public void Dispose()
        {
            _fileSystem?.Dispose();
        }
    }
}