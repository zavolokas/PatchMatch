using Zavolokas.Structures;

namespace Zavolokas.ImageProcessing.PatchMatch
{
    public abstract class ImagePatchDistanceCalculator
    {
        internal abstract unsafe double Calculate(int* destPatchImagePixelIndexesP, int* srcPatchImagePixelIndexesP,
            double maxDistance, double* destPixelsP, double* sourcePixelsP, ZsImage destImage, ZsImage srcImage,
            int patchWidth);
    }
}