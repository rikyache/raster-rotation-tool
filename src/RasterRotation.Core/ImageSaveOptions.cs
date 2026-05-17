namespace RasterRotation.Core;

public sealed class ImageSaveOptions
{
    public int JpegQuality { get; init; } = 92;

    public bool PreserveFormat { get; init; } = true;
}
