﻿using NeoCortexApi.Entities;
using NeoCortexApi.Utility;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using System.Diagnostics;

/**
 * Handles the relationships between the columns of a region 
 * and the inputs bits. The primary public interface to this function is the 
 * "compute" method, which takes in an input vector and returns a list of 
 * activeColumns columns.
 * Example Usage:
 * >
 * > SpatialPooler sp = SpatialPooler();
 * > Connections c = new Connections();
 * > sp.init(c);
 * > for line in file:
 * >   inputVector = prepared int[] (containing 1's and 0's)
 * >   sp.compute(inputVector)
 * 
 * @author David Ray
 *
 */

namespace NeoCortexApi
{
    public class SpatialPooler
    {
        #region Private Fields

        /** Default Serial Version  */
        private static readonly long serialVersionUID = 1L;

        #endregion

        #region Properties

        public double MaxInibitionDensity { get; set; } = 0.5;

        #endregion

        #region Constructors and Initialization
        
        ///Constructs a new {@code SpatialPooler}
        
        public SpatialPooler() { }

        
        public void init(Connections c)
        {
            if (c.NumActiveColumnsPerInhArea == 0 && (c.LocalAreaDensity == 0 ||
                c.LocalAreaDensity > 0.5))
            {
                throw new ArgumentException("Inhibition parameters are invalid");
            }

            c.doSpatialPoolerPostInit();
            InitMatrices(c);
            ConnectAndConfigureInputs(c);
        }

        #endregion

        #region Public Methods

        
        ///Called to initialize the structural anatomy with configured values and prepare
        ///the anatomical entities for activation.
        /// @param c
        

        public void InitMatrices(Connections c)
        {
            SparseObjectMatrix<Column> mem = c.getMemory();
            c.setMemory(mem == null ?
                mem = new SparseObjectMatrix<Column>(c.getColumnDimensions()) : mem);

            c.setInputMatrix(new SparseBinaryMatrix(c.getInputDimensions()));

            // Initiate the topologies
            c.setColumnTopology(new Topology(c.getColumnDimensions()));
            c.setInputTopology(new Topology(c.getInputDimensions()));

            //Calculate numInputs and numColumns
            int numInputs = c.getInputMatrix().getMaxIndex() + 1;
            int numColumns = c.getMemory().getMaxIndex() + 1;
            if (numColumns <= 0)
            {
                throw new ArgumentException("Invalid number of columns: " + numColumns);
            }
            if (numInputs <= 0)
            {
                throw new ArgumentException("Invalid number of inputs: " + numInputs);
            }
            c.NumInputs = numInputs;
            c.setNumColumns(numColumns);

            //Fill the sparse matrix with column objects
            for (int i = 0; i < numColumns; i++) { mem.set(i, new Column(c.getCellsPerColumn(), i)); }

            c.setPotentialPools(new SparseObjectMatrix<Pool>(c.getMemory().getDimensions()));

            c.setConnectedMatrix(new SparseBinaryMatrix(new int[] { numColumns, numInputs }));

            //Initialize state meta-management statistics
            c.setOverlapDutyCycles(new double[numColumns]);
            c.setActiveDutyCycles(new double[numColumns]);
            c.setMinOverlapDutyCycles(new double[numColumns]);
            c.setMinActiveDutyCycles(new double[numColumns]);
            c.BoostFactors = (new double[numColumns]);
            ArrayUtils.fillArray(c.BoostFactors, 1);
        }

        
        ///Step two of pooler initialization kept separate from initialization
        ///of static members so that they may be set at a different point in 
        ///the initialization (as sometimes needed by tests).
        /// This step prepares the proximal dendritic synapse pools with their 
        ///initial permanence values and connected inputs.
        /// @param c     the {@link Connections} memory
        

        public void ConnectAndConfigureInputs(Connections c)
        {
            // Initialize the set of permanence values for each column. Ensure that
            // each column is connected to enough input bits to allow it to be
            // activated.
            int numColumns = c.getNumColumns();
            for (int i = 0; i < numColumns; i++)
            {
                // Gets RF
                int[] potential = MapPotential(c, i, c.isWrapAround());
                Column column = c.getColumn(i);

                // This line initializes all synased in the potential pool of synapces.
                // After initialization permancences are set to zero.
                c.getPotentialPools().set(i, column.createPotentialPool(c, potential));

                double[] perm = InitPermanence(c, potential, i, c.getInitConnectedPct());

                UpdatePermanencesForColumn(c, perm, column, potential, true);
            }

            // The inhibition radius determines the size of a column's local
            // neighborhood.  A cortical column must overcome the overlap score of
            // columns in its neighborhood in order to become active. This radius is
            // updated every learning round. It grows and shrinks with the average
            // number of connected synapses per column.
            UpdateInhibitionRadius(c);
        }

        
        ///This is the primary public method of the SpatialPooler class. This
        ///function takes a input vector and outputs the indices of the active columns.
        ///If 'learn' is set to True, this method also updates the permanences of the
        ///columns. 
        ///@param inputVector       An array of 0's and 1's that comprises the input to
        ///                         the spatial pooler. The array will be treated as a one
        ///                         dimensional array, therefore the dimensions of the array
        ///                         do not have to match the exact dimensions specified in the
        ///                         class constructor. In fact, even a list would suffice.
        ///                         The number of input bits in the vector must, however,
        ///                         match the number of bits specified by the call to the
        ///                         constructor. Therefore there must be a '0' or '1' in the
        ///                         array for every input bit.
        ///@param activeArray       An array whose size is equal to the number of columns.
        ///                         Before the function returns this array will be populated
        ///                         with 1's at the indices of the active columns, and 0's
        ///                         everywhere else.
        ///@param learn             A boolean value indicating whether learning should be
        ///                         performed. Learning entails updating the  permanence
        ///                         values of the synapses, and hence modifying the 'state'
        ///                         of the model. Setting learning to 'off' freezes the SP
        ///                         and has many uses. For example, you might want to feed in
        ///                         various inputs and examine the resulting SDR's.
        ///

        public void Compute(Connections c, int[] inputVector, int[] activeArray, bool learn)
        {
            if (inputVector.Length != c.NumInputs)
            {
                throw new ArgumentException(
                        "Input array must be same size as the defined number of inputs: From Params: " + c.NumInputs +
                        ", From Input Vector: " + inputVector.Length);
            }

            UpdateBookeepingVars(c, learn);

            // Gets overlap ove every single column.
            var overlaps = CalculateOverlap(c, inputVector);
            c.Overlaps = overlaps;

            double[] boostedOverlaps;

            //
            // We perform boosting here and right after that, we will recalculate bossted factors for next cycle.
            if (learn)
            {
                boostedOverlaps = ArrayUtils.multiply(c.BoostFactors, overlaps);
            }
            else
            {
                boostedOverlaps = ArrayUtils.toDoubleArray(overlaps);
            }

            c.BoostedOverlaps = boostedOverlaps;

            int[] activeColumns = InhibitColumns(c, boostedOverlaps);

            if (learn)
            {
                AdaptSynapses(c, inputVector, activeColumns);
                UpdateDutyCycles(c, overlaps, activeColumns);
                BumpUpWeakColumns(c);
                UpdateBoostFactors(c);
                if (IsUpdateRound(c))
                {
                    UpdateInhibitionRadius(c);
                    UpdateMinDutyCycles(c);
                }
            }

            ArrayUtils.fillArray(activeArray, 0);
            if (activeColumns.Length > 0)
            {
                ArrayUtils.setIndexesTo(activeArray, activeColumns, 1);
            }
        }

        
        ///Removes the set of columns who have never been active from the set of
        /// active columns selected in the inhibition round. Such columns cannot
        /// represent learned pattern and are therefore meaningless if only inference
        /// is required. This should not be done when using a random, unlearned SP
        /// since you would end up with no active columns.
        ///  
        /// @param activeColumns An array containing the indices of the active columns
        /// @return  a list of columns with a chance of activation
        
        public int[] StripUnlearnedColumns(Connections c, int[] activeColumns)
        {
            //TIntHashSet active = new TIntHashSet(activeColumns);
            //TIntHashSet aboveZero = new TIntHashSet();
            //int numCols = c.getNumColumns();
            //double[] colDutyCycles = c.getActiveDutyCycles();
            //for (int i = 0; i < numCols; i++)
            //{
            //    if (colDutyCycles[i] <= 0)
            //    {
            //        aboveZero.add(i);
            //    }
            //}
            //active.removeAll(aboveZero);
            //TIntArrayList l = new TIntArrayList(active);
            //l.sort();

            //return Arrays.stream(activeColumns).filter(i->c.getActiveDutyCycles()[i] > 0).toArray();



            ////TINTHashSet 
            //HashSet<int> active = new HashSet<int>(activeColumns);
            //HashSet<int> aboveZero = new HashSet<int>();

            //int numCols = c.getNumColumns();
            //double[] colDutyCycles = c.getActiveDutyCycles();
            //for (int i = 0; i < numCols; i++)
            //{
            //    if (colDutyCycles[i] <= 0)
            //    {
            //        aboveZero.Add(i);
            //    }
            //}

            //foreach (var inactiveColumn in aboveZero)
            //{
            //    active.Remove(inactiveColumn);
            //}
            ////active.Remove(aboveZero);
            ////List<int> l = new List<int>(active);
            ////l.sort();

            var res = activeColumns.Where(i => c.getActiveDutyCycles()[i] > 0).ToArray();
            return res;
            //return Arrays.stream(activeColumns).filter(i->c.getActiveDutyCycles()[i] > 0).toArray();
        }

        
        /// Updates the minimum duty cycles defining normal activity for a column. A
        /// column with activity duty cycle below this minimum threshold is boosted.
        ///  
        /// @param c
         
        public void UpdateMinDutyCycles(Connections c)
        {
            if (c.GlobalInhibition || c.InhibitionRadius > c.NumInputs)
            {
                UpdateMinDutyCyclesGlobal(c);
            }
            else
            {
                UpdateMinDutyCyclesLocal(c);
            }
        }
        
        /// Updates the minimum duty cycles in a global fashion. Sets the minimum duty
        /// cycles for the overlap and activation of all columns to be a percent of the
        /// maximum in the region, specified by {@link Connections#getMinOverlapDutyCycles()} and
        /// minPctActiveDutyCycle respectively. Functionality it is equivalent to
        /// {@link #updateMinDutyCyclesLocal(Connections)}, but this function exploits the globalness of the
        /// computation to perform it in a straightforward, and more efficient manner.
        /// 
        /// @param c
        

        public void UpdateMinDutyCyclesGlobal(Connections c)
        {
            ArrayUtils.fillArray(c.getMinOverlapDutyCycles(),
                   (double)(c.getMinPctOverlapDutyCycles() * ArrayUtils.max(c.getOverlapDutyCycles())));

            ArrayUtils.fillArray(c.getMinActiveDutyCycles(),
                    (double)(c.getMinPctActiveDutyCycles() * ArrayUtils.max(c.getActiveDutyCycles())));
        }

        
        /// Gets a neighborhood of columns.
        ///
        /// Simply calls topology.neighborhood or topology.wrappingNeighborhood
        /// 
        /// A subclass can insert different topology behavior by overriding this method.
        ///
        /// @param c                     the {@link Connections} memory encapsulation
        /// @param centerColumn          The center of the neighborhood.
        /// @param inhibitionRadius      Span of columns included in each neighborhood
        /// @return                      The columns in the neighborhood (1D)
        

        public int[] GetColumnNeighborhood(Connections c, int centerColumn, int inhibitionRadius)
        {
            return c.isWrapAround() ?
                c.getColumnTopology().wrappingNeighborhood(centerColumn, inhibitionRadius) :
                    c.getColumnTopology().GetNeighborhood(centerColumn, inhibitionRadius);
        }
        
        /// Updates the minimum duty cycles. The minimum duty cycles are determined
        /// locally. Each column's minimum duty cycles are set to be a percent of the
        /// maximum duty cycles in the column's neighborhood. Unlike
        /// {@link #updateMinDutyCyclesGlobal(Connections)}, here the values can be 
        /// quite different for different columns.
        /// 
        /// @param c
        
        public void UpdateMinDutyCyclesLocal(Connections c)
        {
            int len = c.getNumColumns();
            int inhibitionRadius = c.InhibitionRadius;
            double[] activeDutyCycles = c.getActiveDutyCycles();
            double minPctActiveDutyCycles = c.getMinPctActiveDutyCycles();
            double[] overlapDutyCycles = c.getOverlapDutyCycles();
            double minPctOverlapDutyCycles = c.getMinPctOverlapDutyCycles();

            Parallel.For(0, len, (i) =>
            {
                int[] neighborhood = GetColumnNeighborhood(c, i, inhibitionRadius);

                double maxActiveDuty = ArrayUtils.max(
                    ArrayUtils.ListOfValuesByIndicies(activeDutyCycles, neighborhood));
                double maxOverlapDuty = ArrayUtils.max(
                    ArrayUtils.ListOfValuesByIndicies(overlapDutyCycles, neighborhood));

                c.getMinActiveDutyCycles()[i] = maxActiveDuty * minPctActiveDutyCycles;

                c.getMinOverlapDutyCycles()[i] = maxOverlapDuty * minPctOverlapDutyCycles;
            });

            //// Parallelize for speed up
            //IntStream.range(0, len).forEach(i-> {
            //    int[] neighborhood = getColumnNeighborhood(c, i, inhibitionRadius);

            //    double maxActiveDuty = ArrayUtils.max(
            //        ArrayUtils.sub(activeDutyCycles, neighborhood));
            //    double maxOverlapDuty = ArrayUtils.max(
            //        ArrayUtils.sub(overlapDutyCycles, neighborhood));

            //    c.getMinActiveDutyCycles()[i] = maxActiveDuty * minPctActiveDutyCycles;

            //    c.getMinOverlapDutyCycles()[i] = maxOverlapDuty * minPctOverlapDutyCycles;
            //});
        }

        
        /// Updates the duty cycles for each column. The OVERLAP duty cycle is a moving
        /// average of the number of inputs which overlapped with each column. The
        /// ACTIVITY duty cycles is a moving average of the frequency of activation for
        /// each column.
        /// 
        /// @param c                 the {@link Connections} (spatial pooler memory)
        /// @param overlaps          an array containing the overlap score for each column.
        ///                          The overlap score for a column is defined as the number
        ///                          of synapses in a "connected state" (connected synapses)
        ///                          that are connected to input bits which are turned on.
        /// @param activeColumns     An array containing the indices of the active columns,
        ///                          the sparse set of columns which survived inhibition
        

        public void UpdateDutyCycles(Connections c, int[] overlaps, int[] activeColumns)
        {
            // All columns with overlap are set to 1. Otherwise 0.
            double[] overlapArray = new double[c.getNumColumns()];

            // All active columns are set on 1, otherwise 0.
            double[] activeArray = new double[c.getNumColumns()];

            //
            // if (sourceA[i] > 0) then targetB[i] = 1;
            // This ensures that all values in overlapArray are set to 1, if column has some overlap.
            ArrayUtils.greaterThanXThanSetToYInB(overlaps, overlapArray, 0, 1);
            if (activeColumns.Length > 0)
            {
                // After this step, all rows in activeArray are set to 1 at the index of active column.
                ArrayUtils.setIndexesTo(activeArray, activeColumns, 1);
            }

            int period = c.getDutyCyclePeriod();
            if (period > c.getIterationNum())
            {
                period = c.getIterationNum();
            }

            c.setOverlapDutyCycles(
                    UpdateDutyCyclesHelper(c, c.getOverlapDutyCycles(), overlapArray, period));

            c.setActiveDutyCycles(
                    UpdateDutyCyclesHelper(c, c.getActiveDutyCycles(), activeArray, period));
        }

        
        /// Updates a duty cycle estimate with a new value. This is a helper
        /// function that is used to update several duty cycle variables in
        /// the Column class, such as: overlapDutyCucle, activeDutyCycle,
        /// minPctDutyCycleBeforeInh, minPctDutyCycleAfterInh, etc. returns
        /// the updated duty cycle. Duty cycles are updated according to the following
        /// formula:
        /// 
        ///  
        ///                (period - 1)*dutyCycle + newValue
        ///  dutyCycle := ----------------------------------
        ///                        period
        ///
        /// @param c             the {@link Connections} (spatial pooler memory)
        /// @param dutyCycles    An array containing one or more duty cycle values that need
        ///                      to be updated
        /// @param newInput      A new numerical value used to update the duty cycle. Typically 1 or 0
        /// @param period        The period of the duty cycle
        /// @return
        

        public double[] UpdateDutyCyclesHelper(Connections c, double[] dutyCycles, double[] newInput, double period)
        {
            return ArrayUtils.divide(ArrayUtils.d_add(ArrayUtils.multiply(dutyCycles, period - 1), newInput), period);
        }

        
        /// Update the inhibition radius. The inhibition radius is a measure of the
        /// square (or hypersquare) of columns that each a column is "connected to"
        /// on average. Since columns are not connected to each other directly, we
        /// determine this quantity by first figuring out how many *inputs* a column is
        /// connected to, and then multiplying it by the total number of columns that
        /// exist for each input. For multiple dimension the aforementioned
        /// calculations are averaged over all dimensions of inputs and columns. This
        /// value is meaningless if global inhibition is enabled.
        /// 
        /// @param c     the {@link Connections} (spatial pooler memory)
        

        public void UpdateInhibitionRadius(Connections c)
        {
            if (c.GlobalInhibition)
            {
                c.InhibitionRadius = ArrayUtils.max(c.getColumnDimensions());
                return;
            }

            List<double> avgCollected = new List<double>();
            int len = c.getNumColumns();
            for (int i = 0; i < len; i++)
            {
                avgCollected.Add(GetAvgSpanOfConnectedSynapsesForColumn(c, i));
            }
            double avgConnectedSpan = ArrayUtils.average(avgCollected.ToArray());

            double diameter = avgConnectedSpan * AvgColumnsPerInput(c);
            double radius = (diameter - 1) / 2.0d;
            radius = Math.Max(1, radius);

            c.InhibitionRadius = (int)(radius + 0.5);
        }
        
        /// The average number of columns per input, taking into account the topology
        /// of the inputs and columns. This value is used to calculate the inhibition
        /// radius. This function supports an arbitrary number of dimensions. If the
        /// number of column dimensions does not match the number of input dimensions,
        /// we treat the missing, or phantom dimensions as 'ones'.
        ///  
        /// @param c     the {@link Connections} (spatial pooler memory)
        /// @return
        

        public virtual double AvgColumnsPerInput(Connections c)
        {
            //int[] colDim = Array.Copy(c.getColumnDimensions(), c.getColumnDimensions().Length);
            int[] colDim = new int[c.getColumnDimensions().Length];
            Array.Copy(c.getColumnDimensions(), colDim, c.getColumnDimensions().Length);

            int[] inputDim = new int[c.getInputDimensions().Length];
            Array.Copy(c.getInputDimensions(), inputDim, c.getInputDimensions().Length);

            double[] columnsPerInput = ArrayUtils.divide(
                ArrayUtils.toDoubleArray(colDim), ArrayUtils.toDoubleArray(inputDim), 0, 0);
            return ArrayUtils.average(columnsPerInput);
        }

       
       /// The range of connectedSynapses per column, averaged for each dimension.
       /// This value is used to calculate the inhibition radius. This variation of
       /// the function supports arbitrary column dimensions.
       ///  
       /// @param c             the {@link Connections} (spatial pooler memory)
       /// @param columnIndex   the current column for which to avg.
       /// @return
      

        /// <summary>
        /// It traverses all connected synapses of the column and calculates the span, which synapses
        /// spans between all input bits. Then it calculates average of spans accross all dimensions. 
        /// </summary>
        /// <param name="c"></param>
        /// <param name="columnIndex"></param>
        /// <returns></returns>
        public virtual double GetAvgSpanOfConnectedSynapsesForColumn(Connections c, int columnIndex)
        {
            int[] dimensions = c.getInputDimensions();

            // Gets synapses connected to input bits.(from pool of the column)
            int[] connected = c.getColumn(columnIndex).getProximalDendrite().getConnectedSynapsesSparse(c);

            if (connected == null || connected.Length == 0) return 0;

            int[] maxCoord = new int[c.getInputDimensions().Length];
            int[] minCoord = new int[c.getInputDimensions().Length];
            ArrayUtils.fillArray(maxCoord, -1);
            ArrayUtils.fillArray(minCoord, ArrayUtils.max(dimensions));
            ISparseMatrix<int> inputMatrix = c.getInputMatrix();

            //
            // It takes all connected synapses
            // 
            for (int i = 0; i < connected.Length; i++)
            {
                maxCoord = ArrayUtils.maxBetween(maxCoord, inputMatrix.computeCoordinates(connected[i]));
                minCoord = ArrayUtils.minBetween(minCoord, inputMatrix.computeCoordinates(connected[i]));
            }
            return ArrayUtils.average(ArrayUtils.add(ArrayUtils.subtract(maxCoord, minCoord), 1));
        }

        
        /// The primary method in charge of learning. Adapts the permanence values of
        /// the synapses based on the input vector, and the chosen columns after
        /// inhibition round. Permanence values are increased for synapses connected to
        /// input bits that are turned on, and decreased for synapses connected to
        /// inputs bits that are turned off.
        /// 
        /// @param c                 the {@link Connections} (spatial pooler memory)
        /// @param inputVector       a integer array that comprises the input to
        ///                          the spatial pooler. There exists an entry in the array
        ///                          for every input bit.
        /// @param activeColumns     an array containing the indices of the columns that
        ///                          survived inhibition.
        

        public void AdaptSynapses(Connections c, int[] inputVector, int[] activeColumns)
        {
            //int[] inputIndices = ArrayUtils.where(inputVector, ArrayUtils.INT_GREATER_THAN_0);

            // Get all indicies of input vector, which are set on '1'.
            var inputIndices = ArrayUtils.IndexWhere(inputVector, inpBit => inpBit > 0);

            double[] permChanges = new double[c.NumInputs];

            // First we initialize all permChanges to minimum decrement values,
            // which are used in a case of none-connections to input.
            ArrayUtils.fillArray(permChanges, -1 * c.getSynPermInactiveDec());

            // Then we update all connected permChanges to increment values for connected values.
            // Permanences are set in conencted input bits to default incremental value.
            ArrayUtils.setIndexesTo(permChanges, inputIndices.ToArray(), c.getSynPermActiveInc());
            for (int i = 0; i < activeColumns.Length; i++)
            {
                Pool pool = c.getPotentialPools().get(activeColumns[i]);
                double[] perm = pool.getDensePermanences(c);
                int[] indexes = pool.getSparsePotential();
                ArrayUtils.raiseValuesBy(permChanges, perm);
                Column col = c.getColumn(activeColumns[i]);
                UpdatePermanencesForColumn(c, perm, col, indexes, true);
            }
        }

        
        /// This method increases the permanence values of synapses of columns whose
        /// activity level has been too low. Such columns are identified by having an
        /// overlap duty cycle that drops too much below those of their peers. The
        /// permanence values for such columns are increased.
        ///  
        /// @param c
        

        public void BumpUpWeakColumns(Connections c)
        {
            //    int[] weakColumns = ArrayUtils.where(c.getMemory().get1DIndexes(), new Condition.Adapter<Integer>() {
            //        @Override public boolean eval(int i)
            //    {
            //        return c.getOverlapDutyCycles()[i] < c.getMinOverlapDutyCycles()[i];
            //    }
            //});

            var weakColumns = c.getMemory().get1DIndexes().Where(i => c.getOverlapDutyCycles()[i] < c.getMinOverlapDutyCycles()[i]).ToArray();

            for (int i = 0; i < weakColumns.Length; i++)
            {
                Pool pool = c.getPotentialPools().get(weakColumns[i]);
                double[] perm = pool.getSparsePermanences();
                ArrayUtils.raiseValuesBy(c.getSynPermBelowStimulusInc(), perm);
                int[] indexes = pool.getSparsePotential();
                Column col = c.getColumn(weakColumns[i]);
                UpdatePermanencesForColumnSparse(c, perm, col, indexes, true);
            }
        }

        
        /// This method ensures that each column has enough connections to input bits
        /// to allow it to become active. Since a column must have at least
        /// 'stimulusThreshold' overlaps in order to be considered during the
        /// inhibition phase, columns without such minimal number of connections, even
        /// if all the input bits they are connected to turn on, have no chance of
        /// obtaining the minimum threshold. For such columns, the permanence values
        /// are increased until the minimum number of connections are formed.
        /// 
        /// @param c                 the {@link Connections} memory
        /// @param perm              the permanence values
        /// @param maskPotential         
        

        public virtual void RaisePermanenceToThreshold(Connections c, double[] perm, int[] maskPotential)
        {
            if (maskPotential.Length < c.StimulusThreshold)
            {
                throw new ArgumentException("This is likely due to a " +
                    "value of stimulusThreshold that is too large relative " +
                    "to the input size. [len(mask) < self._stimulusThreshold]");
            }

            ArrayUtils.clip(perm, c.getSynPermMin(), c.getSynPermMax());
            while (true)
            {
                int numConnected = ArrayUtils.valueGreaterCountAtIndex(c.getSynPermConnected(), perm, maskPotential);
                if (numConnected >= c.StimulusThreshold) return;
                ArrayUtils.raiseValuesBy(c.getSynPermBelowStimulusInc(), perm, maskPotential);
            }
        }

        /**
         * This method ensures that each column has enough connections to input bits
         * to allow it to become active. Since a column must have at least
         * 'stimulusThreshold' overlaps in order to be considered during the
         * inhibition phase, columns without such minimal number of connections, even
         * if all the input bits they are connected to turn on, have no chance of
         * obtaining the minimum threshold. For such columns, the permanence values
         * are increased until the minimum number of connections are formed.
         * 
         * Note: This method services the "sparse" versions of corresponding methods
         * 
         * @param c         The {@link Connections} memory
         * @param perm      permanence values
         */

        public void RaisePermanenceToThresholdSparse(Connections c, double[] perm)
        {
            ArrayUtils.clip(perm, c.getSynPermMin(), c.getSynPermMax());
            while (true)
            {
                int numConnected = ArrayUtils.valueGreaterCount(c.getSynPermConnected(), perm);
                if (numConnected >= c.StimulusThreshold) return;
                ArrayUtils.raiseValuesBy(c.getSynPermBelowStimulusInc(), perm);
            }
        }

        
        /// This method updates the permanence matrix with a column's new permanence
        /// values. The column is identified by its index, which reflects the row in
        /// the matrix, and the permanence is given in 'sparse' form, i.e. an array
        /// whose members are associated with specific indexes. It is in
        /// charge of implementing 'clipping' - ensuring that the permanence values are
        /// always between 0 and 1 - and 'trimming' - enforcing sparseness by zeroing out
        /// all permanence values below 'synPermTrimThreshold'. It also maintains
        /// the consistency between 'permanences' (the matrix storing the
        /// permanence values), 'connectedSynapses', (the matrix storing the bits
        /// each column is connected to), and 'connectedCounts' (an array storing
        /// the number of input bits each column is connected to). Every method wishing
        /// to modify the permanence matrix should do so through this method.
        /// 
        /// @param c                 the {@link Connections} which is the memory model.
        /// @param perm              An array of permanence values for a column. The array is
        ///                          "dense", i.e. it contains an entry for each input bit, even
        ///                          if the permanence value is 0.
        /// @param column            The column in the permanence, potential and connectivity matrices
        /// @param maskPotential     The indexes of inputs in the specified {@link Column}'s pool.
        /// @param raisePerm         a boolean value indicating whether the permanence values
        

        public void UpdatePermanencesForColumn(Connections c, double[] perm, Column column, int[] maskPotential, bool raisePerm)
        {
            if (raisePerm)
            {
                RaisePermanenceToThreshold(c, perm, maskPotential);
            }

            ArrayUtils.lessThanOrEqualXThanSetToY(perm, c.getSynPermTrimThreshold(), 0);
            ArrayUtils.clip(perm, c.getSynPermMin(), c.getSynPermMax());
            column.setProximalPermanences(c, perm);
        }

        
        /// This method updates the permanence matrix with a column's new permanence
        /// values. The column is identified by its index, which reflects the row in
        /// the matrix, and the permanence is given in 'sparse' form, (i.e. an array
        /// whose members are associated with specific indexes). It is in
        /// charge of implementing 'clipping' - ensuring that the permanence values are
        /// always between 0 and 1 - and 'trimming' - enforcing sparseness by zeroing out
        /// all permanence values below 'synPermTrimThreshold'. Every method wishing
        /// to modify the permanence matrix should do so through this method.
        /// 
        /// @param c                 the {@link Connections} which is the memory model.
        /// @param perm              An array of permanence values for a column. The array is
        ///                          "sparse", i.e. it contains an entry for each input bit, even
        ///                          if the permanence value is 0.
        /// @param column            The column in the permanence, potential and connectivity matrices
        /// @param raisePerm         a boolean value indicating whether the permanence values
        

        public void UpdatePermanencesForColumnSparse(Connections c, double[] perm, Column column, int[] maskPotential, bool raisePerm)
        {
            if (raisePerm)
            {
                RaisePermanenceToThresholdSparse(c, perm);
            }

            ArrayUtils.lessThanOrEqualXThanSetToY(perm, c.getSynPermTrimThreshold(), 0);
            ArrayUtils.clip(perm, c.getSynPermMin(), c.getSynPermMax());
            column.setProximalPermanencesSparse(c, perm, maskPotential);
        }

        
        /// Returns a randomly generated permanence value for a synapse that is
        /// initialized in a connected state. The basic idea here is to initialize
        /// permanence values very close to synPermConnected so that a small number of
        /// learning steps could make it disconnected or connected.
        ///
        /// Note: experimentation was done a long time ago on the best way to initialize
        /// permanence values, but the history for this particular scheme has been lost.
        /// 
        /// @return  a randomly generated permanence value
        
        public static double InitPermConnected(Connections c)
        {
            double p = c.getSynPermConnected() + (c.getSynPermMax() - c.getSynPermConnected()) * c.random.NextDouble();

            // Note from Python implementation on conditioning below:
            // Ensure we don't have too much unnecessary precision. A full 64 bits of
            // precision causes numerical stability issues across platforms and across
            // implementations
            p = ((int)(p * 100000)) / 100000.0d;
            return p;
        }

        
        /// Returns a randomly generated permanence value for a synapses that is to be
        /// initialized in a non-connected state.
        /// 
        /// @return  a randomly generated permanence value
        
        public static double InitPermNonConnected(Connections c)
        {
            double p = c.getSynPermConnected() * c.getRandom().NextDouble();

            // Note from Python implementation on conditioning below:
            // Ensure we don't have too much unnecessary precision. A full 64 bits of
            // precision causes numerical stability issues across platforms and across
            // implementations
            p = ((int)(p * 100000)) / 100000.0d;
            return p;
        }

        
        /// Initializes the permanences of a column. The method
        /// returns a 1-D array the size of the input, where each entry in the
        /// array represents the initial permanence value between the input bit
        /// at the particular index in the array, and the column represented by
        /// the 'index' parameter.
        /// 
        /// @param c                 the {@link Connections} which is the memory model
        /// @param potentialPool     An array specifying the potential pool of the column.
        ///                          Permanence values will only be generated for input bits
        ///                          corresponding to indices for which the mask value is 1.
        ///                          WARNING: potentialPool is sparse, not an array of "1's"
        /// @param index             the index of the column being initialized
        /// @param connectedPct      A value between 0 or 1 specifying the percent of the input
        ///                          bits that might maximally start off in a connected state.
        ///                          0.7 means, maximally 70% of potential might be connected
        /// @return
        

        public double[] InitPermanence(Connections c, int[] potentialPool, int colIndx, double connectedPct)
        {
            double[] perm = new double[c.NumInputs];
            foreach (int idx in potentialPool)
            {
                if (c.random.NextDouble() <= connectedPct)
                {
                    perm[idx] = InitPermConnected(c);
                }
                else
                {
                    perm[idx] = InitPermNonConnected(c);
                }

                perm[idx] = perm[idx] < c.getSynPermTrimThreshold() ? 0 : perm[idx];

            }
            c.getColumn(colIndx).setProximalPermanences(c, perm);
            return perm;
        }

        
        /// Uniform Column Mapping 
        /// Maps a column to its respective input index, keeping to the topology of
        /// the region. It takes the index of the column as an argument and determines
        /// what is the index of the flattened input vector that is to be the center of
        /// the column's potential pool. It distributes the columns over the inputs
        /// uniformly. The return value is an integer representing the index of the
        /// input bit. Examples of the expected output of this method:
        ///  If the topology is one dimensional, and the column index is 0, this
        ///   method will return the input index 0. If the column index is 1, and there
        ///   are 3 columns over 7 inputs, this method will return the input index 3.
        /// If the topology is two dimensional, with column dimensions [3, 5] and
        ///   input dimensions [7, 11], and the column index is 3, the method
        ///   returns input index 8. 
        ///   
        /// @param columnIndex   The index identifying a column in the permanence, potential
        ///                      and connectivity matrices.
        /// @return              Flat index of mapped column.
        

        public int MapColumn(Connections c, int columnIndex)
        {
            int[] columnCoords = c.getMemory().computeCoordinates(columnIndex);
            double[] colCoords = ArrayUtils.toDoubleArray(columnCoords);

            double[] columnRatios = ArrayUtils.divide(
                colCoords, ArrayUtils.toDoubleArray(c.getColumnDimensions()), 0, 0);

            double[] inputCoords = ArrayUtils.multiply(
                ArrayUtils.toDoubleArray(c.getInputDimensions()), columnRatios, 0, 0);

            var colSpanOverInputs = ArrayUtils.divide(
                        ArrayUtils.toDoubleArray(c.getInputDimensions()),
                        ArrayUtils.toDoubleArray(c.getColumnDimensions()), 0, 0);

            inputCoords = ArrayUtils.d_add(inputCoords, ArrayUtils.multiply(colSpanOverInputs, 0.5));

            // Makes sure that inputCoords are in range [0, inpDims]
            int[] inputCoordInts = ArrayUtils.clip(ArrayUtils.toIntArray(inputCoords), c.getInputDimensions(), -1);

            return c.getInputMatrix().computeIndex(inputCoordInts);
        }

        
        /// Maps a column to its input bits. This method encapsulates the topology of
        /// the region. It takes the index of the column as an argument and determines
        /// what are the indices of the input vector that are located within the
        /// column's potential pool. The return value is a list containing the indices
        /// of the input bits. The current implementation of the base class only
        /// supports a 1 dimensional topology of columns with a 1 dimensional topology
        /// of inputs. To extend this class to support 2-D topology you will need to
        /// override this method. Examples of the expected output of this method:
        /// * If the potentialRadius is greater than or equal to the entire input
        ///   space, (global visibility), then this method returns an array filled with
        ///   all the indices
        /// * If the topology is one dimensional, and the potentialRadius is 5, this
        ///   method will return an array containing 5 consecutive values centered on
        ///   the index of the column (wrapping around if necessary).
        /// * If the topology is two dimensional (not implemented), and the
        ///   potentialRadius is 5, the method should return an array containing 25
        ///   '1's, where the exact indices are to be determined by the mapping from
        ///   1-D index to 2-D position.
        /// 
        /// @param c             {@link Connections} the main memory model
        /// @param columnIndex   The index identifying a column in the permanence, potential
        ///                      and connectivity matrices.
        /// @param wrapAround    A boolean value indicating that boundaries should be
        ///                      ignored.
        /// @return
        

        public int[] MapPotential(Connections c, int columnIndex, bool wrapAround)
        {
            int centerInput = MapColumn(c, columnIndex);

            // Here we have Receptive Field (RF)
            int[] columnInputs = GetInputNeighborhood(c, centerInput, c.getPotentialRadius());

            // Select a subset of the receptive field to serve as the the potential pool.
            int numPotential = (int)(columnInputs.Length * c.getPotentialPct() + 0.5);
            int[] retVal = new int[numPotential];
            return ArrayUtils.sample(columnInputs, retVal, c.getRandom());
        }

        
        /// Performs inhibition. This method calculates the necessary values needed to
        /// actually perform inhibition and then delegates the task of picking the
        /// active columns to helper functions.
        /// 
        /// @param c             the {@link Connections} matrix
        /// @param overlaps      an array containing the overlap score for each  column.
        ///                      The overlap score for a column is defined as the number
        ///                      of synapses in a "connected state" (connected synapses)
        ///                      that are connected to input bits which are turned on.
        /// @return
        
        public virtual int[] InhibitColumns(Connections c, double[] initialOverlaps)
        {
            double[] overlaps = new List<double>(initialOverlaps).ToArray();

            double density = calcInhibitionDensity(c);

            //Add our fixed little bit of random noise to the scores to help break ties.
            //ArrayUtils.d_add(overlaps, c.getTieBreaker());

            if (c.GlobalInhibition || c.InhibitionRadius > ArrayUtils.max(c.getColumnDimensions()))
            {
                return InhibitColumnsGlobal(c, overlaps, density);
            }

            return InhibitColumnsLocal(c, overlaps, density);
        }


        /// <summary>
        ///  Perform global inhibition. Performing global inhibition entails picking the
        ///  top 'numActive' columns with the highest overlap score in the entire</summary>
        ///  region. At most half of the columns in a local neighborhood are allowed to
        ///  be active.
        /// <param name="c">Connections (memory)</param>
        /// <param name="overlaps">An array containing the overlap score for each  column.</param>
        /// <param name="density"> The fraction of the overlap score for a column is defined as the numbern of columns to survive inhibition.</param>
        /// <returns>We return all columns, whof synapses in a "connected state" (connected synapses)ich have overlap greather than stimulusThreshold.</returns>
        public virtual int[] InhibitColumnsGlobal(Connections c, double[] overlaps, double density)
        {
            int numCols = c.getNumColumns();
            int numActive = (int)(density * numCols);

            Dictionary<int, double> indices = new Dictionary<int, double>();
            for (int i = 0; i < overlaps.Length; i++)
            {
                indices.Add(i, overlaps[i]);
            }

            var sortedWinnerIndices = indices.OrderBy(k => k.Value).ToArray();

            // Enforce the stimulus threshold. This is a minimum number of synapses that must be ON in order for a columns to turn ON. 
            // The purpose of this is to prevent noise input from activating columns. Specified as a percent of a fully grown synapse.
            double stimulusThreshold = c.StimulusThreshold;

            // Calculate difference between num of columns and num of active. Num of active is less than 
            // num of columns, because of specified density.
            int start = sortedWinnerIndices.Count() - numActive;

            //
            // Here we peek columns with highest overlap
            while (start < sortedWinnerIndices.Count())
            {
                int i = sortedWinnerIndices[start].Key;
                if (overlaps[i] >= stimulusThreshold) break;
                ++start;
            }

            // We return all columns, which have overlap greather than stimulusThreshold.
            return sortedWinnerIndices.Skip(start).Select(p => (int)p.Key).ToArray();
        }

        
        /// Performs inhibition. This method calculates the necessary values needed to
        /// actually perform inhibition and then delegates the task of picking the
        /// active columns to helper functions.
        /// 
        /// @param c         the {@link Connections} matrix
        /// @param overlaps  an array containing the overlap score for each  column.
        ///                  The overlap score for a column is defined as the number
        ///                  of synapses in a "connected state" (connected synapses)
        ///                  that are connected to input bits which are turned on.
        /// @param density   The fraction of columns to survive inhibition. This
        ///                  value is only an intended target. Since the surviving
        ///                  columns are picked in a local fashion, the exact fraction
        ///                  of surviving columns is likely to vary.
        /// @return  indices of the winning columns
        

        public virtual int[] InhibitColumnsLocal(Connections c, double[] overlaps, double density)
        {
            double winnerDelta = ArrayUtils.max(overlaps) / 1000.0d;
            if (winnerDelta == 0)
            {
                winnerDelta = 0.001;
            }

            double[] tieBrokenOverlaps = new List<double>(overlaps).ToArray();

            List<int> winners = new List<int>();

            int inhibitionRadius = c.InhibitionRadius;
            for (int column = 0; column < overlaps.Length; column++)
            {
                // int column = i;
                if (overlaps[column] >= c.StimulusThreshold)
                {
                    int[] neighborhood = GetColumnNeighborhood(c, column, inhibitionRadius);

                    // Take overlapps of neighbors
                    double[] neighborhoodOverlaps = ArrayUtils.ListOfValuesByIndicies(tieBrokenOverlaps, neighborhood);

                    // Filter neighbors with overlaps bigger than column overlap
                    long numBigger = neighborhoodOverlaps.Count(d => d > overlaps[column]);

                    // density will reduce radius
                    int numActive = (int)(0.5 + density * neighborhood.Length);
                    if (numBigger < numActive)
                    {
                        winners.Add(column);
                        tieBrokenOverlaps[column] += winnerDelta;
                    }
                }
            }

            return winners.ToArray();
        }

        
        /// Update the boost factors for all columns. The boost factors are used to
        /// increase the overlap of inactive columns to improve their chances of
        /// becoming active. and hence encourage participation of more columns in the
        /// learning process. This is a line defined as: y = mx + b boost =
        /// (1-maxBoost)/minDuty * dutyCycle + maxFiringBoost. Intuitively this means
        /// that columns that have been active enough have a boost factor of 1, meaning
        /// their overlap is not boosted. Columns whose active duty cycle drops too much
        /// below that of their neighbors are boosted depending on how infrequently they
        /// have been active. The more infrequent, the more they are boosted. The exact
        /// boost factor is linearly interpolated between the points (dutyCycle:0,
        /// boost:maxFiringBoost) and (dutyCycle:minDuty, boost:1.0).
        /// 
        ///         boostFactor
        ///             ^
        /// maxBoost _  |
        ///             |\
        ///             | \
        ///       1  _  |  \ _ _ _ _ _ _ _
        ///             |
        ///             +--------------------> activeDutyCycle
        ///                |
        ///         minActiveDutyCycle
        
        public void UpdateBoostFactors(Connections c)
        {
            double[] activeDutyCycles = c.getActiveDutyCycles();
            double[] minActiveDutyCycles = c.getMinActiveDutyCycles();

            List<int> mask = new List<int>();
            //Indexes of values > 0
            for (int i = 0; i < minActiveDutyCycles.Length; i++)
            {
                if (minActiveDutyCycles[i] > 0)
                    mask.Add(i);
            }


            //        int[] mask = ArrayUtils.where(minActiveDutyCycles, ArrayUtils.GREATER_THAN_0);

            double[] boostInterim;
            if (mask.Count < 1)
            {
                boostInterim = c.BoostFactors;
            }
            else
            {
                double[] numerator = new double[c.getNumColumns()];
                ArrayUtils.fillArray(numerator, 1 - c.getMaxBoost());
                boostInterim = ArrayUtils.divide(numerator, minActiveDutyCycles, 0, 0);
                boostInterim = ArrayUtils.multiply(boostInterim, activeDutyCycles, 0, 0);
                boostInterim = ArrayUtils.d_add(boostInterim, c.getMaxBoost());
            }

            List<int> filteredIndexes = new List<int>();

            for (int i = 0; i < activeDutyCycles.Length; i++)
            {
                if (activeDutyCycles[i] > minActiveDutyCycles[i])
                {
                    filteredIndexes.Add(i);
                }
            }

            ArrayUtils.setIndexesTo(boostInterim, filteredIndexes.ToArray(), 1.0d);

            //    ArrayUtils.setIndexesTo(boostInterim, ArrayUtils.where(activeDutyCycles, new Condition.Adapter<Object>() {
            //        int i = 0;
            //    @Override public boolean eval(double d) { return d > minActiveDutyCycles[i++]; }
            //}), 1.0d);

            c.BoostFactors = boostInterim;
        }

        
        /// This function determines each column's overlap with the current input
        /// vector. The overlap of a column is the number of synapses for that column
        /// that are connected (permanence value is greater than '_synPermConnected')
        /// to input bits which are turned on. Overlap values that are lower than
        /// the 'stimulusThreshold' are ignored. The implementation takes advantage of
        /// the SpraseBinaryMatrix class to perform this calculation efficiently.
        ///
        /// @param c             the {@link Connections} memory encapsulation
        /// @param inputVector   an input array of 0's and 1's that comprises the input to
        ///                      the spatial pooler.
        /// @return
        
        public int[] CalculateOverlap(Connections c, int[] inputVector)
        {
            int[] overlaps = new int[c.getNumColumns()];
            c.getConnectedCounts().rightVecSumAtNZ(inputVector, overlaps, c.StimulusThreshold);
            return overlaps;
        }

        
        /// Return the overlap to connected counts ratio for a given column
        /// @param c
        /// @param overlaps
        /// @return
        
        public double[] CalculateOverlapPct(Connections c, int[] overlaps)
        {
            return ArrayUtils.divide(overlaps, c.getConnectedCounts().getTrueCounts());
        }
        
        
        /// Returns true if enough rounds have passed to warrant updates of
        /// duty cycles
        /// 
        /// @param c the {@link Connections} memory encapsulation
        /// @return
        

        public bool IsUpdateRound(Connections c)
        {
            return c.getIterationNum() % c.getUpdatePeriod() == 0;
        }

        
        /// Updates counter instance variables each cycle.
        ///  
        /// @param c         the {@link Connections} memory encapsulation
        /// @param learn     a boolean value indicating whether learning should be
        ///                  performed. Learning entails updating the  permanence
        ///                  values of the synapses, and hence modifying the 'state'
        ///                  of the model. setting learning to 'off' might be useful
        ///                  for indicating separate training vs. testing sets.
        

        public void UpdateBookeepingVars(Connections c, bool learn)
        {
            c.spIterationNum += 1;
            if (learn)
                c.spIterationLearnNum += 1;
        }

        
        /// Gets a neighborhood of inputs.
        /// 
        /// Simply calls topology.wrappingNeighborhood or topology.neighborhood.
        /// 
        /// A subclass can insert different topology behavior by overriding this method.
        /// 
        /// @param c                     the {@link Connections} memory encapsulation
        /// @param centerInput           The center of the neighborhood.
        /// @param potentialRadius       Span of the input field included in each neighborhood
        /// @return                      The input's in the neighborhood. (1D)
        

        public int[] GetInputNeighborhood(Connections c, int centerInput, int potentialRadius)
        {
            return c.isWrapAround() ?
                c.getInputTopology().wrappingNeighborhood(centerInput, potentialRadius) :
                    c.getInputTopology().GetNeighborhood(centerInput, potentialRadius);
        }

        #endregion

        #region Private and Protected methods

        private double calcInhibitionDensity(Connections c)
        {
            double density = c.LocalAreaDensity;
            double inhibitionArea;

            // If density is not specified then inhibition radius must be specified.
            // In that case we calculate density from inhibition radius.
            if (density <= 0)
            {
                // inhibition area can be higher than num of all columns, if 
                // radius is near to number of columns of a dimension with highest number of columns.
                // In that case we limit it to number of all columns.
                inhibitionArea = Math.Pow(2 * c.InhibitionRadius + 1, c.getColumnDimensions().Length);
                inhibitionArea = Math.Min(c.getNumColumns(), inhibitionArea);

                density = c.NumActiveColumnsPerInhArea / inhibitionArea;

                density = Math.Min(density, MaxInibitionDensity);
            }

            return density;
        }
        #endregion  
    }
}

