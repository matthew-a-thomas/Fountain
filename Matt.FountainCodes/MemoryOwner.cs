namespace Matt.FountainCodes
{
    using System;
    using System.Buffers;
    using System.Threading;

    sealed class MemoryOwner<T> : IMemoryOwner<T>
    {
        Action? _dispose;

        public MemoryOwner(
            Memory<T> memory,
            Action dispose)
        {
            Memory = memory;
            _dispose = dispose;
        }

        public Memory<T> Memory { get; private set; }

        public void Dispose()
        {
            Memory = default;
            Interlocked.Exchange(ref _dispose, null)?.Invoke();
        }
    }
}