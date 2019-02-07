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

/* ---------------------------------------------------------------------
 * Numenta Platform for Intelligent Computing (NuPIC)
 * Copyright (C) 2014, Numenta, Inc.  Unless you have an agreement
 * with Numenta, Inc., for a separate license for this software code, the
 * following terms and conditions apply:
 *
 * This program is free software: you can redistribute it and/or modify
 * it under the terms of the GNU Affero Public License version 3 as
 * published by the Free Software Foundation.
 *
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.
 * See the GNU Affero Public License for more details.
 *
 * You should have received a copy of the GNU Affero Public License
 * along with this program.  If not, see http://www.gnu.org/licenses.
 *
 * http://numenta.org/licenses/
 * ---------------------------------------------------------------------
 */

//package org.numenta.nupic.model;

//import java.io.Serializable;
//import java.util.List;
//import java.util.Set;



using System;
using System.Collections.Generic;
using NeoCortexApi.Entities;

namespace NeoCortexApi.Entities {



    /// <summary>
    /// Defines a single cell (neuron).
    /// </summary>
    [Serializable]
    public class  Cell   : IEquatable<Cell>, IComparable<Cell> {
        /** keep it simple */
        //private static readonly long serialVersionUID = 1L;

        /** This cell's index */
        private readonly int index;
        /** Remove boxing where necessary */
        readonly Integer boxedIndex;
        /** The owning {@link Column} */
        private readonly Column column;
        /** Cash this because Cells are immutable */
        private readonly int m_Hashcode;


        /**
         * Constructs a new {@code Cell} object
         * @param column    the containing {@link Column}
         * @param colSeq    this index of this {@code Cell} within its column
         */
        public Cell(Column column, int colSeq)
        {
            this.column = column;
            this.index = column.getIndex() * column.getNumCellsPerColumn() + colSeq;
            this.boxedIndex = new Integer(index);
            //this.hashcode = hashCode();
        }

        /**
         * Returns this {@code Cell}'s index.
         * @return
         */
        public int getIndex()
        {
            return index;
        }

        /**
         * Returns the column within which this cell resides
         * @return
         */
        public Column getColumn()
        {
            return column;
        }

        /**
         * Returns the Set of {@link Synapse}s which have this cell
         * as their source cells.
         *  
         * @param   c               the connections state of the temporal memory
         *                          return an orphaned empty set.
         * @return  the Set of {@link Synapse}s which have this cell
         *          as their source cells.
         */
        public ISet<Synapse> getReceptorSynapses(Connections c)
        {
            return getReceptorSynapses(c, false);
        }

        /**
         * Returns the Set of {@link Synapse}s which have this cell
         * as their source cells.
         *  
         * @param   c               the connections state of the temporal memory
         * @param doLazyCreate      create a container for future use if true, if false
         *                          return an orphaned empty set.
         * @return  the Set of {@link Synapse}s which have this cell
         *          as their source cells.
         */
        public ISet<Synapse> getReceptorSynapses(Connections c, bool doLazyCreate)
        {
            return c.getReceptorSynapses(this, doLazyCreate);
        }

        /**
         * Returns a {@link List} of this {@code Cell}'s {@link DistalDendrite}s
         * 
         * @param   c               the connections state of the temporal memory
         * @param doLazyCreate      create a container for future use if true, if false
         *                          return an orphaned empty set.
         * @return  a {@link List} of this {@code Cell}'s {@link DistalDendrite}s
         */
        public List<DistalDendrite> getSegments(Connections c)
        {
            return getSegments(c, false);
        }

        /**
         * Returns a {@link List} of this {@code Cell}'s {@link DistalDendrite}s
         * 
         * @param   c               the connections state of the temporal memory
         * @param doLazyCreate      create a container for future use if true, if false
         *                          return an orphaned empty set.
         * @return  a {@link List} of this {@code Cell}'s {@link DistalDendrite}s
         */
        public List<DistalDendrite> getSegments(Connections c, bool doLazyCreate)
        {
            return c.getSegments(this, doLazyCreate);
        }

     

        ///**
        // * {@inheritDoc}
        // * 
        // * <em> Note: All comparisons use the cell's index only </em>
        // */
        //@Override
        //public int compareTo(Cell arg0)
        //{
        //    return boxedIndex.compareTo(arg0.boxedIndex);
        //}

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



        public bool Equals(Cell obj)
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

        public override string ToString()
        {
            return $"Cell: Indx={this.getIndex()}, [{this.column}]";
        }


        /// <summary>
        /// Compares two cells.
        /// </summary>
        /// <param name="other"></param>
        /// <returns></returns>
        public int CompareTo(Cell other)
        {
            if (this.index < other.index)
                return -1;
            else if (this.index > other.index)
                return 1;
            else
                return 0;
        }
    }
}