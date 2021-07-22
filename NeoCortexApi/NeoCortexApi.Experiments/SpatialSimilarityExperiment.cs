using Microsoft.VisualStudio.TestTools.UnitTesting;
using NeoCortex;
using NeoCortexApi.Encoders;
using NeoCortexApi.Entities;
using NeoCortexApi.Network;
using NeoCortexApi.Utility;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LearningFoundation.ImageBinarizer;


namespace NeoCortexApi.Experiments
{
    /// <summary>
    /// 
    /// </summary>
    [TestClass]
    public class SpatialSimilarityExperiment
    {
        /// <summary>
        /// 
        /// </summary>
        [TestMethod]
        public void SpatialSimilarityExperimentTest()
        {
            Console.WriteLine($"Hello {nameof(SpatialSimilarityExperiment)} experiment.");

            // Used as a boosting parameters
            // that ensure homeostatic plasticity effect.
            double minOctOverlapCycles = 1.0;
            double maxBoost = 5.0;

            // We will use 200 bits to represent an input vector (pattern).
            int inputBits = 200;

            // We will build a slice of the cortex with the given number of mini-columns
            int numColumns = 2048;

            //
            // This is a set of configuration parameters used in the experiment.
            HtmConfig cfg = new HtmConfig(new int[] { inputBits }, new int[] { numColumns })
            {
                CellsPerColumn = 10,
                MaxBoost = maxBoost,
                DutyCyclePeriod = 100,
                MinPctOverlapDutyCycles = minOctOverlapCycles,
                StimulusThreshold = 5,
                GlobalInhibition = true,
                NumActiveColumnsPerInhArea = 0.02 * numColumns,
                PotentialRadius = (int)(0.15 * inputBits),
                LocalAreaDensity = -1,//0.5,
                ActivationThreshold = 10,
                MaxSynapsesPerSegment = (int)(0.01 * numColumns),
                Random = new ThreadSafeRandom(42)
            };

            double max = 100;
            int width = 15;
            //
            // This dictionary defines a set of typical encoder parameters.
            Dictionary<string, object> settings = new Dictionary<string, object>()
            {
                { "W", width},
                { "N", inputBits},
                { "Radius", -1.0},
                { "MinVal", 0.0},
                { "Periodic", false},
                { "Name", "scalar"},
                { "ClipInput", false},
                { "MaxVal", max}
            };

            EncoderBase encoder = new ScalarEncoder(settings);

            //
            // We create here 100 random input values.
            List<int[]> inputValues = GetTrainingvectors(0, inputBits, width);

            RunExperiment(cfg, encoder, inputValues);
        }

        [DataTestMethod]
        //[DataRow(new string[] { "pixel1_1.png", "pixel1_2.png", "pixel1_3.png", "pixel1_4.png", "pixel1_5.png", "pixel1_6.png", "pixel1_7.png", "pixel1_8.png", "pixel1_9.png", "pixel1_10.png" }, 15, 0.5, 0.2)]
        //[DataRow(new string[] { "slide_1_1.png", "slide_1_2.png", "slide_1_3.png", "slide_1_4.png", "slide_1_5.png"}, 15, 0.5, 0.2)]
        //[DataRow(new string[] { "pixel1_1.png", "pixel1_2.png", "pixel1_3.png" }, 15, 0.3)]//LocalAreaDensity = -1,//0.5,
        //[DataRow(new string[] { "pixel1_1.png", "pixel1_2.png", "pixel1_3.png", "pixel1_4.png", "pixel1_5.png", "pixel1_6.png", "pixel1_7.png", "pixel1_8.png", "pixel1_9.png", "pixel1_10.png" }, 15, 0.4)]
        //[DataRow(new string[] { "box1_8.png", "box1_9.png", "box1_10.png", "box1_11.png", "box1_12.png" , "box1_13.png" , "box1_14.png" , "box1_15.png" , "box1_16.png", "box1_17.png" }, 15, 0.5, 0.45)]//LocalAreaDensity = -1,//0.5,

        //[DataRow(new string[] { "pixel1_1.png", "pixel1_2.png", "pixel1_3.png" }, 15, 0.5, 0.45)]
        [DataRow(new string[] { "pixel1_1.png", "pixel1_2.png" }, 15, 0.2, 0.5)]
        //[DataRow(new string[] { "pixel1_1.png", "pixel1_2.png" }, 15, 0.4, 0.3)]
        //[DataRow(new string[] { "pixel1_1.png", "pixel1_2.png" }, 15, 0.2, 0.4)]
        //[DataRow(new string[] { "pixel1_1.png", "pixel1_2.png", "pixel1_3.png", "pixel1_4.png" }, 15, -1, 0.015)], "pixel2_4.png", "pixel2_5.png", "pixel2_6.png", "pixel2_7.png", "pixel2_8.png", "pixel2_9.png", "pixel2_10.png"
        //[DataRow(new string[] { "pixel1_1.png", "pixel1_2.png", "pixel1_3.png", "pixel1_4.png", "pixel1_5.png" }, 10, 0.4, 0.3)]
        //[DataRow(new string[] { "pixel1_1.png", "pixel1_2.png", "pixel1_3.png", "pixel1_4.png", "pixel1_5.png" }, 10, 0.2, 0.5)]
        //[DataRow(new string[] { "pixel1_1.png", "pixel1_2.png", "pixel1_3.png", "pixel1_4.png", "pixel1_5.png" }, 10, 0.2, 0.48)]
        //[DataRow(new string[] { "pixel1_1.png", "pixel1_2.png", "pixel1_3.png", "pixel1_4.png", "pixel1_5.png" }, 10, 0.2, 0.12)]
        //[DataRow(new string[] { "pixel1_1.png", "pixel1_2.png" }, 15, 0.2, 0.25)]
        //[DataRow(new string[] { "box_2_1.png", "box_2_2.png", "box_2_3.png", "box_2_4.png" }, 15, 0.4)]
        //[DataRow(new string[] { "box1_1.png", "box1_2.png", "box1_3.png", "box1_4.png", "box1_5.png" }, 10, 0.63, 0.23)]//PotentialRadius = (int)(0.15 * inputBits),
        //[DataRow(new string[] { "face_1_8.png", "face_1_9.png", "face_1_10.png" }, 15, 0.3)]
        public void SpatialSimilarityExperimentImageTest(string[] testImageFileNames, int imageSize, double localAreaDensityValue, double potentialRadiusValue)
        {

            Console.WriteLine($"Hello {nameof(SpatialSimilarityExperiment)} experiment.");            
            // Used as a boosting parameters
            // that ensure homeostatic plasticity effect.
            double minOctOverlapCycles = 1.0;
            double maxBoost = 5.0;

            // We will use (square of image size) bits to represent an input vector (pattern).
            int inputBits = imageSize * imageSize;

            // We will build a slice of the cortex with the given number of mini-columns
            int numColumns = 2048;

            //
            // This is a set of configuration parameters used in the experiment.
            HtmConfig cfg = new HtmConfig(new int[] { inputBits }, new int[] { numColumns })
            {
                CellsPerColumn = 10,
                MaxBoost = maxBoost,
                DutyCyclePeriod = 100,
                MinPctOverlapDutyCycles = minOctOverlapCycles,
                StimulusThreshold = 5,
                GlobalInhibition = false,
                NumActiveColumnsPerInhArea = 0.02 * numColumns,
                PotentialRadius = (int)(potentialRadiusValue * inputBits),
                LocalAreaDensity = localAreaDensityValue,// - 1,//0.5,
                ActivationThreshold = 10,
                MaxSynapsesPerSegment = (int)(0.01 * numColumns),
                Random = new ThreadSafeRandom(42)
            };

            Console.WriteLine($"LocalAreaDensity = {cfg.LocalAreaDensity}, PotentialRadius= {cfg.PotentialRadius}");

            double max = 100;
            int width = 15;
            //
            // This dictionary defines a set of typical encoder parameters.
            Dictionary<string, object> settings = new Dictionary<string, object>()
            {
                { "W", width},
                { "N", inputBits},
                { "Radius", -1.0},
                { "MinVal", 0.0},
                { "Periodic", false},
                { "Name", "scalar"},
                { "ClipInput", false},
                { "MaxVal", max}
            };

            EncoderBase encoder = new ScalarEncoder(settings);

            //
            // We create here 100 random input values.
            List<int[]> inputValues = GetTrainingvectors(
                experimentCode: 2, 
                testImageFileNames: testImageFileNames.ToList(), 
                imageSize: imageSize);

            RunExperiment(cfg, encoder, inputValues);
        }

        /// <summary>
        /// Creates training vectors.
        /// </summary>
        /// <param name="experimentCode"></param>
        /// <param name="inputBits"></param>
        /// <returns></returns>
        private List<int[]> GetTrainingvectors(int experimentCode, int inputBits, int width)
        {
            if (experimentCode == 0)
            {
                //
                // We create here 2 vectors.
                List<int[]> inputValues = new List<int[]>();

                for (int i = 0; i < 10; i += 1)
                {
                    inputValues.Add(NeoCortexUtils.CreateVector(inputBits, i, i + width));
                }


                return inputValues;
            }
            else if (experimentCode == 1)
            {
                // todo. create or load other test vectors/images here 
                // We create here 2 vectors.
                List<int[]> inputValues = new List<int[]>();

                for (int i = 0; i < 10; i += 1)
                {
                    inputValues.Add(NeoCortexUtils.CreateVector(inputBits, i, i + width));
                }


                return inputValues;
            }
            else
                throw new ApplicationException("Invalid experimentCode");
        }

        /// <summary>
        /// Implements the experiment.
        /// </summary>
        /// <param name="cfg"></param>
        /// <param name="encoder"></param>
        /// <param name="inputValues"></param>
        private static void RunExperiment(HtmConfig cfg, EncoderBase encoder, List<int[]> inputValues)
        {
            // Creates the htm memory.
            var mem = new Connections(cfg);

            bool isInStableState = false;

            //
            // HPC extends the default Spatial Pooler algorithm.
            // The purpose of HPC is to set the SP in the new-born stage at the begining of the learning process.
            // In this stage the boosting is very active, but the SP behaves instable. After this stage is over
            // (defined by the second argument) the HPC is controlling the learning process of the SP.
            // Once the SDR generated for every input gets stable, the HPC will fire event that notifies your code
            // that SP is stable now.
            HomeostaticPlasticityController hpa = new HomeostaticPlasticityController(mem, inputValues.Count * 40,
                (isStable, numPatterns, actColAvg, seenInputs) =>
                {
                    // Event should only be fired when entering the stable state.
                    // Ideal SP should never enter unstable state after stable state.
                    if (isStable == false)
                    {
                        Debug.WriteLine($"INSTABLE STATE");
                        // This should usually not happen.
                        isInStableState = false;
                    }
                    else
                    {
                        Debug.WriteLine($"STABLE STATE");
                        // Here you can perform any action if required.
                        isInStableState = true;
                    }
                }, requiredSimilarityThreshold: 0.975);

            // It creates the instance of Spatial Pooler Multithreaded version.
            SpatialPooler sp = new SpatialPoolerMT(hpa);

            // Initializes the 
            sp.Init(mem);

            // Holds the indicies of active columns of the SDR.
            Dictionary<string, int[]> prevActiveColIndicies = new Dictionary<string, int[]>();

            // Holds the active column SDRs.
            Dictionary<string, int[]> prevActiveCols = new Dictionary<string, int[]>();

            // Will hold the similarity of SDKk and SDRk-1 fro every input.
            Dictionary<string, double> prevSimilarity = new Dictionary<string, double>();

            //
            // Initiaize start similarity to zero.
            for (int i = 0; i < inputValues.Count; i++)
            {
                string inputKey = GetInputGekFromIndex(i);
                prevSimilarity.Add(inputKey, 0.0);
                prevActiveColIndicies.Add(inputKey, new int[0]);
            }

            // Learning process will take 1000 iterations (cycles)
            int maxSPLearningCycles = 1000;

            for (int cycle = 0; cycle < maxSPLearningCycles; cycle++)
            {
                Debug.WriteLine($"Cycle  ** {cycle} ** Stability: {isInStableState}");

                //
                // This trains the layer on input pattern.
                for (int inputIndx = 0; inputIndx < inputValues.Count; inputIndx++)
                {
                    string inputKey = GetInputGekFromIndex(inputIndx);
                    int[] input = inputValues[inputIndx];

                    double similarity;

                    int[] activeColumns = new int[(int)cfg.NumColumns];

                    // Learn the input pattern.
                    // Output lyrOut is the output of the last module in the layer.
                    sp.compute(input, activeColumns, true);
                   // DrawImages(cfg, inputKey, input, activeColumns);

                    var actColsIndicies = ArrayUtils.IndexWhere(activeColumns, c => c == 1);

                    similarity = MathHelpers.CalcArraySimilarity(actColsIndicies, prevActiveColIndicies[inputKey]);

                    Debug.WriteLine($"[i={inputKey}, cols=:{actColsIndicies.Length} s={similarity}] SDR: {Helpers.StringifyVector(actColsIndicies)}");

                    prevActiveCols[inputKey] = activeColumns;
                    prevActiveColIndicies[inputKey] = actColsIndicies;
                    prevSimilarity[inputKey] = similarity;

                    if (isInStableState)
                    {
                        GenerateResult(cfg, inputValues, prevActiveColIndicies, prevActiveCols);
                        return;
                    }
                }
            }

            Debug.WriteLine($"after {maxSPLearningCycles} cycles, system is still not in stable state.");

        }


        /// <summary>
        /// Draws all inputs and related SDRs. It also outputs the similarity matrix.
        /// </summary>
        /// <param name="cfg"></param>
        /// <param name="inputValues"></param>
        /// <param name="activeColIndicies"></param>
        /// <param name="activeCols"></param>
        private static void GenerateResult(HtmConfig cfg, List<int[]> inputValues,
            Dictionary<string, int[]> activeColIndicies, Dictionary<string, int[]> activeCols)
        {
            int inpLen = (int)(Math.Sqrt(inputValues[0].Length) + 0.5);

            Dictionary<string, int[]> inpVectorsMap = new Dictionary<string, int[]>();

            for (int k = 0; k < inputValues.Count; k++)
            {
                inpVectorsMap.Add(GetInputGekFromIndex(k), ArrayUtils.IndexWhere(inputValues[k], c => c == 1));
            }

            var outRes = MathHelpers.CalculateSimilarityMatrix(activeColIndicies);

            var inRes = MathHelpers.CalculateSimilarityMatrix(inpVectorsMap);

            string[,] matrix = new string[inpVectorsMap.Keys.Count, inpVectorsMap.Keys.Count];
            int i = 0;
            foreach (var inputKey in inpVectorsMap.Keys)
            {
                for (int j = 0; j < inpVectorsMap.Keys.Count; j++)
                {
                    matrix[i, j] = $"{inRes[i, j].ToString("0.##")}/{outRes[i, j].ToString("0.##")}";
                }

                DrawBitmaps(cfg, inputKey, inputValues[i], inpLen, activeCols[inputKey]);

                i++;
            }

            PrintMatrix(inpVectorsMap.Keys.Count, inpVectorsMap.Keys.ToArray(), matrix);
        }

        private static void PrintMatrix(int dim, string[] inpVectorKeys, string[,] matrix)
        {
            Debug.Write($"{String.Format(" {0,-15}", "")} |");

            for (int k = 0; k < dim; k++)
            {
                string st = String.Format(" {0,-15} |", inpVectorKeys[k]);
                Debug.Write($"{st}");
            }

            Debug.WriteLine("");

            for (int k = 0; k <= dim; k++)
            {
                string st = String.Format(" {0,-15} |", "---------------");
                Debug.Write($"{st}");
            }

            Debug.WriteLine("");

            for (int i = 0; i < dim; i++)
            {
                Debug.Write(String.Format(" {0,-15} |", inpVectorKeys[i]));

                for (int j = 0; j < dim; j++)
                {
                    string st = String.Format(" {0,-15} |", matrix[i, j]);
                    Debug.Write(st);
                }

                Debug.WriteLine("");
            }
        }

        /// <summary>
        /// Drwaws the input and the corresponding SDR.
        /// </summary>
        /// <param name="cfg"></param>
        /// <param name="inputKey"></param>
        /// <param name="input"></param>
        /// <param name="inpLen"></param>
        /// <param name="activeColumns"></param>
        private static void DrawBitmaps(HtmConfig cfg, string inputKey, int[] input, int inpLen, int[] activeColumns)
        {
            List<int[,]> twoDimArrays = new List<int[,]>();
            int[,] twoDimInpArray = ArrayUtils.Make2DArray<int>(input, inpLen, inpLen);
            twoDimArrays.Add(twoDimInpArray = ArrayUtils.Transpose(twoDimInpArray));
            int[,] twoDimOutArray = ArrayUtils.Make2DArray<int>(activeColumns, (int)(Math.Sqrt(cfg.NumColumns) + 0.5), (int)(Math.Sqrt(cfg.NumColumns) + 0.5));
            twoDimArrays.Add(twoDimInpArray = ArrayUtils.Transpose(twoDimOutArray));

            NeoCortexUtils.DrawBitmaps(twoDimArrays, $"{inputKey}.png", Color.Yellow, Color.Gray, 1024, 1024);
        }

        private static void DrawImages(HtmConfig cfg, string inputKey, int[] input, int[] activeColumns)
        {
            List<int[,]> twoDimArrays = new List<int[,]>();
            int[,] twoDimInpArray = ArrayUtils.Make2DArray<int>(input, (int)(Math.Sqrt(input.Length) + 0.5), (int)(Math.Sqrt(input.Length) + 0.5));
            twoDimArrays.Add(twoDimInpArray = ArrayUtils.Transpose(twoDimInpArray));
            int[,] twoDimOutArray = ArrayUtils.Make2DArray<int>(activeColumns, (int)(Math.Sqrt(cfg.NumColumns) + 0.5), (int)(Math.Sqrt(cfg.NumColumns) + 0.5));
            twoDimArrays.Add(twoDimInpArray = ArrayUtils.Transpose(twoDimOutArray));

            NeoCortexUtils.DrawBitmaps(twoDimArrays, $"{inputKey}.png", Color.Yellow, Color.Gray, 1024, 1024);
        }

        private static string GetInputGekFromIndex(int i)
        {
            return $"I-{i.ToString("D2")}";
        }
    }
}
