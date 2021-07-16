namespace Fountain
{
    static class FountainFileMath
    {
        public static uint GetRowSize(ulong fileLength, ref ushort numCoefficients)
        {
            var rowSize = (uint) ((fileLength + numCoefficients - 1) / numCoefficients);
            if (rowSize * numCoefficients <= fileLength + rowSize)
            {
                return rowSize;
            }
            numCoefficients = (ushort) ((fileLength + rowSize - 2) / rowSize);
            rowSize = (uint)((fileLength + numCoefficients - 1) / numCoefficients);
            return rowSize;
        }

        public static ushort GetNumCoefficients(ulong fileLength, uint rowSize) => (ushort) ((fileLength + rowSize - 1) / rowSize);

        public static unsafe ushort GetOverviewSize() => (ushort)sizeof(Overview);
    }
}