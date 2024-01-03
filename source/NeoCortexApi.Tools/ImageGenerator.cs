using Microsoft.Extensions.Logging;
using SkiaSharp;

namespace NeoCortexApi.Tools;

public class ImageGenerator
{
    private static readonly string FolderPath = "./ImageWithLines";
    private readonly ILogger<ImageGenerator> _logger;

    public ImageGenerator(ILogger<ImageGenerator> logger)
    {
        _logger = logger;
    }
    
    private static async Task GenerateImage(string filePath, int width, int height, int[][] data)
    {
        var bitmap = new SKBitmap(width, height);

        for (int rowNumber = 0; rowNumber < height; rowNumber++)
        {
            for (int columnNumber = 0; columnNumber < width; columnNumber++)
            {
                var value= data[rowNumber][columnNumber];
                
                // x-axis is columnNumber
                // y-axis is rowNumber
                bitmap.SetPixel(columnNumber, rowNumber , new SKColor(
                    (byte)value, 
                    (byte)value, 
                    (byte)value));
            }
        }

        using var skData = bitmap.Encode(SKEncodedImageFormat.Png, 80);
        await using var stream = File.OpenWrite(filePath);
        skData.SaveTo(stream);
    }

    public async Task CreateImagesWithLine(int numberOfImages = 1)
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
                RowPositionInPercent = random.Next(1, 60),
                ColumnPositionInPercent = random.Next(1, 60),
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

    private async Task CreateHorizontalImage(string fileName, ImageWithLine imageWithLine)
    {
        await Task.Yield();
        var width = imageWithLine.Width;
        var height = imageWithLine.Height;

        var lineThicknessInPercent = imageWithLine.LineThicknessInPercent;
        var lineLengthInPercent = imageWithLine.LineLengthInPercent;
        var rowPositionInPercent = imageWithLine.RowPositionInPercent;
        var columnPositionInPercent = imageWithLine.ColumnPositionInPercent;
        var useJitter = imageWithLine.useJitter;
        
        var rowStartPosition = (rowPositionInPercent * height) / 100;
        var rowEndPosition = rowStartPosition + (lineThicknessInPercent * height) / 100;
        if(rowEndPosition > height)
        {
            rowEndPosition = height;
        }
        var jitterHeight = (rowEndPosition - rowStartPosition) / 3;
        
        var columnStartPosition = (columnPositionInPercent * width) / 100;
        var columnEndPosition = columnStartPosition + (lineLengthInPercent * width) / 100;
        if(columnEndPosition > width)
        {
            columnEndPosition = width;
        }

        var jitterWidth = width / 10;
        var performJitter = new bool[width];
        if(useJitter)
        {
            
            for (int i = 0; i < width; i += jitterWidth)
            {
                var doJitter = (new Random()).Next(0, 2) == 1;
                for (int j = 0; j < jitterWidth; j++)
                {
                    try
                    {
                        performJitter[i + j] = doJitter;
                    }
                    catch (Exception e)
                    {
                        _logger.LogError(e, "jitter array - i: {I}, j: {J}", i, j);
                        throw;
                    }
                    
                }
            }
        }
        
        var data = new int [height][];
        for (int rowNumber = 0; rowNumber < height; rowNumber++)
        {
            data[rowNumber] = new int[width];
        }
        
        for (int rowNumber = 0; rowNumber < height; rowNumber++)
        {
            for (int columnNumber = 0; columnNumber < width; columnNumber++)
            {
                var value = 
                    rowStartPosition <= rowNumber &&
                    rowNumber <= rowEndPosition &&
                    columnStartPosition <= columnNumber &&
                    columnNumber <= columnEndPosition
                        ? 255
                        : 0;

                var newRowIndex = rowNumber;
                if (performJitter[columnNumber])
                {
                    newRowIndex = rowNumber + jitterHeight;
                    if (newRowIndex >= height)
                    {
                        newRowIndex = height - 1;
                    }
                    else if (newRowIndex < 0)
                    {
                        newRowIndex = 0;
                    }
                }

                try
                {
                    data[newRowIndex][columnNumber] = value;
                }
                catch (Exception e)
                {
                    _logger.LogError(e, "data array - newRowIndex: {NewRowIndex}, columnNumber: {ColumnNumber}", newRowIndex, columnNumber);
                    throw;
                }
                
            }
        }

        await GenerateImage(Path.Combine(FolderPath, $"{fileName}.png"), width, height, data);
    }
}
