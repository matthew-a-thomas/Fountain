namespace Fountain
{
    using System;
    using System.Buffers;
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;

    public sealed class CoefficientsFactory
    {
        readonly Func<bool> _bitEntropy;
        readonly Func<int> _intEntropy;

        public CoefficientsFactory(
            Func<bool> bitEntropy,
            Func<int> intEntropy)
        {
            _bitEntropy = bitEntropy;
            _intEntropy = intEntropy;
        }

        [SuppressMessage("ReSharper", "IteratorNeverReturns")]
        public IEnumerable<IMemoryOwner<bool>> Generate(
            int numCoefficients,
            bool systematic)
        {
            if (systematic)
            {
                for (var i = 0; i < numCoefficients; ++i)
                {
                    var rental = RentExact(numCoefficients);
                    var span = rental.Memory.Span;
                    span.Clear();
                    span[i] = true;
                    yield return rental;
                }
            }
            while (true)
            {
                var rental = RentExact(numCoefficients);
                var span = rental.Memory.Span;
                var hasCoefficients = false;
                for (var i = 0; i < numCoefficients; ++i)
                {
                    hasCoefficients |= span[i] = _bitEntropy();
                }
                if (!hasCoefficients)
                    span[_intEntropy() % numCoefficients] = true;
                yield return rental;
            }
        }

        static IMemoryOwner<bool> RentExact(int count)
        {
            var rental = MemoryPool<bool>.Shared.Rent(count);
            rental = new MemoryOwner<bool>(
                rental.Memory[..count],
                rental.Dispose
            );
            return rental;
        }
    }
}