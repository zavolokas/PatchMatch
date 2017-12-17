using System;
using Zavolokas.ImageProcessing.PatchMatch;
using Grapute;

namespace Zavolokas.ImageProcessing.Parallel.PatchMatch
{
    public class PatchMatchPipeline : Node<PmData, PmData>
    {
        private readonly ImagePatchDistanceCalculator _patchDistanceCalculator;

        public PatchMatchPipeline(ImagePatchDistanceCalculator patchDistanceCalculator)
        {
            if (patchDistanceCalculator == null)
                throw new ArgumentNullException(nameof(patchDistanceCalculator));

            _patchDistanceCalculator = patchDistanceCalculator;
        }

        protected override PmData[] Process(PmData data)
        {
            var node = new NnfSplit()
                .ForEachOutput(new NnfInit(_patchDistanceCalculator));

            node.SetInput(data);

            for (int i = 0; i < data.Settings.IterationsAmount; i++)
            {
                var direction = i % 2 == 0
                    ? NeighboursCheckDirection.Forward
                    : NeighboursCheckDirection.Backward;

                node = node.ForEachOutput(new NeighboursCheck(_patchDistanceCalculator, direction));
            }

            return node
                .CollectAllOutputsToOneArray()
                .ForArray(new MergeNnf())
                .Process()
                .Output;
        }
    }
}