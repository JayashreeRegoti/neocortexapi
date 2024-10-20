using System.Diagnostics;
using System.Text.Json;
using Daenet.ImageBinarizerLib.Entities;
using HtmImageEncoder;
using NeoCortexApi.Classifiers;
using NeoCortexApi.Encoders;
using NeoCortexApi.Entities;
using NeoCortexApi.Network;
using NeoCortexApi.SimilarityExperiment.Configuration;
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
        public async Task RunExperiment(string inputSdrsFolderPath, BinarizerParams imageEncoderSettings)
        {
            _logger.LogInformation($"Hello NeocortexApi! Running {nameof(SimilarityExperiment)}");

            int inputBits = imageEncoderSettings.ImageHeight * imageEncoderSettings.ImageWidth;
            int numColumns = inputBits;
            var inputSdrs = GetInputSdrs(inputSdrsFolderPath);

            #region Configuration

            HtmConfig htmConfig = new(inputDims: new[] { inputBits }, columnDims: new[] { numColumns })
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
            _logger.LogInformation("Configuration Completed.");

            #endregion

            #region Generate Output SDRs
            
            _logger.LogInformation("Generating Output SDRs.");
            var outputSdrs = GenerateOutputSdrs(
                htmConfig, 
                homeostaticPlasticityControllerConfiguration, 
                encoder, 
                inputSdrs);
            
            _logger.LogInformation("Creating Output SDR Images.");
            var outputSdrFolderPath = "./OutputSdrs";
            await CreateOutputSdrImages(
                outputSdrFolderPath, 
                outputSdrs, 
                imageEncoderSettings.ImageHeight, 
                imageEncoderSettings.ImageWidth);
            
            #endregion

            #region Train KNN classifier using training output SDRs and Predict test output SDRs
            
            _logger.LogInformation("Training KNN Classifier.");
            var classifier = new KNeighborsClassifier<string, int[]>();
            foreach (var trainingOutputSdr in outputSdrs.Where(x => x.Key.Contains("train")))
            {
                classifier.Learn(trainingOutputSdr.Key, trainingOutputSdr.Value.Select(x => new Cell(0, x)).ToArray());
                _logger.LogInformation("Training for classifier on output SDR of {key} completed.", trainingOutputSdr.Key);
            }
            
            _logger.LogInformation("Finding Similarity.");
            foreach (var testOutputSdr in outputSdrs.Where(x => x.Key.Contains("test")))
            {
                _logger.LogInformation("--------------------------------------------");
                
                var predicted =
                    classifier.GetPredictedInputValues(
                        testOutputSdr.Value.Select(x => new Cell(0, x)).ToArray(), 
                        3).OrderByDescending(x => x.Similarity);
                
                if (!predicted.Any())
                {
                    _logger.LogInformation("No Prediction for output sdr of {key}", testOutputSdr.Key);
                }

                foreach ((ClassifierResult<string> classifierResult, int index) in predicted.Select((value, index) => (value, index)))
                {
                    _logger.LogInformation("Prediction {index} for output sdr of {key}: {predicted}", 
                        index + 1,
                        testOutputSdr.Key,
                        JsonSerializer.Serialize(classifierResult));
                }
            }
            #endregion
            
            _logger.LogInformation($"{nameof(SimilarityExperiment)} completed.");
        }

        /// <summary>
        /// Creates the output sdrs.
        /// </summary>
        /// <param name="htmConfig"></param>
        /// <param name="homeostaticPlasticityControllerConfiguration"></param>
        /// <param name="encoder"></param>
        /// <param name="inputSdrs"></param>
        /// <returns></returns>
        private Dictionary<string, int[]> GenerateOutputSdrs(
            HtmConfig htmConfig, 
            HomeostaticPlasticityControllerConfiguration homeostaticPlasticityControllerConfiguration, 
            EncoderBase encoder, 
            Dictionary<string, string> inputSdrs)
        {
            // Start stopwatch for measuring the time of generating output SDRs.
            Stopwatch sw = new ();
            sw.Start();

            Connections mem = new (htmConfig);
            // For more information see following paper: https://www.scitepress.org/Papers/2021/103142/103142.pdf
            HomeostaticPlasticityController homeostaticPlasticityController = new (
                mem,
                homeostaticPlasticityControllerConfiguration.MinCycles,
                (_, _, _, _) => { },
                homeostaticPlasticityControllerConfiguration.NumOfCyclesToWaitOnChange);

            // Initialize spatial pooler.
            SpatialPooler sp = new (homeostaticPlasticityController);
            sp.Init(mem);
            _logger.LogInformation("Initialized spatial pooler");
            
            CortexLayer<string, int[]> cortexLayer = new ("CortexLayer");
            // Add encoder and spatial pooler to the cortex layer.
            cortexLayer.HtmModules.Add("encoder", encoder);
            cortexLayer.HtmModules.Add("spatial_pooler", sp);
            
            Dictionary<string, int[]> outputSdrs = new ();
            foreach (var inputSdr in inputSdrs)
            {
                // Compute the active columns for the input SDR.
                var activeColumnsArr = cortexLayer.Compute(inputSdr.Value, true);
                string key = inputSdr.Key;
                outputSdrs.Add(key, activeColumnsArr);

                _logger.LogInformation("Output SDR generated [{activeColumnIndices}] for {key}", 
                    string.Join(",", activeColumnsArr ?? Array.Empty<int>()), key);
            }
            
            sw.Stop();
            _logger.LogInformation("Generating Output SDRs took {ElapsedMilliseconds} ms", sw.ElapsedMilliseconds);
            
            return outputSdrs;
        }

        /// <summary>
        /// Creates the output sdr images.
        /// </summary>
        /// <param name="outputSdrFolderPath"></param>
        /// <param name="outputSdrs"></param>
        /// <param name="imageHeight"></param>
        /// <param name="imageWidth"></param>
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

            // Create output SDR images.
            foreach (var (key, value) in outputSdrs)
            {
                var imageData = new int [imageHeight][];
                for (int i = 0; i < imageHeight; i++)
                {
                    imageData[i] = new int[imageWidth];
                    for (int j = 0; j < imageWidth; j++)
                    {
                        // Set the pixel value to 255 if the active column index is present in the output SDR.
                        imageData[i][j] = value.Contains(i*imageHeight +j) ? 255 : 0;
                    }
                }
                
                var filePath = Path.Combine(outputSdrFolderPath, $"{key}.png");
                await ImageGenerator.GenerateImage(filePath,imageWidth, imageHeight, imageData);
                _logger.LogInformation("Output SDR image created at {filePath}", filePath);
            }
        }
        
        /// <summary>
        /// Fetches the input SDRs file names from the input SDRs folder.
        /// </summary>
        /// <param name="inputSdrsFolderPath"></param>
        /// <returns></returns>
        private static Dictionary<string, string> GetInputSdrs(string inputSdrsFolderPath)
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
