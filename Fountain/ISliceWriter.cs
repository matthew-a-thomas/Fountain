namespace Fountain
{
    using System;

    public interface ISliceWriter<in TMedia>
    {
        void Write(
            TMedia media,
            ReadOnlySpan<bool> coefficients,
            ReadOnlySpan<byte> data);
    }
}