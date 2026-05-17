using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace RasterRotation.Core;

public sealed class WicImageTransformer : IImageTransformer
{
    public BitmapSource Crop(BitmapSource source, CropRegion region)
    {
        if (region.IsEmpty)
        {
            throw new ArgumentException("Crop region must have positive width and height.", nameof(region));
        }

        var x = Math.Clamp(region.X, 0, source.PixelWidth - 1);
        var y = Math.Clamp(region.Y, 0, source.PixelHeight - 1);
        var width = Math.Clamp(region.Width, 1, source.PixelWidth - x);
        var height = Math.Clamp(region.Height, 1, source.PixelHeight - y);

        var cropped = new CroppedBitmap(source, new Int32Rect(x, y, width, height));
        cropped.Freeze();
        return cropped;
    }

    public BitmapSource FlipHorizontal(BitmapSource source)
    {
        return RenderTransformed(source, new ScaleTransform(-1, 1, source.Width / 2, source.Height / 2), source.PixelWidth, source.PixelHeight);
    }

    public BitmapSource FlipVertical(BitmapSource source)
    {
        return RenderTransformed(source, new ScaleTransform(1, -1, source.Width / 2, source.Height / 2), source.PixelWidth, source.PixelHeight);
    }

    public BitmapSource RotateRightAngle(BitmapSource source, int degrees)
    {
        if (degrees is not (-90 or 90 or 180 or 270))
        {
            throw new ArgumentOutOfRangeException(nameof(degrees), "Only -90, 90, 180 and 270 degree rotations are supported here.");
        }

        var transform = new RotateTransform(degrees);
        var rotated = new TransformedBitmap(source, transform);
        rotated.Freeze();
        return rotated;
    }

    public BitmapSource RotateFree(BitmapSource source, double degrees, Color background)
    {
        var radians = Math.Abs(degrees) * Math.PI / 180.0;
        var sin = Math.Abs(Math.Sin(radians));
        var cos = Math.Abs(Math.Cos(radians));
        var sourceWidth = source.Width;
        var sourceHeight = source.Height;
        var targetWidth = sourceWidth * cos + sourceHeight * sin;
        var targetHeight = sourceWidth * sin + sourceHeight * cos;
        var targetPixelWidth = (int)Math.Ceiling(targetWidth * source.DpiX / 96.0);
        var targetPixelHeight = (int)Math.Ceiling(targetHeight * source.DpiY / 96.0);

        var visual = new DrawingVisual();
        using (var context = visual.RenderOpen())
        {
            context.DrawRectangle(new SolidColorBrush(background), null, new Rect(0, 0, targetWidth, targetHeight));
            context.PushTransform(new TranslateTransform(targetWidth / 2.0, targetHeight / 2.0));
            context.PushTransform(new RotateTransform(degrees));
            context.PushTransform(new TranslateTransform(-sourceWidth / 2.0, -sourceHeight / 2.0));
            context.DrawImage(source, new Rect(0, 0, sourceWidth, sourceHeight));
            context.Pop();
            context.Pop();
            context.Pop();
        }

        var bitmap = new RenderTargetBitmap(targetPixelWidth, targetPixelHeight, source.DpiX, source.DpiY, PixelFormats.Pbgra32);
        bitmap.Render(visual);
        bitmap.Freeze();
        return bitmap;
    }

    private static BitmapSource RenderTransformed(BitmapSource source, Transform transform, int pixelWidth, int pixelHeight)
    {
        var visual = new DrawingVisual();
        using (var context = visual.RenderOpen())
        {
            context.PushTransform(transform);
            context.DrawImage(source, new Rect(0, 0, source.Width, source.Height));
            context.Pop();
        }

        var bitmap = new RenderTargetBitmap(pixelWidth, pixelHeight, source.DpiX, source.DpiY, PixelFormats.Pbgra32);
        bitmap.Render(visual);
        bitmap.Freeze();
        return bitmap;
    }
}
