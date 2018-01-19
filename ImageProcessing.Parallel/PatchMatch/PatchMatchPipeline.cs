using System;
using System.Runtime.InteropServices;
using Zavolokas.ImageProcessing.PatchMatch;
using Grapute;

namespace Zavolokas.ImageProcessing.Parallel.PatchMatch
{
    public class PatchMatchPipeline : Node<PmData, PmData>
    {
        private readonly IPatchMatchNnfBuilder _patchMatchNnfBuilder;
        private readonly ImagePatchDistanceCalculator _patchDistanceCalculator;

        public PatchMatchPipeline(IPatchMatchNnfBuilder patchMatchNnfBuilder, ImagePatchDistanceCalculator patchDistanceCalculator)
        {
            if (patchMatchNnfBuilder == null)
                throw new ArgumentNullException(nameof(patchMatchNnfBuilder));

            if (patchDistanceCalculator == null)
                throw new ArgumentNullException(nameof(patchDistanceCalculator));

            _patchMatchNnfBuilder = patchMatchNnfBuilder;
            _patchDistanceCalculator = patchDistanceCalculator;
        }

        protected override PmData[] Process(PmData data)
        {
            var node = new NnfSplit()
                .ForEachOutput(new NnfInit(_patchMatchNnfBuilder, _patchDistanceCalculator));

            node.SetInput(data);

            for (int i = 0; i < data.Settings.IterationsAmount; i++)
            {
                var direction = i % 2 == 0
                    ? NeighboursCheckDirection.Forward
                    : NeighboursCheckDirection.Backward;

                node = node.ForEachOutput(new NeighboursCheck(_patchMatchNnfBuilder, _patchDistanceCalculator, direction));
            }

            return node
                .CollectAllOutputsToOneArray()
                .ForArray(new MergeNnf())
                .Process()
                .Output;
        }
    }
}