namespace Fountain
{
    using System;

    static class PackedCoefficients
    {
        static byte BitMask(int bit) => (byte) (0x1 << (7 - bit % 8));

        public static ushort GetNumPackedBytes(ushort numCoefficients) => (ushort) ((numCoefficients + 7) / 8);

        public static bool IsSet(ReadOnlySpan<byte> packed, int bit) => (packed[bit / 8] & BitMask(bit)) != 0;

        public static void Set(Span<byte> packed, int bit) => packed[bit / 8] |= BitMask(bit);
    }
}