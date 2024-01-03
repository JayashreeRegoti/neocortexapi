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
            var random = new Random();

            var fileName = random.Next(0, 2) switch
            {
                0 => $"HorizontalLine_{i}",
                1 => $"VerticalLine_{i}",
                _ => $"HorizontalLine_{i}"
            };

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
        }
        
        foreach ((string fileName, ImageWithLine imageWithLine) in imageWithLines)
        {
            if (fileName.Contains("Horizontal"))
            {
                await CreateHorizontalLineImage(fileName, imageWithLine);
            }
            if (fileName.Contains("Vertical"))
            {
                await CreateVerticalLineImage(fileName, imageWithLine);
            }
        }
    }

    private async Task CreateHorizontalLineImage(string fileName, ImageWithLine imageWithLine)
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

                var newRowNumber = rowNumber;
                if (performJitter[columnNumber])
                {
                    newRowNumber = rowNumber + jitterHeight;
                    if (newRowNumber >= height)
                    {
                        newRowNumber = height - 1;
                    }
                    else if (newRowNumber < 0)
                    {
                        newRowNumber = 0;
                    }
                }

                try
                {
                    data[newRowNumber][columnNumber] = value;
                }
                catch (Exception e)
                {
                    _logger.LogError(e, "data array - newRowNumber: {NewRowNumber}, columnNumber: {ColumnNumber}", newRowNumber, columnNumber);
                    throw;
                }
                
            }
        }

        await GenerateImage(Path.Combine(FolderPath, $"{fileName}.png"), width, height, data);
    }
    
    private async Task CreateVerticalLineImage(string fileName, ImageWithLine imageWithLine)
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
        var rowEndPosition = rowStartPosition + (lineLengthInPercent * height) / 100;
        if(rowEndPosition > height)
        {
            rowEndPosition = height;
        }
        
        var columnStartPosition = (columnPositionInPercent * width) / 100;
        var columnEndPosition = columnStartPosition + (lineThicknessInPercent * width) / 100;
        if(columnEndPosition > width)
        {
            columnEndPosition = width;
        }
        
        var jitterWidth = (columnEndPosition - columnStartPosition) / 3;

        var jitterHeight = height / 10;
        var performJitter = new bool[height];
        if(useJitter)
        {
            
            for (int i = 0; i < width; i += jitterHeight)
            {
                var doJitter = (new Random()).Next(0, 2) == 1;
                for (int j = 0; j < jitterHeight; j++)
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

                var newColumnNumber = columnNumber;
                if (performJitter[rowNumber])
                {
                    newColumnNumber = columnNumber + jitterWidth;
                    if (newColumnNumber >= width)
                    {
                        newColumnNumber = width - 1;
                    }
                    else if (newColumnNumber < 0)
                    {
                        newColumnNumber = 0;
                    }
                }

                try
                {
                    data[rowNumber][newColumnNumber] = value;
                }
                catch (Exception e)
                {
                    _logger.LogError(e, "data array - rowNumber: {RowNumber}, newColumnNumber: {NewColumnNumber}", rowNumber, newColumnNumber);
                    throw;
                }
                
            }
        }

        await GenerateImage(Path.Combine(FolderPath, $"{fileName}.png"), width, height, data);
    }
}
