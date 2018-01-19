using Zavolokas.Structures;

namespace Zavolokas.ImageProcessing.PatchMatch
{
    public interface IPatchMatchNnfBuilder
    {
        void RunBuildNnfIteration(Nnf nnf, Area2DMap map, ZsImage destImage, ZsImage srcImage, Area2D destPixelsArea, ImagePatchDistanceCalculator patchDistanceCalculator, NeighboursCheckDirection direction, PatchMatchSettings settings);
        void RunRandomNnfInitIteration(Nnf nnf, Area2DMap map, ZsImage destImage, ZsImage srcImage, Area2D destPixelsArea, ImagePatchDistanceCalculator patchDistanceCalculator, PatchMatchSettings settings);
    }
}