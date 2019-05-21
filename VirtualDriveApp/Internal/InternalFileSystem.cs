using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using VirtualDrive.Internal.RawData;

namespace VirtualDrive.Internal
{
    internal class InternalFileSystem : IDisposable
    {
        private readonly Dictionary<char, string> _regexReplacementsDict = new Dictionary<char, string>
        {
            { '*', ".*" },
            { '?', ".{1}" }
        };

        private const string _charactersToEscape = "().!$";
        private readonly SynchronizationContext _synchronizationContext;
        private readonly VirtualDirectoryWatcherSource _directoryWatcherSource;
        public Cache Cache { get; } = new Cache();
        public Indexer Indexer { get; } = new Indexer();
        private readonly Locker _locker = new Locker();
        private readonly RawDataManager _rawDataManager;

        public InternalFileSystem(SynchronizationContext synchronizationContext, string fileName, VirtualDriveParameters parameters)
        {
            if (string.IsNullOrWhiteSpace(fileName))
                throw new ArgumentNullException(nameof(fileName));

            _synchronizationContext = synchronizationContext;
            _directoryWatcherSource = new VirtualDirectoryWatcherSource(_synchronizationContext);

            _rawDataManager = RawDataManager.Create(fileName, parameters
                                                    ?? throw new ArgumentNullException(nameof(parameters)));
            try
            {
                var entries = _rawDataManager.ReadEntries();
                Indexer.Populate(entries);
            }
            catch (Exception e)
            {
                _rawDataManager.Dispose();
                throw new Exception("FS initialization failed", e);
            }
        }

        public Task SetFileLength(VirtualFile file, long length)
        {
            var entry = getFileEntry(file);
            if (_locker.TryLockWriting(entry, out var operation))
                throw new AccessViolationException($"File could not be written. File: {file.Name}");

            using (operation)
            {
                return SetFileLength(entry, length);
            }
        }

        public Task SetFileLength(FileEntry file, long length)
        {
            return _rawDataManager.SetFileLength(file, length).Task;
        }

        public VirtualDirectoryWatcher Watch(VirtualDirectory directory)
        {
            return _directoryWatcherSource.Alloc(directory);
        }

        public VirtualDirectory GetRoot()
        {
            return getVirtualDirectory(0);
        }

        public IEnumerable<VirtualFile> GetDirectoryFiles(VirtualDirectory directory, bool recursive, string searchString)
        {
            var entry = getDirectoryEntry(directory);
            return getDirectoryVirtualFiles(entry, recursive, searchString);
        }

        public IEnumerable<VirtualDirectory> GetNestedDirectories(VirtualDirectory directory, bool recursive, string searchString)
        {
            var entry = getDirectoryEntry(directory);
            return getNestedVirtualDirectories(entry, recursive, searchString);
        }

        public async Task<VirtualFile> MoveFile(VirtualFile file, VirtualDirectory targetDirectory, Action<ProgressArgs> progressCallback, CancellationToken cancellationToken)
        {
            if (targetDirectory.FileSystem != this)
                return await CopyFile(file, targetDirectory, args => progressCallback?.Invoke(new MoveProgressArgs(args.Progress, args.Message) { Operation = Operation.Moving }), cancellationToken);

            var fileEntry = getFileEntry(file);
            var oldParentDirectory = getVirtualDirectory(fileEntry.DirectoryId);
            if (fileEntry.DirectoryId == targetDirectory.Id)
                return file;

            Indexer.RemoveEntry(fileEntry);

            fileEntry.DirectoryId = targetDirectory.Id;
            Indexer.AddEntry(fileEntry);
            _rawDataManager.Write(fileEntry);

            _directoryWatcherSource.RaiseDeleted(oldParentDirectory, file);
            _directoryWatcherSource.RaiseCreated(targetDirectory, file);

            return file;
        }

        public async Task<VirtualFile> CopyFile(VirtualFile file, VirtualDirectory targetDirectory, Action<ProgressArgs> progressCallback, CancellationToken cancellationToken)
        {
            var fileEntry = getFileEntry(file);
            var filename = VirtualPath.GetFileName(file.Name);//get filename from full path
            if (fileEntry.DirectoryId == targetDirectory.Id)
                filename = $"Copy {filename}";

            var idx = 0;
            while (targetDirectory.GetFiles(false, filename).Any())
            {
                var name = VirtualPath.GetFileNameWithoutExtension(filename);
                var ext = VirtualPath.GetFileExtension(filename);
                filename = $"{name} ({++idx}).{ext}";
            }

            var createdFile = targetDirectory.CreateFile(filename);

            using (var newFileStream = createdFile.Open(FileMode.Open, FileAccess.Write))
            using (var sourceFileStream = file.Open(FileMode.Open, FileAccess.Read))
            {
                newFileStream.SetLength(sourceFileStream.Length);
                var restBytes = sourceFileStream.Length;
                var buffer = BufferHelper.GetBuffer(sourceFileStream);
                var currentProgress = -1;
                while (restBytes > 0 && !cancellationToken.IsCancellationRequested)
                {
                    try
                    {
                        var count = (int)(restBytes > buffer.Length ? buffer.Length : restBytes);
                        var read = await sourceFileStream.ReadAsync(buffer, 0, count, cancellationToken);
                        await newFileStream.WriteAsync(buffer, 0, count, cancellationToken);
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
                    var message = VirtualPath.GetFileName(createdFile.Name);
                    var progressArgs = new CopyProgressArgs(progress, message) { Operation = Operation.Copying };
                    _synchronizationContext.Post(x => progressCallback?.Invoke((ProgressArgs)x), progressArgs);
                }
            }

            if (!cancellationToken.IsCancellationRequested)
                return createdFile;

            Remove(createdFile);
            _directoryWatcherSource.RaiseDeleted(targetDirectory, file);

            return createdFile;
        }

        public async Task<VirtualDirectory> MoveDirectory(VirtualDirectory directory, VirtualDirectory target, Action<ProgressArgs> progressCallback, CancellationToken cancellationToken)
        {
            var targetDirEntry = getDirectoryEntry(target);
            var chain = getDirectoryChain(targetDirEntry);
            if (target == directory || chain.Any(x => x.Id == directory.Id))
                throw new DirectoryNotFoundException("Cannot move directory into itself or into nested directory");

            if (directory.FileSystem != target.FileSystem)
            {
                return await CopyDirectory(directory, target, args => progressCallback?.Invoke(new MoveProgressArgs(args.Progress, args.Message)
                {
                    Operation = Operation.Moving
                }), cancellationToken);
            }

            var dirEntry = getDirectoryEntry(directory);
            var oldParentDirectory = getVirtualDirectory(dirEntry.DirectoryId);
            var targetEntry = getDirectoryEntry(target);
            Indexer.RemoveEntry(dirEntry);

            dirEntry.DirectoryId = targetEntry.Id;
            Indexer.AddEntry(dirEntry);
            _rawDataManager.Write(dirEntry);

            _directoryWatcherSource.RaiseDeleted(oldParentDirectory, directory);
            _directoryWatcherSource.RaiseCreated(target, directory);

            return directory;
        }

        public async Task<VirtualDirectory> CopyDirectory(VirtualDirectory directory, VirtualDirectory target, Action<ProgressArgs> progressCallback, CancellationToken cancellationToken)
        {
            var directoryEntry = getDirectoryEntry(directory);
            var targetEntry = getDirectoryEntry(target);
            var directoryName = VirtualPath.GetFileName(directoryEntry.Name);//get name from full path
            if (directoryEntry.DirectoryId == targetEntry.Id)
                directoryName = $"Copy {directoryName}";

            var idx = 0;
            while (target.GetDirectories(false, directoryName).Any())
            {
                var name = VirtualPath.GetFileNameWithoutExtension(directoryName);
                directoryName = $"{name} ({++idx})";
            }

            VirtualDirectory createdDirectory = null;
            var srcDirsQueue = new Queue<VirtualDirectory>(new[] { directory });
            var parentTargetDirectories = new Dictionary<VirtualDirectory, VirtualDirectory>//key: dir from the queue, val: parent dir where should be created nested directory
            {
                [directory] = target
            };

            var totalFiles = 1;
            var processedFiles = 0;

            while (srcDirsQueue.Any() && !cancellationToken.IsCancellationRequested)
            {
                var currentSourceDirectory = srcDirsQueue.Dequeue();
                var currentTargetDirectory = parentTargetDirectories[currentSourceDirectory].CreateDirectory(currentSourceDirectory.Name);

                _directoryWatcherSource.RaiseCreated(parentTargetDirectories[currentSourceDirectory], currentTargetDirectory);

                if (currentSourceDirectory == directory)//this should happen during the first iteration
                    createdDirectory = currentTargetDirectory;

                var nestedDirectories = currentSourceDirectory.GetDirectories();
                foreach (var nestedDirectory in nestedDirectories)
                {
                    srcDirsQueue.Enqueue(nestedDirectory);
                    parentTargetDirectories[nestedDirectory] = currentTargetDirectory;
                }

                var directoryFiles = currentSourceDirectory.GetFiles().ToList();
                totalFiles += directoryFiles.Count;
                foreach (var directoryFile in directoryFiles)
                {
                    processedFiles++;
                    var processedFilesLocal = processedFiles;
                    var totalFilesLocal = totalFiles;
                    await directoryFile.CopyTo(currentTargetDirectory, args =>
                    {
                        var progress = (float)args.Progress * processedFilesLocal / totalFilesLocal * 100;
                        progressCallback?.Invoke(new CopyProgressArgs((int)progress, args.Message) { Operation = Operation.Copying });
                    }, cancellationToken);
                }
            }

            return createdDirectory;
        }

        public Stream OpenFile(VirtualFile file, FileMode mode, FileAccess access)
        {
            var fileEntry = getFileEntry(file);

            Locker.LockerOperation writeLock = null;
            Locker.LockerOperation readLock = null;
            Locker.LockerOperation directoriesLock = null;

            if (access.HasFlag(FileAccess.Write))
            {
                if (!_locker.TryLockWriting(fileEntry, out writeLock))
                    throw new AccessViolationException("Unable to write file: file is locked");

                readLock = _locker.LockReading(fileEntry);
                var directories = getDirectoryChain(fileEntry).ToList();

                directoriesLock = _locker.LockWriting(directories);
            }

            if (access.HasFlag(FileAccess.Read))
            {
                if (!_locker.CanRead(fileEntry) && !access.HasFlag(FileAccess.Write))
                    throw new AccessViolationException("Unable to read file: file is locked");

                if (writeLock == null)
                    writeLock = _locker.LockWriting(fileEntry);
            }

            var lockerOperations = new List<Locker.LockerOperation>();

            if (writeLock != null)
                lockerOperations.Add(writeLock);
            if (readLock != null)
                lockerOperations.Add(readLock);
            if (directoriesLock != null)
                lockerOperations.Add(directoriesLock);

            var retv = new VirtualFileStream(this, fileEntry, file, lockerOperations, mode, access);
            return retv;
        }

        public Task<long> Write(FileEntry file, long startPosition, byte[] buffer, int offset, int count)
        {
            var retv = _rawDataManager.Write(file, startPosition, buffer, offset, count);
            return retv.Task;
        }

        public Task<int> Read(FileEntry file, long position, byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            var retv = _rawDataManager.Read(file, position, buffer, offset, count, cancellationToken);
            return retv.Task;
        }

        public string GetFileName(long id)
        {
            if (Cache.FileNames.TryGetName(id, out var retv))
                return retv;

            retv = computeFileName(id);
            Cache.FileNames.Add(id, retv);
            return retv;
        }

        public string GetDirectoryName(long id)
        {
            if (Cache.DirectoryNames.TryGetName(id, out var retv))
                return retv;

            retv = computeDirectoryName(id);
            Cache.DirectoryNames.Add(id, retv);
            return retv;
        }

        public long GetFileLength(VirtualFile file)
        {
            var entry = getFileEntry(file);
            return entry.FileLength;
        }

        public void RenameFile(VirtualFile file, string newName)
        {
            var fileEntry = getFileEntry(file);
            var parentDirectory = getVirtualDirectory(fileEntry.DirectoryId);

            if (!_locker.CanWrite(fileEntry))
                throw new AccessViolationException("Unable to rename file: file is locked");

            var name = VirtualPath.GetFileNameWithoutExtension(newName);
            VirtualPath.CheckRestrictedSymbols(name);

            if (fileEntry.Name.Equals(name, StringComparison.InvariantCultureIgnoreCase))
                return;

            Indexer.TryGetParentDirectory(fileEntry, out var parentDirectoryEntry);
            if (getDirectoryFiles(parentDirectoryEntry, false, makeRegexPattern(name)).Any())
                throw new InvalidOperationException($"Item with the same name exists in the directory {GetDirectoryName(parentDirectoryEntry.Id)}");

            using (_locker.LockWriting(fileEntry))
            using (_locker.LockReading(fileEntry))
            {
                var newExtension = VirtualPath.GetFileExtension(newName);
                fileEntry.ModificationTime = DateTime.Now;
                fileEntry.Name = name;
                fileEntry.Extension = newExtension;
                Cache.FileNames.Remove(fileEntry.Id);
                _rawDataManager.Write(fileEntry);
            }

            _directoryWatcherSource.RaiseUpdated(parentDirectory, file);
        }

        public void RenameDirectory(VirtualDirectory directory, string newName)
        {
            var directoryEntry = getDirectoryEntry(directory);
            var parentDirectory = getVirtualDirectory(directoryEntry.DirectoryId);

            if (directory.Id == 0)
                throw new InvalidOperationException("Root directory cannot be renamed");

            var name = VirtualPath.GetFileName(newName);
            VirtualPath.CheckRestrictedSymbols(name);

            if (directoryEntry.Name.Equals(name, StringComparison.InvariantCultureIgnoreCase))
                return;

            Indexer.TryGetParentDirectory(directoryEntry, out var parentDirectoryEntry);
            if (getNestedDirecories(parentDirectoryEntry, false, makeRegexPattern(name)).Any())
                throw new InvalidOperationException($"Item with the same name exists in the directory {GetDirectoryName(parentDirectoryEntry.Id)}");

            if (!_locker.TryLockWriting(directoryEntry, out var operation))
                throw new AccessViolationException("Unable to rename directory: directory is locked");

            using (operation)
            using (_locker.LockReading(directoryEntry))
            {
                directoryEntry.Name = name;

                Cache.DirectoryNames.Remove(directoryEntry.Id);
                var nestedDirectories = getNestedDirecories(directoryEntry, true, null);
                var directoryFiles = getDirectoryFiles(directoryEntry, true, null);
                var nestedEntries = nestedDirectories.Cast<BaseEntry>()
                    .Union(directoryFiles);

                foreach (var nestedEntry in nestedEntries)
                {
                    switch (nestedEntry)
                    {
                        case FileEntry directoryFile:
                            Cache.FileNames.Remove(directoryFile.Id);
                            break;
                        case DirectoryEntry nestedDirectory:
                            Cache.DirectoryNames.Remove(nestedDirectory.Id);
                            break;
                    }
                }

                _rawDataManager.Write(directoryEntry);
            }


            _directoryWatcherSource.RaiseNameChanged(directory);
            _directoryWatcherSource.RaiseUpdated(parentDirectory, directory);
        }

        public VirtualFile CreateFile(VirtualDirectory directory, string name)
        {
            name = VirtualPath.GetFileName(name);
            VirtualPath.CheckRestrictedSymbols(name);
            var retv = createVirtualEntity<VirtualFile>(directory, name);
            _directoryWatcherSource.RaiseCreated(directory, retv);
            return retv;
        }

        public VirtualDirectory CreateDirectory(VirtualDirectory directory, string name)
        {
            name = VirtualPath.GetFileName(name);
            VirtualPath.CheckRestrictedSymbols(name);
            var retv = createVirtualEntity<VirtualDirectory>(directory, name);
            _directoryWatcherSource.RaiseCreated(directory, retv);
            return retv;
        }

        public void Remove(VirtualFile file)
        {
            var entry = getFileEntry(file);
            var parentDirectory = getVirtualDirectory(entry.DirectoryId);
            if (!_locker.CanWrite(entry))
                throw new AccessViolationException("Unable to remove file: file is locked");

            using (_locker.LockReading(entry))
            using (_locker.LockWriting(entry))
            {
                remove(entry);
            }

            _directoryWatcherSource.RaiseDeleted(parentDirectory, file);
        }

        public void Remove(VirtualDirectory directory)
        {
            var entry = getDirectoryEntry(directory);
            var parentDirectory = getVirtualDirectory(entry.DirectoryId);
            var nestedDirectories = getNestedDirecories(entry, true, null);
            var nestedFiles = getDirectoryFiles(entry, true, null);

            var allEntries = nestedFiles.Cast<BaseEntry>().Concat(nestedDirectories).Concat(new[] { entry }).ToList();

            if (!_locker.TryLockWriting(allEntries, out var operation))
                throw new AccessViolationException("Unable to remove directory: directory is locked");

            using (operation)
            {
                foreach (var e in allEntries)
                {
                    switch (e)
                    {
                        case FileEntry f:
                            remove(f);
                            break;
                        case DirectoryEntry d:
                            remove(d);
                            break;
                    }
                }
            }

            _directoryWatcherSource.RaiseDeleted(parentDirectory, directory);
        }

        public void SaveFile(VirtualFile file)
        {
            var fileEntry = getFileEntry(file);
            var parentDirectory = getVirtualDirectory(fileEntry.DirectoryId);
            _rawDataManager.Write(fileEntry);
            _directoryWatcherSource.RaiseUpdated(parentDirectory, file);
        }

        public VirtualFile GetFile(long id)
        {
            return getVirtualFile(id);
        }

        public VirtualDirectory GetDirectory(long id)
        {
            return getVirtualDirectory(id);
        }

        #region Private
        private VirtualDirectory getVirtualDirectory(long id)
        {

            if (Cache.Directories.TryGet(id, out var retv))
                return retv;

            if (!Indexer.TryGetDirectory(id, out var entry))
                throw new DirectoryNotFoundException();

            retv = new VirtualDirectory(this, entry);
            Cache.Directories.Add(id, retv);
            return retv;
        }

        private T createVirtualEntity<T>(VirtualDirectory parentDirectory, string name)
            where T : BaseVirtualEntity
        {//todo: refactoring required
            Func<DirectoryEntry, Regex, IEnumerable<BaseEntry>> getInnerEntities;
            Func<long> getNewEntryId;
            Func<BaseEntry> createEntry;
            Func<long, T> getVirtualEntry;

            var parentDirectoryEntry = getDirectoryEntry(parentDirectory);
            var entryName = VirtualPath.GetFileNameWithoutExtension(name);

            if (typeof(T) == typeof(VirtualFile))
            {
                getNewEntryId = () => Indexer.GetNewFileId();
                getInnerEntities = (directoryEntry, searchString) => getDirectoryFiles(directoryEntry, false, searchString);
                createEntry = () =>
                {
                    var extension = VirtualPath.GetFileExtension(name);
                    return new FileEntry
                    {
                        Id = getNewEntryId(),
                        DirectoryId = parentDirectory.Id,
                        Name = entryName,
                        Extension = extension
                    };
                };
                getVirtualEntry = id => getVirtualFile(id) as T;
            }
            else if (typeof(T) == typeof(VirtualDirectory))
            {
                getNewEntryId = () => Indexer.GetNewDirectoryId();
                getInnerEntities = (directoryEntry, searchString) => getNestedDirecories(directoryEntry, false, searchString);
                createEntry = () => new DirectoryEntry
                {
                    Id = getNewEntryId(),
                    DirectoryId = parentDirectory.Id,
                    Name = entryName
                };
                getVirtualEntry = id => getVirtualDirectory(id) as T;
            }
            else
                throw new InvalidOperationException();

            var innerEntities = getInnerEntities(parentDirectoryEntry, makeRegexPattern(name));
            if (innerEntities.Any())
                throw new InvalidOperationException($"Item with the same name exists in the directory {GetDirectoryName(parentDirectoryEntry.Id)}");

            var newEntry = createEntry();
            _rawDataManager.Write(newEntry);
            Indexer.AddEntry(newEntry);
            var retv = getVirtualEntry(newEntry.Id);
            return retv;
        }

        private void remove(FileEntry entry)
        {
            Indexer.RemoveEntry(entry);
            Cache.Files.Remove(entry.Id);
            Cache.FileNames.Remove(entry.Id);
            _rawDataManager.Remove(entry);
        }

        private void remove(DirectoryEntry entry)
        {
            Indexer.RemoveEntry(entry);
            Cache.Directories.Remove(entry.Id);
            Cache.DirectoryNames.Remove(entry.Id);
            _rawDataManager.Remove(entry);
        }

        private IEnumerable<DirectoryEntry> getDirectoryChain(BaseEntry entry)
        {
            var tmpEntry = entry;
            while (tmpEntry.DirectoryId > -1)
            {
                if (!Indexer.TryGetParentDirectory(tmpEntry, out var retv))
                    continue;

                tmpEntry = retv;
                yield return retv;
            }
        }

        /// <summary>
        /// computes directory name and put it to cache. Names of all directories in path will be cached
        /// </summary>
        /// <param name="id">directory id</param>
        /// <returns>directory name</returns>
        private string computeDirectoryName(long id)
        {
            var directoryId = id;
            var directoryPaths = new Dictionary<long, StringBuilder>();
            while (directoryId > -1)
            {
                if (!Indexer.TryGetDirectory(directoryId, out var directoryEntry))
                    throw new DirectoryNotFoundException();

                if (Cache.DirectoryNames.TryGetName(directoryId, out var directoryName))
                {
                    foreach (var directoryPathPair in directoryPaths)
                        if (directoryId != 0)
                            directoryPathPair.Value.Insert(0, $"{directoryName}{VirtualPath.Separator}");
                        else
                            directoryPathPair.Value.Insert(0, $"{VirtualPath.Separator}");

                    if (!directoryPaths.ContainsKey(directoryId))
                        directoryPaths[directoryId] = new StringBuilder(directoryName);

                    break;
                }

                directoryName = directoryEntry.Name;

                foreach (var directoryPathPair in directoryPaths)
                    directoryPathPair.Value.Insert(0, $"{directoryName}{VirtualPath.Separator}");

                directoryPaths[directoryId] = new StringBuilder(directoryName);
                directoryId = directoryEntry.DirectoryId;
            }

            foreach (var directoryPathPair in directoryPaths)
                if (!Cache.DirectoryNames.TryGetName(directoryPathPair.Key, out var val) && id != directoryPathPair.Key)
                    Cache.DirectoryNames.Add(directoryPathPair.Key, directoryPathPair.Value.ToString());

            return directoryPaths[id].ToString();
        }

        private string computeFileName(long id)
        {
            if (!Indexer.TryGetFile(id, out var fileEntry))
                throw new FileNotFoundException();

            var parentDirectoryName = GetDirectoryName(fileEntry.DirectoryId);
            var retv = $"{parentDirectoryName}{VirtualPath.Separator}{fileEntry.Name}.{fileEntry.Extension}";
            return retv;
        }

        private DirectoryEntry getDirectoryEntry(VirtualDirectory directory)
        {
            if (!Indexer.TryGetDirectory(directory.Id, out var retv))
                throw new DirectoryNotFoundException();

            return retv;
        }

        private FileEntry getFileEntry(VirtualFile file)
        {
            if (!Indexer.TryGetFile(file.Id, out var retv))
                throw new FileNotFoundException();

            return retv;
        }

        private IEnumerable<VirtualFile> getDirectoryVirtualFiles(DirectoryEntry directory, bool recursive, string searchString)
        {
            var searchPattern = makeRegexPattern(searchString);
            return getDirectoryFiles(directory, recursive, searchPattern).Select(x => getVirtualFile(x.Id));
        }

        private IEnumerable<VirtualDirectory> getNestedVirtualDirectories(DirectoryEntry directory, bool recursive, string searchString)
        {
            var searchPattern = makeRegexPattern(searchString);
            return getNestedDirecories(directory, recursive, searchPattern).Select(x => getVirtualDirectory(x.Id));
        }

        private IEnumerable<FileEntry> getDirectoryFiles(DirectoryEntry directoryEntry, bool recursive, Regex searchPattern)
        {
            var dirsQueue = new Queue<DirectoryEntry>(new[] { directoryEntry });
            while (dirsQueue.Any())
            {
                var d = dirsQueue.Dequeue();
                if (recursive)
                {
                    var nestedDirs = getNestedDirecories(d, true, null);
                    foreach (var nestedDir in nestedDirs)
                        if (!dirsQueue.Contains(nestedDir))
                            dirsQueue.Enqueue(nestedDir);
                }

                if (!Indexer.TryGetDirectoryFiles(d.Id, out var files))
                    continue;

                foreach (var fileEntry in files.ToList())
                    if (checkSearchPatternMatch($"{fileEntry.Name}.{fileEntry.Extension}", searchPattern))
                        yield return fileEntry;
            }
        }

        private IEnumerable<DirectoryEntry> getNestedDirecories(DirectoryEntry directoryEntry, bool recursive, Regex searchPattern)
        {
            var dirsQueue = new Queue<DirectoryEntry>(new[] { directoryEntry });
            while (dirsQueue.Any())
            {
                var processingDirectoryEntry = dirsQueue.Dequeue();

                if (!Indexer.TryGetNestedDirectories(processingDirectoryEntry.Id, out var nestedDirs))
                    continue;

                foreach (var nestedDir in nestedDirs.ToList())
                {
                    if (recursive && !dirsQueue.Contains(nestedDir))
                        dirsQueue.Enqueue(nestedDir);

                    if (checkSearchPatternMatch(nestedDir.Name, searchPattern))
                        yield return nestedDir;
                }
            }
        }

        private bool checkSearchPatternMatch(string fileName, Regex searchPattern)
        {
            return searchPattern?.IsMatch(fileName) ?? true;
        }

        private Regex makeRegexPattern(string searchString)
        {
            if (string.IsNullOrWhiteSpace(searchString))
                return null;

            var patternBuilder = new StringBuilder();

            foreach (var c in searchString)
            {
                if (_regexReplacementsDict.ContainsKey(c))
                    patternBuilder.Append(_regexReplacementsDict[c]);
                else if (_charactersToEscape.IndexOf(c) > -1)
                    patternBuilder.Append($"\\{c}");
                else
                    patternBuilder.Append(c);
            }

            var retv = new Regex(patternBuilder.ToString(), RegexOptions.IgnoreCase|RegexOptions.Singleline);
            return retv;
        }

        private VirtualFile getVirtualFile(long id)
        {
            if (Cache.Files.TryGet(id, out var retv))
                return retv;

            if (!Indexer.TryGetFile(id, out var entry))
                throw new FileNotFoundException();

            retv = new VirtualFile(this, entry);
            Cache.Files.Add(id, retv);
            return retv;
        }

        #endregion

        public void Dispose()
        {
            _directoryWatcherSource?.Dispose();
            _rawDataManager?.Dispose();
        }
    }
}