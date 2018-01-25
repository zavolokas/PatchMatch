using System.Collections.Generic;

namespace Zavolokas.ImageProcessing.PatchMatch
{
    internal static class Utils
    {
        /// <summary>
        /// Populates tha patch with corresponding image pixel indexes.
        /// </summary>
        /// <param name="patchPixelsIndexesP">Pointer to the patch array to populate with pixel indexes.</param>
        /// <param name="x">The x coordinate of the patch center point on the image.</param>
        /// <param name="y">The y coordinate of the patch center point on the image.</param>
        /// <param name="patchSize">Size of the patch.</param>
        /// <param name="imageWidth">Width of the image.</param>
        /// <param name="allowedPointIndexesSet">
        /// The set of point indexes that can be used to fill the patch. 
        /// Indexes that are outside of this set are not marked and can not be
        /// taken into the consideration.
        /// </param>
        public static unsafe void PopulatePatchPixelsIndexes(int* patchPixelsIndexesP, int x, int y, int patchSize, int imageWidth, HashSet<int> allowedPointIndexesSet, out bool fits)
        {
            var patchOffset = (patchSize - 1) / 2;
            var length = patchSize * patchSize;
            int imagePointIndex;
            int xOffs, yOffs;
            fits = true;

            for (int patchPointIndex = length - 1; patchPointIndex >= 0; patchPointIndex--)
            {
                // Convert patch point index to offset from the patch central point
                xOffs = patchPointIndex % patchSize - patchOffset;
                yOffs = patchPointIndex / patchSize - patchOffset;

                // Find corresponding image point index
                imagePointIndex = (y + yOffs) * imageWidth + x + xOffs;
                if (allowedPointIndexesSet.Contains(imagePointIndex))
                {
                    *(patchPixelsIndexesP + patchPointIndex) = imagePointIndex;
                }
                else
                {
                    // Point is outside of the marked area.
                    *(patchPixelsIndexesP + patchPointIndex) = -1;
                    fits = false;
                }
            }
        }
    }
}