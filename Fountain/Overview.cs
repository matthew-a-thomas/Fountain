namespace Fountain
{
    using System;
    using System.Runtime.InteropServices;

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    unsafe struct Overview
    {
        public fixed byte Magic[4];
        public fixed byte SHA256[32];
        public ulong FileSize;
        public uint RowSize;

        public static bool AreEqual(ref Overview a, ref Overview b)
        {
            var spanA = MemoryMarshal.AsBytes(MemoryMarshal.CreateReadOnlySpan(ref a, 1));
            var spanB = MemoryMarshal.AsBytes(MemoryMarshal.CreateReadOnlySpan(ref b, 1));
            return spanA.SequenceEqual(spanB);
        }

        public static bool IsMagic(in Overview overview) =>
            overview.Magic[0] == (byte) 'F'
            && overview.Magic[1] == (byte) 'N'
            && overview.Magic[2] == (byte) 'T'
            && overview.Magic[3] == (byte) '0';

        public static void WriteMagic(ref Overview overview)
        {
            overview.Magic[0] = (byte) 'F';
            overview.Magic[1] = (byte) 'N';
            overview.Magic[2] = (byte) 'T';
            overview.Magic[3] = (byte) '0';
        }
    }
}