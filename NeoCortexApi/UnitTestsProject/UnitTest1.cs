////////////////////////////////////////////////////////////////////////////////////////////////
// Copyright (c) 2015 Frankfurt University of Applied Sciences / daenet GmbH
//
// Permission is hereby granted, free of charge, to any person
// obtaining a copy of this software and associated documentation
// files (the "Software"), to deal in the Software without
// restriction, including without limitation the rights to use,
// copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the
// Software is furnished to do so, subject to the following
// conditions:
//
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES
// OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT
// HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY,
// WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING
// FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR
// OTHER DEALINGS IN THE SOFTWARE.
////////////////////////////////////////////////////////////////////////////////////////////////

using Microsoft.VisualStudio.TestTools.UnitTesting;
using NeoCortex;
using NeoCortexApi.Entities;
using NeoCortexApi.Utility;
using System;
using System.Collections.Generic;
using System.Linq;

namespace UnitTestsProject
{
    [TestClass]
    public class UnitTest1
    {

        [TestMethod]
        [DataRow(new int[] { 2048, 6 })]
        [DataRow(new int[] { 100, 20 })]
        public void TestMethod1(int[] data)
        {
            byte[] d = new byte[10000];

            var res = initDimensionMultiples(data, 3);

            res = initDimensionMultiples(data, 2);
        }

        protected int[] initDimensionMultiples(int[] dimensions, int numDimensions)
        {
            int holder = 1;
            int len = dimensions.Length;
            int[] dimensionMultiples = new int[numDimensions];
            for (int i = 0; i < len; i++)
            {
                holder *= (i == 0 ? 1 : dimensions[len - i]);
                dimensionMultiples[len - 1 - i] = holder;
            }
            return dimensionMultiples;
        }


        [TestMethod]
        [DataRow(42)]
        public void RandomSeed(int seed)
        {
            Random x = new Random(seed);
            var res = x.NextDouble();
            Assert.AreEqual(res, 0.668, 0.01);
        }



        [TestMethod]
        public void TestMethod2()
        {
            Topology t = new Topology(new int[] { 2048, 40 });
            int[] coords = new int[] { 200, 10 };
            var indx = t.GndexFromCoordinates(coords);
        }


        [TestMethod]
        public void TestMethod3()
        {
            Topology t = new Topology(new int[] { 2048, 40 });
            int[] coords = new int[] { 200, 10 };
            var indx = t.GndexFromCoordinates(coords);
            var coords2 = t.computeCoordinates(indx);

            Assert.AreEqual(coords[0], coords2[0]);
            Assert.AreEqual(coords[1], coords2[1]);
        }

        [TestMethod]
        public void ModuloTest()
        {
            int r = ArrayUtils.modulo(2, 7);
            r = ArrayUtils.modulo(3, 7);
            r = ArrayUtils.modulo(6, 7);
            r = ArrayUtils.modulo(7, 7);

            r = ArrayUtils.modulo(14, 7);
            r = ArrayUtils.modulo(-14, 7);
            r = ArrayUtils.modulo(15, 7);
            Assert.IsTrue(r == 1);

            r = ArrayUtils.modulo(-15, 7);
            Assert.IsTrue(r == 6);

            r = ArrayUtils.modulo(20, 7);
            Assert.IsTrue(r == 6);

            r = ArrayUtils.modulo(-20, 7);
            Assert.IsTrue(r == 1);
        }


        [TestMethod]
        public void ColumnCompareTest()
        {
            Column c1 = new Column(10, 0);
            Column c2 = new Column(10, 1);

            List<Column> l = new List<Column>(new Column[] { c1, c2});

            Assert.IsTrue(l.Min(i=>i) == c1);

        }


        [TestMethod]
        public void CastTest()
        {
            int[] a = new int[] { 1, 2, 3, 4, 5 };
            object[] b = Array.ConvertAll(a, item => (object)item);
        }


        [TestMethod]
        public void CompareDentrites()
        {
            var empty1 = (NeoCortexApi.Utility.GroupBy2<object>.Slot < Pair<object, List<object>> > )NeoCortexApi.Utility.GroupBy2<object>.Slot<Pair<object, List<object>>>.empty();
            var empty2 = (NeoCortexApi.Utility.GroupBy2<object>.Slot < Pair<object, List<object>> > )NeoCortexApi.Utility.GroupBy2<object>.Slot<Pair<object, List<object>>>.empty();

            var slot2 = NeoCortexApi.Utility.GroupBy2<object>.Slot<object>.of(7);
            Assert.AreNotEqual(empty1, slot2);

            Assert.AreEqual(empty1, empty2);
        }

        [TestMethod]
        public void CreateDutyCycleGraphTest()
        {
            for (int i = 0; i < 1; i++)
            {

            }
        }

        [TestMethod]
        [DataRow(20)]
        [DataRow(30)]
        [DataRow(40)]
        [DataRow(50)]
        public void TestHeatmapCreation(int threshold)
        {
            List<double[,]> bostArrays = new List<double[,]>();
            bostArrays.Add(new double[64, 64]);
            bostArrays.Add(new double[64, 64]);

            double v = 0;
            for (int i = 0; i < 64; i++)
            {
                for (int j = 0; j < 64; j++)
                {
                    bostArrays[0][i, j] = v;
                    bostArrays[1][j, i] = v;
                }

                v += 1;
            }

            NeoCortexUtils.DrawHeatmaps(bostArrays, $"tessheat_{threshold}.png", 1024, 1024, 60, threshold, 10);

        }

        //[TestMethod]
        //public void CompareDentrites()
        //{
        //    DistalDendrite d1 = new DistalDendrite();
        //    DistalDendrite bestSegment = matchingSegments.Max();
        //}


    }
}
