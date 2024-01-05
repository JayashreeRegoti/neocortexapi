using NeoCortexApi.Classifiers;
using NeoCortexApi.Entities;
using NeoCortexEntities.NeuroVisualizer;
using SkiaSharp;

namespace NeoCortexApi.KnnSample
{
    public class KnnClassifierFactory
    {

        internal async Task<KNeighborsClassifier<string, ComputeCycle>> GetTrainModel(string trainingDataFolderPath)
        {
            await Task.Yield();
            var knnClassifier = new KNeighborsClassifier<string, ComputeCycle>();
            var sequences = new Dictionary<string, List<double>>();
            sequences.Add("S1", new List<double>(new double[] { 0.0, 1.0, 2.0, 3.0, 4.0, 2.0, 5.0 }));

            var trainingData = GetTrainingData(trainingDataFolderPath);

            Console.WriteLine("Training model...");

            int maxCycles = 60;

            foreach (var sequenceKeyPair in sequences)
            {
                int maxPrevInputs = sequenceKeyPair.Value.Count - 1;

                List<string> previousInputs = new List<string>();

                previousInputs.Add("-1.0");

                // Now training with SP+TM. SP is pretrained on the given input pattern set.
                for (int i = 0; i < maxCycles; i++)
                {
                    foreach (var input in sequenceKeyPair.Value)
                    {
                        previousInputs.Add(input.ToString());
                        if (previousInputs.Count > maxPrevInputs + 1)
                            previousInputs.RemoveAt(0);

                        /* In the pretrained SP with HPC, the TM will quickly learn cells for patterns
                        In that case the starting sequence 4-5-6 might have the sam SDR as 1-2-3-4-5-6,
                        Which will result in returning of 4-5-6 instead of 1-2-3-4-5-6.
                        knnClassifier allways return the first matching sequence. Because 4-5-6 will be as first
                        memorized, it will match as the first one. */
                        if (previousInputs.Count < maxPrevInputs)
                            continue;

                        string key = GetKey(previousInputs, input, sequenceKeyPair.Key);
                        List<Cell> actCells = GetMockCells(CellActivity.ActiveCell);
                        knnClassifier.Learn(key, actCells.ToArray());
                    }
                }
            }
            return knnClassifier;
        }

        private List<KeyValuePair<string, int[]>> GetTrainingData(string trainingDataFolderPath)
        {
            var trainingImages = new List<KeyValuePair<string, int[][]>>();
            var trainingFilePaths = Directory.EnumerateFiles(trainingDataFolderPath, "*.png", SearchOption.TopDirectoryOnly).ToList();
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
