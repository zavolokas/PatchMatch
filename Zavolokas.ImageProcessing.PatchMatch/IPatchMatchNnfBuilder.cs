using Zavolokas.Structures;

namespace Zavolokas.ImageProcessing.PatchMatch
{
    public interface IPatchMatchNnfBuilder
    {
        void RunRandomNnfInitIteration(Nnf nnf, ZsImage destImage, ZsImage srcImage, PatchMatchSettings settings);
        void RunRandomNnfInitIteration(Nnf nnf, ZsImage destImage, ZsImage srcImage, PatchMatchSettings settings, ImagePatchDistanceCalculator patchDistanceCalculator);
        void RunRandomNnfInitIteration(Nnf nnf, ZsImage destImage, ZsImage srcImage, PatchMatchSettings settings, ImagePatchDistanceCalculator patchDistanceCalculator, Area2DMap areasMapping);
        void RunRandomNnfInitIteration(Nnf nnf, ZsImage destImage, ZsImage srcImage, PatchMatchSettings settings, ImagePatchDistanceCalculator patchDistanceCalculator, Area2DMap areasMapping, Area2D destPixelsArea);

        void RunBuildNnfIteration(Nnf nnf, ZsImage destImage, ZsImage srcImage, NeighboursCheckDirection direction, PatchMatchSettings settings);
        void RunBuildNnfIteration(Nnf nnf, ZsImage destImage, ZsImage srcImage, NeighboursCheckDirection direction, PatchMatchSettings settings, ImagePatchDistanceCalculator patchDistanceCalculator);
        void RunBuildNnfIteration(Nnf nnf, ZsImage destImage, ZsImage srcImage, NeighboursCheckDirection direction, PatchMatchSettings settings, ImagePatchDistanceCalculator patchDistanceCalculator, Area2DMap areasMapping);
        void RunBuildNnfIteration(Nnf nnf, ZsImage destImage, ZsImage srcImage, NeighboursCheckDirection direction, PatchMatchSettings settings, ImagePatchDistanceCalculator patchDistanceCalculator, Area2DMap areasMapping, Area2D destPixelsArea);
    }
}