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

using NeoCortexApi.Types;
using System;
using System.Collections.Generic;
using System.Text;

namespace NeoCortexApi.Entities
{
    /**
     * Contains a snapshot of the state attained during one computational
     * call to the {@link TemporalMemory}. The {@code TemporalMemory} uses
     * data from previous compute cycles to derive new data for the current cycle
     * through a comparison between states of those different cycles, therefore
     * this state container is necessary.
     * 
     * @author David Ray
     */
    public class ComputeCycle : IEquatable<object>
    {

        //private static readonly long serialVersionUID = 1L;

        public List<DistalDendrite> activeSegments = new List<DistalDendrite>();

        public List<DistalDendrite> matchingSegments = new List<DistalDendrite>();

        public ISet<Cell> m_predictiveCells = new LinkedHashSet<Cell>();

        /// <summary>
        /// Gets the list of active cells.
        /// </summary>
        public ISet<Cell> activeCells { get; set; } = new LinkedHashSet<Cell>();

        /// <summary>
        /// Gets the list of winner cells.
        /// </summary>
        public ISet<Cell> winnerCells { get; set; } = new LinkedHashSet<Cell>();

        /**
         * Constructs a new {@code ComputeCycle}
         */
        public ComputeCycle() { }

        /**
         * Constructs a new {@code ComputeCycle} initialized with
         * the connections relevant to the current calling {@link Thread} for
         * the specified {@link TemporalMemory}
         * 
         * @param   c       the current connections state of the TemporalMemory
         */
        public ComputeCycle(Connections c)
        {
            this.activeCells = new LinkedHashSet<Cell>(c.getWinnerCells());//TODO potential bug. activeCells or winnerCells?!
            this.winnerCells = new LinkedHashSet<Cell>(c.getWinnerCells());
            this.m_predictiveCells = new LinkedHashSet<Cell>(c.getPredictiveCells());
            this.activeSegments = new List<DistalDendrite>(c.getActiveSegments());
            this.matchingSegments = new List<DistalDendrite>(c.getMatchingSegments());
        }

        public int MyProperty { get; set; }
        /**
         * Returns the current {@link Set} of active cells
         * 
         * @return  the current {@link Set} of active cells
         */



        /**
         * Returns the {@link List} of sorted predictive cells.
         * @return
         */
        public ISet<Cell> predictiveCells
        {
            get
            {
                if (m_predictiveCells == null || m_predictiveCells.Count == 0)
                {
                    Cell previousCell = null;
                    Cell currCell = null;

                    foreach (DistalDendrite activeSegment in activeSegments)
                    {
                        if ((currCell = activeSegment.getParentCell()) != previousCell)
                        {
                            m_predictiveCells.Add(previousCell = currCell);
                        }
                    }
                }

                return m_predictiveCells;
            }
        }

        /* (non-Javadoc)
         * @see java.lang.Object#hashCode()
         */

        public override int GetHashCode()
        {
            int prime = 31;
            int result = 1;
            result = prime * result + ((activeCells == null) ? 0 : activeCells.GetHashCode());
            result = prime * result + ((predictiveCells == null) ? 0 : predictiveCells.GetHashCode());
            result = prime * result + ((winnerCells == null) ? 0 : winnerCells.GetHashCode());
            result = prime * result + ((activeSegments == null) ? 0 : activeSegments.GetHashCode());
            result = prime * result + ((matchingSegments == null) ? 0 : matchingSegments.GetHashCode());
            return result;
        }

        /* (non-Javadoc)
         * @see java.lang.Object#equals(java.lang.Object)
         */

        public bool Equals(Object obj)
        {
            if (this == obj)
                return true;
            if (obj == null)
                return false;
            if (this.GetType() != obj.GetType())
                return false;
            ComputeCycle other = (ComputeCycle)obj;
            if (activeCells == null)
            {
                if (other.activeCells != null)
                    return false;
            }
            else if (!activeCells.Equals(other.activeCells))
                return false;
            if (predictiveCells == null)
            {
                if (other.predictiveCells != null)
                    return false;
            }
            else if (!predictiveCells.Equals(other.predictiveCells))
                return false;
            if (winnerCells == null)
            {
                if (other.winnerCells != null)
                    return false;
            }
            else if (!winnerCells.Equals(other.winnerCells))
                return false;
            if (activeSegments == null)
            {
                if (other.activeSegments != null)
                    return false;
            }
            else if (!activeSegments.Equals(other.activeSegments))
                return false;
            if (matchingSegments == null)
            {
                if (other.matchingSegments != null)
                    return false;
            }
            else if (!matchingSegments.Equals(other.matchingSegments))
                return false;
            return true;
        }
    }
}
