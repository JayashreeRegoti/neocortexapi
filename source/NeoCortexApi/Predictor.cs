﻿using NeoCortexApi.Classifiers;
using NeoCortexApi.Entities;
using NeoCortexApi.Network;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace NeoCortexApi
{
    /// <summary>
    /// Implements the class that helps inferring (prediction). It is typically used by the Sequence Learning algorithm.
    /// After the learning process, the algorithm returns the instance of this class. This class provides a method <see cref="nameof(Predictor.Predict)"/> with a list of input elements.
    /// For every presented input element the predictor tries to predict the next element. The more element provided in a sequence the predictor returns wit the higher score.
    /// For example, assume there ar two sequences: ABCD and ABGHI. By presenting the element B, the predictor is not sure if the next element is C or D. 
    /// When presenting after B the element C, the predictor knows that the next element must be C.
    /// </summary>
    public class Predictor<ClassifierTIn, CortexLayerTIn>
    {
        private Connections connections { get; set; }

        private CortexLayer<CortexLayerTIn, ComputeCycle> layer { get; set; }

        private IClassifier<ClassifierTIn, ComputeCycle> classifier { get; set; }

        /// <summary>
        /// Initializes the predictor functionality.
        /// </summary>
        /// <param name="layer">The HTM Layer.</param>
        /// <param name="connections">The HTM memory in the learned state.</param>
        /// <param name="classifier">The classifier that contains the state of learned sequences.</param>

        public Predictor(CortexLayer<CortexLayerTIn, ComputeCycle> layer, Connections connections, IClassifier<ClassifierTIn, ComputeCycle> classifier)
        {
            this.connections = connections;
            this.layer = layer;
            this.classifier = classifier;
        }

        /// <summary>
        /// Starts predicting of the next subsequences.
        /// </summary>
        public void Reset()
        {
            var tm = this.layer.HtmModules.FirstOrDefault(m => m.Value is TemporalMemory);
            ((TemporalMemory)tm.Value).Reset(this.connections);
        }


        /// <summary>
        /// Predicts the list of next expected elements.
        /// </summary>
        /// <param name="input">The element that will cause the next expected element.</param>
        /// <returns>The list of expected (predicting) elements.</returns>
        public List<ClassifierResult<ClassifierTIn>> Predict(CortexLayerTIn input)
        {
            var lyrOut = this.layer.Compute(input, false) as ComputeCycle;

            List<ClassifierResult<ClassifierTIn>> predictedInputValues = this.classifier.GetPredictedInputValues(lyrOut.PredictiveCells.Select(idx => idx.Index).ToArray(), 3);

            return predictedInputValues;
        }
        
        private static int[] GetCellIndicies(Cell[] output)
        {
            int[] arr = new int[output.Length];
            for (int i = 0; i < output.Length; i++)
            {
                arr[i] = output[i].Index;
            }
            return arr;
        }


        public void Serialize(object obj, string name, StreamWriter sw)
        {
            this.connections.Serialize(obj, name, sw);
        }

        public static object Deserialize<T>(StreamReader sr, string name)
        {
            throw new NotImplementedException();
        }
    }
}
