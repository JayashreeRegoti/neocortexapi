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
        private readonly MultiSequenceLearning _multiSequenceLearning;

        public KnnClassifierFactory(ILogger<KnnClassifierFactory> logger, MultiSequenceLearning multiSequenceLearning)
        {
            _logger = logger;
            _multiSequenceLearning = multiSequenceLearning;
        }

        internal async Task<Predictor> CreatePredictor(string trainingDataFolderPath)
        {
            await Task.Yield();
            var groupedTrainingData = this.GetGroupedSet(trainingDataFolderPath);

            var trainingData = this.GetDataSet(trainingDataFolderPath);

            var sequences = trainingData.GroupBy(x => x.Key)
                .ToDictionary(trainingEntry => trainingEntry.Key,
                trainingEntry => trainingEntry.Select(x => x.Value).ToList());

            Console.WriteLine("Training model...");

            var sw = new Stopwatch();
            sw.Start();

            var predictor = _multiSequenceLearning.Run(groupedTrainingData);
            sw.Stop();
            _logger.LogInformation("Training model took {ElapsedMilliseconds} ms", sw.ElapsedMilliseconds);
            return predictor;
        }

        private Dictionary<string, List<string>> GetGroupedSet(string dataSetFolderPath)
        {
            var groupedTrainingData = new Dictionary<string, List<string>>();
            
            var trainingFilePaths = Directory.EnumerateFiles(dataSetFolderPath, "*.png", SearchOption.TopDirectoryOnly).ToList();
            
            foreach (string trainingFilePath in trainingFilePaths)
            {
                var key = "";
                if (trainingFilePath.Contains("Horizontal"))
                {
                    key = "Horizontal";
                }
                else if (trainingFilePath.Contains("Vertical"))
                {
                    key = "Vertical";
                }
                else if (trainingFilePath.Contains("Diagonal"))
                {
                    key = "Diagonal";
                }
                else
                {
                    throw new System.Exception($"Unable to determine key: {trainingFilePath}");
                }

                if (groupedTrainingData.ContainsKey(key))
                {
                    groupedTrainingData[key].Add(trainingFilePath);
                }
                else
                {
                    groupedTrainingData.Add(key, new List<string> { trainingFilePath });
                }
            }

            return groupedTrainingData;
        }

        internal async Task ValidateTestData(string testDataFolderPath, Predictor predictor)
        {
            await Task.Yield();
            var groupedTestData = this.GetGroupedSet(testDataFolderPath);

            Console.WriteLine("Validating model...");

            var sw = new Stopwatch();
            sw.Start();

            foreach (var testDataGroup in groupedTestData)
            {
                foreach (var testInputFile in testDataGroup.Value)
                {
                    var predictedValue = predictor.Predict(testInputFile);
                    if (predictedValue.Any())
                    {
                        var predictedSequence = predictedValue.First().PredictedInput.Split('_')[0];
                        _logger.LogInformation("Predicted sequence: {PredictedSequence}, for key: {Key}", predictedSequence,
                            testDataGroup.Key);
                    }
                    else
                    {
                        _logger.LogInformation("No Predicted sequence for key {Key}", testDataGroup.Key);
                    }
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
