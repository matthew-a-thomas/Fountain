namespace Fountain
{
    using System.IO;
    using System.Runtime.InteropServices;

    class OverviewReader
    {
        public bool TryReadOverview(Stream stream, out Overview overview)
        {
            overview = default;
            if (stream.Length < FountainFileMath.GetOverviewSize())
                return false;
            var overviewSpan = MemoryMarshal.AsBytes(MemoryMarshal.CreateSpan(ref overview, 1));
            return stream.Read(overviewSpan) == overviewSpan.Length && Overview.IsMagic(in overview);
        }
    }
}