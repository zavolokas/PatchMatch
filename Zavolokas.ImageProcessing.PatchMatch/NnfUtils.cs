using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Zavolokas.Structures;

namespace Zavolokas.ImageProcessing.PatchMatch
{
    public static class NnfUtils
    {

        

        // TODO: (nnfs are should be for the same images)
        public static unsafe Nnf MergeNnfs(this Nnf[] nnfs, Area2DMap[] maps, ParallelOptions options)
        {
            if (nnfs == null || nnfs.Length < 1) throw new ArgumentException("At least one nnf is expected");
            if (nnfs.Length != maps.Length) throw new ArgumentException("Amount of passed Maps should be equal to amount of passed Nnfs");

            if (nnfs.Length > 1)
            {
                // We assume that all the inputs contain NNFs 
                // for the same dest image.
                // The source images can not be different as well.
                // When different source images are desired those
                // images should be merged to one image and mappings 
                // should be adjusted to map to a particular region 
                // on the source image.
                // Differen source images problem. In that case we 
                // have a problem with different mappings when we 
                // merge NNfs. Mappings can not be merged as they 
                // would have totally different source areas.

                const bool forward = true;
                var destNnf = nnfs[0].GetNnfItems();
                var destNnfMap = maps[0];
                var destImageWidth = nnfs[0].DstWidth;
                var srcImageWidth = nnfs[0].SourceWidth;

                // Merge the rest NNFs to the first one.
                for (int i = 1; i < nnfs.Length; i++)
                {
                    // Nnf that needs to be merged to our dest nnf.
                    var srcNnf = nnfs[i].GetNnfItems();

                    // Current src nnf is build for a particular
                    // area which is defined in the corresponding mapping.
                    var srcNnfMap = maps[i];

                    var destNnfPointsIndexes = GetAreaPointsIndexes((destNnfMap as IAreasMapping).DestArea, destImageWidth, forward);
                    var srcNnfPointsIndexes = GetAreaPointsIndexes((srcNnfMap as IAreasMapping).DestArea, destImageWidth, forward);
                    var mappings = ExtractMappedAreasInfo(srcNnfMap, destImageWidth, srcImageWidth, forward);

                    // Decide on how many partitions we should divade the processing
                    // of the elements.
                    int partsCount = srcNnfMap.DestElementsCount > options.NotDividableMinAmountElements
                        ? options.ThreadsCount
                        : 1;
                    var partSize = (int)(srcNnfMap.DestElementsCount / partsCount);

                    Parallel.For(0, partsCount, partIndex =>
                    {
                        // Colne mapping to avoid conflicts in multithread
                        var destNnfPointsIndexesSet = new HashSet<int>(destNnfPointsIndexes);

                        // Clone mappings to avoid problems in multithread
                        var mappedAreasInfos = new MappedAreasInfo[mappings.Length];
                        for (int j = 0; j < mappings.Length; j++)
                        {
                            mappedAreasInfos[j] = mappings[j].Clone();
                        }

                        var firstPointIndex = partIndex * partSize;
                        var lastPointIndex = firstPointIndex + partSize - 1;
                        if (partIndex == partsCount - 1) lastPointIndex = srcNnfMap.DestElementsCount - 1;
                        if (lastPointIndex > srcNnfMap.DestElementsCount) lastPointIndex = srcNnfMap.DestElementsCount - 1;

                        fixed (double* destNnfP = destNnf)
                        fixed (double* srcNnfP = srcNnf)
                        fixed (int* srcNnfPointIndexesP = srcNnfPointsIndexes)
                        {
                            for (var srcNnfMapDestPointIndex = firstPointIndex; srcNnfMapDestPointIndex <= lastPointIndex; srcNnfMapDestPointIndex++)
                            {
                                var destPointIndex = *(srcNnfPointIndexesP + srcNnfMapDestPointIndex);

                                if (destNnfPointsIndexesSet.Contains(destPointIndex))
                                {
                                    // The value of the NNF in the dest point can
                                    // present in the resulting destNnf as well. 
                                    // In that case we need to merge NNFs at the point
                                    // by taking the best value.

                                    // compare and set the best
                                    var srcVal = *(srcNnfP + destPointIndex * 2 + 1);
                                    var destVal = *(destNnfP + destPointIndex * 2 + 1);

                                    if (srcVal < destVal)
                                    {
                                        *(destNnfP + destPointIndex * 2 + 0) = *(srcNnfP + destPointIndex * 2 + 0);
                                        *(destNnfP + destPointIndex * 2 + 1) = srcVal;
                                    }
                                }
                                else
                                {
                                    // When the destNnf doesn't contain the value
                                    // for that point we simply copy it
                                    *(destNnfP + destPointIndex * 2 + 0) = *(srcNnfP + destPointIndex * 2 + 0);
                                    *(destNnfP + destPointIndex * 2 + 1) = *(srcNnfP + destPointIndex * 2 + 1);
                                }
                            }
                        }
                    });

                    // Since the dest nnf contains values for the both
                    // dest areas of two mappings, we need to merge these
                    // mappings to one.
                    destNnfMap = new Area2DMapBuilder()
                        .InitNewMap(destNnfMap)
                        .AddMapping(srcNnfMap)
                        .Build();
                }
            }

            return nnfs[0];
        }

        private static MappedAreasInfo[] ExtractMappedAreasInfo(IAreasMapping map, int destImageWidth, int srcImageWidth, bool forward)
        {
            var areaAssociations = map.AssociatedAreasAsc.Reverse().ToArray();
            var mapping = new MappedAreasInfo[areaAssociations.Length];
            for (int i = 0; i < areaAssociations.Length; i++)
            {
                var areaAssociation = areaAssociations[i];
                var ass = new MappedAreasInfo
                {
                    DestAreaPointsIndexes = new int[areaAssociation.Item1.ElementsCount],
                    SrcAreaPointsIndexes = new int[areaAssociation.Item2.ElementsCount],
                    SrcBound = areaAssociation.Item2.Bound
                };

                areaAssociation.Item1.FillMappedPointsIndexes(ass.DestAreaPointsIndexes, destImageWidth, forward);
                areaAssociation.Item2.FillMappedPointsIndexes(ass.SrcAreaPointsIndexes, srcImageWidth, forward);

                mapping[i] = ass;
            }
            return mapping;
        }

        private static int[] GetAreaPointsIndexes(Area2D area, int destImageWidth, bool forward)
        {
            int[] dstPointIndexes = new int[area.ElementsCount];
            area.FillMappedPointsIndexes(dstPointIndexes, destImageWidth, forward);
            return dstPointIndexes;
        }

        /// <summary>
        /// Populates tha patch with corresponding image pixel indexes.
        /// </summary>
        /// <param name="patchPixelsIndexesP">Pointer to the patch array to populate with pixel indexes.</param>
        /// <param name="x">The x coordinate of the patch center point on the image.</param>
        /// <param name="y">The y coordinate of the patch center point on the image.</param>
        /// <param name="patchSize">Size of the patch.</param>
        /// <param name="imageWidth">Width of the image.</param>
        /// <param name="allowedPointIndexesSet">
        /// The set of point indexes that can be used to fill the patch. 
        /// Indexes that are outside of this set are not marked and can not be
        /// taken into the consideration.
        /// </param>
        private static unsafe void PopulatePatchPixelsIndexes(int* patchPixelsIndexesP, int x, int y, int patchSize, int imageWidth, HashSet<int> allowedPointIndexesSet, out bool fits)
        {
            var patchOffset = (patchSize - 1) / 2;
            var length = patchSize * patchSize;
            int imagePointIndex;
            int xOffs, yOffs;
            fits = true;

            for (int patchPointIndex = length - 1; patchPointIndex >= 0; patchPointIndex--)
            {
                // Convert patch point index to offset from the patch central point
                xOffs = patchPointIndex % patchSize - patchOffset;
                yOffs = patchPointIndex / patchSize - patchOffset;

                // Find corresponding image point index
                imagePointIndex = (y + yOffs) * imageWidth + x + xOffs;
                if (allowedPointIndexesSet.Contains(imagePointIndex))
                {
                    *(patchPixelsIndexesP + patchPointIndex) = imagePointIndex;
                }
                else
                {
                    // Point is outside of the marked area.
                    *(patchPixelsIndexesP + patchPointIndex) = -1;
                    fits = false;
                }
            }
        }
    }
}