namespace Fountain
{
    using System;
    using System.IO;
    using System.Security.Cryptography;
    using Matt.GaussianElimination;

    class FountainFileDecoder
    {
        readonly OverviewReader _overviewReader;

        public FountainFileDecoder(
            OverviewReader overviewReader)
        {
            _overviewReader = overviewReader;
        }

        public unsafe void Decode(
            string fountain,
            string destinationFileName)
        {
            using var file = new FileStream(fountain, FileMode.Open, FileAccess.ReadWrite, FileShare.Read, 4096, FileOptions.RandomAccess);
            if (!_overviewReader.TryReadOverview(file, out var overview))
                throw new Exception("Not a fountain file");
            var problem = new FountainFileProblem(file, overview.FileSize, overview.RowSize);
            if (!GaussianSolver.Solve(problem))
            {
                Console.WriteLine($"{fountain} is not solvable");
                return;
            }

            using var destination = new FileStream(destinationFileName, FileMode.Create, FileAccess.Write);
            destination.SetLength((long) overview.FileSize);
            using var hash = SHA256.Create();
            using (var hashingStream = new CryptoStream(destination, hash, CryptoStreamMode.Write, true))
            {
                var numPackedCoefficients = PackedCoefficients.GetNumPackedBytes(FountainFileMath.GetNumCoefficients(overview.FileSize, overview.RowSize));
                var rowBuffer = new byte[overview.RowSize];
                var blockSize = numPackedCoefficients + overview.RowSize;
                var numBytesLeft = (long)overview.FileSize;
                for (file.Position = FountainFileMath.GetOverviewSize(); file.Position <= file.Length - blockSize;)
                {
                    file.Seek(numPackedCoefficients, SeekOrigin.Current);
                    if (file.Read(rowBuffer) != rowBuffer.Length)
                        throw new Exception("Couldn't read enough bytes");
                    hashingStream.Write(
                        numBytesLeft >= rowBuffer.Length
                            ? rowBuffer
                            : rowBuffer.AsSpan(0, (int) numBytesLeft)
                    );
                    numBytesLeft -= rowBuffer.Length;
                }
            }

            if (new ReadOnlySpan<byte>(overview.SHA256, 256 / 8).SequenceEqual(hash.Hash))
                return;

            destination.Close();
            File.Delete(destinationFileName);
            throw new Exception("SHA256 mismatch");
        }
    }
}