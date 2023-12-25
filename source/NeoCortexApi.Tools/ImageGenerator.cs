using SkiaSharp;

namespace NeoCortexApi.Tools;

public class ImageGenerator
{
    private static readonly string FolderPath = "./TestData";
    public static async Task GenerateImage(string filePath, int width, int height, int[] data)
    {
        var bitmap = new SKBitmap(width, height);

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                bitmap.SetPixel(x, y, new SKColor(
                    (byte)data[x + y + (x * width)], 
                    (byte)data[x + y + (x * width)], 
                    (byte)data[x + y + (x * width)]));
            }
        }

        using var skData = bitmap.Encode(SKEncodedImageFormat.Png, 80);
        await using var stream = File.OpenWrite(filePath);
        skData.SaveTo(stream);
    }

    public static async Task CreateHorizontalImage()
    {
        await Task.Yield();
        var width = 100;
        var height = 100;

        var lineThicknessInPercent = 10;
        var lineLengthInPercent = 100;
        var lineXAxisPositionInPercent = 50;
        var lineYAxisPositionInPercent = 0;
        var jitterInPixel = 2;
        
        var lineXAxisStartPosition = (lineXAxisPositionInPercent * width) / 100;
        var lineXAxisEndPosition = (lineXAxisPositionInPercent + lineThicknessInPercent * width) / 100;
        
        var lineYAxisStartPosition = (lineYAxisPositionInPercent * height) / 100;
        var lineYAxisEndPosition = (lineYAxisPositionInPercent + lineLengthInPercent * width) / 100;
        if(lineYAxisEndPosition > height)
        {
            lineYAxisEndPosition = height;
        }
        
        var data = new int[width * height];
        for (int i = 0; i < width; i++)
        {
            for (int j = 0; j < height; j++)
            {
                var index = i + j + (i * width);
                data[index] =
                    lineXAxisStartPosition <= i &&
                    i <= lineXAxisEndPosition &&
                    lineYAxisStartPosition <= j &&
                    j <= lineYAxisEndPosition
                        ? 255
                        : 0;
            }
        }
        
        await GenerateImage(Path.Combine(FolderPath, "HorizontalLine.png"), width, height, data);
    }
}
