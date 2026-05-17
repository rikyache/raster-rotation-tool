using System.Windows.Media.Imaging;

namespace RasterRotation.Core;

public sealed class ImageDocument
{
    public ImageDocument(string sourcePath, BitmapSource bitmap, ImageMetadata metadata)
    {
        SourcePath = sourcePath;
        Bitmap = bitmap;
        OriginalMetadata = metadata;
        CurrentAngle = 0;
    }

    public event EventHandler? Changed;

    public string SourcePath { get; }

    public BitmapSource Bitmap { get; private set; }

    public ImageMetadata OriginalMetadata { get; }

    public double CurrentAngle { get; private set; }

    public ImageMetadata CurrentMetadata => new(
        Bitmap.PixelWidth,
        Bitmap.PixelHeight,
        Bitmap.DpiX,
        Bitmap.DpiY,
        OriginalMetadata.Format);

    public void ReplaceBitmap(BitmapSource bitmap, double angleDelta = 0)
    {
        Bitmap = bitmap;
        CurrentAngle = NormalizeAngle(CurrentAngle + angleDelta);
        Changed?.Invoke(this, EventArgs.Empty);
    }

    public void ResetAngle()
    {
        CurrentAngle = 0;
        Changed?.Invoke(this, EventArgs.Empty);
    }

    private static double NormalizeAngle(double angle)
    {
        var normalized = angle % 360;
        return normalized < 0 ? normalized + 360 : normalized;
    }
}
