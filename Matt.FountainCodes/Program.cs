using System;

namespace Matt.FountainCodes
{
    using System.Buffers;
    using System.Collections.Generic;
    using System.IO;
    using System.IO.MemoryMappedFiles;
    using System.Linq;
    using System.Reactive.Disposables;
    using System.Text;
    using Matt.Accelerated;
    using Matt.GaussianElimination;
    using Matt.MemoryMappedFiles;

    class MyGaussianProblem : IGaussianProblem, IDisposable
    {
        readonly List<FileStream> _streams = new();
        readonly int _rowWidth;
        readonly Action<string> _log;
        readonly MostUsedCache<int, MemoryManager<byte>> _cache;

        public MyGaussianProblem(int numCoefficients, int rowWidth, Action<string> log)
        {
            NumCoefficients = numCoefficients;
            _rowWidth = rowWidth;
            _log = log;
            _cache = new MostUsedCache<int, MemoryManager<byte>>(
                i =>
                {
                    // log($"+++ {i}");
                    return MemoryMappedFileHelper.CreateMemoryManager(
                        _streams[i],
                        MemoryMappedFileAccess.ReadWrite,
                        out _,
                        keepOpen: true);
                },
                (i, x) =>
                {
                    // log($"--- {i}");
                    ((IDisposable) x).Dispose();
                },
                3
            );
        }

        public int NumCoefficients { get; }

        public int NumRows => _streams.Count;

        public void Add(FileStream stream)
        {
            if (!stream.CanRead)
                throw new Exception("Can't read from this stream");
            if (!stream.CanWrite)
                throw new Exception("Can't write to this stream");
            if (!stream.CanSeek)
                throw new Exception("Can't seek within this stream");
            if (stream.Length != _rowWidth + NumCoefficients)
                throw new Exception("Incorrect stream length");
            _streams.Add(stream);
        }

        public void Dispose() => _cache.Dispose();

        public bool HasCoefficient(int row, int coefficient)
        {
            var stream = _streams[row];
            stream.Position = coefficient;
            return stream.ReadByte() > 0;
        }

        public void Xor(int from, int to)
        {
            var memoryManagerFrom = _cache.Get(from);
            var memoryManagerTo = _cache.Get(to);
            Bitwise.Xor(
                memoryManagerFrom.GetSpan(),
                memoryManagerTo.GetSpan()
            );
            _log($"XOR {from} -> {to}");
        }
    }

    class Program
    {
        static void Main()
        {
            // Formula for estimating how many XOR operations will be needed:
            // y = 0.4992 * x * x + 3.1447 * x - 31.738

            using var logFile = new FileStream("log.txt", FileMode.Create, FileAccess.Write, FileShare.Read, 4096, FileOptions.SequentialScan);
            using var logger = new StreamWriter(logFile, new UTF8Encoding(false));

            // Set up the Gaussian Elimination problem using files on disk
            using var disposable = new CompositeDisposable();
            const int numCoefficients = 32;
            const int rowWidth = 1024 * 1024;
            const int numSlices = numCoefficients + 5;
            using var problem = new MyGaussianProblem(numCoefficients, rowWidth, logger.WriteLine);
            var segments = new List<FileStream>();
            for (var i = 0; i < numSlices; ++i)
            {
                var file = new FileStream(
                    $"{i}.dat",
                    FileMode.Create,
                    FileAccess.ReadWrite,
                    FileShare.Read
                );
                file.SetLength(numCoefficients + rowWidth);
                disposable.Add(file);
                problem.Add(file);
                segments.Add(file);
            }

            // Generate the message on disk
            FileStream message;
            {
                message = new FileStream("message.txt", FileMode.Create, FileAccess.ReadWrite, FileShare.Read);
                disposable.Add(message);
                const long desiredLength = (long)numCoefficients * rowWidth;
                var line = $"This message was encoded at {DateTime.Now} on {Environment.MachineName}. This is a test of the emergency broadcast system. Do not be alarmed, this is only a test.";
                using (var writer = new StreamWriter(message, new UTF8Encoding(false), 4096, true))
                {
                    while (message.Length < desiredLength)
                    {
                        writer.WriteLine(line);
                        writer.Flush();
                    }
                }
                message.SetLength(desiredLength);
                message.Flush();
            }

            // Populate the slices with the message
            var random = new Random();
            var coefficientsFactory = new CoefficientsFactory(
                () => random.Next() % 2 == 0,
                () => random.Next()
            );
            foreach (var (rental, stream) in coefficientsFactory.Generate(numCoefficients, false).Take(numSlices).Zip(segments))
            {
                using var _ = rental;
                var coefficients = rental.Memory.Span;
                var offsetsInMessage = new List<long>();
                stream.Position = 0;
                for (var i = 0; i < numCoefficients; ++i)
                {
                    stream.WriteByte(coefficients[i] ? byte.MaxValue : byte.MinValue);
                    if (coefficients[i])
                        offsetsInMessage.Add(i * rowWidth);
                }
                StreamHelpers.MultiXor(
                    message,
                    stream,
                    rowWidth,
                    offsetsInMessage
                );
            }

            // Solve the Gaussian Elimination problem
            if (!GaussianSolver.Solve(problem))
                throw new Exception("Couldn't solve it");
        }
    }
}