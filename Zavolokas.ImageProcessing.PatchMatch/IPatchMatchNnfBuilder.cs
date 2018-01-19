using Zavolokas.Structures;

namespace Zavolokas.ImageProcessing.PatchMatch
{
    public interface IPatchMatchNnfBuilder
    {
        void RunRandomNnfInitIteration(Nnf nnf, ZsImage destImage, ZsImage srcImage, PatchMatchSettings settings, ImagePatchDistanceCalculator patchDistanceCalculator, Area2DMap map, Area2D destPixelsArea);
        void RunBuildNnfIteration(Nnf nnf, ZsImage destImage, ZsImage srcImage, NeighboursCheckDirection direction, PatchMatchSettings settings, ImagePatchDistanceCalculator patchDistanceCalculator, Area2DMap map, Area2D destPixelsArea);
    }
}