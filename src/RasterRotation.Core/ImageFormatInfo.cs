using System.Windows.Media.Imaging;
using System.IO;

namespace RasterRotation.Core;

public sealed class ImageFormatInfo
{
    public ImageFormatInfo(string displayName, string extension, Guid containerFormat)
    {
        DisplayName = displayName;
        Extension = extension;
        ContainerFormat = containerFormat;
    }

    public string DisplayName { get; }

    public string Extension { get; }

    public Guid ContainerFormat { get; }

    public static ImageFormatInfo FromDecoder(BitmapDecoder decoder, string filePath)
    {
        var codecName = decoder.CodecInfo?.FriendlyName ?? "Unknown image";
        var extension = Path.GetExtension(filePath);
        var container = decoder.CodecInfo?.ContainerFormat ?? Guid.Empty;

        return new ImageFormatInfo(codecName, extension, container);
    }
}
