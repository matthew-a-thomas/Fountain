namespace Fountain
{
    using System;
    using System.Diagnostics.CodeAnalysis;
    using System.IO;
    using Matt.GaussianElimination;

    class FountainFileShrinker
    {
        readonly OverviewReader _overviewReader;

        public FountainFileShrinker(OverviewReader overviewReader)
        {
            _overviewReader = overviewReader;
        }

        public void Shrink(string fountain)
        {
            using var file = new FileStream(fountain, FileMode.Open, FileAccess.ReadWrite, FileShare.Read);
            file.Position = 0;
            if (!_overviewReader.TryReadOverview(file, out var overview))
                throw new Exception("Not a fountain file");
            var problem = new FountainFileProblem(file, overview.FileSize, overview.RowSize);
            GaussianSolver.Solve(problem);
            ShrinkAfterSolving(file, in overview);
        }

        static void ShrinkAfterSolving(Stream file, in Overview overview)
        {
            var rowSize = overview.RowSize;
            var numCoefficients = FountainFileMath.GetNumCoefficients(overview.FileSize, rowSize);
            var numPackedBytes = PackedCoefficients.GetNumPackedBytes(numCoefficients);
            var packedCoefficients = new byte[numPackedBytes];
            var blockSize = rowSize + numPackedBytes;

            // Truncate all trailing empty blocks from the file
            var position = (file.Length - FountainFileMath.GetOverviewSize()) / blockSize * blockSize + FountainFileMath.GetOverviewSize() - blockSize;
            for (; position >= FountainFileMath.GetOverviewSize(); position -= blockSize)
            {
                file.Position = position;
                if (file.Read(packedCoefficients) != packedCoefficients.Length)
                    throw new Exception("Couldn't read enough bytes");
                if (!IsEmpty(packedCoefficients))
                {
                    file.SetLength(position + blockSize);
                    break;
                }
            }
        }

        [SuppressMessage("ReSharper", "ForCanBeConvertedToForeach")]
        static bool IsEmpty(ReadOnlySpan<byte> packedCoefficients)
        {
            for (var i = 0; i < packedCoefficients.Length; ++i)
            {
                if (packedCoefficients[i] != 0)
                    return false;
            }
            return true;
        }
    }
}