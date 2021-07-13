namespace Fountain
{
    using System;
    using System.Diagnostics.CodeAnalysis;
    using System.IO;

    public sealed class SliceWriter : ISliceWriter<FileStream>
    {
        [SuppressMessage("ReSharper", "ForCanBeConvertedToForeach")]
        public void Write(
            FileStream media,
            ReadOnlySpan<bool> coefficients,
            ReadOnlySpan<byte> data)
        {
            media.SetLength(coefficients.Length + data.Length);
            media.Position = 0;
            for (var i = 0; i < coefficients.Length; ++i)
            {
                media.WriteByte(coefficients[i] ? byte.MaxValue : byte.MinValue);
            }
            media.Write(data);
        }
    }
}