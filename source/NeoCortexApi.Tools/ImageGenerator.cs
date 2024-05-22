﻿using Microsoft.Extensions.Logging;
using SkiaSharp;

namespace NeoCortexApi.Tools;

public class ImageGenerator
{
        
    private readonly ILogger<ImageGenerator> _logger;

    public ImageGenerator(ILogger<ImageGenerator> logger)
    {
        _logger = logger;
    }
    
    public static async Task GenerateImage(string filePath, int width, int height, int[][] data)
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

    public async Task CreateImagesWithLine(string folderPath, int width, int height, int numberOfImages = 1)
    {
        if(Directory.Exists(folderPath))
        {
            Directory.Delete(folderPath, true);
        }
        Directory.CreateDirectory(folderPath);

        var imageWithLines = new Dictionary<string, ImageWithLine>();

        for (int i = 0; i < numberOfImages; i++)
        {

            var filePaths = new List<string>
            {
                Path.Combine(folderPath, $"HorizontalLine_{i}.png"),
                Path.Combine(folderPath, $"VerticalLine_{i}.png"),
                Path.Combine(folderPath, $"DiagonalLine_{i}.png"),
            };

            foreach (var filePath in filePaths)
            {
                var random = new Random();

                imageWithLines.Add(filePath, new ImageWithLine
                {
                    Width = width,
                    Height = height,
                    LineThicknessInPercent = 3,
                    RowPositionInPercent = random.Next(1, 60),
                    ColumnPositionInPercent = random.Next(1, 60),
                    LineLengthInPercent = random.Next(40, 100),
                    useJitter = random.Next(0, 2) == 1
                });
            }
        }
        
        foreach ((string filePath, ImageWithLine imageWithLine) in imageWithLines)
        {
            _logger.LogInformation("Creating image {FileName}", filePath);
            if (filePath.Contains("Horizontal"))
            {
                await CreateHorizontalLineImage(filePath, imageWithLine);
            }
            else if (filePath.Contains("Vertical"))
            {
                await CreateVerticalLineImage(filePath, imageWithLine);
            }
            else if (filePath.Contains("Diagonal"))
            {
                await CreateDiagonalLineImage(filePath, imageWithLine);
            }
        }
    }

    private async Task CreateHorizontalLineImage(string filePath, ImageWithLine imageWithLine)
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
        //var rowEndPosition = rowStartPosition + (lineThicknessInPercent * height) / 100;
        var rowEndPosition = rowStartPosition + 1;
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

        await GenerateImage(filePath, width, height, data);
    }
    
    private async Task CreateVerticalLineImage(string filePath, ImageWithLine imageWithLine)
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
        //var columnEndPosition = columnStartPosition + (lineThicknessInPercent * width) / 100;
        var columnEndPosition = columnStartPosition + 1;
        
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

        await GenerateImage(filePath, width, height, data);
    }
    
    private async Task CreateDiagonalLineImage(string filePath, ImageWithLine imageWithLine)
    {
        await Task.Yield();
        var width = imageWithLine.Width;
        var height = imageWithLine.Height;

        var lineThicknessInPercent = imageWithLine.LineThicknessInPercent;
        var lineLengthInPercent = imageWithLine.LineLengthInPercent;
        var rowPositionInPercent = imageWithLine.RowPositionInPercent;
        var columnPositionInPercent = imageWithLine.ColumnPositionInPercent;
        var useJitter = imageWithLine.useJitter;
        var lineThickness = 1;
        
        var rowStartPosition = (rowPositionInPercent * height) / 100;
        
        var lineLength = (lineLengthInPercent * width) / 100;
        var rowEndPosition = rowStartPosition + lineLength;
        if(rowEndPosition > height)
        {
            rowEndPosition = height;
        }
        if (rowEndPosition < rowStartPosition + 10)
        {
            rowEndPosition = rowStartPosition + 10;
        }
        
        var jitterHeight = lineThickness / 3;
        
        var columnStartPosition = (columnPositionInPercent * width) / 100;
        var columnEndPosition = columnStartPosition + lineLength;
        if(columnEndPosition > width)
        {
            columnEndPosition = width;
        }
        if (columnEndPosition < columnStartPosition + 10)
        {
            columnEndPosition = columnStartPosition + 10;
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
                    columnNumber <= columnEndPosition &&
                    rowNumber == columnNumber
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
        
        
        if(data.All(x => x.All(y => y == 0)))
        {
            _logger.LogInformation(
                "Empty image generated: {FilePath}, {LineThicknessInPercent}, {LineLengthInPercent}, {RowPositionInPercent}, {ColumnPositionInPercent}, {UseJitter}, {RowStartPosition}, {RowEndPosition}, {ColumnStartPosition}, {ColumnEndPosition}",
                filePath, lineThicknessInPercent, lineLengthInPercent, rowPositionInPercent, columnPositionInPercent, useJitter, rowStartPosition, rowEndPosition, columnStartPosition, columnEndPosition);
            throw new Exception("Empty image generated.");
        }

        await GenerateImage(filePath, width, height, data);
    }
    
}
