namespace Fountain
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.IO.MemoryMappedFiles;
    using System.Runtime.InteropServices;
    using Matt.GaussianElimination;
    using Matt.MemoryMappedFiles;

    class FountainFileInfoProvider
    {
        public unsafe void PrintInfo(IEnumerable<string> fileNames)
        {
            foreach (var fileName in fileNames)
            {
                Console.WriteLine(fileName);
                if (!File.Exists(fileName))
                {
                    Console.WriteLine("\tDoes not exist");
                    continue;
                }

                if (new FileInfo(fileName).Length < FountainFileMath.GetOverviewSize())
                {
                    Console.WriteLine("\tNot a fountain file");
                    continue;
                }

                using var file = new FileStream(
                    fileName,
                    FileMode.Open,
                    FileAccess.Read,
                    FileShare.ReadWrite);
                Overview overview;
                using (var memoryManager = MemoryMappedFileHelper.CreateMemoryManager(
                    file,
                    MemoryMappedFileAccess.Read,
                    out _,
                    0,
                    FountainFileMath.GetOverviewSize(),
                    true))
                {
                    overview = MemoryMarshal.AsRef<Overview>((ReadOnlySpan<byte>) memoryManager.GetSpan());
                    if (!Overview.IsMagic(in overview))
                    {
                        Console.WriteLine("\tNot a fountain file");
                        continue;
                    }
                }

                Console.WriteLine($"\t{file.Length:N0} bytes");

                // Print raw overview information
                Console.WriteLine($"\tSource file SHA256: {BitConverter.ToString(new ReadOnlySpan<byte>(overview.SHA256, 256 / 8).ToArray())}");
                Console.WriteLine($"\tSource file size: {overview.FileSize:N0} bytes");
                Console.WriteLine($"\tRow size: {overview.RowSize:N0}");

                // Print derived information
                var numCoefficients = FountainFileMath.GetNumCoefficients(overview.FileSize, overview.RowSize);
                Console.WriteLine($"\tNum coefficients: {numCoefficients:N0}");
                var numPackedBytes = PackedCoefficients.GetNumPackedBytes(numCoefficients);
                var blockSize = numPackedBytes + overview.RowSize;
                var numRows = (file.Length - FountainFileMath.GetOverviewSize()) / blockSize;
                Console.WriteLine($"\tNum rows: {numRows:N0}");

                // Determine whether this fountain file is solvable
                var packedCoefficients = new byte[numPackedBytes];
                var unpackedCoefficients = new bool[numCoefficients];
                var problem = new JustCoefficientsProblem(numCoefficients);
                for (long position = FountainFileMath.GetOverviewSize();
                    position <= file.Length - blockSize;
                    position += blockSize)
                {
                    file.Position = position;
                    if (file.Read(packedCoefficients) != packedCoefficients.Length)
                        throw new Exception("Couldn't read enough bytes");
                    for (var i = 0; i < numCoefficients; ++i)
                    {
                        unpackedCoefficients[i] = PackedCoefficients.IsSet(packedCoefficients, i);
                    }

                    problem.Add(unpackedCoefficients);
                }

                var stepCountingProblem = new StepCountingProblem(problem);
                var solvable = GaussianSolver.Solve(stepCountingProblem);
                Console.WriteLine(solvable
                    ? $"\tSolvable in {stepCountingProblem.NumSteps:N0} steps"
                    : "\tNot solvable"
                );
            }
        }
    }
}