namespace Fountain
{
    using System;
    using System.IO;
    using System.IO.MemoryMappedFiles;
    using System.Security.Cryptography;
    using Matt.Accelerated;
    using Matt.GaussianElimination;

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
            ushort numCoefficients,
            ushort? numRows)
        {
            using var file = new FileStream(filename, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, FileOptions.RandomAccess);
            numCoefficients = (ushort)Math.Min(numCoefficients, file.Length);
            var rowSize = (int)(file.Length + numCoefficients - 1) / numCoefficients;
            FileMode fileMode;
            var fountainFileName = filename + ".fountain";
            if (File.Exists(fountainFileName))
            {
                Console.WriteLine($"{fountainFileName} already exists. Overwrite? Y/[N]");
                var response = Console.ReadLine();
                if (string.IsNullOrWhiteSpace(response))
                {
                    return;
                }
                if (response[0] is 'y' or 'Y')
                {
                    fileMode = FileMode.Create;
                }
                else
                {
                    return;
                }
            }
            else
            {
                fileMode = FileMode.CreateNew;
            }
            using var fountain = new FileStream(fountainFileName, fileMode, FileAccess.ReadWrite, FileShare.Read, 4096, FileOptions.RandomAccess);
            fountain.Write(new[] { (byte) 'F', (byte) 'N', (byte) 'T', (byte) '0' });
            using (var hash = SHA256.Create())
            {
                hash.ComputeHash(file);
                file.Position = 0;
                fountain.Write(hash.Hash);
            }
            fountain.Write(BitConverter.GetBytes((ulong)file.Length));
            fountain.Write(BitConverter.GetBytes((ushort)numCoefficients));

            if (numRows is 0)
                return;

            var packedCoefficientsSize = (int)Math.Ceiling(numCoefficients / 8.0);
            var packedCoefficientsBuffer = new byte[packedCoefficientsSize];
            var rowBuffer = new byte[rowSize];

            var problem = numRows.HasValue
                ? null
                : new JustCoefficientsProblem(numCoefficients);
            using var fileMemory = new SlidingMemoryMappedFile(file, MemoryMappedFileAccess.Read, true, 1024 * 1024 * 100);
            foreach (var coefficients in _coefficientsFactory.Generate(numCoefficients, systematic))
            {
                using var _ = coefficients;
                var coefficientsSpan = coefficients.Memory.Span;
                Array.Clear(packedCoefficientsBuffer, 0, packedCoefficientsBuffer.Length);
                Array.Clear(rowBuffer, 0, rowBuffer.Length);
                for (var j = 0; j < numCoefficients; ++j)
                {
                    if (!coefficientsSpan[j])
                        continue;
                    packedCoefficientsBuffer[j / 8] |= (byte)(0x1 << (7 - j % 8));
                    var fileChunk = fileMemory
                        .GetMemory(j * rowSize, (int)Math.Min(rowSize, file.Length - j * rowSize))
                        .Span;
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