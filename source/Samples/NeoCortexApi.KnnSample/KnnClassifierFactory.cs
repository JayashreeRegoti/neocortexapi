using System.Diagnostics;
using Microsoft.Extensions.Logging;
using NeoCortexApi.Classifiers;
using NeoCortexApi.Entities;
using NeoCortexEntities.NeuroVisualizer;
using SkiaSharp;

namespace NeoCortexApi.KnnSample
{
    public class KnnClassifierFactory
    {
        private readonly ILogger<KnnClassifierFactory> _logger;

        public KnnClassifierFactory(ILogger<KnnClassifierFactory> logger)
        {
            _logger = logger;
        }

        internal async Task<Predictor> CreatePredictor(string trainingDataFolderPath)
        {
            await Task.Yield();

            var trainingData = this.GetDataSet(trainingDataFolderPath);

            var sequences = trainingData.GroupBy(x => x.Key)
                .ToDictionary(trainingEntry => trainingEntry.Key,
                trainingEntry => trainingEntry.Select(x => x.Value).ToList());

            Console.WriteLine("Training model...");

            var sw = new Stopwatch();
            sw.Start();

            var predictor = new MultiSequenceLearning().Run(sequences);
            sw.Stop();
            _logger.LogInformation("Training model took {ElapsedMilliseconds} ms", sw.ElapsedMilliseconds);
            return predictor;
        }
        
        internal async Task ValidateTestData(string testDataFolderPath, Predictor predictor)
        {
            await Task.Yield();
            var testData = GetDataSet(testDataFolderPath);

            Console.WriteLine("Validating model...");

            var sw = new Stopwatch();
            sw.Start();

            foreach (var testImage in testData)
            {
                var image = SKBitmap.Decode(testImage.Key);
                var imageBinary = new int[image.Width][];
                for (int x = 0; x < image.Width; x++)
                {
                    imageBinary[x] = new int[image.Height];
                    for (int y = 0; y < image.Height; y++)
                    {
                        var pixel = image.GetPixel(x, y);
                        var red = pixel.Red;
                        var green = pixel.Green;
                        var blue = pixel.Blue;
                        if (red > 128 && green > 128 && blue > 128)
                        {
                            imageBinary[x][y] = 1;
                        }
                        else
                        {
                            imageBinary[x][y] = 0;
                        }
                    }
                }

                
                var predictedValue = predictor.Predict(imageBinary.SelectMany(x => x).ToArray());
                var predictedSequence = predictedValue.First().PredictedInput.Split('_')[0];
                var actualSequence = Path.GetFileNameWithoutExtension(testImage.Key);
                if (predictedSequence != actualSequence)
                {
                    _logger.LogInformation("Predicted sequence: {PredictedSequence}, Actual sequence: {ActualSequence}", predictedSequence,
                        actualSequence);
                }
            }

            sw.Stop();
            _logger.LogInformation("Validating model took {ElapsedMilliseconds} ms", sw.ElapsedMilliseconds);
        }

        private List<KeyValuePair<string, int[]>> GetDataSet(string dataSetFolderPath)
        {
            var trainingImages = new List<KeyValuePair<string, int[][]>>();
            var trainingFilePaths = Directory.EnumerateFiles(dataSetFolderPath, "*.png", SearchOption.TopDirectoryOnly).ToList();
            foreach (string trainingFilePath in trainingFilePaths)
            {
                var image = SKBitmap.Decode(trainingFilePath);
                var imageBinary = new int[image.Width][];
                for (int x = 0; x < image.Width; x++)
                {
                    imageBinary[x] = new int[image.Height];
                    for (int y = 0; y < image.Height; y++)
                    {
                        var pixel = image.GetPixel(x, y);
                        var red = pixel.Red;
                        var green = pixel.Green;
                        var blue = pixel.Blue;
                        if (red > 128 && green > 128 && blue > 128)
                        {
                            imageBinary[x][y] = 1;
                        }
                        else
                        {
                            imageBinary[x][y] = 0;
                        }
                    }
                }

                trainingImages.Add(new KeyValuePair<string, int[][]>(trainingFilePath, imageBinary));
            }

            return trainingImages.Select(x => new KeyValuePair<string, int[]>(x.Key, x.Value.SelectMany(y => y).ToArray())).ToList();
        }


        /// <summary>
        /// Mock the cells data that we get from the Temporal Memory
        /// </summary>
        private List<Cell> GetMockCells(CellActivity cellActivity)
        {

            var lastActiveCells = new List<Cell>();
            var numColumns = 1024;
            var cellsPerColumn = 25;
            var cells = new List<Cell>();
            for (int k = 0; k < Random.Shared.Next(5, 20); k++)
            {
                int parentColumnIndx = Random.Shared.Next(0, numColumns);
                int numCellsPerColumn = Random.Shared.Next(0, cellsPerColumn);
                int colSeq = Random.Shared.Next(0, cellsPerColumn);

                cells.Add(new Cell(parentColumnIndx, colSeq, numCellsPerColumn, cellActivity));
            }

            if (cellActivity == CellActivity.ActiveCell)
                lastActiveCells = cells;

            else if (cellActivity == CellActivity.PredictiveCell)
                /* Append one of the cell from lastActiveCells to the randomly generated predictive cells to have some
                similarity */
                cells.AddRange(lastActiveCells.GetRange(Random.Shared.Next(lastActiveCells.Count), 1));

            return cells;
        }

        private string GetKey(List<string> prevInputs, double input, string sequence)
        {
            string key = string.Empty;

            for (int i = 0; i < prevInputs.Count; i++)
            {
                if (i > 0)
                    key += "-";

                key += prevInputs[i];
            }

            return $"{sequence}_{key}";
        }

    }
}
