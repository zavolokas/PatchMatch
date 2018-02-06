using Zavolokas.Structures;

namespace Zavolokas.ImageProcessing.PatchMatch
{
    public interface IPatchMatchNnfBuilder
    {
        /// <summary>
        /// Runs the random NNF initialization iteration using the whole areas of the dest and the source images. Uses CIE76 to calculate patch similarity.
        /// </summary>
        /// <param name="nnf">The NNF.</param>
        /// <param name="destImage">The dest image. For each patch at this image we will look for a similar one at the source image.</param>
        /// <param name="srcImage">The source image. Source of the patches for the dest image. </param>
        /// <param name="settings">The settings that control parameters of the algorithm.</param>
        void RunRandomNnfInitIteration(Nnf nnf, ZsImage destImage, ZsImage srcImage, PatchMatchSettings settings);

        /// <summary>
        /// Runs the random NNF initialization iteration using the whole areas of the dest and the source images.
        /// </summary>
        /// <param name="nnf">The NNF.</param>
        /// <param name="destImage">The dest image. For each patch at this image we will look for a similar one at the source image.</param>
        /// <param name="srcImage">The source image. Source of the patches for the dest image.</param>
        /// <param name="settings">The settings that control parameters of the algorithm.</param>
        /// <param name="patchDistanceCalculator">The calculator that calculates similarity of two patches. By deafult the Cie76 is used.</param>
        void RunRandomNnfInitIteration(Nnf nnf, ZsImage destImage, ZsImage srcImage, PatchMatchSettings settings, ImagePatchDistanceCalculator patchDistanceCalculator);

        /// <summary>
        /// Runs the random NNF initialization iteration for the associated areas of the dest and the source images.
        /// </summary>
        /// <param name="nnf">The NNF.</param>
        /// <param name="destImage">The dest image. For each patch at this image we will look for a similar one at the source image.</param>
        /// <param name="srcImage">The source image. Source of the patches for the dest image.</param>
        /// <param name="settings">The settings that control parameters of the algorithm.</param>
        /// <param name="patchDistanceCalculator">The calculator that calculates similarity of two patches. By deafult the Cie76 is used.</param>
        /// <param name="areasMapping">The areas mapping. By default whole area of the dest image is associated with the whole area of the source image.</param>
        void RunRandomNnfInitIteration(Nnf nnf, ZsImage destImage, ZsImage srcImage, PatchMatchSettings settings, ImagePatchDistanceCalculator patchDistanceCalculator, Area2DMap areasMapping);

        /// <summary>
        /// Runs the random NNF initialization iteration for the associated areas of the dest and the source images.
        /// </summary>
        /// <param name="nnf">The NNF.</param>
        /// <param name="destImage">The dest image. For each patch at this image we will look for a similar one at the source image.</param>
        /// <param name="srcImage">The source image. Source of the patches for the dest image.</param>
        /// <param name="settings">The settings that control parameters of the algorithm.</param>
        /// <param name="patchDistanceCalculator">The calculator that calculates similarity of two patches. By deafult the Cie76 is used.</param>
        /// <param name="areasMapping">The areas mapping. By default whole area of the dest image is associated with the whole area of the source image.</param>
        /// <param name="destPixelsArea">Area on the dest image that actually containes pixels. By default is the area of the entire image.</param>
        void RunRandomNnfInitIteration(Nnf nnf, ZsImage destImage, ZsImage srcImage, PatchMatchSettings settings, ImagePatchDistanceCalculator patchDistanceCalculator, Area2DMap areasMapping, Area2D destPixelsArea);

        /// <summary>
        /// Runs the NNF build iteration using the whole areas of the dest and the source images. Uses CIE76 to calculate patch similarity.
        /// </summary>
        /// <param name="nnf">The NNF.</param>
        /// <param name="destImage">The dest image. For each patch at this image we will look for a similar one at the source image.</param>
        /// <param name="srcImage">The source image. Source of the patches for the dest image.</param>
        /// <param name="direction">The direction to look for a patches.</param>
        /// <param name="settings">The settings that control parameters of the algorithm.</param>
        void RunBuildNnfIteration(Nnf nnf, ZsImage destImage, ZsImage srcImage, NeighboursCheckDirection direction, PatchMatchSettings settings);

        /// <summary>
        /// Runs the NNF build iteration using the whole areas of the dest and the source images.
        /// </summary>
        /// <param name="nnf">The NNF.</param>
        /// <param name="destImage">The dest image. For each patch at this image we will look for a similar one at the source image.</param>
        /// <param name="srcImage">The source image. Source of the patches for the dest image.</param>
        /// <param name="direction">The direction to look for a patches.</param>
        /// <param name="settings">The settings that control parameters of the algorithm.</param>
        /// <param name="patchDistanceCalculator">The calculator that calculates similarity of two patches. By deafult the Cie76 is used.</param>
        void RunBuildNnfIteration(Nnf nnf, ZsImage destImage, ZsImage srcImage, NeighboursCheckDirection direction, PatchMatchSettings settings, ImagePatchDistanceCalculator patchDistanceCalculator);

        /// <summary>
        /// Runs the NNF build iteration for the associated areas of the dest and the source images.
        /// </summary>
        /// <param name="nnf">The NNF.</param>
        /// <param name="destImage">The dest image. For each patch at this image we will look for a similar one at the source image.</param>
        /// <param name="srcImage">The source image. Source of the patches for the dest image.</param>
        /// <param name="direction">The direction to look for a patches.</param>
        /// <param name="settings">The settings that control parameters of the algorithm.</param>
        /// <param name="patchDistanceCalculator">The calculator that calculates similarity of two patches. By deafult the Cie76 is used.</param>
        /// <param name="areasMapping">The areas mapping. By default whole area of the dest image is associated with the whole area of the source image.</param>
        void RunBuildNnfIteration(Nnf nnf, ZsImage destImage, ZsImage srcImage, NeighboursCheckDirection direction, PatchMatchSettings settings, ImagePatchDistanceCalculator patchDistanceCalculator, Area2DMap areasMapping);

        /// <summary>
        /// Runs the NNF build iteration for the associated areas of the dest and the source images.
        /// </summary>
        /// <param name="nnf">The NNF.</param>
        /// <param name="destImage">The dest image. For each patch at this image we will look for a similar one at the source image.</param>
        /// <param name="srcImage">The source image. Source of the patches for the dest image.</param>
        /// <param name="direction">The direction to look for a patches.</param>
        /// <param name="settings">The settings that control parameters of the algorithm.</param>
        /// <param name="patchDistanceCalculator">The calculator that calculates similarity of two patches. By deafult the Cie76 is used.</param>
        /// <param name="areasMapping">The areas mapping. By default whole area of the dest image is associated with the whole area of the source image.</param>
        /// <param name="destPixelsArea">Area on the dest image that actually containes pixels. By default is the area of the entire image.</param>
        void RunBuildNnfIteration(Nnf nnf, ZsImage destImage, ZsImage srcImage, NeighboursCheckDirection direction, PatchMatchSettings settings, ImagePatchDistanceCalculator patchDistanceCalculator, Area2DMap areasMapping, Area2D destPixelsArea);
    }
}