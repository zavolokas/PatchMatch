using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Zavolokas.Structures;

namespace Zavolokas.ImageProcessing.PatchMatch
{
    public static class NnfUtils
    {
        public static void NormalizeNnf(Nnf nnf, Area2D destArea)
        {
            var nnfdata = nnf.GetNnfItems();

            checked
            {
                var distances = new double[destArea.ElementsCount];
                var pointIndexes = new int[destArea.ElementsCount];
                destArea.FillMappedPointsIndexes(pointIndexes, nnf.DstWidth);

                var dinstancesSum = 0.0;
                for (int destPointIndex = 0; destPointIndex < distances.Length; destPointIndex++)
                {
                    var nnfPos = pointIndexes[destPointIndex] * 2;
                    double distance = nnfdata[nnfPos + 1];
                    distances[destPointIndex] = distance;
                    dinstancesSum += distance;
                }
                double mean = dinstancesSum / distances.Length;

                var squareDistances = new double[distances.Length];
                double squreDistancesSum = 0.0;
                for (int i = 0; i < distances.Length; i++)
                {
                    var distToMean = distances[i] - mean;
                    double distToMeanCube = distToMean * distToMean;
                    squareDistances[i] = distToMeanCube;
                    squreDistancesSum += distToMeanCube;
                }
                double sigma = System.Math.Sqrt(squreDistancesSum / (squareDistances.Length - 1));

                for (int destPointIndex = 0; destPointIndex < distances.Length; destPointIndex++)
                {
                    var nnfPos = pointIndexes[destPointIndex] * 2;
                    var dist = distances[destPointIndex];
                    dist = (dist - mean) / sigma;
                    if (dist < 0) dist = -dist;
                    nnfdata[nnfPos + 1] = dist;
                }
            }
        }

        public static unsafe Nnf ScaleNnf2X(Nnf nnf, Area2DMap scaledMap, ZsImage scaledDestImage, ZsImage scaledSrcImage, Area2D destPixelsArea, ImagePatchDistanceCalculator patchDistanceCalculator, PatchMatchSettings settings)
        {
            var patchSize = nnf.PatchSize;
            var patchLength = patchSize * patchSize;
            var destImageWidth = scaledDestImage.Width;
            var srcImageWidth = scaledSrcImage.Width;
            var sameSrcAndDest = scaledDestImage == scaledSrcImage;

            var pixelsArea = (scaledMap as IAreasMapping).DestArea;
            //var destPointIndexes = GetDestPointsIndexes(pixelsArea, destImageWidth, NeighboursCheckDirection.Forward);
            pixelsArea = pixelsArea.Intersect(destPixelsArea);
            var destAvailablePixelsIndexes = GetAreaPointsIndexes(pixelsArea, destImageWidth, true);
            var mappings = ExtractMappedAreasInfo(scaledMap, destImageWidth, srcImageWidth, true);

            var nnf2x = new Nnf(nnf.DstWidth * 2, nnf.DstHeight * 2, nnf.SourceWidth * 2, nnf.SourceHeight * 2, nnf.PatchSize);

            var nnfDestWidth = nnf.DstWidth;
            var nnfSourceWidth = nnf.SourceWidth;
            var nnf2xSourceWidth = nnf2x.SourceWidth;
            var nnf2xDstWidth = nnf2x.DstWidth;

            var nnfData = nnf.GetNnfItems();
            var nnf2xData = nnf2x.GetNnfItems();

            // Decide on how many partitions we should divade the processing
            // of the elements.
            int nnfPointsAmount = nnf.DstWidth * nnf.DstHeight;
            var partsCount = nnfPointsAmount > settings.NotDividableMinAmountElements
                ? settings.ThreadsCount
                : 1;
            var partSize = nnfPointsAmount / partsCount;

            var offs = new[]
            {
                new[] { 0, 0 },
                new[] { 1, 0 },
                new[] { 0, 1 },
                new[] { 1, 1 }
            };

            Parallel.For(0, partsCount, partIndex =>
                    //for (int partIndex = 0; partIndex < partsCount; partIndex++)
                {
                    bool isPatchFit = false;

                    // Init the dest & source patch
                    var destPatchPixelsIndexes = new int[patchLength];
                    var srcPatchPixelsIndexes = new int[patchLength];

                    //var destPointsIndexesSet = new HashSet<int>(destPointIndexes);
                    var destAvailablePixelsIndexesSet = new HashSet<int>(destAvailablePixelsIndexes);
                    var mappedAreasInfos = new MappedAreasInfo[mappings.Length];
                    for (var i = 0; i < mappings.Length; i++)
                    {
                        mappedAreasInfos[i] = mappings[i].Clone();
                    }

                    var firstPointIndex = partIndex * partSize;
                    var lastPointIndex = firstPointIndex + partSize - 1;
                    if (partIndex == partsCount - 1) lastPointIndex = nnfPointsAmount - 1;
                    if (lastPointIndex > nnfPointsAmount) lastPointIndex = nnfPointsAmount - 1;

                    fixed (double* destImagePixelsDataP = scaledDestImage.PixelsData)
                    fixed (double* sourceImagePixelsDataP = scaledSrcImage.PixelsData)
                    fixed (int* srcPatchPixelsIndexesP = srcPatchPixelsIndexes)
                    fixed (int* destPatchPixelsIndexesP = destPatchPixelsIndexes)
                    {
                        MappedAreasInfo mappedAreasInfo = null;

                        for (var j = firstPointIndex; j <= lastPointIndex; j++)
                        {
                            var destPx = j;
                            var destX = destPx % nnfDestWidth;
                            var destY = destPx / nnfDestWidth;

                            // Find 
                            var srcPointIndex = nnfData[destY * nnfDestWidth * 2 + destX * 2]; // / 2;
                            int srcY = (int)(srcPointIndex / nnfSourceWidth); // * 2;
                            int srcX = (int)(srcPointIndex % nnfSourceWidth); // * 2;

                            var nY = destY * 2;
                            var nX = destX * 2;

                            for (int i = 0; i < offs.Length; i++)
                            {
                                var destPointIndex = (nY + offs[i][1]) * nnf2xDstWidth + nX + offs[i][0];
                                mappedAreasInfo = mappedAreasInfos.FirstOrDefault(mai => mai.DestAreaPointsIndexesSet.Contains(destPointIndex));
                                if (mappedAreasInfo != null)
                                {
                                    nnf2xData[destPointIndex * 2] = ((srcY + offs[i][1]) * nnf2xSourceWidth + (srcX + offs[i][0])) * 2;
                                    PopulatePatchPixelsIndexes(srcPatchPixelsIndexesP, srcX + offs[i][0], srcY + offs[i][1], patchSize, nnf2xSourceWidth, mappedAreasInfo.SrcAreaPointsIndexesSet, out isPatchFit);
                                    PopulatePatchPixelsIndexes(destPatchPixelsIndexesP, destX + offs[i][0], destY + offs[i][1], patchSize, destImageWidth, destAvailablePixelsIndexesSet, out isPatchFit);
                                    nnf2xData[destPointIndex * 2 + 1] = patchDistanceCalculator.Calculate(destPatchPixelsIndexesP, srcPatchPixelsIndexesP, double.MaxValue, destImagePixelsDataP, sourceImagePixelsDataP, scaledDestImage, scaledSrcImage, patchLength);
                                }
                                else
                                {
                                    if (sameSrcAndDest)
                                    {
                                        // when the source and the dest image is the same one, the best
                                        // corresponding patch is the patch itself!
                                        nnf2xData[destPointIndex * 2] = destPointIndex;
                                        nnf2xData[destPointIndex * 2 + 1] = 0;
                                    }
                                    else
                                    {
                                        nnf2xData[destPointIndex * 2] = ((srcY + offs[i][1]) * nnf2xSourceWidth + (srcX + offs[i][0])) * 2;
                                        nnf2xData[destPointIndex * 2 + 1] = nnfData[destY * nnfDestWidth * 2 + destX * 2 + 1];
                                    }
                                }
                            }
                        }
                    }
                }
            );
            return nnf2x;
        }

        public static Nnf ScaleNnf2X(Nnf nnf, Area2DMap scaledMap, ZsImage scaledDestImage, ZsImage scaledSrcImage, PatchMatchSettings settings)
        {
            var destImageWidth = scaledDestImage.Width;
            var srcImageWidth = scaledSrcImage.Width;
            var sameSrcAndDest = scaledDestImage == scaledSrcImage;

            var mappings = ExtractMappedAreasInfo(scaledMap, destImageWidth, srcImageWidth, true);

            var nnf2x = new Nnf(nnf.DstWidth * 2, nnf.DstHeight * 2, nnf.SourceWidth * 2, nnf.SourceHeight * 2, nnf.PatchSize);

            var nnfDestWidth = nnf.DstWidth;
            var nnfSourceWidth = nnf.SourceWidth;
            var nnf2xSourceWidth = nnf2x.SourceWidth;
            var nnf2xDstWidth = nnf2x.DstWidth;

            var nnfData = nnf.GetNnfItems();
            var nnf2xData = nnf2x.GetNnfItems();

            // Decide on how many partitions we should divade the processing
            // of the elements.
            int nnfPointsAmount = nnf.DstWidth * nnf.DstHeight;
            var partsCount = nnfPointsAmount > settings.NotDividableMinAmountElements
                ? settings.ThreadsCount
                : 1;
            var partSize = nnfPointsAmount / partsCount;

            var offs = new[]
            {
                new[] { 0, 0 },
                new[] { 1, 0 },
                new[] { 0, 1 },
                new[] { 1, 1 }
            };

            Parallel.For(0, partsCount, partIndex =>
                    //for (int partIndex = 0; partIndex < partsCount; partIndex++)
                {
                    bool isPatchFit = false;

                    var mappedAreasInfos = new MappedAreasInfo[mappings.Length];
                    for (var i = 0; i < mappings.Length; i++)
                    {
                        mappedAreasInfos[i] = mappings[i].Clone();
                    }

                    var firstPointIndex = partIndex * partSize;
                    var lastPointIndex = firstPointIndex + partSize - 1;
                    if (partIndex == partsCount - 1) lastPointIndex = nnfPointsAmount - 1;
                    if (lastPointIndex > nnfPointsAmount) lastPointIndex = nnfPointsAmount - 1;

                    MappedAreasInfo mappedAreasInfo = null;

                    for (var j = firstPointIndex; j <= lastPointIndex; j++)
                    {
                        var destPx = j;
                        var destX = destPx % nnfDestWidth;
                        var destY = destPx / nnfDestWidth;

                        // Find 
                        var srcPointIndex = nnfData[destY * nnfDestWidth * 2 + destX * 2]; // / 2;
                        int srcY = (int)(srcPointIndex / nnfSourceWidth); // * 2;
                        int srcX = (int)(srcPointIndex % nnfSourceWidth); // * 2;

                        var dist = nnfData[destY * nnfDestWidth * 2 + destX * 2 + 1];

                        var nY = destY * 2;
                        var nX = destX * 2;

                        for (int i = 0; i < offs.Length; i++)
                        {
                            var destPointIndex = (nY + offs[i][1]) * nnf2xDstWidth + nX + offs[i][0];
                            mappedAreasInfo = mappedAreasInfos.FirstOrDefault(mai => mai.DestAreaPointsIndexesSet.Contains(destPointIndex));
                            if (mappedAreasInfo != null)
                            {
                                nnf2xData[destPointIndex * 2] = ((srcY + offs[i][1]) * nnf2xSourceWidth + (srcX + offs[i][0])) * 2;
                                nnf2xData[destPointIndex * 2 + 1] = dist;
                            }
                            else
                            {
                                if (sameSrcAndDest)
                                {
                                    // when the source and the dest image is the same one, the best
                                    // corresponding patch is the patch itself!
                                    nnf2xData[destPointIndex * 2] = destPointIndex;
                                    nnf2xData[destPointIndex * 2 + 1] = 0;
                                }
                                else
                                {
                                    nnf2xData[destPointIndex * 2] = ((srcY + offs[i][1]) * nnf2xSourceWidth + (srcX + offs[i][0])) * 2;
                                    nnf2xData[destPointIndex * 2 + 1] = nnfData[destY * nnfDestWidth * 2 + destX * 2 + 1];
                                }
                            }
                        }
                    }
                }
            );
            return nnf2x;
        }

        // TODO: (nnfs are should be for the same images)
        public static unsafe Nnf MergeNnfs(this Nnf[] nnfs, Area2DMap[] maps)
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
                    int partsCount = srcNnfMap.DestElementsCount > 20// settings.NotDividableMinAmountElements
                        ? 4//settings.ThreadsCount
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