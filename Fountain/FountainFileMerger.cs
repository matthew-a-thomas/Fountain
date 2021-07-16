namespace Fountain
{
    using System;
    using System.IO;

    class FountainFileMerger
    {
        readonly OverviewReader _overviewReader;

        public FountainFileMerger(
            OverviewReader overviewReader)
        {
            _overviewReader = overviewReader;
        }

        public void Merge(string fountain)
        {
            using var file = new FileStream(fountain, FileMode.Open, FileAccess.ReadWrite, FileShare.Read);
            if (!_overviewReader.TryReadOverview(file, out var overview))
                throw new Exception("Not a fountain file");
            var blockSize = PackedCoefficients.GetNumPackedBytes(FountainFileMath.GetNumCoefficients(overview.FileSize, overview.RowSize)) + overview.RowSize;
            file.Position = (file.Length - FountainFileMath.GetOverviewSize()) / blockSize * blockSize + FountainFileMath.GetOverviewSize();
            foreach (var otherFileName in Directory.EnumerateFiles(Path.GetDirectoryName(file.Name)!))
            {
                if (file.Name == otherFileName)
                    continue;
                FileStream otherFile;
                try
                {
                    otherFile = new FileStream(otherFileName, FileMode.Open, FileAccess.Read, FileShare.None);
                }
                catch
                {
                    continue;
                }
                using (otherFile)
                {
                    if (!_overviewReader.TryReadOverview(otherFile, out var otherOverview))
                        continue;
                    if (!Overview.AreEqual(ref overview, ref otherOverview))
                        continue;
                    Console.WriteLine($"Merging in {otherFileName}...");
                    otherFile.CopyTo(file);
                    var newLength = (file.Length - FountainFileMath.GetOverviewSize()) / blockSize * blockSize + FountainFileMath.GetOverviewSize();
                    file.Position = newLength;
                    file.SetLength(newLength);
                    otherFile.Close();
                    File.Delete(otherFileName);
                }
            }
        }
    }
}