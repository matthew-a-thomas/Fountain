namespace Fountain
{
    using System;
    using System.IO;
    using System.Linq;
    using System.Reflection;

    class App
    {
        readonly CommandLineArgs _args;
        readonly CommandLineParser _commandLineParser;
        readonly FountainFileDecoder _decoder;
        readonly FountainFileEncoder _encoder;
        readonly FountainFileMerger _merger;
        readonly FountainFileShrinker _shrinker;

        public App(
            CommandLineArgs args,
            CommandLineParser commandLineParser,
            FountainFileDecoder decoder,
            FountainFileEncoder encoder,
            FountainFileMerger merger,
            FountainFileShrinker shrinker)
        {
            _args = args;
            _commandLineParser = commandLineParser;
            _decoder = decoder;
            _encoder = encoder;
            _merger = merger;
            _shrinker = shrinker;
        }

        static void PrintUsage()
        {
            var exeName = Path.GetFileName(Assembly.GetEntryAssembly()?.Location);
            Console.Error.WriteLine(@$"Fountain Codes {Assembly.GetExecutingAssembly().GetName().Version}
Usage:
{exeName} --encode <filename> [--coeff=<n>] [--rows=<n>]
    Encodes the given <filename> as a .fountain file
{exeName} --merge <fountain-from> <fountain-to>
    Merges the <fountain-from> .fountain file into the <fountain-to> .fountain file
{exeName} --shrink <fountain>
    Shrinks the <fountain> .fountain file if possible
{exeName} --decode <fountain>
    Decodes the <fountain> .fountain file if possible");
            Environment.ExitCode = 1;
        }

        public void Run()
        {
            var args = _commandLineParser.Parse(_args.Args);
            var fileNames = args
                .Where(kvp => !kvp.Key.StartsWith("--") && kvp.Value is null)
                .Select(kvp => kvp.Key)
                .ToList();
            if (args.ContainsKey("--encode"))
            {
                if (fileNames.Count == 1)
                {
                    var systematic =
                        !args.TryGetValue("--systematic", out var systematicString)
                        || !bool.TryParse(systematicString, out var x)
                        || x;

                    ushort numCoefficients;
                    if (args.TryGetValue("--coeff", out var coeffString))
                    {
                        if (!ushort.TryParse(coeffString, out numCoefficients))
                            throw new Exception("--coeff needs to be an unsigned 16 bit integer");
                    }
                    else
                    {
                        numCoefficients = 64;
                    }

                    ushort numRows;
                    if (args.TryGetValue("--rows", out var rowsString))
                    {
                        if (!ushort.TryParse(rowsString, out numRows))
                            throw new Exception("--rows needs to be an unsigned 16 bit integer");
                    }
                    else
                    {
                        numRows = systematic
                            ? numCoefficients
                            : (ushort)Math.Min(ushort.MaxValue, numCoefficients + 7);
                    }

                    Encode(
                        fileNames[0],
                        systematic,
                        numCoefficients,
                        numRows);
                }
                else
                {
                    PrintUsage();
                }
            }
            else if (args.ContainsKey("--merge"))
            {
                if (fileNames.Count == 2)
                {
                    Merge(fileNames[0], fileNames[1]);
                }
                else
                {
                    PrintUsage();
                }
            }
            else if (args.ContainsKey("--shrink"))
            {
                if (fileNames.Count == 1)
                {
                    Shrink(fileNames[0]);
                }
                else
                {
                    PrintUsage();
                }
            }
            else if (args.ContainsKey("--decode"))
            {
                if (fileNames.Count == 1)
                {
                    Decode(fileNames[0]);
                }
                else
                {
                    PrintUsage();
                }
            }
            else
            {
                PrintUsage();
            }
        }

        void Decode(string fountain)
        {
            FailIfNotExists(fountain);
            FailIfNotFountain(fountain);
            _decoder.Decode(fountain);
        }

        void Encode(
            string filename,
            bool systematic,
            ushort numCoefficients,
            ushort numRows)
        {
            FailIfNotExists(filename);
            _encoder.Encode(
                filename,
                systematic,
                numCoefficients,
                numRows);
        }

        void Merge(string fountainFrom, string fountainTo)
        {
            FailIfNotExists(fountainFrom);
            FailIfNotExists(fountainTo);
            FailIfNotFountain(fountainFrom);
            FailIfNotFountain(fountainTo);
            _merger.Merge(fountainFrom, fountainTo);
        }

        void Shrink(string fountain)
        {
            FailIfNotExists(fountain);
            FailIfNotFountain(fountain);
            _shrinker.Shrink(fountain);
        }

        static void FailIfNotExists(string filename)
        {
            if (File.Exists(filename))
                return;
            var message = $"Cannot find file {filename}";
            throw new Exception(message);
        }

        static void FailIfNotFountain(string fountain)
        {
            using var file = new FileStream(fountain, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            var buffer = new byte[4];
            if (file.Read(buffer) != buffer.Length)
                throw new Exception($"{fountain} is not a fountain file");
            if (buffer[0] != 'F' || buffer[1] != 'N' || buffer[2] != 'T')
                throw new Exception($"{fountain} is not a fountain file");
            if (buffer[3] != '0')
                throw new Exception($"{fountain} might be a fountain file, but not of a recognized version");
        }
    }
}