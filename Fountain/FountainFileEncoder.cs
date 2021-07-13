namespace Fountain
{
    using System;
    using System.IO;
    using System.IO.MemoryMappedFiles;
    using System.Linq;
    using System.Security.Cryptography;
    using Matt.Accelerated;

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
            ushort numRows)
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
            fountain.Write(BitConverter.GetBytes((ulong)rowSize));
            fountain.Flush();

            var packedCoefficientsSize = (int)Math.Ceiling(numCoefficients / 8.0);
            var blockSize = rowSize + packedCoefficientsSize;
            var dataSize = blockSize * numRows;
            var dataOffset = fountain.Position;
            fountain.SetLength(fountain.Length + dataSize);
            using var fountainMemory = new SlidingMemoryMappedFile(fountain, MemoryMappedFileAccess.ReadWrite, true, 1024 * 1024 * 100);

            using var fileMemory = new SlidingMemoryMappedFile(file, MemoryMappedFileAccess.Read, true, 1024 * 1024 * 100);
            foreach (var (coefficients, i) in _coefficientsFactory
                .Generate(numCoefficients, systematic)
                .Take(numRows)
                .Select((x, i) => (x, i)))
            {
                using var _ = coefficients;
                var coefficientsSpan = coefficients.Memory.Span;
                var block = fountainMemory.GetMemory(dataOffset + i * blockSize, blockSize).Span;
                var packedCoefficients = block[..packedCoefficientsSize];
                var row = block[packedCoefficientsSize..];
                for (var j = 0; j < numCoefficients; ++j)
                {
                    if (!coefficientsSpan[j])
                        continue;
                    packedCoefficients[j / 8] |= (byte)(0x1 << j % 8);
                    var fileChunk = fileMemory
                        .GetMemory(j * rowSize, (int)Math.Min(rowSize, file.Length - j * rowSize))
                        .Span;
                    Bitwise.Xor(
                        fileChunk,
                        row[..fileChunk.Length]
                    );
                }
            }
        }
    }
}