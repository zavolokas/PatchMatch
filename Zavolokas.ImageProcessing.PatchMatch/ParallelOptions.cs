namespace Zavolokas.ImageProcessing.PatchMatch
{
    /// <summary>
    /// Holds settings for the parallel processing.
    /// </summary>
    public class ParallelOptions
    {
        /// <summary>
        /// Amount of elements(pixels) that should be processed in one thread. To control the overhead.
        /// </summary>
        /// <value>
        /// The not dividable minimum amount elements.
        /// </value>
        public int NotDividableMinAmountElements { get; set; } = 80;

        /// <summary>
        /// Gets or sets the threads count (good when matches to the amount of processor's cores).
        /// </summary>
        /// <value>
        /// The threads count.
        /// </value>
        public byte ThreadsCount { get; set; } = 4;
    }
}