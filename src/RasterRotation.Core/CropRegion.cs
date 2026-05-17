namespace RasterRotation.Core;

public readonly record struct CropRegion(int X, int Y, int Width, int Height)
{
    public bool IsEmpty => Width <= 0 || Height <= 0;
}
