using System.Windows.Media.Imaging;

namespace RasterRotation.Core;

public interface IImageTransformer
{
    BitmapSource Crop(BitmapSource source, CropRegion region);

    BitmapSource FlipHorizontal(BitmapSource source);

    BitmapSource FlipVertical(BitmapSource source);

    BitmapSource RotateRightAngle(BitmapSource source, int degrees);

    BitmapSource RotateFree(BitmapSource source, double degrees, System.Windows.Media.Color background);
}
