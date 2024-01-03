using SkiaSharp;

namespace NeoCortexApi.Tools;

public class ImageGenerator
{
    private static readonly string FolderPath = "./ImageWithLines";

    private static async Task GenerateImage(string filePath, int width, int height, int[][] data)
    {
        var bitmap = new SKBitmap(width, height);

        for (int rowNumber = 0; rowNumber < width; rowNumber++)
        {
            for (int columnNumber = 0; columnNumber < height; columnNumber++)
            {
                var value= data[rowNumber][columnNumber];
                bitmap.SetPixel(columnNumber, rowNumber, new SKColor(
                    (byte)value, 
                    (byte)value, 
                    (byte)value));
            }
        }

        using var skData = bitmap.Encode(SKEncodedImageFormat.Png, 80);
        await using var stream = File.OpenWrite(filePath);
        skData.SaveTo(stream);
    }

    public static async Task CreateImageWithLines(int numberOfImages = 1)
    {
        if(Directory.Exists(FolderPath))
        {
            Directory.Delete(FolderPath, true);
        }
        Directory.CreateDirectory(FolderPath);

        var imageWithLines = new Dictionary<string, ImageWithLine>();

        for (int i = 0; i < numberOfImages; i++)
        {
            var fileName = $"HorizontalLine_{i}";
            var random = new Random();
            imageWithLines.Add(fileName, new ImageWithLine
            {
                Width = 500,
                Height = 500,
                LineThicknessInPercent = random.Next(1, 10),
                LineXAxisPositionInPercent = random.Next(1, 60),
                LineYAxisPositionInPercent = random.Next(1, 60),
                LineLengthInPercent = random.Next(40, 100),
                useJitter = random.Next(0, 2) == 1
            });
        };
        
        foreach (var (fileName, imageWithLine) in imageWithLines)
        {
            if (fileName.Contains("Horizontal"))
            {
                await CreateHorizontalImage(fileName, imageWithLine);
            }
        }
    }

    private static async Task CreateHorizontalImage(string fileName, ImageWithLine imageWithLine)
    {
        await Task.Yield();
        var width = imageWithLine.Width;
        var height = imageWithLine.Height;

        var lineThicknessInPercent = imageWithLine.LineThicknessInPercent;
        var lineLengthInPercent = imageWithLine.LineLengthInPercent;
        var lineXAxisPositionInPercent = imageWithLine.LineXAxisPositionInPercent;
        var lineYAxisPositionInPercent = imageWithLine.LineYAxisPositionInPercent;
        var useJitter = imageWithLine.useJitter;
        
        var lineXAxisStartPosition = (lineXAxisPositionInPercent * height) / 100;
        var lineXAxisEndPosition = lineXAxisStartPosition + (lineThicknessInPercent * height) / 100;
        if(lineXAxisEndPosition > width)
        {
            lineXAxisEndPosition = width;
        }
        
        var lineYAxisStartPosition = (lineYAxisPositionInPercent * width) / 100;
        var lineYAxisEndPosition = lineYAxisStartPosition + (lineLengthInPercent * width) / 100;
        if(lineYAxisEndPosition > height)
        {
            lineYAxisEndPosition = height;
        }

        var jitterValues = new bool[height];
        if(useJitter)
        {
            var batchSize = 10;
            for (int i = 0; i < height; i += batchSize)
            {
                var performJitter = (new Random()).Next(0, 2) == 1;
                for (int j = 0; j < batchSize; j++)
                {
                    jitterValues[i + j] = performJitter;
                }
            }
        }
        
        var data = new int [width][];
        for (int i = 0; i < width; i++)
        {
            data[i] = new int[height];
        }
        
        for (int i = 0; i < width; i++)
        {
            for (int j = 0; j < height; j++)
            {
                var value = 
                    lineXAxisStartPosition <= i &&
                    i <= lineXAxisEndPosition &&
                    lineYAxisStartPosition <= j &&
                    j <= lineYAxisEndPosition
                        ? 255
                        : 0;

                var jitterValue = 0;
                if (value == 255 && jitterValues[j])
                {
                    if (i % 15 == 0)
                    {
                        jitterValue = lineXAxisEndPosition - lineXAxisStartPosition;
                    }
                    if (i % 30 == 0)
                    {
                        jitterValue = lineXAxisStartPosition - lineXAxisEndPosition;
                    }
                }
                
                data[i + jitterValue][j] = value;
            }
        }

        await GenerateImage(Path.Combine(FolderPath, $"{fileName}.png"), width, height, data);
    }
}
