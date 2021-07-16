namespace Fountain
{
    using System;
    using System.IO;
    using System.IO.MemoryMappedFiles;
    using System.Linq;
    using System.Reflection;
    using System.Runtime.InteropServices;
    using Matt.MemoryMappedFiles;

    class App
    {
        readonly CommandLineArgs _args;
        readonly CommandLineParser _commandLineParser;
        readonly FountainFileDecoder _decoder;
        readonly FountainFileEncoder _encoder;
        readonly FountainFileMerger _merger;
        readonly FountainFileShrinker _shrinker;
        readonly FountainFileInfoProvider _infoProvider;

        public App(
            CommandLineArgs args,
            CommandLineParser commandLineParser,
            FountainFileDecoder decoder,
            FountainFileEncoder encoder,
            FountainFileMerger merger,
            FountainFileShrinker shrinker,
            FountainFileInfoProvider infoProvider)
        {
            _args = args;
            _commandLineParser = commandLineParser;
            _decoder = decoder;
            _encoder = encoder;
            _merger = merger;
            _shrinker = shrinker;
            _infoProvider = infoProvider;
        }

        static void PrintUsage()
        {
            var exeName = Path.GetFileName(Assembly.GetEntryAssembly()?.Location);
            Console.Error.WriteLine(@$"Fountain Codes {Assembly.GetExecutingAssembly().GetName().Version}
Usage:
{exeName} --encode <filename> <output> [--slice=<n>] [--rows=<n>] [--systematic=(true|false)] [--percent=5]
    Encodes the given <filename> as a .fountain file
{exeName} --merge <fountain>
    Merges all compatible .fountain files in the same directory as <fountain> into <fountain>
{exeName} --shrink <fountain>
    Shrinks the <fountain> .fountain file if possible
{exeName} --decode <fountain> <output>
    Decodes the <fountain> .fountain file if possible
{exeName} --info <fountain> [<fountain> ...]
    Prints information about the given fountain files");
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
                if (fileNames.Count == 2)
                {
                    var systematic =
                        args.TryGetValue("--systematic", out var systematicString)
                        && bool.TryParse(systematicString, out var x)
                        && x;

                    uint? rowSize = null;
                    if (args.TryGetValue("--slice", out var coeffString))
                    {
                        if (uint.TryParse(coeffString, out var parsedRowSize))
                            rowSize = parsedRowSize;
                        else
                            throw new Exception("--slice needs to be an unsigned 32 bit integer");
                    }

                    ushort? numRows = null;
                    if (args.TryGetValue("--rows", out var rowsString))
                    {
                        if (ushort.TryParse(rowsString, out var parsedNumRows))
                            numRows = parsedNumRows;
                        else
                            throw new Exception("--rows needs to be an unsigned 16 bit integer");
                    }

                    double? percent = null;
                    if (args.TryGetValue("--percent", out var percentString))
                    {
                        if (double.TryParse(percentString, out var parsedPercent))
                            percent = parsedPercent / 100.0;
                        else
                            throw new Exception("--percent needs to be a number");
                    }

                    string filename = fileNames[0];
                    FailIfNotExists(filename);

                    _encoder.Encode(
                        filename,
                        systematic,
                        rowSize,
                        numRows,
                        percent,
                        fileNames[1]);
                }
                else
                {
                    PrintUsage();
                }
            }
            else if (args.ContainsKey("--merge"))
            {
                if (fileNames.Count == 1)
                {
                    string fountain = fileNames[0];
                    FailIfNotExists(fountain);
                    FailIfNotFountain(fountain);
                    _merger.Merge(fountain);
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
                    string fountain = fileNames[0];
                    FailIfNotExists(fountain);
                    FailIfNotFountain(fountain);
                    _shrinker.Shrink(fountain);
                }
                else
                {
                    PrintUsage();
                }
            }
            else if (args.ContainsKey("--decode"))
            {
                if (fileNames.Count == 2)
                {
                    string fountain = fileNames[0];
                    FailIfNotExists(fountain);
                    FailIfNotFountain(fountain);
                    _decoder.Decode(fountain, fileNames[1]);
                }
                else
                {
                    PrintUsage();
                }
            }
            else if (args.ContainsKey("--info"))
            {
                if (fileNames.Count > 0)
                {
                    _infoProvider.PrintInfo(fileNames);
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
            if (file.Length < FountainFileMath.GetOverviewSize())
                throw new Exception($"{fountain} is not a fountain file");
            using var memoryManager = MemoryMappedFileHelper.CreateMemoryManager(file, MemoryMappedFileAccess.Read, out _, 0, FountainFileMath.GetOverviewSize(), true);
            ref readonly var overview = ref MemoryMarshal.AsRef<Overview>(memoryManager.GetSpan());
            if (!Overview.IsMagic(in overview))
                throw new Exception($"{fountain} is not a fountain file");
        }
    }
}