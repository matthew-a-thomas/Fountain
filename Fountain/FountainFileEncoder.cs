namespace Fountain
{
    using System;
    using System.IO;
    using System.IO.MemoryMappedFiles;
    using System.Security.Cryptography;
    using Matt.Accelerated;
    using Matt.GaussianElimination;
    using Matt.MemoryMappedFiles;

    class FountainFileEncoder
    {
        readonly CoefficientsFactory _coefficientsFactory;

        public FountainFileEncoder(
            CoefficientsFactory coefficientsFactory)
        {
            _coefficientsFactory = coefficientsFactory;
        }

        public void Encode(
            string filename,
            bool systematic,
            uint? rowSize,
            ushort? numRows,
            double? percent,
            string fountainFileName)
        {
            // Open the source file and finalize parameters
            using var file = new FileStream(filename, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, FileOptions.RandomAccess);
            ushort numCoefficients;
            if (rowSize.HasValue)
            {
                numCoefficients = FountainFileMath.GetNumCoefficients((ulong) file.Length, rowSize.Value);
            }
            else
            {
                numCoefficients = 64;
                rowSize = FountainFileMath.GetRowSize((ulong) file.Length, ref numCoefficients);
            }

            // Write the overview section
            var overview = new Overview();
            Overview.WriteMagic(ref overview);
            using (var hash = SHA256.Create())
            {
                hash.ComputeHash(file);
                file.Position = 0;
                unsafe
                {
                    var span = new Span<byte>(overview.SHA256, 256 / 8);
                    hash.Hash.CopyTo(span);
                }
            }
            overview.FileSize = (ulong) file.Length;
            overview.RowSize = (uint) rowSize;
            using var fountain = new FileStream(fountainFileName, FileMode.Create, FileAccess.ReadWrite, FileShare.Read, 4096, FileOptions.RandomAccess);
            unsafe
            {
                fountain.Write(new Span<byte>(&overview, sizeof(Overview)));
            }

            // Write data rows
            if (!numRows.HasValue && percent.HasValue)
                numRows = (ushort)Math.Ceiling(percent.Value * numCoefficients);
            if (numRows is 0)
                return;
            var packedCoefficientsSize = PackedCoefficients.GetNumPackedBytes(numCoefficients);
            var packedCoefficientsBuffer = new byte[packedCoefficientsSize];
            var rowBuffer = new byte[rowSize.Value];
            var problem = numRows.HasValue
                ? null
                : new JustCoefficientsProblem(numCoefficients);
            foreach (var coefficients in _coefficientsFactory.Generate(numCoefficients, systematic))
            {
                using var __ = coefficients;
                var coefficientsSpan = coefficients.Memory.Span;
                Array.Clear(packedCoefficientsBuffer, 0, packedCoefficientsBuffer.Length);
                Array.Clear(rowBuffer, 0, rowBuffer.Length);
                for (var j = 0; j < numCoefficients; ++j)
                {
                    if (!coefficientsSpan[j])
                        continue;
                    PackedCoefficients.Set(packedCoefficientsBuffer, j);
                    using var fileMemory = MemoryMappedFileHelper.CreateMemoryManager(
                        file,
                        MemoryMappedFileAccess.Read,
                        out _,
                        j * rowSize.Value,
                        (int)Math.Min(rowSize.Value, file.Length - j * rowSize.Value),
                        true
                    );
                    var fileChunk = fileMemory.GetSpan();
                    Bitwise.Xor(
                        fileChunk,
                        rowBuffer.AsSpan(0, fileChunk.Length)
                    );
                }
                fountain.Write(packedCoefficientsBuffer);
                fountain.Write(rowBuffer);

                if (problem is not null)
                {
                    problem.Add(coefficients.Memory);
                    if (GaussianSolver.Solve(problem))
                        return;
                }
                else if (--numRows == 0)
                {
                    return;
                }
            }
        }
    }
}