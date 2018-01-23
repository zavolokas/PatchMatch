using System;

namespace Zavolokas.ImageProcessing.PatchMatch
{

    /// <summary>
    /// Holds settings to control PatchMatch algorithm.
    /// </summary>
    /// <seealso cref="Zavolokas.ImageProcessing.PatchMatch.ParallelOptions" />
    /// <seealso cref="System.ICloneable" />
    public sealed class PatchMatchSettings : ParallelOptions, ICloneable
    {
        /// <summary>
        /// The patch size (should be odd).
        /// </summary>
        public byte PatchSize = 11;

        /// <summary>
        /// The iterations amount.
        /// </summary>
        public byte IterationsAmount = 5;

        /// <summary>
        /// The premerge iterations amount.
        /// </summary>
        public byte PremergeIterationsAmount = 2;

        /// <summary>
        /// The radius of the area for the random search phase of the algorithm.
        /// </summary>
        public double RandomSearchRadius = 2;

        /// <summary>
        /// The random search alpha.
        /// </summary>
        public double RandomSearchAlpha = 0.5;

        /// <summary>
        /// The minimum size of the window to search in.
        /// </summary>
        public int MinSearchWindowSize = 240;

        /// <summary>
        /// The iterations amount for the processing within a markup.
        /// </summary>
        public byte MarkupIterationsAmount = 5;

        /// <summary>
        /// Initializes a new instance of the <see cref="PatchMatchSettings"/> class.
        /// </summary>
        public PatchMatchSettings()
        {
            ThreadsCount = (byte)Environment.ProcessorCount;
            NotDividableMinAmountElements = 80;
        }

        /// <summary>
        /// Clones this instance.
        /// </summary>
        /// <returns></returns>
        public object Clone()
        {
            return new PatchMatchSettings
            {
                PatchSize = PatchSize,
                IterationsAmount = IterationsAmount,
                PremergeIterationsAmount = PremergeIterationsAmount,
                RandomSearchRadius = RandomSearchRadius,
                RandomSearchAlpha = RandomSearchAlpha,
                MinSearchWindowSize = MinSearchWindowSize,
                ThreadsCount = ThreadsCount,
                NotDividableMinAmountElements = NotDividableMinAmountElements,
                MarkupIterationsAmount = MarkupIterationsAmount,
            };
        }
    }
}