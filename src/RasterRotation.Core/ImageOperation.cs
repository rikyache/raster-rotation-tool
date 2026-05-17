using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace RasterRotation.Core;

public abstract class ImageOperation
{
    public abstract string Name { get; }

    public virtual double AngleDelta => 0;

    public abstract BitmapSource Apply(IImageTransformer transformer, BitmapSource source);
}

public sealed class CropOperation : ImageOperation
{
    public CropOperation(CropRegion region)
    {
        Region = region;
    }

    public CropRegion Region { get; }

    public override string Name => "Crop";

    public override BitmapSource Apply(IImageTransformer transformer, BitmapSource source)
    {
        return transformer.Crop(source, Region);
    }
}

public enum FlipDirection
{
    Horizontal,
    Vertical
}

public sealed class FlipOperation : ImageOperation
{
    public FlipOperation(FlipDirection direction)
    {
        Direction = direction;
    }

    public FlipDirection Direction { get; }

    public override string Name => Direction == FlipDirection.Horizontal ? "Flip horizontal" : "Flip vertical";

    public override BitmapSource Apply(IImageTransformer transformer, BitmapSource source)
    {
        return Direction == FlipDirection.Horizontal
            ? transformer.FlipHorizontal(source)
            : transformer.FlipVertical(source);
    }
}

public sealed class RotateOperation : ImageOperation
{
    public RotateOperation(double degrees, Color background)
    {
        Degrees = degrees;
        Background = background;
    }

    public double Degrees { get; }

    public Color Background { get; }

    public override string Name => $"Rotate {Degrees:0.##}";

    public override double AngleDelta => Degrees;

    public override BitmapSource Apply(IImageTransformer transformer, BitmapSource source)
    {
        var rightAngle = NormalizeRightAngle(Degrees);
        if (rightAngle is -90 or 90 or 180 or 270)
        {
            return transformer.RotateRightAngle(source, rightAngle);
        }

        return Math.Abs(Degrees) < 0.0001
            ? source
            : transformer.RotateFree(source, Degrees, Background);
    }

    private static int NormalizeRightAngle(double degrees)
    {
        if (Math.Abs(degrees % 90) > 0.0001)
        {
            return 0;
        }

        var normalized = (int)(degrees % 360);
        return normalized switch
        {
            -270 => 90,
            270 => -90,
            360 or -360 or 0 => 0,
            _ => normalized
        };
    }
}
