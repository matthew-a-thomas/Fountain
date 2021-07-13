namespace Fountain
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.IO.MemoryMappedFiles;
    using Matt.Accelerated;
    using Matt.MemoryMappedFiles;

    static class StreamHelpers
    {
        public struct Options
        {
            public int? MapSizeHint;
        }

        public static void MultiXor(
            FileStream from,
            FileStream to,
            long chunkSize,
            IReadOnlyCollection<long> offsetsInFrom,
            Options options = default)
        {
            var mapSizeHint = options.MapSizeHint ?? 1024 * 1024 * 100;
            using var fromSlidingMemory = new SlidingMemoryMappedFile(
                from,
                MemoryMappedFileAccess.Read,
                true,
                mapSizeHint
            );
            var toOffset = to.Position;
            var fromOffset = 0;
            while (chunkSize > 0)
            {
                var mappedSize = (int)Math.Min(mapSizeHint, chunkSize);
                using var memoryManagerTo = MemoryMappedFileHelper.CreateMemoryManager(to, MemoryMappedFileAccess.ReadWrite, out _, toOffset, mappedSize, true);
                var spanTo = memoryManagerTo.GetSpan();
                foreach (var offsetInFrom in offsetsInFrom)
                {
                    var memoryFrom = fromSlidingMemory.GetMemory(
                        offsetInFrom + fromOffset,
                        mappedSize
                    );
                    Bitwise.Xor(
                        memoryFrom.Span,
                        spanTo
                    );
                }
                chunkSize -= mappedSize;
                toOffset += mappedSize;
                fromOffset += mappedSize;
            }
        }

        public static void Xor(
            FileStream from,
            FileStream to,
            long count,
            Options options = default)
        {
            var mapSizeHint = options.MapSizeHint ?? 1024 * 1024 * 100;
            var fromOffset = from.Position;
            var toOffset = to.Position;
            while (count > 0)
            {
                var chunkSize = (int)Math.Min(mapSizeHint, count);
                using var memoryManagerFrom = MemoryMappedFileHelper.CreateMemoryManager(from, MemoryMappedFileAccess.Read, out _, fromOffset, chunkSize, true);
                using var memoryManagerTo = MemoryMappedFileHelper.CreateMemoryManager(to, MemoryMappedFileAccess.ReadWrite, out _, toOffset, chunkSize, true);
                Bitwise.Xor(
                    memoryManagerFrom.GetSpan(),
                    memoryManagerTo.GetSpan()
                );
                count -= chunkSize;
                fromOffset += chunkSize;
                toOffset += chunkSize;
            }
        }
    }
}