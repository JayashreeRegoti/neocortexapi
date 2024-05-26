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
        /// <param name="inputSdrs">Dictionary of sequences. KEY is the sequence name, the VALUE is the list of element of the sequence.</param>
        /// <param name="imageEncoderSettings"></param>
        public async Task RunExperiment(Dictionary<string, string> inputSdrs,
            BinarizerParams imageEncoderSettings)
        {
            _logger.LogInformation($"Hello NeocortexApi! Running {nameof(SimilarityExperiment)}");

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

            var numUniqueInputs = inputSdrs.Count;

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
            var outputSdrs = GenerateOutputSdrs(cfg, homeostaticPlasticityControllerConfiguration, encoder, inputSdrs);
            
            _logger.LogInformation("Creating Output SDR Images.");
            var outputSdrFolderPath = "./OutputSdrs";
            await CreateOutputSdrImages(
                outputSdrFolderPath, 
                outputSdrs, 
                imageEncoderSettings.ImageHeight, 
                imageEncoderSettings.ImageWidth);
            
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

            _logger.LogInformation($"{nameof(SimilarityExperiment)} completed.");
        }

        /// <summary>
        /// Creates the output sdrs.
        /// </summary>
        /// <param name="cfg"></param>
        /// <param name="homeostaticPlasticityControllerConfiguration"></param>
        /// <param name="encoder"></param>
        /// <param name="inputSdrs"></param>
        /// <returns></returns>
        private Dictionary<string, int[]> GenerateOutputSdrs(
            HtmConfig cfg, 
            HomeostaticPlasticityControllerConfiguration homeostaticPlasticityControllerConfiguration, 
            EncoderBase encoder, 
            Dictionary<string, string> inputSdrs)
        {
            Stopwatch sw = new ();
            sw.Start();

            Connections mem = new (cfg);
            
            // For more information see following paper: https://www.scitepress.org/Papers/2021/103142/103142.pdf
            HomeostaticPlasticityController hpc = new (
                mem,
                homeostaticPlasticityControllerConfiguration.MinCycles,
                (_, _, _, _) => { },
                homeostaticPlasticityControllerConfiguration.NumOfCyclesToWaitOnChange);

            SpatialPooler sp = new (hpc);
            sp.Init(mem);
            _logger.LogInformation("Initialized spatial poller");
            
            CortexLayer<string, int[]> cortexLayer = new ("CortexLayer");
            cortexLayer.HtmModules.Add("encoder", encoder);
            cortexLayer.HtmModules.Add("spatial_pooler", sp);
            
            Dictionary<string, int[]> outputSdrs = new ();
            foreach (var inputSdr in inputSdrs)
            {
                var lyrOut = cortexLayer.Compute(inputSdr.Value, true);
                string key = inputSdr.Key;
                outputSdrs.Add(key, lyrOut);

                _logger.LogInformation("Col SDR for {key}: {activeColumnIndices}", key, string.Join(",", lyrOut ?? Array.Empty<int>()));
            }
            
            sw.Stop();
            _logger.LogInformation("Generating Output SDRs took {ElapsedMilliseconds} ms", sw.ElapsedMilliseconds);
            
            return outputSdrs;
        }

        private async Task CreateOutputSdrImages(
            string outputSdrFolderPath, 
            Dictionary<string, int[]> outputSdrs,
            int imageHeight, 
            int imageWidth)
        {
            if (Directory.Exists(outputSdrFolderPath))
            {
                Directory.Delete(outputSdrFolderPath, true);
            }
            Directory.CreateDirectory(outputSdrFolderPath);

            foreach (var (key, value) in outputSdrs)
            {
                var imageData = new int [imageHeight][];
                for (int i = 0; i < imageHeight; i++)
                {
                    imageData[i] = new int[imageWidth];
                    for (int j = 0; j < imageWidth; j++)
                    {
                        imageData[i][j] = value.Contains(i*imageHeight +j) ? 255 : 0;
                    }
                }
                
                await ImageGenerator.GenerateImage(Path.Combine(outputSdrFolderPath, $"{key}.png"),imageWidth, imageHeight, imageData);
            }
        }
        
        public Dictionary<string, string> GetInputSdrs(string inputSdrsFolderPath)
        {
            var inputSdrs = new Dictionary<string, string>();
            
            var filePaths = Directory.EnumerateFiles(inputSdrsFolderPath, "*.png", SearchOption.TopDirectoryOnly).ToList();
            
            foreach (string filePath in filePaths)
            {
                var key = Path.GetFileNameWithoutExtension(filePath);

                inputSdrs.Add(key, filePath);
            }

            return inputSdrs;
        }

    }
}
