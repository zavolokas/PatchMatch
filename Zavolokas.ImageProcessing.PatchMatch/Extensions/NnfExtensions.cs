using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Zavolokas.Structures;

namespace Zavolokas.ImageProcessing.PatchMatch
{
    public static class NnfExtensions
    {
        /// <summary>
        /// Normalizes the specified NNF.
        /// </summary>
        /// <param name="nnf">The NNF.</param>
        /// <exception cref="ArgumentNullException">nnf</exception>
        public static void Normalize(this Nnf nnf)
        {
            if (nnf == null)
                throw new ArgumentNullException(nameof(nnf));

            var destArea = Area2D.Create(0, 0, nnf.DstWidth, nnf.DstHeight);

            nnf.Normalize(destArea);
        }

        /// <summary>
        /// Normalizes the NNF in the specified dest area.
        /// </summary>
        /// <param name="nnf">The NNF.</param>
        /// <param name="destArea">The dest area.</param>
        /// <exception cref="ArgumentNullException">
        /// nnf
        /// or
        /// destArea
        /// </exception>
        public static void Normalize(this Nnf nnf, Area2D destArea)
        {
            if (nnf == null)
                throw new ArgumentNullException(nameof(nnf));

            if (destArea == null)
                throw new ArgumentNullException(nameof(destArea));

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

        /// <summary>
        /// Clones the NNF and scales it up 2 times with distances recalculation.
        /// </summary>
        /// <param name="nnf">The NNF.</param>
        /// <param name="scaledDestImage">The scaled dest image.</param>
        /// <param name="scaledSrcImage">The scaled source image.</param>
        /// <param name="options">The options for parallel processing.</param>
        /// <param name="patchDistanceCalculator">The calculator that calculates similarity of two patches. By deafult the Cie76 is used.</param>
        /// <param name="destPixelsArea">Area on the dest image that actually containes pixels. By default is the area of the entire image.</param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException">
        /// nnf
        /// or
        /// scaledDestImage
        /// or
        /// scaledSrcImage
        /// </exception>
        public static Nnf CloneAndScale2XWithUpdate(this Nnf nnf, ZsImage scaledDestImage, ZsImage scaledSrcImage, ParallelOptions options = null, ImagePatchDistanceCalculator patchDistanceCalculator = null, Area2D destPixelsArea = null)
        {
            if (scaledDestImage == null) throw new ArgumentNullException(nameof(scaledDestImage));
            if (scaledSrcImage == null) throw new ArgumentNullException(nameof(scaledSrcImage));

            var destArea = Area2D.Create(0, 0, scaledDestImage.Width, scaledDestImage.Height);
            var srcArea = Area2D.Create(0, 0, scaledSrcImage.Width, scaledSrcImage.Height);

            Area2DMap scaledMap = new Area2DMapBuilder()
                .InitNewMap(destArea, srcArea)
                .Build();

            return nnf.CloneAndScale2XWithUpdate(scaledDestImage, scaledSrcImage, options, scaledMap, patchDistanceCalculator,
                destPixelsArea);
        }

        /// <summary>
        /// Clones the NNF and scales it up 2 times with distances recalculation.
        /// </summary>
        /// <param name="nnf">The NNF.</param>
        /// <param name="scaledDestImage">The scaled dest image.</param>
        /// <param name="scaledSrcImage">The scaled source image.</param>
        /// <param name="options">The options for parallel processing.</param>
        /// <param name="scaledMap">The areas mapping. By default whole area of the dest image is associated with the whole area of the source image.</param>
        /// <param name="patchDistanceCalculator">The calculator that calculates similarity of two patches. By deafult the Cie76 is used.</param>
        /// <param name="destPixelsArea">Area on the dest image that actually containes pixels. By default is the area of the entire image.</param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException">nnf
        /// or
        /// scaledDestImage
        /// or
        /// scaledSrcImage
        /// or
        /// scaledMap</exception>
        public static unsafe Nnf CloneAndScale2XWithUpdate(this Nnf nnf, ZsImage scaledDestImage, ZsImage scaledSrcImage, ParallelOptions options, Area2DMap scaledMap, ImagePatchDistanceCalculator patchDistanceCalculator = null, Area2D destPixelsArea = null)
        {
            if (nnf == null) throw new ArgumentNullException(nameof(nnf));
            if (scaledDestImage == null) throw new ArgumentNullException(nameof(scaledDestImage));
            if (scaledSrcImage == null) throw new ArgumentNullException(nameof(scaledSrcImage));
            if (scaledMap == null) throw new ArgumentNullException(nameof(scaledMap));

            if (destPixelsArea == null)
                destPixelsArea = Area2D.Create(0, 0, scaledDestImage.Width, scaledDestImage.Height);

            if (patchDistanceCalculator == null)
                patchDistanceCalculator = ImagePatchDistance.Cie76;

            if (options == null) options = new ParallelOptions();

            var patchSize = nnf.PatchSize;
            var patchLength = patchSize * patchSize;
            var destImageWidth = scaledDestImage.Width;
            var srcImageWidth = scaledSrcImage.Width;
            var sameSrcAndDest = scaledDestImage == scaledSrcImage;

            var pixelsArea = (scaledMap as IAreasMapping).DestArea;
            //var destPointIndexes = GetDestPointsIndexes(pixelsArea, destImageWidth, NeighboursCheckDirection.Forward);
            pixelsArea = pixelsArea.Intersect(destPixelsArea);
            var destAvailablePixelsIndexes = pixelsArea.GetPointsIndexes(destImageWidth);
            var mappings = scaledMap.ExtractMappedAreasInfo(destImageWidth, srcImageWidth, true);

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
            var partsCount = nnfPointsAmount > options.NotDividableMinAmountElements
                ? options.ThreadsCount
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
                                    Utils.PopulatePatchPixelsIndexes(srcPatchPixelsIndexesP, srcX + offs[i][0], srcY + offs[i][1], patchSize, nnf2xSourceWidth, mappedAreasInfo.SrcAreaPointsIndexesSet, out isPatchFit);
                                    Utils.PopulatePatchPixelsIndexes(destPatchPixelsIndexesP, destX + offs[i][0], destY + offs[i][1], patchSize, destImageWidth, destAvailablePixelsIndexesSet, out isPatchFit);
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

        /// <summary>
        /// Clones the NNF and scales it up 2 times without distances recalculation.
        /// </summary>
        /// <param name="nnf">The NNF.</param>
        /// <param name="scaledDestImage">The scaled dest image.</param>
        /// <param name="scaledSrcImage">The scaled source image.</param>
        /// <param name="options">The options for parallel processing.</param>
        /// <param name="scaledMap">The areas mapping. By default whole area of the dest image is associated with the whole area of the source image.</param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException">
        /// nnf
        /// or
        /// scaledDestImage
        /// or
        /// scaledSrcImage
        /// </exception>
        public static Nnf CloneAndScaleNnf2X(this Nnf nnf, ZsImage scaledDestImage, ZsImage scaledSrcImage, ParallelOptions options = null, Area2DMap scaledMap = null)
        {
            if (nnf == null) throw new ArgumentNullException(nameof(nnf));
            if (scaledDestImage == null) throw new ArgumentNullException(nameof(scaledDestImage));
            if (scaledSrcImage == null) throw new ArgumentNullException(nameof(scaledSrcImage));

            if (scaledMap == null)
            {
                var destArea = Area2D.Create(0, 0, scaledDestImage.Width, scaledDestImage.Height);
                var srcArea = Area2D.Create(0, 0, scaledSrcImage.Width, scaledSrcImage.Height);

                scaledMap = new Area2DMapBuilder()
                    .InitNewMap(destArea, srcArea)
                    .Build();
            }

            if (options == null) options = new ParallelOptions();

            var destImageWidth = scaledDestImage.Width;
            var srcImageWidth = scaledSrcImage.Width;
            var sameSrcAndDest = scaledDestImage == scaledSrcImage;

            var mappings = scaledMap.ExtractMappedAreasInfo(destImageWidth, srcImageWidth, true);

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
            var partsCount = nnfPointsAmount > options.NotDividableMinAmountElements
                ? options.ThreadsCount
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

        /// <summary>
        /// Merges provided NNF into.
        /// </summary>
        /// <param name="destNnf">The dest NNF.</param>
        /// <param name="srcNnf">The source NNF.</param>
        /// <param name="destNnfMap">The dest NNF areas mapping(by default whole source area is mapped to the dest area).</param>
        /// <param name="srcNnfMap">The source NNF areas mapping(by default whole source area is mapped to the dest area).</param>
        /// <param name="options">The parallel processing options.</param>
        /// <exception cref="ArgumentNullException">
        /// destNnf
        /// or
        /// srcNnf
        /// </exception>
        /// <exception cref="ArgumentException">NNFs should be built for the same source and dest images</exception>
        public static unsafe void Merge(this Nnf destNnf, Nnf srcNnf, Area2DMap destNnfMap = null, Area2DMap srcNnfMap = null, ParallelOptions options = null)
        {
            if (destNnf == null) throw new ArgumentNullException(nameof(destNnf));
            if (srcNnf == null) throw new ArgumentNullException(nameof(srcNnf));

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

            // make sure that both NNFs are built for the same dest and source images
            if (destNnf.DstWidth != srcNnf.DstWidth || destNnf.DstHeight != srcNnf.DstHeight
                || destNnf.SourceWidth != srcNnf.SourceWidth || destNnf.SourceHeight != srcNnf.SourceHeight)
                throw new ArgumentException("NNFs should be built for the same source and dest images");

            // NNFs are built for particular areas that are defined in the corresponding mapping.
            // When mappings are not provided, we assume NNF was built for the whole areas of the
            // dest and source images.
            if (destNnfMap == null) destNnfMap = CreateMapping(destNnf);
            if (srcNnfMap == null) srcNnfMap = CreateMapping(srcNnf);

            if (options == null) options = new ParallelOptions();

            var destImageWidth = destNnf.DstWidth;
            var srcImageWidth = destNnf.SourceWidth;

            // Nnf that needs to be merged to our dest nnf.
            var srcNnfData = srcNnf.GetNnfItems();
            var destNnfData = destNnf.GetNnfItems();

            var destNnfPointsIndexes = (destNnfMap as IAreasMapping).DestArea.GetPointsIndexes(destImageWidth);
            var srcNnfPointsIndexes = (srcNnfMap as IAreasMapping).DestArea.GetPointsIndexes(destImageWidth);
            var mappings = srcNnfMap.ExtractMappedAreasInfo(destImageWidth, srcImageWidth);

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

                fixed (double* destNnfP = destNnfData)
                fixed (double* srcNnfP = srcNnfData)
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
        }

        private static Area2DMap CreateMapping(Nnf nnf)
        {
            var dest = Area2D.Create(0, 0, nnf.DstWidth, nnf.DstHeight);
            var src = Area2D.Create(0, 0, nnf.SourceWidth, nnf.SourceHeight);

            return new Area2DMapBuilder()
                .InitNewMap(dest, src)
                .Build();
        }
    }
}