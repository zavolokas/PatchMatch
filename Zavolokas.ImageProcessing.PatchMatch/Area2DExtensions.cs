using Zavolokas.Structures;

namespace Zavolokas.ImageProcessing.PatchMatch
{
    internal static class Area2DExtensions
    {
        /// <summary>
        /// Gets the area points indexes.
        /// </summary>
        /// <param name="area">The area.</param>
        /// <param name="imageWidth">Width of the image.</param>
        /// <param name="forward">if set to <c>true</c> [forward].</param>
        /// <returns></returns>
        public static int[] GetAreaPointsIndexes(this Area2D area, int imageWidth, bool forward = true)
        {
            int[] dstPointIndexes = new int[area.ElementsCount];
            area.FillMappedPointsIndexes(dstPointIndexes, imageWidth, forward);
            return dstPointIndexes;
        }
    }
}