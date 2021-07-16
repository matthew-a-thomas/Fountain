namespace Fountain
{
    using System.IO;
    using System.IO.MemoryMappedFiles;
    using Matt.Accelerated;
    using Matt.GaussianElimination;
    using Matt.MemoryMappedFiles;

    public class FountainFileProblem : IGaussianProblem
    {
        readonly FileStream _file;
        readonly uint _blockSize;
        readonly ushort _numPackedBytes;

        public FountainFileProblem(
            FileStream file,
            ulong fileSize,
            uint rowSize)
        {
            _file = file;
            var numCoefficients = FountainFileMath.GetNumCoefficients(fileSize, rowSize);
            NumCoefficients = numCoefficients;
            _numPackedBytes = PackedCoefficients.GetNumPackedBytes(numCoefficients);
            _blockSize = rowSize + _numPackedBytes;
        }

        public bool HasCoefficient(int row, int coefficient)
        {
            using var packedCoefficients = MemoryMappedFileHelper.CreateMemoryManager(
                _file,
                MemoryMappedFileAccess.Read,
                out _,
                FountainFileMath.GetOverviewSize() + row * _blockSize,
                _numPackedBytes,
                true
            );
            return PackedCoefficients.IsSet(packedCoefficients.GetSpan(), coefficient);
        }

        public void Xor(int from, int to)
        {
            using var fromBlock = MemoryMappedFileHelper.CreateMemoryManager(
                _file,
                MemoryMappedFileAccess.Read,
                out _,
                FountainFileMath.GetOverviewSize() + from * _blockSize,
                (int)_blockSize,
                true
            );
            using var toBlock = MemoryMappedFileHelper.CreateMemoryManager(
                _file,
                MemoryMappedFileAccess.ReadWrite,
                out _,
                FountainFileMath.GetOverviewSize() + to * _blockSize,
                (int)_blockSize,
                true
            );
            Bitwise.Xor(
                fromBlock.GetSpan(),
                toBlock.GetSpan()
            );
        }

        public int NumCoefficients { get; }

        public int NumRows => (int) ((_file.Length - FountainFileMath.GetOverviewSize()) / _blockSize);
    }
}