using System;
using System.Linq;
using Grapute;
using Zavolokas.ImageProcessing.PatchMatch;
using Zavolokas.Structures;

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

                for (int i = 1; i < nnfs.Length; i++)
                {
                    mergedData.Nnf.Merge(nnfs[i], mergedData.Map, maps[i], inputs[0].Settings );

                    mergedData.Map = new Area2DMapBuilder()
                        .InitNewMap(mergedData.Map)
                        .AddMapping(maps[i])
                        .Build();
                }
            }

            return new[] { inputs[0] };
        }
    }
}