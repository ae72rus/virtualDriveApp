using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using VirtualDrive.Internal.Drive.Operations;
using VirtualDrive.Internal.RawData.Readers;
using VirtualDrive.Internal.RawData.Threading;
using VirtualDrive.Internal.RawData.Writers;

namespace VirtualDrive.Internal.RawData
{
    internal class RawDataManager : IDisposable
    {
        public static RawDataManager Create(string storageFile, VirtualDriveParameters parameters)
        {
            var drive = new Drive.VirtualDrive(storageFile);
            return drive.IsEmpty
                ? InitializeNewDrive(drive, parameters)
                : Initialize(drive);
        }

        private static RawDataManager InitializeNewDrive(Drive.VirtualDrive drive, VirtualDriveParameters parameters)
        {
            var synchronizer = new DriveAccessSynchronizer(drive);
            var retv = new RawDataManager(synchronizer);
            retv.writeVirtualDriveParameters(parameters);

            retv._sectorInfoWriter = new SectorInfoRawWriter(parameters, synchronizer);
            retv.inititalizeSectors(parameters);

            retv.writeAvailableContentBlocks();

            synchronizer.DriveAccess.Wait();//wait for writing before initializing

            try
            {
                retv.init();
            }
            catch (Exception e)
            {
                synchronizer.Dispose();
                throw;
            }

            var rootEntry = new DirectoryEntry
            {
                Id = 0,
                DirectoryId = -1,
                Name = string.Empty
            };

            retv.Write(rootEntry).Task.Wait(); //make sure that root is created

            return retv;
        }

        private static RawDataManager Initialize(Drive.VirtualDrive drive)
        {
            var synchronizer = new DriveAccessSynchronizer(drive);
            var retv = new RawDataManager(synchronizer);

            try
            {
                retv.init();
            }
            catch (Exception e)
            {
                synchronizer.Dispose();
                throw;
            }

            return retv;
        }

        private volatile object _entriesTableLock = new object();
        private volatile object _contentLock = new object();
        private volatile object _sectorsLock = new object();
        private volatile object _virtualDriveParametersLock = new object();
        private readonly Dictionary<SectorInfo, long> _sectorInfosStartPositions = new Dictionary<SectorInfo, long>();
        private int _sectorNextId = 0;
        private SectorInfo _currentFileEntriesSector;
        private SectorInfo _currentContentSector;
        private bool _isDisposing;


        private readonly DriveAccessSynchronizer _synchronizer;
        private readonly VirtualDriveParametersRawWriter _driveParametersWriter;
        private VirtualDriveParameters _parameters;
        private SectorInfoRawWriter _sectorInfoWriter;
        private EntryRawWriter _entryWriter;
        private ContentRawWriter _contentWriter;

        private RawDataManager(DriveAccessSynchronizer synchronizer)
        {
            _synchronizer = synchronizer;
            _driveParametersWriter = new VirtualDriveParametersRawWriter(_synchronizer);
        }

        private void init()
        {
            try
            {
                _parameters = readParameters();
                _sectorInfoWriter = new SectorInfoRawWriter(_parameters, _synchronizer);
                var sectors = readSectors();
                foreach (var sectorInfo in sectors)
                {
                    switch (sectorInfo.Mark)
                    {
                        case ServiceMarks.EntriesSector:
                            _currentFileEntriesSector = sectorInfo;
                            break;
                        case ServiceMarks.ContentSector:
                            _currentContentSector = sectorInfo;
                            break;
                    }
                }

                var entriesStartPosition = _currentFileEntriesSector.StartPosition;
                _entryWriter = new EntryRawWriter(_parameters, _synchronizer, entriesStartPosition);

                var contentStartPosition = _currentContentSector.StartPosition;
                _contentWriter = new ContentRawWriter(_parameters, _synchronizer, contentStartPosition);
                _contentWriter.SetLength(_currentContentSector.Length);

                var availableContentBlocks = checkIfDriveIsFinalized()
                    ? readAvailableContentBlocks()
                    : restoreAvailableContentBlocks();

                foreach (var availableContentBlock in availableContentBlocks)
                    _contentWriter.AddAvailableBlock(availableContentBlock);
            }
            catch (Exception e)
            {
                throw new IOException("Virtual drive is corrupted", e);
            }
        }

        public WriteOperation Write(IByteSource dataSource)
        {
            if (_isDisposing)
                return WriteOperation.Empty;

            switch (dataSource)
            {
                case BaseEntry ds:
                    return writeEntry(ds);
                case SectorInfo ds:
                    return writeSectorInfo(ds);
                case VirtualDriveParameters ds:
                    return writeVirtualDriveParameters(ds);
                default:
                    throw new InvalidOperationException("Unable to write object");
            }
        }

        public WriteOperation Write(FileEntry fileEntry, long filePosition, byte[] buffer, int offset, int count)
        {
            if (_isDisposing)
                return WriteOperation.Empty;


            if (fileEntry.FileLength - (filePosition + count) < 0)
                SetFileLength(fileEntry, filePosition + count).Task.Wait();

            var bytesToWrite = count;
            var currentFilePosition = 0L;
            var blocksToWrite = new List<DriveBlock>();

            foreach (var fileEntryBlock in fileEntry.Blocks)
            {
                if (filePosition > fileEntryBlock.Length + currentFilePosition)
                {
                    currentFilePosition += fileEntryBlock.Length;
                    continue;
                }

                var shift = filePosition - currentFilePosition;
                var blockRestLength = (int)(fileEntryBlock.Length - shift);
                blockRestLength = blockRestLength > bytesToWrite ? bytesToWrite : blockRestLength;

                var block = new DriveBlock
                {
                    Position = fileEntryBlock.Position + shift,
                    Length = blockRestLength
                };

                bytesToWrite -= blockRestLength;
                blocksToWrite.Add(block);
                currentFilePosition += blockRestLength;
                if (bytesToWrite <= 0)
                    break;
            }

            lock (_contentLock)
            {
                return _contentWriter.Write(blocksToWrite, buffer, offset);
            }
        }

        public IEnumerable<BaseEntry> ReadEntries()
        {
            if (_isDisposing)
                yield break;

            lock (_entriesTableLock)
            {
                var buffer = new byte[4096];
                var breakReading = false;
                foreach (var sectorInfo in _sectorInfosStartPositions.Keys)
                {
                    if (sectorInfo.Mark != ServiceMarks.EntriesSector)
                        continue;

                    var reader = new EntriesTableRawReader(_synchronizer, _parameters, sectorInfo.StartPosition);

                    reader.Read(buffer, 0, ByteHelper.GetLength<int>()).Task.Wait();
                    var blockLength = BitConverter.ToInt32(buffer, 0);
                    while (blockLength != (int)ServiceBytes.End && blockLength > 0)
                    {
                        var readBlockBytesCount = 0L;
                        var block = new byte[blockLength];
                        var blockBodyPosition = reader.CurrentPosition;

                        while (readBlockBytesCount < blockLength)//in case if block longer than buffer
                        {
                            var bytesToRead = buffer.Length > blockLength ? blockLength : buffer.Length;
                            if (!reader.CheckCanRead(bytesToRead))
                            {
                                breakReading = true;
                                break;
                            }

                            var readBytesCount = reader.Read(buffer, 0, bytesToRead).Task.Result;
                            for (var i = readBlockBytesCount; i < readBlockBytesCount + readBytesCount; i++)
                                block[i] = buffer[i - readBlockBytesCount];

                            readBlockBytesCount += readBytesCount;
                        }

                        if (breakReading)
                            break;

                        var entryReader = EntryReaderFactory.Create(block.ToArray(), blockBodyPosition - ByteHelper.GetLength<int>());//it requires position of length bytes
                        if (entryReader != null)
                        {
                            var entry = entryReader.GetEntry<BaseEntry>();
                            yield return entry;
                        }
                        else _entryWriter.AddAvailableBlock(new DriveBlock
                        {
                            Length = blockLength,
                            Position = blockBodyPosition - ByteHelper.GetLength<int>() //position where length bytes are located
                        });

                        _entryWriter.SetCurrentPosition(reader.CurrentPosition);//set position just after the last entry

                        if (!reader.CheckCanRead(ByteHelper.GetLength<int>()))
                            break;

                        reader.Read(buffer, 0, ByteHelper.GetLength<int>()).Task.Wait();
                        blockLength = BitConverter.ToInt32(buffer, 0);
                    }
                }
            }
        }

        public ReadOperation Read(FileEntry fileEntry, long filePosition, byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            if (_isDisposing)
                return ReadOperation.Empty;

            var blocks = new List<DriveBlock>();
            var currentFilePosition = 0L;
            var allBlocksToReadLength = 0;
            var restReadCount = count;
            foreach (var fileEntryBlock in fileEntry.Blocks)
            {
                if (cancellationToken.IsCancellationRequested)
                    break;

                if (filePosition >= currentFilePosition + fileEntryBlock.Length)
                {
                    currentFilePosition += fileEntryBlock.Length;
                    continue;
                }

                var shift = (int)(filePosition - currentFilePosition);
                var restFileEntryBlockLength = fileEntryBlock.Length - shift;
                restFileEntryBlockLength = restFileEntryBlockLength > restReadCount ? restReadCount : restFileEntryBlockLength;
                var block = new DriveBlock
                {
                    Length = restFileEntryBlockLength,
                    Position = fileEntryBlock.Position + shift
                };

                currentFilePosition += restFileEntryBlockLength;
                blocks.Add(block);
                allBlocksToReadLength += restFileEntryBlockLength;

                if (allBlocksToReadLength == count)
                    break;

                restReadCount -= restFileEntryBlockLength;
            }

            var reader = new ContentRawReader(_synchronizer, _currentContentSector.StartPosition, -1);
            if (!reader.CheckCanRead(count))
                throw new InvalidOperationException("Unable to perform read operation. Trying to read protected memory");

            return reader.Read(blocks, buffer, offset, cancellationToken);
        }

        public WriteOperation SetFileLength(FileEntry fileEntry, long length)
        {
            if (_isDisposing)
                return WriteOperation.Empty;


            if (fileEntry.FileLength == length)
                return WriteOperation.Empty;

            eraseEntry(fileEntry);

            if (length == 0)
            {
                foreach (var block in fileEntry.Blocks)
                    _contentWriter.AddAvailableBlock(block);

                fileEntry.Blocks.Clear();

                _entryWriter.Write(fileEntry.GetBytes()).Task.Wait();
                return shrinkDrive();
            }

            if (fileEntry.FileLength > length)
            {
                var currentFileLength = 0L;
                foreach (var block in fileEntry.Blocks.ToList())
                {
                    if (currentFileLength + block.Length <= length)
                    {
                        currentFileLength += block.Length;
                        continue;
                    }

                    if (currentFileLength < length)
                    {
                        var lengthDiff = (int)(length - currentFileLength);
                        block.Length -= lengthDiff;
                        currentFileLength += lengthDiff;
                        continue;
                    }

                    fileEntry.Blocks.Remove(block);
                }
            }

            var restLength = length - fileEntry.FileLength;
            while (restLength > 0)
            {
                var blockLength = restLength > int.MaxValue ? int.MaxValue : (int)restLength;
                restLength -= blockLength;
                var blocks = _contentWriter.AllocateSpace(blockLength);
                fileEntry.Blocks.AddRange(blocks);
            }

            lock (_sectorsLock)
                _sectorInfoWriter.SetContentSectorLength(_sectorInfosStartPositions[_currentContentSector], _contentWriter.Length);

            return writeEntry(fileEntry);
        }

        public WriteOperation Remove(BaseEntry entry)
        {
            switch (entry)
            {
                case FileEntry file:
                    return removeFile(file);
                case DirectoryEntry directory:
                    return removeDirectory(directory);
                default:
                    throw new InvalidOperationException("Not supported entry");
            }
        }

        private WriteOperation writeEntry(BaseEntry entry)
        {
            lock (_entriesTableLock)
            {
                var bytes = entry.GetBytes();
                if (!_entryWriter.CheckCanWrite(bytes.Length))
                    handleEntryTableSector();

                var operation = _entryWriter.Write(bytes);
                entry.Position = operation.Position;//position of length bytes
                return operation;
            }
        }

        private WriteOperation eraseEntry(BaseEntry entry)
        {
            lock (_entriesTableLock)
            {
                var position = entry.Position;//position of length bytes
                var mark = (byte)ServiceMarks.Proceed;
                _entryWriter.AddAvailableBlock(new DriveBlock
                {
                    Position = position,//position where length bytes located
                    Length = entry.Length
                });

                var operation = _entryWriter.WriteTo(position + ByteHelper.GetLength<int>(), new[] { mark });// add 4 bytes to the position to rewrite block mark
                return operation;
            }
        }

        /// <summary>
        /// Finalize current content sector, create new sector for file table and create new content sector
        /// </summary>
        private void handleEntryTableSector()
        {
            lock (_sectorsLock)
                lock (_contentLock)
                {
                    var currentContentSectorInfoPosition = _sectorInfosStartPositions[_currentContentSector];
                    _sectorInfoWriter.SetContentSectorLength(currentContentSectorInfoPosition, _contentWriter.Length);
                    var fileTableSector = new SectorInfo
                    {
                        StartPosition = _contentWriter.InitialPosition + _contentWriter.Length,
                        Length = _parameters.EntriesTableSectorLength,
                        Mark = ServiceMarks.EntriesSector,
                        Id = _sectorNextId++
                    };

                    var contentSector = new SectorInfo
                    {
                        StartPosition = fileTableSector.StartPosition + fileTableSector.Length,
                        Length = 0,
                        Mark = ServiceMarks.ContentSector,
                        Id = _sectorNextId++,
                    };

                    var fileTableSectorWriteOperation = writeSectorInfo(fileTableSector);
                    var contentSectorWriteOperation = writeSectorInfo(contentSector);

                    _sectorInfosStartPositions[_currentFileEntriesSector] = fileTableSectorWriteOperation.Position;
                    _sectorInfosStartPositions[_currentContentSector] = contentSectorWriteOperation.Position;

                    _currentFileEntriesSector = fileTableSector;
                    _currentContentSector = contentSector;

                    fileTableSectorWriteOperation.Task.Wait();
                    contentSectorWriteOperation.Task.Wait();
                    finalizeSectorRegion().Task.Wait();

                    _entryWriter.SetInitialPosition(_currentFileEntriesSector.StartPosition);
                    _contentWriter.SetInitialPosition(_currentContentSector.StartPosition);
                    _contentWriter.SetLength(contentSector.Length);
                }
        }

        private void inititalizeSectors(VirtualDriveParameters parameters)
        {
            lock (_sectorsLock)
            {
                var fileTableSector = new SectorInfo
                {
                    StartPosition = FixedPositions.SectorsInformation + parameters.SectorsInformationRegionLength,
                    Length = parameters.EntriesTableSectorLength,
                    Mark = ServiceMarks.EntriesSector,
                    Id = 0
                };

                var contentSector = new SectorInfo
                {
                    StartPosition = fileTableSector.StartPosition + fileTableSector.Length,
                    Length = 0,
                    Mark = ServiceMarks.ContentSector,
                    Id = 1
                };

                var fileTableSectorWriteOperation = writeSectorInfo(fileTableSector);
                var contentSectorWriteOperation = writeSectorInfo(contentSector);

                fileTableSectorWriteOperation.Task.Wait();
                contentSectorWriteOperation.Task.Wait();
                finalizeSectorRegion().Task.Wait();
            }
        }

        private WriteOperation writeSectorInfo(SectorInfo sectorInfo)
        {
            var bytes = sectorInfo.GetBytes();
            lock (_sectorsLock)
            {
                if (!_sectorInfoWriter.CheckCanWrite(bytes.Length))
                    throw new InvalidOperationException("Unable to write sector info");

                if (sectorInfo.Mark != ServiceMarks.EntriesSector)
                    return _sectorInfoWriter.Write(bytes);

                var position = sectorInfo.StartPosition + sectorInfo.Length - ByteHelper.GetLength<int>();
                var op = new FileTableOperation(drive => drive.Write(position, ServiceBytes.End), position);
                _synchronizer.EnqueueOperation(op);

                return _sectorInfoWriter.Write(bytes);
            }
        }

        private WriteOperation finalizeSectorRegion()
        {
            var bytes = BitConverter.GetBytes((int)ServiceBytes.End);
            lock (_sectorsLock)
            {
                if (!_sectorInfoWriter.CheckCanWrite(bytes.Length))
                    throw new InvalidOperationException("Unable to write sector info");

                return _sectorInfoWriter.Write(bytes);
            }
        }

        private WriteOperation writeVirtualDriveParameters(VirtualDriveParameters parameters)
        {
            lock (_virtualDriveParametersLock)
            {
                var bytes = new InternalVirtualDriveParameters(parameters).GetBytes();
                return _driveParametersWriter.Write(bytes);
            }
        }

        private WriteOperation removeFile(FileEntry entry)
        {
            SetFileLength(entry, 0);
            return eraseEntry(entry);
        }

        private WriteOperation removeDirectory(DirectoryEntry entry)
        {
            return eraseEntry(entry);
        }

        private VirtualDriveParameters readParameters()
        {
            lock (_virtualDriveParametersLock)
            {
                var buffer = new byte[8];
                var rawReader = new VirtualDriveParametersRawReader(_synchronizer);
                rawReader.Read(buffer, 0, buffer.Length).Task.Wait();

                var parameters = InternalVirtualDriveParameters.Read(buffer);
                return parameters;
            }
        }

        private IEnumerable<SectorInfo> readSectors()
        {
            lock (_sectorsLock)
            {
                var buffer = new byte[4096];
                var reader = new SectorInfoRawReader(_synchronizer, _parameters);
                var lengthBytesLength = ByteHelper.GetLength<int>();
                while (reader.CheckCanRead(lengthBytesLength))
                {
                    reader.Read(buffer, 0, lengthBytesLength).Task.Wait();

                    var sectorInfoEntryLength = BitConverter.ToInt32(buffer, 0);
                    if (sectorInfoEntryLength == (int)ServiceBytes.End)
                        yield break;

                    buffer = new byte[sectorInfoEntryLength];
                    var position = reader.CurrentPosition;
                    reader.Read(buffer, 0, sectorInfoEntryLength).Task.Wait();

                    var sector = SectorInfo.Read(buffer);
                    _sectorInfosStartPositions[sector] = position;

                    _sectorInfoWriter.SetCurrentPostion(reader.CurrentPosition);

                    if (_sectorNextId <= sector.Id)
                        _sectorNextId = sector.Id + 1;

                    yield return sector;

                    if (sector.Length == 0)
                        yield break;
                }
            }
        }

        /// <summary>
        /// Removes empty blocks at the end of the drive
        /// </summary>
        /// <returns>BaseDriveOperation</returns>
        private WriteOperation shrinkDrive()
        {
            lock (_contentLock)
            {
                var removedBlocksLength = 0L;
                DriveBlock getLastBlock()//just showing off that I know about local functions, but I don't like them
                {
                    var driveLength = _synchronizer.GetDriveLength();
                    return _contentWriter.AvailableBlocks.FirstOrDefault(x =>
                        x.Position + x.Length + removedBlocksLength - driveLength >= 0);
                }

                var endPositionBlock = getLastBlock();

                while (endPositionBlock != null)
                {
                    removedBlocksLength += endPositionBlock.Length;
                    _contentWriter.RemoveAvailableBlock(endPositionBlock);
                    endPositionBlock = getLastBlock();
                }

                if (removedBlocksLength == 0)
                    return WriteOperation.Empty;
                _contentWriter.SetLength(_contentWriter.Length - removedBlocksLength);
                var sectorOperation = _sectorInfoWriter.SetContentSectorLength(_sectorInfosStartPositions[_currentContentSector], _contentWriter.Length);
                _synchronizer.EnqueueOperation(sectorOperation);
                sectorOperation.Task.Wait();

                var operation = new FileTableOperation(drive =>
                {
                    drive.Position = 0;
                    drive.SetLength(_currentContentSector.StartPosition + _contentWriter.Length);
                    return removedBlocksLength;
                }, 0);

                _synchronizer.EnqueueOperation(operation);
                return sectorOperation;
            }
        }

        private IEnumerable<DriveBlock> readAvailableContentBlocks()
        {
            lock (_entriesTableLock)
            {
                var operation = _synchronizer.EnqueueOperation(drive =>
                {
                    if (drive.Length < ByteHelper.GetLength<int>())
                        return new byte[0];

                    var buffer = new byte[ByteHelper.GetLength<int>()];
                    drive.Position = drive.Length - ByteHelper.GetLength<int>() * 2;// read last 4 but 4 bytes

                    drive.Read(buffer, 0, buffer.Length);

                    var infoLength = BitConverter.ToInt32(buffer, 0);
                    if (infoLength <= 0)
                        return new byte[0];

                    buffer = new byte[infoLength];

                    drive.Position = drive.Length - ByteHelper.GetLength<int>() * 2 - infoLength;
                    drive.Read(buffer, 0, infoLength);

                    drive.SetLength(drive.Length - infoLength - ByteHelper.GetLength<int>() * 2); //get rid of available blocks info
                    return buffer;
                }, OperationType.FileTable);

                _synchronizer.EnqueueOperation(operation);
                var readBytes = operation.Task.Result;

                var processedBytes = 0;
                while (processedBytes < readBytes.Length)
                {
                    var retv = AvailableDriveBlock.Read(readBytes.Skip(processedBytes).Take(AvailableDriveBlock.BytesBlockLength)
                        .ToArray());

                    processedBytes += AvailableDriveBlock.BytesBlockLength;

                    yield return retv;
                }
            }
        }

        /// <summary>
        /// Potentially expensive operation that restores available blocks after reading of filetable
        /// </summary>
        /// <returns></returns>
        private IEnumerable<DriveBlock> restoreAvailableContentBlocks()
        {
            lock (_entriesTableLock)
            {
                DriveBlock prevBlock = null;
                var processedBlocks = 0;
                foreach (var block in ReadEntries().OfType<FileEntry>().SelectMany(x => x.Blocks).OrderBy(x => x.Position))
                {
                    processedBlocks++;
                    if (prevBlock == null || block.Position == prevBlock.Position + prevBlock.Length)
                    {
                        prevBlock = block;
                        continue;
                    }

                    var position = prevBlock.Position + prevBlock.Length;
                    var fullLength = block.Position - position;
                    do
                    {
                        var length = fullLength > int.MaxValue ? int.MaxValue : (int)fullLength;
                        var retv = new DriveBlock
                        {
                            Position = position,
                            Length = length
                        };

                        yield return retv;

                        fullLength -= length;
                        position += length;

                    } while (fullLength > 0);

                    prevBlock = block;
                }

                if (processedBlocks != 0 || _currentContentSector == null)
                    yield break;

                //if files blocks not found set all content sector as available block
                var restLength = _currentContentSector.Length;
                while (restLength > 0)
                {
                    var blockLength = (int)(_currentContentSector.Length > int.MaxValue
                        ? int.MaxValue
                        : _currentContentSector.Length);

                    restLength -= blockLength;
                    yield return new DriveBlock
                    {
                        Position = _currentContentSector.StartPosition,
                        Length = blockLength
                    };
                }
            }
        }

        private bool checkIfDriveIsFinalized()
        {
            return _synchronizer.EnqueueOperation(drive =>
             {
                 if (drive.Length < ByteHelper.GetLength<int>())
                     return false;

                 drive.Position = drive.Length - ByteHelper.GetLength<int>();
                 var buffer = new byte[ByteHelper.GetLength<int>()];

                 drive.Read(buffer, 0, buffer.Length); // read last 4 bytes

                 var readInt = BitConverter.ToInt32(buffer, 0);
                 return readInt == (int)ServiceBytes.DriveEnd;

             }, OperationType.FileTable).Task.Result;
        }

        private void writeAvailableContentBlocks()
        {
            lock (_entriesTableLock)
            {
                var endDriveBytes = BitConverter.GetBytes((int)ServiceBytes.DriveEnd);
                var blockBytes = (_contentWriter?
                    .AvailableBlocks ?? new DriveBlock[0])
                    .SelectMany(x => new AvailableDriveBlock(x).GetBytes())
                    .ToArray();

                var blockLengthBytes = BitConverter.GetBytes(blockBytes.Length);

                var finalizeBytes = blockBytes
                                        .Concat(blockLengthBytes)
                                        .Concat(endDriveBytes)
                                        .ToArray();

                var operation = new FileTableOperation(drive =>
                {
                    var writePosition = drive.Length;
                    drive.SetLength(drive.Length + finalizeBytes.Length);
                    drive.Position = writePosition;
                    return drive.Write(finalizeBytes);
                }, -1);

                _synchronizer.EnqueueOperation(operation);
                operation.Task.Wait();
            }
        }

        public void Dispose()
        {
            if (_isDisposing)
                return;

            _isDisposing = true;

            writeAvailableContentBlocks();
            _synchronizer.Dispose();
        }
    }
}