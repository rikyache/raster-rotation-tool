namespace RasterRotation.Core;

public interface IImageFileService
{
    ImageDocument Load(string filePath);

    void Save(ImageDocument document, string outputPath, ImageSaveOptions? options = null);
}
