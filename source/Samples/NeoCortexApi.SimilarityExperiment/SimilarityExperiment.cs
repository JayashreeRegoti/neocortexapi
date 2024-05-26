using System.Diagnostics;
using System.Text.Json;
using Daenet.ImageBinarizerLib.Entities;
using HtmImageEncoder;
using NeoCortexApi.Classifiers;
using NeoCortexApi.Encoders;
using NeoCortexApi.Entities;
using NeoCortexApi.KnnSample;
using NeoCortexApi.Network;
using NeoCortexApi.Tools;

namespace NeoCortexApi.SimilarityExperiment
{
    public class SimilarityExperiment
    {
        private readonly ILogger<SimilarityExperiment> _logger;

        public SimilarityExperiment(ILogger<SimilarityExperiment> logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// Runs the similarity experiment.
        /// </summary>
        /// <param name="sequences">Dictionary of sequences. KEY is the sequence name, the VALUE is the list of element of the sequence.</param>
        /// <param name="imageEncoderSettings"></param>
        public async Task RunExperiment(Dictionary<string, List<string>> sequences,
            BinarizerParams imageEncoderSettings)
        {
            Console.WriteLine($"Hello NeocortexApi! Running {nameof(SimilarityExperiment)}");

            int inputBits = imageEncoderSettings.ImageHeight * imageEncoderSettings.ImageWidth;
            int numColumns = inputBits;

            #region Configuration

            HtmConfig cfg = new(inputDims: new[] { inputBits }, columnDims: new[] { numColumns })
            {
                Random = new ThreadSafeRandom(42),

                CellsPerColumn = 30,
                GlobalInhibition = true,
                LocalAreaDensity = -1,
                NumActiveColumnsPerInhArea = 0.02 * numColumns,
                PotentialRadius = (int)(0.15 * inputBits),
                //InhibitionRadius = 15,

                MaxBoost = 10.0,
                DutyCyclePeriod = 25,
                MinPctOverlapDutyCycles = 0.75,
                MaxSynapsesPerSegment = (int)(0.02 * numColumns),

                ActivationThreshold = 15,
                SynPermConnected = 0.5,

                // Learning is slower than forgetting in this case.
                PermanenceDecrement = 0.25,
                PermanenceIncrement = 0.15,

                // Used by punishing of segments.
                PredictedSegmentDecrement = 0.1
            };

            var numUniqueInputs = GetNumberOfInputs(sequences);

            var homeostaticPlasticityControllerConfiguration = new HomeostaticPlasticityControllerConfiguration()
            {
                MinCycles = numUniqueInputs * 3,
                MaxCycles = (int)((numUniqueInputs * 3) * 1.5),
                NumOfCyclesToWaitOnChange = 50
            };
            
            var encoder = new ImageEncoder(imageEncoderSettings);

            #endregion

            _logger.LogInformation("Configuration Completed.");

            _logger.LogInformation("Generating Output SDRs.");
            var outputSdrs = GenerateOutputSdrs(cfg, homeostaticPlasticityControllerConfiguration, encoder, sequences);
            
            _logger.LogInformation("Creating Output SDR Images.");
            await CreateOutputSdrImages(outputSdrs);
            
            _logger.LogInformation("Training KNN Classifier.");
            var cls = new KNeighborsClassifier<string, int[]>();
            foreach (var trainingOutputSdr in outputSdrs.Where(x => x.Key.Contains("train")))
            {
                cls.Learn(trainingOutputSdr.Key, trainingOutputSdr.Value.Select(x => new Cell(0, x)).ToArray());
            }
            
            _logger.LogInformation("Finding Similarity.");
            foreach (var testOutputSdr in outputSdrs.Where(x => x.Key.Contains("test")))
            {
                _logger.LogInformation("--------------------------------------------");
                
                var predicted =
                    cls.GetPredictedInputValues(
                        testOutputSdr.Value.Select(x => new Cell(0, x)).ToArray(), 
                        3).OrderByDescending(x => x.Similarity);

                foreach (ClassifierResult<string> classifierResult in predicted)
                {
                    _logger.LogInformation("Prediction for {key}: {predicted}", testOutputSdr.Key,
                        JsonSerializer.Serialize(classifierResult));
                }
            }

            _logger.LogInformation("------------ END ------------");
        }

        /// <summary>
        /// Creates the output sdrs.
        /// </summary>
        /// <param name="cfg"></param>
        /// <param name="homeostaticPlasticityControllerConfiguration"></param>
        /// <param name="encoder"></param>
        /// <param name="sequences"></param>
        /// <returns></returns>
        private Dictionary<string, int[]> GenerateOutputSdrs(
            HtmConfig cfg, 
            HomeostaticPlasticityControllerConfiguration homeostaticPlasticityControllerConfiguration, 
            EncoderBase encoder, 
            Dictionary<string, List<string>> sequences)
        {
            Stopwatch sw = new ();
            sw.Start();

            bool isInStableState = false;
            Connections mem = new (cfg);
            
            // For more information see following paper: https://www.scitepress.org/Papers/2021/103142/103142.pdf
            HomeostaticPlasticityController hpc = new (
                mem,
                homeostaticPlasticityControllerConfiguration.MinCycles,
                (isStable, numPatterns, _, seenInputs) =>
                {
                    _logger.LogInformation(
                        "Stable: {isStable}, Patterns: {numPatterns}, Inputs: {seenInputs}, iteration: {iteration}",
                        isStable, numPatterns, seenInputs, seenInputs / numPatterns);
                   
                    // We are not learning in unstable state.
                    isInStableState = isStable;

                    // Clear active and predictive cells.
                    //tm.Reset(mem);
                },
                homeostaticPlasticityControllerConfiguration.NumOfCyclesToWaitOnChange);

            SpatialPooler sp = new (hpc);
            sp.Init(mem);
            _logger.LogInformation("Initialized spatial poller");
            
            CortexLayer<string, int[]> cortexLayer = new ("CortexLayer");
            cortexLayer.HtmModules.Add("encoder", encoder);
            cortexLayer.HtmModules.Add("sp", sp);
            
            Dictionary<string, int[]> outputSdrs = new ();
            foreach (var sequenceKeyPair in sequences)
            {
                _logger.LogInformation("-------------- Sequences {sequenceKeyPairKey} ---------------",
                    sequenceKeyPair.Key);

                foreach (var inputFilePath in sequenceKeyPair.Value)
                {
                    _logger.LogInformation("-------------- {inputFilePath} ---------------", inputFilePath);
                    
                    var lyrOut = cortexLayer.Compute(inputFilePath, true);
                    string key = sequenceKeyPair.Key;
                    outputSdrs.Add(key, lyrOut);

                    _logger.LogInformation("Col  SDR for {key}: {activeColumnIndices}", key, string.Join(",", lyrOut ?? Array.Empty<int>()));
                }
            }
            
            return outputSdrs;
        }

        private async Task CreateOutputSdrImages(Dictionary<string, int[]> outputSdrs)
        {
            var outputSdrFolderPath = "./OutputSdrs";
            if (Directory.Exists(outputSdrFolderPath))
            {
                Directory.Delete(outputSdrFolderPath, true);
            }
            Directory.CreateDirectory(outputSdrFolderPath);

            foreach (var (key, value) in outputSdrs)
            {
                var height = 30;
                var width = 30;
                var imageData = new int [height][];
                for (int i = 0; i < height; i++)
                {
                    imageData[i] = new int[width];
                    for (int j = 0; j < width; j++)
                    {
                        imageData[i][j] = value.Contains(i*height +j) ? 255 : 0;
                    }
                }
                
                await ImageGenerator.GenerateImage(Path.Combine(outputSdrFolderPath, $"{key}.png"),width, height, imageData);
            }
        }


        /// <summary>
        /// Gets the number of all unique inputs.
        /// </summary>
        /// <param name="sequences">Alle sequences.</param>
        /// <returns></returns>
        private static int GetNumberOfInputs(Dictionary<string, List<string>> sequences) =>
            sequences.Sum(inputs => inputs.Value.Count);
        
        public Dictionary<string, List<string>> GetGroupedSet(string dataSetFolderPath)
        {
            var groupedTrainingData = new Dictionary<string, List<string>>();
            
            var trainingFilePaths = Directory.EnumerateFiles(dataSetFolderPath, "*.png", SearchOption.TopDirectoryOnly).ToList();
            
            foreach (string trainingFilePath in trainingFilePaths)
            {
                var key = Path.GetFileNameWithoutExtension(trainingFilePath);

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

    }
}
