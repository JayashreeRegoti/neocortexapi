﻿////////////////////////////////////////////////////////////////////////////////////////////////
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

using System;
using System.Collections.Generic;
using System.Text;

namespace NeoCortexApi.Utility
{
    /**
  * Specializes in handling coordinate transforms for N-dimensional
  * integer arrays, between flat and coordinate indexing.
  * 
  * @author cogmission
  * @see Topology
  */
    public class Coordinator //implements Serializable
    {
    /** keep it simple */
    private const long serialVersionUID = 1L;

    protected int[] dimensions;
    protected int[] dimensionMultiples;

        /// <summary>
        /// 
        /// </summary>
    protected bool isColumnMajor;


    protected int numDimensions;

    /**
     * Constructs a new {@link Coordinator} object to be configured with specified
     * dimensions and major ordering.
     * @param shape  the dimensions of this matrix 
     */
    public Coordinator(int[] shape) : this(shape, false)
    {
     
    }

    /**
     * Constructs a new {@link Coordinator} object to be configured with specified
     * dimensions and major ordering.
     * 
     * @param shape                     the dimensions of this sparse array 
     * @param useColumnMajorOrdering    flag indicating whether to use column ordering or
     *                                  row major ordering. if false (the default), then row
     *                                  major ordering will be used. If true, then column major
     *                                  ordering will be used.
     */
    public Coordinator(int[] shape, bool useColumnMajorOrdering)
    {
        this.dimensions = shape;
        this.numDimensions = shape.Length;
        this.dimensionMultiples = initDimensionMultiples(
            useColumnMajorOrdering ? reverse(shape) : shape);
        isColumnMajor = useColumnMajorOrdering;
    }

    /**
     * Returns a flat index computed from the specified coordinates
     * which represent a "dimensioned" index.
     * 
     * @param   coordinates     an array of coordinates
     * @return  a flat index
     */
    public int computeIndex(int[] coordinates)
    {
        int[] localMults = isColumnMajor ? reverse(dimensionMultiples) : dimensionMultiples;
        int baseNum = 0;
        for (int i = 0; i < coordinates.Length; i++)
        {
                baseNum += (localMults[i] * coordinates[i]);
        }
        return baseNum;
    }

    /**
     * Returns an array of coordinates calculated from
     * a flat index.
     * 
     * @param   index   specified flat index
     * @return  a coordinate array
     */
    public int[] computeCoordinates(int index)
    {
        int[] returnVal = new int[numDimensions];
        int baseNum = index;
        for (int i = 0; i < dimensionMultiples.Length; i++)
        {
            int quotient = baseNum / dimensionMultiples[i];
                baseNum %= dimensionMultiples[i];
            returnVal[i] = quotient;
        }
        return isColumnMajor ? reverse(returnVal) : returnVal;
    }

    /**
     * Initializes internal helper array which is used for multidimensional
     * index computation.
     * @param dimensions matrix dimensions
     * @return array for use in coordinates to flat index computation.
     */
    protected int[] initDimensionMultiples(int[] dimensions)
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

    /**
     * Reverses the specified array.
     * @param input
     * @return
     */
    public static int[] reverse(int[] input)
    {
        int[] retVal = new int[input.Length];
        for (int i = input.Length - 1, j = 0; i >= 0; i--, j++)
        {
            retVal[j] = input[i];
        }
        return retVal;
    }
}
}
