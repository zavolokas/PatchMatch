namespace Zavolokas.ImageProcessing.PatchMatch
{
    public static class ImagePatchDistance
    {
        public static ImagePatchDistanceCalculator Cie76 { get; } = new Cie76ImagePatchDistanceCalculator();

        public static ImagePatchDistanceCalculator Cie2000 { get; } = new Cie2000ImagePatchDistanceCalculator();
    }
}