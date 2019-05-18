using System;
using System.Collections.Generic;
using System.Linq;
using VirtualDrive.Internal.RawData.Threading;

namespace VirtualDrive.Internal.RawData.Writers
{
    internal abstract class OptimizedRawWriter : BaseRawWriter
    {
        private volatile object _blocksLockObject = new object();
        private readonly Dictionary<int, Queue<DriveBlock>> _availableBlocks = new Dictionary<int, Queue<DriveBlock>>();

        public IEnumerable<DriveBlock> AvailableBlocks => _availableBlocks.Values.SelectMany(x => x);

        protected OptimizedRawWriter(VirtualDriveParameters virtualDriveParameters, DriveAccessSynchronizer synchronizer, long initialPosition)
            : base(virtualDriveParameters, synchronizer, initialPosition)
        {
        }

        public void AddAvailableBlock(DriveBlock block)
        {
            if (block == null)
                throw new ArgumentNullException(nameof(block));

            if (!HandleAddedAvailableBlock(block))
                return;

            lock (_blocksLockObject)
            {
                if (!_availableBlocks.ContainsKey(block.Length))
                    _availableBlocks[block.Length] = new Queue<DriveBlock>();

                _availableBlocks[block.Length].Enqueue(block);
            }

        }

        protected virtual bool HandleAddedAvailableBlock(DriveBlock block)
        {
            return true;
        }

        protected override OperationHint GetOperationHint()
        {
            return new OptimizedOperation();
        }

        protected override long GetWritePositionLocked(OperationHint hint, int length)
        {
            var contentHint = hint as OptimizedOperation ?? throw new ArgumentException(nameof(hint));
            var retv = -1L;
            lock (_blocksLockObject)
            {
                if (!TryGetAvailableBlock(length, out var block))
                    retv = CurrentPosition;
                else
                {
                    contentHint.IsExistingBlockUsed = true;
                    retv = block.Position;

                    if (block.Length <= length)
                        return retv;

                    var generatedBlock = new DriveBlock
                    {
                        Length = block.Length - length,
                        Position = block.Position + length
                    };

                    AddAvailableBlock(generatedBlock);
                    contentHint.GeneratedAvailableBlock = generatedBlock;
                }
            }

            return retv;
        }

        protected virtual bool TryGetAvailableBlock(int length, out DriveBlock block)
        {
            lock (_blocksLockObject)
            {
                block = null;
                if (!_availableBlocks.TryGetValue(length, out var queue) ||
                    _availableBlocks.Keys.All(x => x < length)) 
                    return false;

                if (queue == null)
                {
                    var key = _availableBlocks.Keys.FirstOrDefault(x => x > length);
                    queue = _availableBlocks[key];
                }

                block = queue.Dequeue();

                if (block.Length > length)
                {
                    var generatedBlock = new DriveBlock
                    {
                        Length = block.Length - length,
                        Position = block.Position + length
                    };

                    AddAvailableBlock(generatedBlock);
                }

                if (!queue.Any())
                    _availableBlocks.Remove(length);

                return true;
            }
        }

        public void RemoveAvailableBlock(DriveBlock block)
        {
            lock (_blocksLockObject)
            {
                var queue = _availableBlocks[block.Length];
                var list = queue.ToList();
                list.Remove(block);
                if (!list.Any())
                {
                    _availableBlocks.Remove(block.Length);
                    return;
                }

                _availableBlocks[block.Length] = new Queue<DriveBlock>(list);
            }
        }

        protected override void SetCurrentPositionLocked(OperationHint hint, int writtenBytesCount)
        {
            var contentHint = hint as OptimizedOperation ?? throw new ArgumentException(nameof(hint));
            if (contentHint.IsExistingBlockUsed)
                return;

            CurrentPosition += writtenBytesCount;
        }
    }
}