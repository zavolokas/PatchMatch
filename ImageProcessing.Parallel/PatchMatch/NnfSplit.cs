using Grapute;

namespace Zavolokas.ImageProcessing.Parallel.PatchMatch
{
    //public class PatchMatchPipeline2 : Node<PmData, PmData>
    //{
    //    private readonly ImagePatchDistanceCalculator _patchDistanceCalculator;

    //    //public PatchMatchPipeline2(ImagePatchDistanceCalculator patchDistanceCalculator)
    //    //{
    //    //    if (patchDistanceCalculator == null)
    //    //        throw new ArgumentNullException(nameof(patchDistanceCalculator));

    //    //    _patchDistanceCalculator = patchDistanceCalculator;
    //    //}

    //    protected PmData[] Process(PmData data)
    //    {
    //        return new[] { data };
    //        //var task = new NnfSplit()
    //        //    .ForEachOutput(new NnfInit(_patchDistanceCalculator));

    //        //task.SetInput(data);

    //        //for (int i = 0; i < data.Settings.IterationsAmount; i++)
    //        //{
    //        //    var direction = i % 2 == 0
    //        //        ? NeighboursCheckDirection.Forward
    //        //        : NeighboursCheckDirection.Backward;

    //        //    task = task.ForEachOutput(new NeighboursCheck(_patchDistanceCalculator, direction));
    //        //}

    //        //return task
    //        //    .CollectAllOutputsToOneArray()
    //        //    .ForArray(new MergeNnf())
    //        //    .Process()
    //        //    .Output;
    //    }
    //}

    internal class NnfSplit : Node<PmData, PmData>
    {
        protected override PmData[] Process(PmData data)
        {
            return new[] { data };
        }
    }
}