using System.Diagnostics;
using Daenet.ImageBinarizerLib.Entities;
using HtmImageEncoder;
using Microsoft.Extensions.Logging;
using NeoCortexApi.Classifiers;
using NeoCortexApi.Encoders;
using NeoCortexApi.Entities;
using NeoCortexApi.Network;

namespace NeoCortexApi.KnnSample
{
    public class MultiSequenceLearning
    {
        private readonly ILogger<MultiSequenceLearning> _logger;

        public MultiSequenceLearning(ILogger<MultiSequenceLearning> logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// Runs the learning of sequences.
        /// </summary>
        /// <param name="sequences">Dictionary of sequences. KEY is the sequence name, the VALUE is the list of element of the sequence.</param>
        /// <param name="imageEncoderSettings"></param>
        public Predictor<string, string> Run(Dictionary<string, List<string>> sequences, BinarizerParams imageEncoderSettings)
        {
            Console.WriteLine($"Hello NeocortexApi! Experiment {nameof(MultiSequenceLearning)}");

            int inputBits = imageEncoderSettings.ImageHeight * imageEncoderSettings.ImageWidth;
            int numColumns = 300;
            
            #region Configuration
            HtmConfig cfg = new HtmConfig(new int[] { inputBits }, new int[] { numColumns })
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
                MaxCycles = 1000,
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
            
            _logger.LogInformation("Completed configuration");

            return RunExperiment(inputBits, cfg, homeostaticPlasticityControllerConfiguration, encoder, sequences);
        }

        /// <summary>
        ///
        /// </summary>
        private Predictor<string, string> RunExperiment(
            int inputBits, 
            HtmConfig cfg, 
            HomeostaticPlasticityControllerConfiguration homeostaticPlasticityControllerConfiguration, 
            EncoderBase encoder, 
            Dictionary<string, List<string>> sequences)
        {
            Stopwatch sw = new Stopwatch();
            sw.Start();

            int maxMatchCnt = 0;
            var mem = new Connections(cfg);
            bool isInStableState = false;
            CortexLayer<string, int[]> cortexLayer = new CortexLayer<string, int[]>("L1");
            
            // For more information see following paper: https://www.scitepress.org/Papers/2021/103142/103142.pdf
            HomeostaticPlasticityController hpc = new HomeostaticPlasticityController(
                mem,
                homeostaticPlasticityControllerConfiguration.MinCycles,
                (isStable, numPatterns, actColAvg, seenInputs) =>
                {
                    if (isStable)
                        // Event should be fired when entering the stable state.
                        _logger.LogInformation(
                            $"STABLE: Patterns: {numPatterns}, Inputs: {seenInputs}, iteration: {seenInputs / numPatterns}");
                    else
                        // Ideal SP should never enter unstable state after stable state.
                        _logger.LogInformation(
                            $"INSTABLE: Patterns: {numPatterns}, Inputs: {seenInputs}, iteration: {seenInputs / numPatterns}");

                    // We are not learning in instable state.
                    isInStableState = isStable;

                    // Clear active and predictive cells.
                    //tm.Reset(mem);
                },
                homeostaticPlasticityControllerConfiguration.NumOfCyclesToWaitOnChange);

            
            SpatialPoolerMT sp = new SpatialPoolerMT(hpc);
            sp.Init(mem);
            _logger.LogInformation("Initialized spatial poller");

            // Please note that we do not add here TM in the layer.
            // This is omitted for practical reasons, because we first eneter the newborn-stage of the algorithm
            // In this stage we want that SP get boosted and see all elements before we start learning with TM.
            // All would also work fine with TM in layer, but it would work much slower.
            // So, to improve the speed of experiment, we first ommit the TM and then after the newborn-stage we add it to the layer.
            cortexLayer.HtmModules.Add("encoder", encoder);
            cortexLayer.HtmModules.Add("sp", sp);
            _logger.LogInformation("Added encoder and spatial poller to compute layer");

            //double[] inputs = inputValues.ToArray();
            int[] prevActiveCols = new int[0];
            
            int cycle = 0;
            int matches = 0;
            
            int maxCycles = homeostaticPlasticityControllerConfiguration.MaxCycles;

            //
            // Training SP to get stable. New-born stage.
            //

            for (int i = 0; i < maxCycles && isInStableState == false; i++)
            {

                cycle++;

                _logger.LogInformation($"-------------- Newborn Cycle {cycle} ---------------");

                foreach (var inputs in sequences)
                {
                    foreach (var input in inputs.Value)
                    {
                        _logger.LogInformation($" -- {inputs.Key} - {input} --");
                        var lyrOut = cortexLayer.Compute(input, true);

                        if (isInStableState)
                            break;
                    }

                    if (isInStableState)
                        break;
                }
            }

            // Clear all learned patterns in the classifier.
            var cls = new KNeighborsClassifier<string, ComputeCycle>();
            cls.ClearState();

            // We activate here the Temporal Memory algorithm.
            TemporalMemory tm = new TemporalMemory();
            tm.Init(mem);
            
            CortexLayer<string, ComputeCycle> cortexLayerWithTemporalMemory = new CortexLayer<string, ComputeCycle>("L1");
            cortexLayerWithTemporalMemory.HtmModules.Add("encoder", encoder);
            cortexLayerWithTemporalMemory.HtmModules.Add("sp", sp);
            cortexLayerWithTemporalMemory.HtmModules.Add("tm", tm);

            //
            // Loop over all sequences.
            foreach (var sequenceKeyPair in sequences)
            {
                _logger.LogInformation($"-------------- Sequences {sequenceKeyPair.Key} ---------------");

                // Set on true if the system has learned the sequence with a maximum acurracy.
                bool isLearningCompleted = false;

                //
                // Now training with SP+TM. SP is pretrained on the given input pattern set.
                for (int i = 0; i < maxCycles; i++)
                {
                    cycle++;

                    _logger.LogInformation($"-------------- Cycle {cycle} ---------------");
                    _logger.LogInformation("");

                    foreach (var inputFilePath in sequenceKeyPair.Value)
                    {
                        _logger.LogInformation($"-------------- {inputFilePath} ---------------");

                        var lyrOut = cortexLayerWithTemporalMemory.Compute(inputFilePath, true) as ComputeCycle;

                        var activeColumns = cortexLayerWithTemporalMemory.GetResult("sp") as int[];

                        string key = sequenceKeyPair.Key;

                        List<Cell> actCells;

                        if (lyrOut.ActiveCells.Count == lyrOut.WinnerCells.Count)
                        {
                            actCells = lyrOut.ActiveCells;
                        }
                        else
                        {
                            actCells = lyrOut.WinnerCells;
                        }

                        cls.Learn(key, actCells.ToArray());

                        _logger.LogInformation($"Col  SDR: {Helpers.StringifyVector(lyrOut.ActivColumnIndicies)}");
                        _logger.LogInformation($"Cell SDR: {Helpers.StringifyVector(actCells.Select(c => c.Index).ToArray())}");
                    }
                }
            }

            _logger.LogInformation("------------ END ------------");
            return new Predictor<string, string>(cortexLayerWithTemporalMemory, mem, cls);
        }

      
        /// <summary>
        /// Gets the number of all unique inputs.
        /// </summary>
        /// <param name="sequences">Alle sequences.</param>
        /// <returns></returns>
        private int GetNumberOfInputs(Dictionary<string, List<string>> sequences)
        {
            int num = 0;

            foreach (var inputs in sequences)
            {
                //num += inputs.Value.Distinct().Count();
                num += inputs.Value.Count;
            }

            return num;
        }


        /// <summary>
        /// Constracts the unique key of the element of an sequece. This key is used as input for HtmClassifier.
        /// It makes sure that alle elements that belong to the same sequence are prefixed with the sequence.
        /// The prediction code can then extract the sequence prefix to the predicted element.
        /// </summary>
        /// <param name="prevInputs"></param>
        /// <param name="input"></param>
        /// <param name="sequence"></param>
        /// <returns></returns>
        private static string GetKey(List<string> prevInputs, int[] input, string sequence)
        {
            string key = String.Empty;

            for (int i = 0; i < prevInputs.Count; i++)
            {
                if (i > 0)
                    key += "-";

                key += (prevInputs[i]);
            }

            return $"{sequence}_{key}";
        }
    }
}
