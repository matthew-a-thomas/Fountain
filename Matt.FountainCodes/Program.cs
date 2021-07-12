using System;

namespace Matt.FountainCodes
{
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Text;
    using Matt.Accelerated;
    using Matt.GaussianElimination;

    class Program
    {
        static void Main()
        {
            Directory.SetCurrentDirectory(@"D:\Downloads");
            // Formula for estimating how many XOR operations will be needed:
            // y = 0.4992 * x * x + 3.1447 * x - 31.738

            // Set up the Gaussian Elimination problem using files on disk
            const int numCoefficients = 32;
            const int rowWidth = 1024 * 1024;
            const int numSlices = numCoefficients + 5;

            // Generate the message
            var message = new byte[(long)numCoefficients * rowWidth];
            {
                var line = Encoding.UTF8.GetBytes($"This message was encoded at {DateTime.Now} on {Environment.MachineName}. This is a test of the emergency broadcast system. Do not be alarmed, this is only a test.");
                for (var i = 0; i < message.Length - line.Length; i += line.Length)
                {
                    line.CopyTo(message, i);
                }
                Array.Copy(
                    line,
                    0,
                    message,
                    message.Length / line.Length * line.Length,
                    message.Length % line.Length
                );
            }

            // Populate the slices with the message
            var random = new Random();
            var coefficientsFactory = new CoefficientsFactory(
                () => random.Next() % 2 == 0,
                () => random.Next()
            );
            ISliceWriter<FileStream> sliceWriter = new SliceWriter();
            var enumerable = coefficientsFactory
                .Generate(numCoefficients, false)
                .Take(numSlices)
                .Select((x, i) => (x, i));
            var buffer = new byte[rowWidth];
            var slices = new List<FileStream>();
            foreach (var (rental, i) in enumerable)
            {
                using var _ = rental;
                var coefficients = rental.Memory.Span;

                // Prepare the slice data
                for (var j = 0; j < numCoefficients; ++j)
                {
                    if (!coefficients[j])
                        continue;
                    Bitwise.Xor(
                        message.AsSpan(j * rowWidth, rowWidth),
                        buffer
                    );
                }

                // Write the slice
                var stream = new FileStream(
                    $"{i}.dat",
                    FileMode.Create,
                    FileAccess.ReadWrite,
                    FileShare.Read
                );
                sliceWriter.Write(
                    stream,
                    coefficients,
                    buffer
                );
                stream.Flush();
                Array.Clear(buffer, 0, buffer.Length);

                // Make the slice part of the problem to solve
                slices.Add(stream);
            }

            // Combine all the slices into one file
            using (var coefficientsFile = new FileStream("coefficients.dat", FileMode.Create, FileAccess.ReadWrite, FileShare.Read))
            using (var solutionFile = new FileStream("solution.dat", FileMode.Create, FileAccess.ReadWrite, FileShare.Read))
            {
                var coefficientsBuffer = new byte[numCoefficients];
                foreach (var sliceFile in slices)
                {
                    sliceFile.Position = 0;
                    if (sliceFile.Read(coefficientsBuffer) != coefficientsBuffer.Length)
                        throw new Exception("Didn't read enough bytes");
                    coefficientsFile.Write(coefficientsBuffer);
                    sliceFile.CopyTo(solutionFile);

                    coefficientsFile.Flush();
                    solutionFile.Flush();
                    sliceFile.Dispose();
                    File.Delete(sliceFile.Name);
                }

                // Solve the Gaussian Elimination problem
                var problem = new InPlaceProblem(
                    coefficientsFile,
                    solutionFile,
                    numCoefficients,
                    rowWidth
                );
                if (!GaussianSolver.Solve(problem))
                    throw new Exception("Couldn't solve it");
            }
        }
    }
}