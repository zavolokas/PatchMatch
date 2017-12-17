using System;
using System.Linq;
using Grapute;
using Zavolokas.ImageProcessing.PatchMatch;

namespace Zavolokas.ImageProcessing.Parallel.PatchMatch
{
    /// <summary>
    /// Merges NNFs provided in the input data instances and return a new data
    /// instance that contains the NNF (which is the result of merging) and
    /// an updated mapping that contains all the areas from the inputs.
    /// </summary>
    /// <seealso cref="PmData" />
    public class MergeNnf : Node<PmData[], PmData>
    {
        protected override PmData[] Process(PmData[] inputs)
        {
            if (inputs.Length > 1)
            {

                var mergedData = inputs[0];

                var nnfs = inputs.Select(i => i.Nnf).ToArray();
                var maps = inputs.Select(i => i.Map).ToArray();

                var result = PatchMatchNnfBuilder.MergeNnfs(nnfs, maps, mergedData.DestImage.Width, mergedData.SrcImage.Width,
                    mergedData.Settings);

                mergedData.Nnf = result.Item1;
                mergedData.Map = result.Item2;

                return new[] { mergedData };
            }

            return new[] { inputs[0] };
        }
    }
}