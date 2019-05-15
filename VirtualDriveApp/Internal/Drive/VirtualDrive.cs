using System;
using System.IO;
using VirtualDrive.Internal.RawData;

namespace VirtualDrive.Internal.Drive
{
    //Virtual drive structure:
    //_______________________________
    //| settings section (8 bytes)  | 
    //|-----------------------------|
    //| sectors information section |
    //|                             |
    //|-----------------------------|
    //| entries table sector        |
    //|                             |
    //|                             |
    //|-----------------------------|
    //| files content sector        |
    //|                             |
    //|                             |
    //|                             |
    //|                             |
    //|-----------------------------|
    //| entries table sector        |
    //|                             |
    //|                             |
    //|-----------------------------|
    //| files content sector        |
    //|                             |
    //|~~~~~~~~~~~~~~~~~~~~~~~~~~~~~| - after finalization
    //| available content blocks    |
    //| END DRIVE (4 bytes)         |
    //|_____________________________|

    internal class VirtualDrive : Stream
    {
        private readonly FileStream _fileStream;

        public bool IsEmpty => _fileStream.Length == 0;
        public override bool CanRead => _fileStream.CanRead;
        public override bool CanSeek => _fileStream.CanSeek;
        public override bool CanWrite => _fileStream.CanWrite;
        public override long Length => _fileStream.Length;

        public override long Position
        {
            get => _fileStream.Position;
            set => _fileStream.Position = value;
        }

        public VirtualDrive(string fileName)
        {
            try
            {
                _fileStream = new FileStream(fileName ?? throw new ArgumentNullException(nameof(fileName)), FileMode.OpenOrCreate, FileAccess.ReadWrite);
            }
            catch (Exception e)
            {
                throw new InvalidOperationException($"File {fileName}: unable to initialize", e);
            }

            if (!_fileStream.CanRead)
                throw new AccessViolationException($"File {fileName} could not be read");

            if (!_fileStream.CanWrite)
                throw new AccessViolationException($"File {fileName} is read-only");
        }

        public override void Flush()
        {
            _fileStream.Flush();
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            return _fileStream.Seek(offset, origin);
        }

        public override void SetLength(long value)
        {
            _fileStream.SetLength(value);
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            return _fileStream.Read(buffer, offset, count);
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            _fileStream.Write(buffer, offset, count);
        }

        internal int Write(long position, ServiceBytes bytes)
        {
            return Write(position, BitConverter.GetBytes((int)bytes));
        }

        internal int Write(long position, byte[] data)
        {
            var dataLength = data.Length;
            Position = position;
            var initialLength = Length;

            if (initialLength < position + dataLength)
            {
                initialLength = position + dataLength;
                SetLength(initialLength);
            }

            _fileStream.Write(data, 0, dataLength);
            return dataLength;
        }

        internal int Write(byte[] data)
        {
            return Write(Length, data);
        }

        public new void Dispose()
        {
            base.Dispose();
            _fileStream?.Dispose();
        }
    }
}