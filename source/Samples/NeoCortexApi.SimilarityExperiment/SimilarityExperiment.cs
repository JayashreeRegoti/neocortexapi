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
    /// <summary>
    /// Similarity experiment class to run similarity experiment using input SDR data.
    /// First, it fetches and reads input SDR images from the input SDR folder.
    /// Then, it generates output SDRs using input SDR data.
    /// Then, we save the output SDRs as images. (For visualization, Not necessary for the experiment)
    /// Then, it trains KNN classifier using training output SDRs and predicts test output SDRs.
    /// Finally, we display the predicted output SDRs, with its similarity compared to test output SDRs.
    /// </summary>
    public class SimilarityExperiment
    {
        private readonly ILogger<SimilarityExperiment> _logger;

        public SimilarityExperiment(ILogger<SimilarityExperiment> logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// First, it fetches and reads input SDR images from the input SDR folder.
        /// Then, it generates output SDRs using input SDR data.
        /// Then, we save the output SDRs as images. (For visualization, Not necessary for the experiment)
        /// Then, it trains KNN classifier using training output SDRs and predicts test output SDRs.
        /// Finally, we display the predicted output SDRs, with its similarity compared to test output SDRs.
        /// </summary>
        /// <param name="inputSdrsFolderPath">folder path of input SDR images</param>
        /// <param name="imageEncoderSettings">image settings to read input SDR images by ImageEncoder class</param>
        /// <seealso cref="HtmImageEncoder.ImageEncoder"/>
        public async Task RunExperiment(string inputSdrsFolderPath, BinarizerParams imageEncoderSettings)
        {
            _logger.LogInformation($"Hello NeocortexApi! Running {nameof(SimilarityExperiment)}");

            // Fetch input SDRs file names from the input SDRs folder.
            var inputSdrFilePaths = GetInputSdrFilePaths(inputSdrsFolderPath);

            #region Configuration

            int inputBits = imageEncoderSettings.ImageHeight * imageEncoderSettings.ImageWidth;
            int numColumns = inputBits;

            // define HTM configuration
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

            var numUniqueInputs = inputSdrFilePaths.Count;

            // define homeostatic plasticity controller configuration
            var homeostaticPlasticityControllerConfiguration = new HomeostaticPlasticityControllerConfiguration()
            {
                MinCycles = numUniqueInputs * 3,
                MaxCycles = (int)((numUniqueInputs * 3) * 1.5),
                NumOfCyclesToWaitOnChange = 50
            };
            
            // Initialize image encoder.
            // It takes the image settings to read input SDR images and create one dimensional array of input SDR.
            var encoder = new ImageEncoder(imageEncoderSettings);
            _logger.LogInformation("Configuration Completed.");

            #endregion

            #region Generate Output SDRs
            
            // Generate output SDRs by passing input SDR image file paths to image encoder and spatial pooler.
            // The image encoder reads the image and converts it to a binary array.
            // The spatial pooler computes the active columns for the input SDR.
            // The output SDR is the list of active column indices.
            _logger.LogInformation("Generating Output SDRs.");
            var outputSdrs = GenerateOutputSdrs(
                htmConfig, 
                homeostaticPlasticityControllerConfiguration, 
                encoder, 
                inputSdrFilePaths);
            
            // Save output SDR images. For visualization, not necessary for the experiment.
            _logger.LogInformation("Creating Output SDR Images.");
            var outputSdrFolderPath = "./OutputSdrs";
            
            // Create output SDR images, by passing the output SDRs and image settings.
            await CreateOutputSdrImages(
                outputSdrFolderPath, 
                outputSdrs, 
                imageEncoderSettings.ImageHeight, 
                imageEncoderSettings.ImageWidth);
            
            #endregion

            #region Train KNN classifier using training output SDRs and Predict test output SDRs
            
            _logger.LogInformation("Training KNN Classifier.");
            // Initialize KNN classifier to train and predict output SDRs.
            var classifier = new KNeighborsClassifier<string, int[]>();
            
            // Train KNN classifier using training output SDRs.
            foreach (var trainingOutputSdr in outputSdrs.Where(x => x.Key.Contains("train")))
            {
                // Train classifier on output SDR of training data.
                classifier.Learn(trainingOutputSdr.Key, trainingOutputSdr.Value.Select(x => new Cell(0, x)).ToArray());
                _logger.LogInformation("Training for classifier on output SDR of {key} completed.", trainingOutputSdr.Key);
            }
            
            // Predict test output SDRs using KNN classifier.
            _logger.LogInformation("Finding Similarity.");
            // Take each test output SDR and predict the output SDR using KNN classifier.
            foreach (var testOutputSdr in outputSdrs.Where(x => x.Key.Contains("test")))
            {
                _logger.LogInformation("--------------------------------------------");
                
                // Predict output SDR using KNN classifier.
                // Get top 3 predicted output SDRs with similarity.
                // Sort the predicted output SDRs by similarity in descending order.
                var predicted =
                    classifier.GetPredictedInputValues(
                        testOutputSdr.Value.Select(x => new Cell(0, x)).ToArray(), 
                        howMany:3).OrderByDescending(x => x.Similarity).ToList();
                
                // If KNN classifier found any prediction for the output SDR, log the prediction.
                if (predicted.Any())
                {
                    // Log the prediction for the output SDR.
                    foreach ((ClassifierResult<string> classifierResult, int index) in predicted.Select((value, index) => (value, index)))
                    {
                        _logger.LogInformation("Prediction {index} for output sdr of {key}: {predicted}", 
                            index + 1,
                            testOutputSdr.Key,
                            JsonSerializer.Serialize(classifierResult));
                    }
                }
                else
                {
                    // If no prediction found for the output SDR, log the no prediction message.
                    _logger.LogInformation("No Prediction for output sdr of {key}", testOutputSdr.Key);
                }
            }
            #endregion
            
            _logger.LogInformation($"{nameof(SimilarityExperiment)} completed.");
        }

        /// <summary>
        /// Generate output SDRs by passing input SDR image file paths to image encoder and spatial pooler.
        /// The image encoder reads the image and converts it to a binary array.
        /// The spatial pooler computes the active columns for the input SDR.
        /// The output SDR is the list of active column indices.
        /// </summary>
        /// <param name="htmConfig">HTM config for spatial pooler
        /// <seealso cref="NeoCortexApi.Entities.HtmConfig"/>
        /// </param>
        /// <param name="homeostaticPlasticityControllerConfiguration">
        /// Homeostatic Plasticity Controller Configuration for spatial pooler
        /// <seealso cref="NeoCortexApi.SimilarityExperiment.Configuration.HomeostaticPlasticityControllerConfiguration"/>
        /// </param>
        /// <param name="encoder">encoder settings for decoding input SDRs</param>
        /// <param name="inputSdrs">input SDRs for encoder to decode</param>
        /// <returns>
        /// output SDRs generated by spatial pooler,
        /// it is a dictionary of output SDRs with key as input SDR file name and
        /// value as output SDR which is a list of active column indices.
        /// </returns>
        private Dictionary<string, int[]> GenerateOutputSdrs(
            HtmConfig htmConfig, 
            HomeostaticPlasticityControllerConfiguration homeostaticPlasticityControllerConfiguration, 
            EncoderBase encoder, 
            Dictionary<string, string> inputSdrs)
        {
            // Start stopwatch for measuring the time of generating output SDRs.
            Stopwatch sw = new ();
            sw.Start();

            // Initialize memory and homeostatic plasticity controller.
            Connections mem = new (htmConfig);
            
            // Initialize homeostatic plasticity controller.
            HomeostaticPlasticityController homeostaticPlasticityController = new (
                mem,
                homeostaticPlasticityControllerConfiguration.MinCycles,
                (_, _, _, _) => { },
                homeostaticPlasticityControllerConfiguration.NumOfCyclesToWaitOnChange);

            // Initialize spatial pooler.
            SpatialPooler sp = new (homeostaticPlasticityController);
            sp.Init(mem);
            _logger.LogInformation("Initialized spatial pooler");
            
            // Initialize cortex layer to chain encoder and spatial pooler.
            CortexLayer<string, int[]> cortexLayer = new ("CortexLayer");
            // Add encoder and spatial pooler to the cortex layer.
            cortexLayer.HtmModules.Add("encoder", encoder);
            cortexLayer.HtmModules.Add("spatial_pooler", sp);
            
            // Initialize output SDRs dictionary to store output SDRs with key as input SDR file name.
            Dictionary<string, int[]> outputSdrs = new ();
            
            // Generate output SDRs by passing each input SDR image file paths to image encoder and spatial pooler.
            foreach (var inputSdr in inputSdrs)
            {
                // Decode input SDR image to 1D array of 1 and 0 values.
                // Send that input SDR to spatial pooler to generate output SDR.
                var activeColumnsArr = cortexLayer.Compute(inputSdr.Value, true);
                
                // Add output SDR to the dictionary with key as input SDR file name.
                string key = inputSdr.Key;
                outputSdrs.Add(key, activeColumnsArr);

                _logger.LogInformation("Output SDR generated [{activeColumnIndices}] for {key}", 
                    string.Join(",", activeColumnsArr ?? Array.Empty<int>()), key);
            }
            
            // Stop stopwatch for measuring the time of generating output SDRs.
            sw.Stop();
            _logger.LogInformation("Generating Output SDRs took {ElapsedMilliseconds} ms", sw.ElapsedMilliseconds);
            
            return outputSdrs;
        }

        /// <summary>
        /// Create output SDR images from output SDRs which is a list of active column indices.
        /// First, it recreates a folder to store output SDR images.
        /// Then, it creates output SDR images by setting the pixel value to 255 (white)
        /// if the active column index is present in the output SDR.
        /// Set all other pixel values to 0 (black).
        /// Then, the output SDR images are saved in the output SDR folder.
        /// </summary>
        /// <param name="outputSdrFolderPath">folder path to store output sdr images</param>
        /// <param name="outputSdrs">
        /// key value pairs of output sdr file name and list of active column indices generated by spatial pooler.
        /// </param>
        /// <param name="imageHeight">height of the image</param>
        /// <param name="imageWidth">width of the image</param>
        private async Task CreateOutputSdrImages(
            string outputSdrFolderPath, 
            Dictionary<string, int[]> outputSdrs,
            int imageHeight, 
            int imageWidth)
        {
            // Recreate output SDR folder to store output SDR images.
            if (Directory.Exists(outputSdrFolderPath))
            {
                Directory.Delete(outputSdrFolderPath, true);
            }
            Directory.CreateDirectory(outputSdrFolderPath);

            // Take each output SDR and create an image with active column indices.
            foreach (var (key, value) in outputSdrs)
            {
                var imageData = new int [imageHeight][];
                for (int i = 0; i < imageHeight; i++)
                {
                    imageData[i] = new int[imageWidth];
                    for (int j = 0; j < imageWidth; j++)
                    {
                        // Set the pixel value to 255(white) if the active column index is present in the output SDR.
                        // Otherwise, set pixel values to 0(black).
                        imageData[i][j] = value.Contains(i*imageHeight +j) ? 255 : 0;
                    }
                }
                
                // Create output SDR image file path to save the image.
                var filePath = Path.Combine(outputSdrFolderPath, $"{key}.png");
                
                // Create output SDR image by passing the image file path, image width, image height, and pixel values.
                await ImageGenerator.GenerateImage(filePath,imageWidth, imageHeight, imageData);
                _logger.LogInformation("Output SDR image created at {filePath}", filePath);
            }
        }
        
        /// <summary>
        /// Fetch input SDRs file names with png extension from the input SDRs folder.
        /// </summary>
        /// <param name="inputSdrsFolderPath">folder path to fetch input SDR file paths from</param>
        /// <returns>key value pairs, with input sdr file name as key and file path as value</returns>
        private static Dictionary<string, string> GetInputSdrFilePaths(string inputSdrsFolderPath)
        {
            // Define dictionary to store input SDR file names with key as file name and value as file path.
            var inputSdrFilePaths = new Dictionary<string, string>();
            
            // Fetch all the file paths from the input SDR folder.
            var filePaths = Directory.EnumerateFiles(inputSdrsFolderPath, "*.png", SearchOption.TopDirectoryOnly).ToList();
            
            // Add each file name with file path to the dictionary.
            foreach (string filePath in filePaths)
            {
                // Get the file name without extension.
                var key = Path.GetFileNameWithoutExtension(filePath);

                inputSdrFilePaths.Add(key, filePath);
            }

            return inputSdrFilePaths;
        }

    }
}
