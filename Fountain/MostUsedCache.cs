namespace Fountain
{
    using System;

    public class MostUsedCache<TKey, TValue> : IDisposable
    {
        struct Slot
        {
            public long Generation;
            public TKey Key;
            public TValue Value;
        }

        readonly Func<TKey, TValue> _create;
        readonly Action<TKey, TValue> _dispose;
        long _generation;
        readonly Slot[] _slots;

        public MostUsedCache(
            Func<TKey, TValue> create,
            Action<TKey, TValue> dispose,
            int capacity)
        {
            _create = create;
            _dispose = dispose;
            _slots = new Slot[capacity];
        }

        public void Dispose()
        {
            for (var i = 0; i < _slots.Length; ++i)
            {
                ref var slot = ref _slots[i];
                if (slot.Generation > 0)
                {
                    _dispose(slot.Key, slot.Value);
                    slot = default;
                }
            }
        }

        public TValue Get(TKey key)
        {
            var generation = ++_generation;

            var oldestIndex = 0;
            var oldestGeneration = generation;
            for (var i = 0; i < _slots.Length; ++i)
            {
                ref var slot = ref _slots[i];
                if (slot.Generation == 0)
                {
                    // Nothing in this slot
                    if (0 < oldestGeneration)
                    {
                        oldestGeneration = 0;
                        oldestIndex = i;
                    }
                }
                else if (Equals(slot.Key, key))
                {
                    // This is the one we want. Freshen it up
                    slot.Generation = generation;
                    return slot.Value;
                }
                else if (slot.Generation < oldestGeneration)
                {
                    // This slot is a candidate for replacement
                    oldestGeneration = slot.Generation;
                    oldestIndex = i;
                }
            }

            {
                // Couldn't find the value in any of the slots.
                // Replace the slot in the least used spot
                ref var slot = ref _slots[oldestIndex];
                if (slot.Generation > 0)
                {
                    // There was something here before. Dispose of it
                    _dispose(slot.Key, slot.Value);
                }
                var value = _create(key);
                slot = new Slot
                {
                    Generation = generation,
                    Key = key,
                    Value = value
                };
                return value;
            }
        }
    }
}