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