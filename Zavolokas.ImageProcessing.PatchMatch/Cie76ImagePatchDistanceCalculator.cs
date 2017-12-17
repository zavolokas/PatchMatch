using Zavolokas.Structures;

namespace Zavolokas.ImageProcessing.PatchMatch
{
    internal class Cie76ImagePatchDistanceCalculator : ImagePatchDistanceCalculator
    {
        internal override unsafe double Calculate(int* destPatchImagePixelIndexesP, int* srcPatchImagePixelIndexesP, double maxDistance, double* destPixelsP, double* sourcePixelsP, ZsImage destImage, ZsImage srcImage, int patchLength)
        {
            // Constants for distance calculation
            const double BiggestDistance = 22044.69;
            const double AvgDistance = BiggestDistance * .5;

            double distance = 0;

            var destImageStride = destImage.Stride;
            var ercImageStride = srcImage.Stride;

            var destNumberOfComponents = destImage.NumberOfComponents;
            var srcNumberOfComponents = srcImage.NumberOfComponents;

            var destImageWidth = destImage.Width;
            var srcImageWidth = srcImage.Width;

            for (int i = 0; i < patchLength; i++)
            {
                var destPixelIndex = *(destPatchImagePixelIndexesP + i);
                var srcPixelIndex = *(srcPatchImagePixelIndexesP + i);
                if (destPixelIndex != -1 && srcPixelIndex != -1)
                {
                    int dstPointY = destPixelIndex / destImageWidth;
                    int dstPointX = destPixelIndex % destImageWidth;
                    var destColorIndex = destImageStride * dstPointY + dstPointX * destNumberOfComponents;

                    int srcPointY = srcPixelIndex / srcImageWidth;
                    int srcPointX = srcPixelIndex % srcImageWidth;
                    var sourceColorIndex = ercImageStride * srcPointY + srcPointX * srcNumberOfComponents;

                    //compute deltaE using CIE76, but without taking the square root to gain better speed performance
                    var dL = *(destPixelsP + destColorIndex + 0) - *(sourcePixelsP + sourceColorIndex + 0);
                    var da = *(destPixelsP + destColorIndex + 1) - *(sourcePixelsP + sourceColorIndex + 1);
                    var db = *(destPixelsP + destColorIndex + 2) - *(sourcePixelsP + sourceColorIndex + 2);
                    distance += dL * dL + da * da + db * db;
                }
                else
                {
                    distance += BiggestDistance;// AvgDistance;
                }

                if (distance > maxDistance)
                    break;
            }

            return distance;
        }
    }
}