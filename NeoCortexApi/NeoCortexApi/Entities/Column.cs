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
using System.Collections.ObjectModel;
using System.Text;
using NeoCortexApi.Entities;

namespace NeoCortexApi.Entities
{

    /**
     * Abstraction of both an input bit and a columnal collection of
     * {@link Cell}s which have behavior associated with membership to
     * a given {@code Column}
     * 
     * @author Chetan Surpur
     * @author David Ray
     *
     */
     //[Serializable]
    public class Column : IEquatable<Column>, IComparable<Column>
    {
        /** keep it simple */
        //private static readonly long serialVersionUID = 1L;

        /** The flat non-topological index of this column */
        private readonly int index;
        /** Stored boxed form to eliminate need for boxing on the fly */
        private readonly Integer boxedIndex;
        /** Configuration of cell count */
        private readonly int numCells;
        /** Connects {@link SpatialPooler} input pools */
        private ProximalDendrite proximalDendrite;

        private Cell[] cells;
        private ReadOnlyCollection<Cell> cellList;

        private readonly int hashcode;



        /// <summary>
        /// Creates a new collumn with specified number of cells and a single proximal dendtrite segment.
        /// </summary>
        /// <param name="numCells">Number of cells in the column.</param>
        /// <param name="index">Colun index.</param>
        public Column(int numCells, int index)
        {
            this.numCells = numCells;
            this.index = index;
            this.boxedIndex = index;
            this.hashcode = GetHashCode();
            cells = new Cell[numCells];
            for (int i = 0; i < numCells; i++)
            {
                cells[i] = new Cell(this, i);
            }

            cellList = new ReadOnlyCollection<Cell>(cells);

            proximalDendrite = new ProximalDendrite(index);
        }

        /**
         * Returns the {@link Cell} residing at the specified index.
         * <p>
         * <b>IMPORTANT NOTE:</b> the index provided is the index of the Cell within this
         * column and is <b>not</b> the actual index of the Cell within the total
         * list of Cells of all columns. Each Cell maintains it's own <i><b>GLOBAL</i></b>
         * index which is the index describing the occurrence of a cell within the
         * total list of all cells. Thus, {@link Cell#getIndex()} returns the <i><b>GLOBAL</i></b>
         * index and <b>not</b> the index within this column.
         * 
         * @param index     the index of the {@link Cell} to return.
         * @return          the {@link Cell} residing at the specified index.
         */
        public Cell getCell(int index)
        {
            return cells[index];
        }

        /**
         * Returns a {@link List} view of this {@code Column}'s {@link Cell}s.
         * @return
         */
        public IList<Cell> getCells()
        {
            return cellList;
        }

        /**
         * Returns the index of this {@code Column}
         * @return  the index of this {@code Column}
         */
        public int getIndex()
        {
            return index;
        }

        /**
         * Returns the configured number of cells per column for
         * all {@code Column} objects within the current {@link TemporalMemory}
         * @return
         */
        public int getNumCellsPerColumn()
        {
            return numCells;
        }

        /**
         * Returns the {@link Cell} with the least number of {@link DistalDendrite}s.
         * 
         * @param c         the connections state of the temporal memory
         * @param random
         * @return
         */
        public Cell getLeastUsedCell(Connections c, Random random)
        {
            List<Cell> leastUsedCells = new List<Cell>();
            int minNumSegments = Integer.MaxValue;

            foreach (var cell in cellList)
            {
                
                int numSegments = cell.getSegments(c).Count;

                if (numSegments < minNumSegments)
                {
                    minNumSegments = numSegments;
                    leastUsedCells.Clear();
                }

                if (numSegments == minNumSegments)
                {
                    leastUsedCells.Add(cell);
                }
            }

            int index = random.Next(leastUsedCells.Count);
            leastUsedCells.Sort();
            return leastUsedCells[index];
        }

        /**
         * Returns this {@code Column}'s single {@link ProximalDendrite}
         * @return
         */
        public ProximalDendrite getProximalDendrite()
        {
            return proximalDendrite;
        }

        /**
         * This method creates connections between columns and inputs.
         * It delegates the potential synapse creation to the one {@link ProximalDendrite}.
         * 
         * @param c						the {@link Connections} memory
         * @param inputVectorIndexes	indexes specifying the input vector bit
         */
        public Pool createPotentialPool(Connections c, int[] inputVectorIndexes)
        {
            return proximalDendrite.createPool(c, inputVectorIndexes);
        }

        /**
         * Sets the permanences on the {@link ProximalDendrite} {@link Synapse}s
         * 
         * @param c				the {@link Connections} memory object
         * @param permanences	floating point degree of connectedness
         */
        public void setProximalPermanences(Connections c, double[] permanences)
        {
            proximalDendrite.setPermanences(c, permanences);
        }

        /**
         * Sets the permanences on the {@link ProximalDendrite} {@link Synapse}s
         * 
         * @param c				the {@link Connections} memory object
         * @param permanences	floating point degree of connectedness
         */
        public void setProximalPermanencesSparse(Connections c, double[] permanences, int[] indexes)
        {
            proximalDendrite.setPermanences(c, permanences, indexes);
        }

        /**
         * Delegates the call to set synapse connected indexes to this 
         * {@code Column}'s {@link ProximalDendrite}
         * @param c
         * @param connections
         */
        public void setProximalConnectedSynapsesForTest(Connections c, int[] connections)
        {
            proximalDendrite.setConnectedSynapsesForTest(c, connections);
        }

      
        /**
         * {@inheritDoc}
         * @param otherColumn     the {@code Column} to compare to
         * @return
         */
        //@Override
    //public int compareTo(Column otherColumn)
    //    {
    //        return boxedIndex(otherColumn.boxedIndex);
    //    }


        private readonly int m_Hashcode;

        public override int GetHashCode()
        {
            if (m_Hashcode == 0)
            {
                int prime = 31;
                int result = 1;
                result = prime * result + index;
                return result;
            }
            return m_Hashcode;
        }

        public bool Equals(Column obj)
        {
            if (this == obj)
                return true;
            if (obj == null)
                return false;

            if (index != obj.index)
                return false;
            else
                return true;
        }

        public int CompareTo(Column other)
        {
            if (this.index < other.index)
                return -1;
            else if (this.index > other.index)
                return 1;
            else
                return 0;
        }

        /// <summary>
        /// Gets readable version of cell.
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return $"Column: Indx:{this.getIndex()}, Cells:{this.cells.Length}";
        }
    }
}
