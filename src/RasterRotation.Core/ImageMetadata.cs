namespace RasterRotation.Core;

public sealed class ImageMetadata
{
    public ImageMetadata(int width, int height, double dpiX, double dpiY, ImageFormatInfo format)
    {
        Width = width;
        Height = height;
        DpiX = dpiX;
        DpiY = dpiY;
        Format = format;
    }

    public int Width { get; }

    public int Height { get; }

    public double DpiX { get; }

    public double DpiY { get; }

    public ImageFormatInfo Format { get; }
}
