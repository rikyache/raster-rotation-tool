namespace RasterRotation.Wpf;

public sealed class AppSettings
{
    public string Theme { get; set; } = "Light";

    public string Background { get; set; } = "Neutral";

    public bool ShowGrid { get; set; }

    public int JpegQuality { get; set; } = 92;
}
