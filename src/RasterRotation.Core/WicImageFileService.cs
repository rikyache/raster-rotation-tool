using System.IO;
using System.Windows.Media.Imaging;

namespace RasterRotation.Core;

public sealed class WicImageFileService : IImageFileService
{
    public ImageDocument Load(string filePath)
    {
        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException("Image file was not found.", filePath);
        }

        using var stream = File.OpenRead(filePath);
        var decoder = BitmapDecoder.Create(stream, BitmapCreateOptions.PreservePixelFormat, BitmapCacheOption.OnLoad);
        var frame = decoder.Frames[0];
        var bitmap = new WriteableBitmap(frame);
        bitmap.Freeze();

        var format = ImageFormatInfo.FromDecoder(decoder, filePath);
        var metadata = new ImageMetadata(bitmap.PixelWidth, bitmap.PixelHeight, bitmap.DpiX, bitmap.DpiY, format);
        return new ImageDocument(filePath, bitmap, metadata);
    }

    public void Save(ImageDocument document, string outputPath, ImageSaveOptions? options = null)
    {
        options ??= new ImageSaveOptions();

        var encoder = CreateEncoder(document.OriginalMetadata.Format, outputPath, options);
        encoder.Frames.Add(BitmapFrame.Create(document.Bitmap));

        var directory = Path.GetDirectoryName(Path.GetFullPath(outputPath));
        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        using var stream = File.Create(outputPath);
        encoder.Save(stream);
    }

    private static BitmapEncoder CreateEncoder(ImageFormatInfo originalFormat, string outputPath, ImageSaveOptions options)
    {
        if (options.PreserveFormat)
        {
            var preserved = TryCreateEncoder(originalFormat.ContainerFormat);
            if (preserved is not null)
            {
                ApplyQuality(preserved, options);
                return preserved;
            }

            var fallback = TryCreateKnownEncoder(originalFormat.Extension);
            if (fallback is not null)
            {
                ApplyQuality(fallback, options);
                return fallback;
            }

            throw new NotSupportedException($"No writable WIC encoder is available for {originalFormat.DisplayName}.");
        }

        var extension = Path.GetExtension(outputPath).ToLowerInvariant();
        var encoder = TryCreateKnownEncoder(extension) ?? new PngBitmapEncoder();

        ApplyQuality(encoder, options);
        return encoder;
    }

    private static BitmapEncoder? TryCreateKnownEncoder(string extension)
    {
        return extension.ToLowerInvariant() switch
        {
            ".jpg" or ".jpeg" or ".jfif" => new JpegBitmapEncoder(),
            ".tif" or ".tiff" => new TiffBitmapEncoder(),
            ".bmp" => new BmpBitmapEncoder(),
            ".gif" => new GifBitmapEncoder(),
            ".wdp" or ".jxr" => new WmpBitmapEncoder(),
            ".png" => new PngBitmapEncoder(),
            _ => null
        };
    }

    private static BitmapEncoder? TryCreateEncoder(Guid containerFormat)
    {
        if (containerFormat == Guid.Empty)
        {
            return null;
        }

        try
        {
            return BitmapEncoder.Create(containerFormat);
        }
        catch (Exception ex) when (ex is NotSupportedException or ArgumentException)
        {
            return null;
        }
    }

    private static void ApplyQuality(BitmapEncoder encoder, ImageSaveOptions options)
    {
        if (encoder is JpegBitmapEncoder jpeg)
        {
            jpeg.QualityLevel = Math.Clamp(options.JpegQuality, 1, 100);
        }
    }
}
