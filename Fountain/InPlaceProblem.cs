namespace Fountain
{
    using System.IO;
    using System.IO.MemoryMappedFiles;
    using Matt.Accelerated;
    using Matt.GaussianElimination;
    using Matt.MemoryMappedFiles;

    public class InPlaceProblem : IGaussianProblem
    {
        readonly FileStream _coefficientsFile;
        readonly FileStream _dataFile;
        readonly int _rowWidth;

        public InPlaceProblem(
            FileStream coefficientsFile,
            FileStream dataFile,
            int numCoefficients,
            int rowWidth)
        {
            NumCoefficients = numCoefficients;
            _coefficientsFile = coefficientsFile;
            _dataFile = dataFile;
            _rowWidth = rowWidth;
        }

        public bool HasCoefficient(int row, int coefficient)
        {
            var address = row * NumCoefficients + coefficient;
            if (address >= _coefficientsFile.Length)
                return false;
            _coefficientsFile.Position = address;
            return _coefficientsFile.ReadByte() > 0;
        }

        public void Xor(int from, int to)
        {
            // XOR coefficients first
            using (var fromMemory = MemoryMappedFileHelper.CreateMemoryManager(_coefficientsFile, MemoryMappedFileAccess.Read, out _, from * NumCoefficients, NumCoefficients, true))
            using (var toMemory = MemoryMappedFileHelper.CreateMemoryManager(_coefficientsFile, MemoryMappedFileAccess.ReadWrite, out _, to * NumCoefficients, NumCoefficients, true))
            {
                Bitwise.Xor(
                    fromMemory.GetSpan(),
                    toMemory.GetSpan()
                );
            }

            // Now XOR the data
            using (var fromMemory = MemoryMappedFileHelper.CreateMemoryManager(_dataFile, MemoryMappedFileAccess.Read, out _, from * _rowWidth, _rowWidth, true))
            using (var toMemory = MemoryMappedFileHelper.CreateMemoryManager(_dataFile, MemoryMappedFileAccess.ReadWrite, out _, to * _rowWidth, _rowWidth, true))
            {
                Bitwise.Xor(
                    fromMemory.GetSpan(),
                    toMemory.GetSpan()
                );
            }
        }

        public int NumCoefficients { get; }
        public int NumRows => (int) (_dataFile.Length / _rowWidth);
    }
}