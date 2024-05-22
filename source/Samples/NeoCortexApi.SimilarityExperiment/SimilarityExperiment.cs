using System.Diagnostics;
using Daenet.ImageBinarizerLib.Entities;
using HtmImageEncoder;
using NeoCortexApi.Classifiers;
using NeoCortexApi.Encoders;
using NeoCortexApi.Entities;
using NeoCortexApi.KnnSample;
using NeoCortexApi.Network;
using SkiaSharp;

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
        /// Runs the learning of sequences.
        /// </summary>
        /// <param name="sequences">Dictionary of sequences. KEY is the sequence name, the VALUE is the list of element of the sequence.</param>
        /// <param name="imageEncoderSettings"></param>
        public Predictor<string, string> GeneratePredictorModel(Dictionary<string, List<string>> sequences, BinarizerParams imageEncoderSettings)
        {
            Console.WriteLine($"Hello NeocortexApi! Running {nameof(SimilarityExperiment)}");

            int inputBits = imageEncoderSettings.ImageHeight * imageEncoderSettings.ImageWidth;
            int numColumns = inputBits;
            
            #region Configuration
            HtmConfig cfg = new (inputDims:new [] { inputBits }, columnDims:new [] { numColumns })
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

            /*
            double max = 20;

            Dictionary<string, object> settings = new Dictionary<string, object>()
            {
                { "W", 15},
                { "N", inputBits},
                { "Radius", -1.0},
                { "MinVal", 0.0},
                { "Periodic", false},
                { "Name", "scalar"},
                { "ClipInput", false},
                { "MaxVal", max}
            };
            */

            
            var encoder = new ImageEncoder(imageEncoderSettings);
            // use image binarizer to encode the image as string binary and parse back into input vector and send it to sp.compute()
            // see example SchemaImageClassificationExperiment.cs
            // var imageBinarizer = new ImageBinarizer(imageEncoderSettings);
            #endregion
            _logger.LogInformation("Configuration Completed.");
            
            _logger.LogInformation("Generating Predictor Model.");
            var predictor = this.GenerateKnnModel(cfg, homeostaticPlasticityControllerConfiguration, encoder, sequences);
            _logger.LogInformation("Predictor Model Generated.");
            
            return predictor;
        }

        /// <summary>
        /// Creates the KNN model.
        /// </summary>
        /// <param name="cfg"></param>
        /// <param name="homeostaticPlasticityControllerConfiguration"></param>
        /// <param name="encoder"></param>
        /// <param name="sequences"></param>
        /// <returns></returns>
        private Predictor<string, string> GenerateKnnModel(
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

            SpatialPoolerMT sp = new (hpc);
            sp.Init(mem);
            _logger.LogInformation("Initialized spatial poller");

            int maxCycles = homeostaticPlasticityControllerConfiguration.MaxCycles;

            /*
            // Please note that we do not add here TM in the layer.
            // This is omitted for practical reasons, because we first enter the newborn-stage of the algorithm
            // In this stage we want that SP get boosted and see all elements before we start learning with TM.
            // All would also work fine with TM in layer, but it would work much slower.
            // So, to improve the speed of experiment, we first omit the TM and then after the newborn-stage we add it to the layer.
            CortexLayer<string, int[]> cortexLayer = new ("CortexLayer");
            cortexLayer.HtmModules.Add("encoder", encoder);
            cortexLayer.HtmModules.Add("sp", sp);
            _logger.LogInformation("Added encoder and spatial poller to compute layer");
            
            int cycle = 0;
            

            //
            // Training SP to get stable. New-born stage.
            //

            
            foreach (var inputs in sequences)
            {
                for (int i = 0; i < maxCycles && isInStableState == false; i++)
                {
                    _logger.LogInformation("-------------- Newborn Cycle {cycle} ---------------", i);

                    foreach (var input in inputs.Value)
                    {                        
                        _logger.LogInformation(" -- {inputsKey} - {input} --", inputs.Key, input);
                        
                        var lyrOut = cortexLayer.Compute(input, true);

                        if (isInStableState)
                        {
                            break;
                        }
                    }

                    if (isInStableState)
                    {
                        break;
                    }
                    
                    if(i == maxCycles - 1)
                    {
                        _logger.LogInformation("-------------- Max Cycle reached ---------------");
                    }
                }
            }

            // Clear all learned patterns in the classifier.
            var cls = new KNeighborsClassifier<string, ComputeCycle>();
            cls.ClearState();

            // We activate here the Temporal Memory algorithm.
            TemporalMemory tm = new ();
            tm.Init(mem);
            
            */
            CortexLayer<string, int[]> cortexLayerWithTemporalMemory = new ("CortexLayerWithTemporalMemory");
            cortexLayerWithTemporalMemory.HtmModules.Add("encoder", encoder);
            cortexLayerWithTemporalMemory.HtmModules.Add("sp", sp);
            //cortexLayerWithTemporalMemory.HtmModules.Add("tm", tm);
            
            foreach (var sequenceKeyPair in sequences)
            {
                _logger.LogInformation("-------------- Sequences {sequenceKeyPairKey} ---------------", sequenceKeyPair.Key);
                for (int i = 0; i < maxCycles; i++)
                {
                    _logger.LogInformation("-------------- Cycle {cycle} ---------------", i);
                    _logger.LogInformation("");
                    
                    foreach (var inputFilePath in sequenceKeyPair.Value)
                    {
                        _logger.LogInformation("-------------- {inputFilePath} ---------------", inputFilePath);
                    
                        // Now training with SP+TM. SP is pretrained on the given input pattern set.
                    
                        var lyrOut = cortexLayerWithTemporalMemory.Compute(inputFilePath, true);

                        var activeColumns = cortexLayerWithTemporalMemory.GetResult("sp") as int[];

                        string key = sequenceKeyPair.Key;
                        
                        _logger.LogInformation("Col  SDR for {key}: {activeColumnIndices}", key, string.Join(",", activeColumns ?? Array.Empty<int>()));

                        /*
                        List<Cell> actCells = lyrOut.ActiveCells.Count == lyrOut.WinnerCells.Count
                            ? lyrOut.ActiveCells
                            : lyrOut.WinnerCells;

                        //cls.Learn(key, actCells.ToArray());

                        _logger.LogInformation("Col  SDR: {activeColumnIndices}",string.Join(",", lyrOut.ActivColumnIndicies));
                        _logger.LogInformation("Cell SDR: {activeCellIndices}", string.Join(",", actCells.Select(c => c.Index).ToArray()));
                        */
                    }
                    
                    if(i == maxCycles - 1)
                    {
                        _logger.LogInformation("-------------- Max Cycle reached ---------------");
                    }
                }
            }

            _logger.LogInformation("------------ END ------------");
            return new Predictor<string, string>(new CortexLayer<string, ComputeCycle>(), mem, new HtmClassifier<string, ComputeCycle>());
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
