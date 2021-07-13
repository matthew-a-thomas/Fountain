namespace Fountain
{
    using System;
    using System.Buffers;
    using System.IO;
    using System.IO.MemoryMappedFiles;
    using System.Threading;
    using Matt.MemoryMappedFiles;

    public sealed class SlidingMemoryMappedFile : IDisposable
    {
        int _count;
        readonly FileStream _file;
        readonly MemoryMappedFileAccess _access;
        Action? _flush;
        readonly bool _keepOpen;
        readonly int _minMapSize;
        MemoryManager<byte>? _memoryManager;
        long _offset;

        public SlidingMemoryMappedFile(
            FileStream file,
            MemoryMappedFileAccess access,
            bool keepOpen,
            int minMapSize)
        {
            _file = file;
            _access = access;
            _keepOpen = keepOpen;
            _minMapSize = minMapSize;
        }

        public void Dispose()
        {
            _flush = null;
            ((IDisposable?)Interlocked.Exchange(ref _memoryManager, null))?.Dispose();
            if (!_keepOpen)
                _file.Dispose();
        }

        public void Flush() => _flush?.Invoke();

        public Memory<byte> GetMemory(long offset, int count)
        {
            if (offset + count > _file.Length)
                throw new ArgumentOutOfRangeException();
            var desiredEnd = offset + count;
            var currentEnd = _offset + _count;
            if (_memoryManager is {} memoryManager && offset >= _offset && desiredEnd <= currentEnd)
            {
                var memory = memoryManager.Memory;
                return memory.Slice(
                    (int)(offset - _offset),
                    count
                );
            }
            ((IDisposable?)_memoryManager)?.Dispose();
            _memoryManager = memoryManager = MemoryMappedFileHelper.CreateMemoryManager(
                _file,
                _access,
                out _flush,
                _offset = offset,
                _count = (int)Math.Min(_file.Length - offset, Math.Max(count, _minMapSize)),
                true
            );
            return memoryManager.Memory[..count];
        }
    }
}