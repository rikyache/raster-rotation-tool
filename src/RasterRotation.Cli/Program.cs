using System.Globalization;
using System.IO;
using System.Windows.Media;
using RasterRotation.Core;

namespace RasterRotation.Cli;

internal static class Program
{
    private static readonly IImageFileService FileService = new WicImageFileService();
    private static readonly IImageTransformer Transformer = new WicImageTransformer();

    public static int Main(string[] args)
    {
        try
        {
            var options = CliOptions.Parse(args);
            if (options.ShowHelp)
            {
                PrintHelp();
                return 0;
            }

            if (string.IsNullOrWhiteSpace(options.InputPath))
            {
                Console.Error.WriteLine("Input image path is required.");
                PrintHelp();
                return 2;
            }

            var document = FileService.Load(options.InputPath);

            if (options.ShowInfo)
            {
                PrintInfo(document);
            }

            ApplyOperations(document, options);

            if (options.HasMutations)
            {
                var outputPath = options.ResolveOutputPath();
                var savedPath = Save(document, outputPath, options.Quality);
                Console.WriteLine($"Saved: {savedPath}");
            }

            return 0;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine(ex.Message);
            return 1;
        }
    }

    private static void ApplyOperations(ImageDocument document, CliOptions options)
    {
        var operations = new List<ImageOperation>();

        if (options.Crop is { } crop)
        {
            operations.Add(new CropOperation(crop));
        }

        if (options.FlipHorizontal)
        {
            operations.Add(new FlipOperation(FlipDirection.Horizontal));
        }

        if (options.FlipVertical)
        {
            operations.Add(new FlipOperation(FlipDirection.Vertical));
        }

        if (options.RotateDegrees is { } degrees)
        {
            operations.Add(new RotateOperation(degrees, Colors.White));
        }

        foreach (var operation in operations)
        {
            document.ReplaceBitmap(operation.Apply(Transformer, document.Bitmap), operation.AngleDelta);
            Console.WriteLine($"Applied: {operation.Name}");
        }
    }

    private static string Save(ImageDocument document, string outputPath, int quality)
    {
        outputPath = EnsureOriginalExtension(outputPath, document);
        var fullOutput = Path.GetFullPath(outputPath);
        var fullInput = Path.GetFullPath(document.SourcePath);

        if (string.Equals(fullOutput, fullInput, StringComparison.OrdinalIgnoreCase))
        {
            var tempPath = Path.Combine(Path.GetDirectoryName(fullOutput)!, Path.GetFileNameWithoutExtension(fullOutput) + ".tmp" + Path.GetExtension(fullOutput));
            FileService.Save(document, tempPath, new ImageSaveOptions { JpegQuality = quality });
            File.Copy(tempPath, fullOutput, overwrite: true);
            File.Delete(tempPath);
            return fullOutput;
        }

        FileService.Save(document, fullOutput, new ImageSaveOptions { JpegQuality = quality });
        return fullOutput;
    }

    private static string EnsureOriginalExtension(string outputPath, ImageDocument document)
    {
        var originalExtension = document.OriginalMetadata.Format.Extension;
        if (string.IsNullOrWhiteSpace(originalExtension))
        {
            return outputPath;
        }

        var currentExtension = Path.GetExtension(outputPath);
        return string.Equals(currentExtension, originalExtension, StringComparison.OrdinalIgnoreCase)
            ? outputPath
            : Path.ChangeExtension(outputPath, originalExtension);
    }

    private static void PrintInfo(ImageDocument document)
    {
        var metadata = document.CurrentMetadata;
        Console.WriteLine($"File: {document.SourcePath}");
        Console.WriteLine($"Size: {metadata.Width} x {metadata.Height}");
        Console.WriteLine($"DPI: {metadata.DpiX:0.##} x {metadata.DpiY:0.##}");
        Console.WriteLine($"Format: {metadata.Format.DisplayName} ({metadata.Format.Extension})");
    }

    private static void PrintHelp()
    {
        Console.WriteLine("""
RasterRotation.Cli

Usage:
  RasterRotation.Cli.exe <input> [options]

Options:
  --output <path>       Save result to path. Default: <input>_edited.<ext>
  --overwrite           Save over input file.
  --rotate <degrees>    Rotate by -90, 90, 180 or any free angle.
  --flip <h|v|hv>       Flip horizontally, vertically, or both.
  --crop <x,y,w,h>      Crop rectangle in source pixels.
  --quality <1-100>     JPEG quality for saving. Default: 92.
  --info                Print image information.
  --help                Show this help.

Examples:
  RasterRotation.Cli.exe photo.jpg --rotate 90 --overwrite
  RasterRotation.Cli.exe photo.png --crop 20,20,640,480 --flip h --output photo_crop.png
  RasterRotation.Cli.exe scan.tiff --rotate -3.5 --quality 95 --output scan_fixed.tiff
""");
    }

    private sealed class CliOptions
    {
        public string? InputPath { get; private init; }

        public string? OutputPath { get; private init; }

        public bool Overwrite { get; private init; }

        public double? RotateDegrees { get; private init; }

        public bool FlipHorizontal { get; private init; }

        public bool FlipVertical { get; private init; }

        public CropRegion? Crop { get; private init; }

        public int Quality { get; private init; } = 92;

        public bool ShowInfo { get; private init; }

        public bool ShowHelp { get; private init; }

        public bool HasMutations => RotateDegrees.HasValue || FlipHorizontal || FlipVertical || Crop.HasValue;

        public string ResolveOutputPath()
        {
            if (Overwrite)
            {
                return InputPath!;
            }

            if (!string.IsNullOrWhiteSpace(OutputPath))
            {
                return OutputPath;
            }

            var directory = Path.GetDirectoryName(InputPath!);
            var name = Path.GetFileNameWithoutExtension(InputPath!);
            var extension = Path.GetExtension(InputPath!);
            return Path.Combine(string.IsNullOrWhiteSpace(directory) ? "." : directory, $"{name}_edited{extension}");
        }

        public static CliOptions Parse(string[] args)
        {
            var inputPath = default(string);
            var outputPath = default(string);
            var overwrite = false;
            var rotate = default(double?);
            var flipH = false;
            var flipV = false;
            var crop = default(CropRegion?);
            var quality = 92;
            var info = false;
            var help = args.Length == 0;

            for (var index = 0; index < args.Length; index++)
            {
                var arg = args[index];
                switch (arg.ToLowerInvariant())
                {
                    case "--help" or "-h" or "/?":
                        help = true;
                        break;
                    case "--output" or "-o":
                        outputPath = Next(args, ref index, arg);
                        break;
                    case "--overwrite":
                        overwrite = true;
                        break;
                    case "--rotate" or "-r":
                        rotate = double.Parse(Next(args, ref index, arg).Replace(',', '.'), CultureInfo.InvariantCulture);
                        break;
                    case "--flip" or "-f":
                        var flip = Next(args, ref index, arg).ToLowerInvariant();
                        flipH = flip.Contains('h');
                        flipV = flip.Contains('v');
                        break;
                    case "--crop" or "-c":
                        crop = ParseCrop(Next(args, ref index, arg));
                        break;
                    case "--quality" or "-q":
                        quality = Math.Clamp(int.Parse(Next(args, ref index, arg), CultureInfo.InvariantCulture), 1, 100);
                        break;
                    case "--info" or "-i":
                        info = true;
                        break;
                    default:
                        if (arg.StartsWith("-", StringComparison.Ordinal))
                        {
                            throw new ArgumentException($"Unknown option: {arg}");
                        }

                        inputPath ??= arg;
                        break;
                }
            }

            return new CliOptions
            {
                InputPath = inputPath,
                OutputPath = outputPath,
                Overwrite = overwrite,
                RotateDegrees = rotate,
                FlipHorizontal = flipH,
                FlipVertical = flipV,
                Crop = crop,
                Quality = quality,
                ShowInfo = info,
                ShowHelp = help
            };
        }

        private static string Next(string[] args, ref int index, string option)
        {
            if (index + 1 >= args.Length)
            {
                throw new ArgumentException($"Option {option} requires a value.");
            }

            index++;
            return args[index];
        }

        private static CropRegion ParseCrop(string value)
        {
            var parts = value.Split(',', StringSplitOptions.TrimEntries);
            if (parts.Length != 4)
            {
                throw new ArgumentException("Crop must be in x,y,width,height format.");
            }

            return new CropRegion(
                int.Parse(parts[0], CultureInfo.InvariantCulture),
                int.Parse(parts[1], CultureInfo.InvariantCulture),
                int.Parse(parts[2], CultureInfo.InvariantCulture),
                int.Parse(parts[3], CultureInfo.InvariantCulture));
        }
    }
}
