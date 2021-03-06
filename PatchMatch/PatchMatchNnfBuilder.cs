using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Zavolokas.Math.Random;
using Zavolokas.Structures;

namespace Zavolokas.ImageProcessing.PatchMatch
{
    // NOTE: We avoid source patch overlap with the remove area when we build NNF 
    // because than we have a chance to calculate a new color either based
    // on an empty pixels or on the existing one that we want to inpaint.

    public class PatchMatchNnfBuilder : IPatchMatchNnfBuilder
    {
        /// <summary>
        /// Runs the random NNF initialization iteration using the whole areas of the dest and the source images. Uses CIE76 to calculate patch similarity.
        /// </summary>
        /// <param name="nnf">The NNF.</param>
        /// <param name="destImage">The dest image. For each patch at this image we will look for a similar one at the source image.</param>
        /// <param name="srcImage">The source image. Source of the patches for the dest image.</param>
        /// <param name="settings">The settings that control parameters of the algorithm.</param>
        public void RunRandomNnfInitIteration(Nnf nnf, ZsImage destImage, ZsImage srcImage, PatchMatchSettings settings)
        {
            var patchDistanceCalculator = ImagePatchDistance.Cie76;
            RunRandomNnfInitIteration(nnf, destImage, srcImage, settings, patchDistanceCalculator);
        }

        /// <summary>
        /// Runs the random NNF initialization iteration using the whole areas of the dest and the source images.
        /// </summary>
        /// <param name="nnf">The NNF.</param>
        /// <param name="destImage">The dest image. For each patch at this image we will look for a similar one at the source image.</param>
        /// <param name="srcImage">The source image. Source of the patches for the dest image.</param>
        /// <param name="settings">The settings that control parameters of the algorithm.</param>
        /// <param name="patchDistanceCalculator">The calculator that calculates similarity of two patches. By deafult the Cie76 is used.</param>
        /// <exception cref="ArgumentNullException">
        /// destImage
        /// or
        /// srcImage
        /// </exception>
        public void RunRandomNnfInitIteration(Nnf nnf, ZsImage destImage, ZsImage srcImage, PatchMatchSettings settings,
            ImagePatchDistanceCalculator patchDistanceCalculator)
        {
            if (destImage == null) throw new ArgumentNullException(nameof(destImage));
            if (srcImage == null) throw new ArgumentNullException(nameof(srcImage));

            var destArea = Area2D.Create(0, 0, destImage.Width, destImage.Height);
            var srcArea = Area2D.Create(0, 0, srcImage.Width, srcImage.Height);
            var map = new Area2DMapBuilder()
                .InitNewMap(destArea, srcArea)
                .Build();
            RunRandomNnfInitIteration(nnf, destImage, srcImage, settings, patchDistanceCalculator, map);
        }

        /// <summary>
        /// Runs the random NNF initialization iteration for the associated areas of the dest and the source images.
        /// </summary>
        /// <param name="nnf">The NNF.</param>
        /// <param name="destImage">The dest image. For each patch at this image we will look for a similar one at the source image.</param>
        /// <param name="srcImage">The source image. Source of the patches for the dest image.</param>
        /// <param name="settings">The settings that control parameters of the algorithm.</param>
        /// <param name="patchDistanceCalculator">The calculator that calculates similarity of two patches. By deafult the Cie76 is used.</param>
        /// <param name="areasMapping">The areas mapping. By default whole area of the dest image is associated with the whole area of the source image.</param>
        /// <exception cref="ArgumentNullException">destImage</exception>
        public void RunRandomNnfInitIteration(Nnf nnf, ZsImage destImage, ZsImage srcImage, PatchMatchSettings settings,
            ImagePatchDistanceCalculator patchDistanceCalculator, Area2DMap areasMapping)
        {
            if (destImage == null) throw new ArgumentNullException(nameof(destImage));

            var destPixelsArea = Area2D.Create(0, 0, destImage.Width, destImage.Height);
            RunRandomNnfInitIteration(nnf, destImage, srcImage, settings, patchDistanceCalculator, areasMapping, destPixelsArea);
        }

        /// <summary>
        /// Runs the random NNF initialization iteration for the associated areas of the dest and the source images.
        /// </summary>
        /// <param name="nnf">The NNF.</param>
        /// <param name="destImage">The dest image. For each patch at this image we will look for a similar one at the source image.</param>
        /// <param name="srcImage">The source image. Source of the patches for the dest image.</param>
        /// <param name="settings">The settings that control parameters of the algorithm.</param>
        /// <param name="patchDistanceCalculator">The calculator that calculates similarity of two patches. By deafult the Cie76 is used.</param>
        /// <param name="areasMapping">The areas mapping. By default whole area of the dest image is associated with the whole area of the source image.</param>
        /// <param name="destPixelsArea">Area on the dest image that actually containes pixels. By default is the area of the entire image.</param>
        /// <exception cref="ArgumentNullException">
        /// nnf
        /// or
        /// destImage
        /// or
        /// srcImage
        /// or
        /// settings
        /// or
        /// patchDistanceCalculator
        /// or
        /// areasMapping
        /// or
        /// destPixelsArea
        /// </exception>
        public unsafe void RunRandomNnfInitIteration(Nnf nnf, ZsImage destImage, ZsImage srcImage, PatchMatchSettings settings, ImagePatchDistanceCalculator patchDistanceCalculator, Area2DMap areasMapping, Area2D destPixelsArea)
        {
            if (nnf == null) throw new ArgumentNullException(nameof(nnf));
            if (destImage == null) throw new ArgumentNullException(nameof(destImage));
            if (srcImage == null) throw new ArgumentNullException(nameof(srcImage));
            if (settings == null) throw new ArgumentNullException(nameof(settings));
            if (patchDistanceCalculator == null) throw new ArgumentNullException(nameof(patchDistanceCalculator));
            if (areasMapping == null) throw new ArgumentNullException(nameof(areasMapping));
            if (destPixelsArea == null) throw new ArgumentNullException(nameof(destPixelsArea));

            const byte MaxAttempts = 32;

            var nnfdata = nnf.GetNnfItems();
            var patchSize = settings.PatchSize;
            var patchPointsAmount = patchSize * patchSize;
            var destImageWidth = destImage.Width;
            var srcImageWidth = srcImage.Width;

            // Decide on how many partitions we should divade the processing
            // of the elements.
            var partsCount = areasMapping.DestElementsCount > settings.NotDividableMinAmountElements
                ? settings.ThreadsCount
                : 1;
            var partSize = (int)(areasMapping.DestElementsCount / partsCount);

            var pixelsArea = (areasMapping as IAreasMapping).DestArea;
            var destPointsIndexes = pixelsArea.GetPointsIndexes(destImageWidth);
            pixelsArea = pixelsArea.Intersect(destPixelsArea);
            var destAvailablePixelsIndexes = pixelsArea.GetPointsIndexes(destImageWidth);
            var mappings = areasMapping.ExtractMappedAreasInfo(destImageWidth, srcImageWidth);

            //for (int partIndex = 0; partIndex < partsCount; partIndex++)
            Parallel.For(0, partsCount, partIndex =>
            {
                // Colne mapping to avoid conflicts in multithread
                //var destPointsIndexesSet = new HashSet<int>(destPointsIndexes);
                var destAvailablePixelsIndexesSet = new HashSet<int>(destAvailablePixelsIndexes);

                var mappedAreasInfos = new MappedAreasInfo[mappings.Length];
                for (int i = 0; i < mappings.Length; i++)
                {
                    mappedAreasInfos[i] = mappings[i].Clone();
                }

                #region Variables definition
                int destPointIndex;
                int destPointX;
                int destPointY;

                int srcPointIndex = 0;
                int srcPointX;
                int srcPointY;

                double distance;
                int nnfPos;
                byte attemptsCount = 0;
                #endregion

                var rnd = new FastRandom();

                var firstPointIndex = partIndex * partSize;
                var lastPointIndex = firstPointIndex + partSize - 1;
                if (partIndex == partsCount - 1) lastPointIndex = areasMapping.DestElementsCount - 1;
                if (lastPointIndex > areasMapping.DestElementsCount) lastPointIndex = areasMapping.DestElementsCount - 1;

                // Init the dest & source patch
                var destPatchPointIndexes = new int[patchPointsAmount];
                var srcPatchPointIndexes = new int[patchPointsAmount];
                bool isPatchFit = false;

                fixed (double* nnfdataP = nnfdata)
                fixed (int* dstPointIndexesP = destPointsIndexes)
                fixed (int* srcPatchPointIndexesP = srcPatchPointIndexes)
                fixed (int* destPatchPointIndexesP = destPatchPointIndexes)
                fixed (double* destPixelsDataP = destImage.PixelsData)
                fixed (double* sourcePixelsDataP = srcImage.PixelsData)
                {
                    for (var pointIndex = firstPointIndex; pointIndex <= lastPointIndex; pointIndex++)
                    {
                        destPointIndex = *(dstPointIndexesP + pointIndex);
                        destPointX = destPointIndex % destImageWidth;
                        destPointY = destPointIndex / destImageWidth;

                        Utils.PopulatePatchPixelsIndexes(destPatchPointIndexesP, destPointX, destPointY, patchSize, destImageWidth, destAvailablePixelsIndexesSet, out isPatchFit);

                        nnfPos = destPointIndex * 2;

                        // Obtain the source area associated with the destination area
                        // to which the current dest point belongs to.
                        // In that area we are allowed to look for the source patches.
                        MappedAreasInfo mappedAreasInfo = mappedAreasInfos.FirstOrDefault(mai => mai.DestAreaPointsIndexesSet.Contains(destPointIndex));

                        isPatchFit = false;
                        attemptsCount = 0;
                        var srcPointsAmount = mappedAreasInfo.SrcAreaPointsIndexes.Length;
                        while (!isPatchFit && attemptsCount < MaxAttempts && attemptsCount < srcPointsAmount)
                        {
                            srcPointIndex = mappedAreasInfo.SrcAreaPointsIndexes[rnd.Next(srcPointsAmount)];
                            srcPointX = srcPointIndex % srcImageWidth;
                            srcPointY = srcPointIndex / srcImageWidth;

                            Utils.PopulatePatchPixelsIndexes(srcPatchPointIndexesP, srcPointX, srcPointY, patchSize, srcImageWidth, mappedAreasInfo.SrcAreaPointsIndexesSet, out isPatchFit);
                            attemptsCount++;
                        }

                        distance = patchDistanceCalculator.Calculate(destPatchPointIndexesP, srcPatchPointIndexesP, double.MaxValue,
                            destPixelsDataP, sourcePixelsDataP, destImage, srcImage, patchPointsAmount);

                        *(nnfdataP + nnfPos + 0) = srcPointIndex;
                        *(nnfdataP + nnfPos + 1) = distance;
                    }
                }
            });
        }

        /// <summary>
        /// Runs the NNF build iteration using the whole areas of the dest and the source images. Uses CIE76 to calculate patch similarity.
        /// </summary>
        /// <param name="nnf">The NNF.</param>
        /// <param name="destImage">The dest image. For each patch at this image we will look for a similar one at the source image.</param>
        /// <param name="srcImage">The source image. Source of the patches for the dest image.</param>
        /// <param name="direction">The direction to look for a patches.</param>
        /// <param name="settings">The settings that control parameters of the algorithm.</param>
        public void RunBuildNnfIteration(Nnf nnf, ZsImage destImage, ZsImage srcImage,
            NeighboursCheckDirection direction, PatchMatchSettings settings)
        {
            var patchDistanceCalculator = ImagePatchDistance.Cie76;

            RunBuildNnfIteration(nnf, destImage, srcImage, direction, settings, patchDistanceCalculator);
        }

        /// <summary>
        /// Runs the NNF build iteration using the whole areas of the dest and the source images.
        /// </summary>
        /// <param name="nnf">The NNF.</param>
        /// <param name="destImage">The dest image. For each patch at this image we will look for a similar one at the source image.</param>
        /// <param name="srcImage">The source image. Source of the patches for the dest image.</param>
        /// <param name="direction">The direction to look for a patches.</param>
        /// <param name="settings">The settings that control parameters of the algorithm.</param>
        /// <param name="patchDistanceCalculator">The calculator that calculates similarity of two patches. By deafult the Cie76 is used.</param>
        /// <exception cref="ArgumentNullException">
        /// destImage
        /// or
        /// srcImage
        /// </exception>
        public void RunBuildNnfIteration(Nnf nnf, ZsImage destImage, ZsImage srcImage,
            NeighboursCheckDirection direction, PatchMatchSettings settings,
            ImagePatchDistanceCalculator patchDistanceCalculator)
        {
            if (destImage == null) throw new ArgumentNullException(nameof(destImage));
            if (srcImage == null) throw new ArgumentNullException(nameof(srcImage));

            var destArea = Area2D.Create(0, 0, destImage.Width, destImage.Height);
            var srcArea = Area2D.Create(0, 0, srcImage.Width, srcImage.Height);
            var map = new Area2DMapBuilder()
                .InitNewMap(destArea, srcArea)
                .Build();

            RunBuildNnfIteration(nnf, destImage, srcImage, direction, settings, patchDistanceCalculator, map);
        }

        /// <summary>
        /// Runs the NNF build iteration for the associated areas of the dest and the source images.
        /// </summary>
        /// <param name="nnf">The NNF.</param>
        /// <param name="destImage">The dest image. For each patch at this image we will look for a similar one at the source image.</param>
        /// <param name="srcImage">The source image. Source of the patches for the dest image.</param>
        /// <param name="direction">The direction to look for a patches.</param>
        /// <param name="settings">The settings that control parameters of the algorithm.</param>
        /// <param name="patchDistanceCalculator">The calculator that calculates similarity of two patches. By deafult the Cie76 is used.</param>
        /// <param name="areasMapping">The areas mapping. By default whole area of the dest image is associated with the whole area of the source image.</param>
        /// <exception cref="ArgumentNullException">destImage</exception>
        public void RunBuildNnfIteration(Nnf nnf, ZsImage destImage, ZsImage srcImage,
            NeighboursCheckDirection direction, PatchMatchSettings settings,
            ImagePatchDistanceCalculator patchDistanceCalculator, Area2DMap areasMapping)
        {
            if (destImage == null) throw new ArgumentNullException(nameof(destImage));

            var destPixelsArea = Area2D.Create(0, 0, destImage.Width, destImage.Height);

            RunBuildNnfIteration(nnf, destImage, srcImage, direction, settings, patchDistanceCalculator, areasMapping, destPixelsArea);
        }

        /// <summary>
        /// Runs the NNF build iteration for the associated areas of the dest and the source images.
        /// </summary>
        /// <param name="nnf">The NNF.</param>
        /// <param name="destImage">The dest image. For each patch at this image we will look for a similar one at the source image.</param>
        /// <param name="srcImage">The source image. Source of the patches for the dest image.</param>
        /// <param name="direction">The direction to look for a patches.</param>
        /// <param name="settings">The settings that control parameters of the algorithm.</param>
        /// <param name="patchDistanceCalculator">The calculator that calculates similarity of two patches. By deafult the Cie76 is used.</param>
        /// <param name="areasMapping">The areas mapping. By default whole area of the dest image is associated with the whole area of the source image.</param>
        /// <param name="destPixelsArea">Area on the dest image that actually containes pixels. By default is the area of the entire image.</param>
        /// <exception cref="ArgumentNullException">
        /// nnf
        /// or
        /// destImage
        /// or
        /// srcImage
        /// or
        /// settings
        /// or
        /// patchDistanceCalculator
        /// or
        /// areasMapping
        /// or
        /// destPixelsArea
        /// </exception>
        public unsafe void RunBuildNnfIteration(Nnf nnf, ZsImage destImage, ZsImage srcImage, NeighboursCheckDirection direction, PatchMatchSettings settings, ImagePatchDistanceCalculator patchDistanceCalculator, Area2DMap areasMapping, Area2D destPixelsArea)
        {
            if (nnf == null) throw new ArgumentNullException(nameof(nnf));
            if (destImage == null) throw new ArgumentNullException(nameof(destImage));
            if (srcImage == null) throw new ArgumentNullException(nameof(srcImage));
            if (settings == null) throw new ArgumentNullException(nameof(settings));
            if (patchDistanceCalculator == null) throw new ArgumentNullException(nameof(patchDistanceCalculator));
            if (areasMapping == null) throw new ArgumentNullException(nameof(areasMapping));
            if (destPixelsArea == null) throw new ArgumentNullException(nameof(destPixelsArea));

            sbyte offs = (sbyte)(direction == NeighboursCheckDirection.Forward ? 1 : -1);
            sbyte[][] offsets =
            {
                new[] {offs, (sbyte)0},
                new[] {(sbyte)0, offs}
            };

            #region Cache settings and inputs in the local variables
            var nnfdata = nnf.GetNnfItems();
            var nnfSrcWidth = nnf.SourceWidth;

            var patchSize = settings.PatchSize;
            var patchLength = patchSize * patchSize;
            var randomSearchRadius = settings.RandomSearchRadius;
            var alpha = settings.RandomSearchAlpha;
            var minSearchWindowSize = settings.MinSearchWindowSize;

            var destImageWidth = destImage.Width;
            var srcImageWidth = srcImage.Width;
            #endregion

            // Obtain the data that allow us getting a point index
            // regarding to the process direction(forward or backward)
            var addModData = GetAddModData(direction, areasMapping.DestElementsCount);
            var add = addModData.Item1;
            var mod = addModData.Item2;

            // Decide on how many partitions we should divade the processing
            // of the elements.
            var partsCount = areasMapping.DestElementsCount > settings.NotDividableMinAmountElements
                ? settings.ThreadsCount
                : 1;
            var partSize = areasMapping.DestElementsCount / partsCount;

            var pixelsArea = (areasMapping as IAreasMapping).DestArea;
            var destPointIndexes = pixelsArea.GetPointsIndexes(destImageWidth, direction == NeighboursCheckDirection.Forward);
            pixelsArea = pixelsArea.Intersect(destPixelsArea);
            var destAvailablePixelsIndexes = pixelsArea.GetPointsIndexes(destImageWidth, direction == NeighboursCheckDirection.Forward);

            var mappings = areasMapping.ExtractMappedAreasInfo(destImageWidth, srcImageWidth, direction == NeighboursCheckDirection.Forward);

            //for (int partIndex = 0; partIndex < partsCount; partIndex++)
            Parallel.For(0, partsCount, partIndex =>
            {
                var rnd = new FastRandom();
                int i;

                // Colne mapping to avoid conflicts in multithread
                //var destPointsIndexesSet = new HashSet<int>(destPointIndexes);
                var destAvailablePixelsIndexesSet = new HashSet<int>(destAvailablePixelsIndexes);
                var mappedAreasInfos = new MappedAreasInfo[mappings.Length];
                for (i = 0; i < mappings.Length; i++)
                {
                    mappedAreasInfos[i] = mappings[i].Clone();
                }

                var firstPointIndex = partIndex * partSize;
                var lastPointIndex = firstPointIndex + partSize - 1;
                if (partIndex == partsCount - 1) lastPointIndex = destPointIndexes.Length - 1;
                if (lastPointIndex > destPointIndexes.Length) lastPointIndex = destPointIndexes.Length - 1;

                // Init the dest & source patch
                var destPatchPixelsIndexes = new int[patchSize * patchSize];
                var srcPatchPixelsIndexes = new int[patchSize * patchSize];

                bool isPatchFit = false;

                fixed (double* nnfP = nnfdata)
                fixed (int* destPointIndexesP = destPointIndexes)
                fixed (int* srcPatchPixelsIndexesP = srcPatchPixelsIndexes)
                fixed (int* destPatchPixelsIndexesP = destPatchPixelsIndexes)
                fixed (double* destImagePixelsDataP = destImage.PixelsData)
                fixed (double* sourceImagePixelsDataP = srcImage.PixelsData)
                {
                    for (var j = firstPointIndex; j <= lastPointIndex; j++)
                    {
                        // Obtain a destination point from the mapping.
                        // The dest point is the position of the dest patch.
                        // Chocie of the destination point depends on the 
                        // direction in which we iterate the points (forward or backward).
                        var destPointIndex = *(destPointIndexesP + add + j * mod);
                        var destPointX = destPointIndex % destImageWidth;
                        var destPointY = destPointIndex / destImageWidth;

                        // We going to find the most similar source patch to the dest patch 
                        // by comparing them. 
                        // Note: we don't care about the coverage state of the dest patch
                        // because it is at least partially covered. The situation when it is
                        // not covered at all is impossible, since the dest point is a part of the
                        // dest area.

                        // We store our patch in a flat array of points for simplicity.
                        // Populate dest patch array with corresponding dest image points indexes.
                        Utils.PopulatePatchPixelsIndexes(destPatchPixelsIndexesP, destPointX, destPointY, patchSize, destImageWidth, destAvailablePixelsIndexesSet, out isPatchFit);

                        // Obtain the mapped areas info with the destination area
                        // that contains the current dest point. In the associated
                        // source area we are allowed to look for the source patches.
                        MappedAreasInfo mappedAreasInfo = mappedAreasInfos.FirstOrDefault(mai => mai.DestAreaPointsIndexesSet.Contains(destPointIndex));

                        // To improve performance, we cache the value of the NNF
                        // at the current point in the local variables.
                        var nnfPos = destPointIndex * 2;
                        var bestSrcPointIndex = (int)*(nnfP + nnfPos + 0);
                        var distanceToBestSrcPatch = *(nnfP + nnfPos + 1);

                        // We assume that the value of the NNF at the current 
                        // point is the best so far and we have not found anything beter.
                        var isBetterSrcPatchFound = false;

                        // Now we check two close neighbours. First - horisontal, than vertical.

                        #region f(x - p) + (1, 0) & f(x - p) + (0, 1)

                        //1st iteration: f(x - p) + (1, 0)
                        //2nd iteration: f(x - p) + (0, 1)

                        int candidateSrcPointY = 0;
                        int candidateSrcPointX = 0;
                        double distance;
                        for (var offsetIndex = 0; offsetIndex < offsets.Length; offsetIndex++)
                        {
                            // Obtain the position of the neighbour.
                            // During the forward search 
                            //  on the first iteration it is the neighbour at the left side 
                            //  on the second iteration it is the neighbour at the top
                            // During the backward search 
                            //  on the first iteration it is the neighbour at the right side 
                            //  on the second iteration it is the neighbour at the bottom

                            var offset = offsets[offsetIndex];
                            var destPointNeighbourX = destPointX - offset[0];
                            var destPointNeighbourY = destPointY - offset[1];
                            var destPointNeighbourIndex = destPointNeighbourY * destImageWidth + destPointNeighbourX;

                            // Make sure that the neighbour point belongs to the mapping
                            if (!mappedAreasInfo.DestAreaPointsIndexesSet.Contains(destPointNeighbourIndex))
                                continue;

                            // There is a high chance that the neighbour's source patch neighbour 
                            // is similar to the current dest patch. Let's check it.
                            // Obtain the position of the neighbour's source patch neighbour.
                            var neighbourNnfPos = 2 * destPointNeighbourIndex;
                            var neighbourSrcPointIndex = (int)*(nnfP + neighbourNnfPos + 0);
                            candidateSrcPointY = neighbourSrcPointIndex / nnfSrcWidth + offset[1];
                            candidateSrcPointX = neighbourSrcPointIndex % nnfSrcWidth + offset[0];
                            var candidateSrcPointIndex = candidateSrcPointY * srcImageWidth + candidateSrcPointX;

                            // Make sure that the source patch resides inside the 
                            // associated source area of the dest area.
                            if (!mappedAreasInfo.SrcAreaPointsIndexesSet.Contains(candidateSrcPointIndex))
                                continue;

                            // Populate source patch array with corresponding source image points indexes
                            Utils.PopulatePatchPixelsIndexes(srcPatchPixelsIndexesP, candidateSrcPointX, candidateSrcPointY, patchSize, srcImageWidth, mappedAreasInfo.SrcAreaPointsIndexesSet, out isPatchFit);

                            // Calculate distance between the patches
                            distance = patchDistanceCalculator.Calculate(destPatchPixelsIndexesP, srcPatchPixelsIndexesP, distanceToBestSrcPatch, destImagePixelsDataP, sourceImagePixelsDataP, destImage, srcImage, patchLength);

                            if (isPatchFit && distance < distanceToBestSrcPatch)
                            {
                                isBetterSrcPatchFound = true;
                                bestSrcPointIndex = candidateSrcPointIndex;
                                distanceToBestSrcPatch = distance;
                            }
                        }

                        #endregion

                        #region random search

                        //v0 = f(x)

                        // For the random search we need to find out 
                        // the size of the window where to search
                        var srcAreaBound = mappedAreasInfo.SrcBound;
                        var srcAreaWidth = srcAreaBound.Width;
                        var srcAreaHeight = srcAreaBound.Height;
                        var searchWindowSize = srcAreaHeight < srcAreaWidth ? srcAreaHeight : srcAreaWidth;
                        if (searchWindowSize > minSearchWindowSize) searchWindowSize = minSearchWindowSize;

                        // Init the search radius;
                        var searchRadius = randomSearchRadius;
                        var bestSrcPointX = bestSrcPointIndex % srcImageWidth;
                        var bestSrcPointY = bestSrcPointIndex / srcImageWidth;

                        for (i = 0; searchRadius >= 1; i++)
                        {
                            //TODO: change and measure inpact of changing call to Pow
                            searchRadius = searchWindowSize * System.Math.Pow(alpha, i);

                            isPatchFit = false;
                            var attempts = 0;

                            while (!isPatchFit && attempts < 5)
                            {
                                attempts++;
                                candidateSrcPointX = (int)(bestSrcPointX + (rnd.NextDouble() * 2 - 1.0) * searchRadius);
                                candidateSrcPointY = (int)(bestSrcPointY + (rnd.NextDouble() * 2 - 1.0) * searchRadius);

                                // Populate source patch array with corresponding source image points indexes
                                Utils.PopulatePatchPixelsIndexes(srcPatchPixelsIndexesP, candidateSrcPointX, candidateSrcPointY, patchSize, srcImageWidth, mappedAreasInfo.SrcAreaPointsIndexesSet, out isPatchFit);
                            }

                            // Calculate distance between the patches
                            distance = patchDistanceCalculator.Calculate(destPatchPixelsIndexesP, srcPatchPixelsIndexesP, distanceToBestSrcPatch, destImagePixelsDataP, sourceImagePixelsDataP, destImage, srcImage, patchLength);

                            if (isPatchFit && distance < distanceToBestSrcPatch)
                            {
                                distanceToBestSrcPatch = distance;
                                bestSrcPointIndex = candidateSrcPointY * srcImageWidth + candidateSrcPointX;
                                isBetterSrcPatchFound = true;
                            }
                        }

                        #endregion

                        if (isBetterSrcPatchFound)
                        {
                            *(nnfP + nnfPos + 0) = bestSrcPointIndex;
                            *(nnfP + nnfPos + 1) = distanceToBestSrcPatch;
                        }
                    }
                }
            });
        }

        private static Tuple<int, int> GetAddModData(NeighboursCheckDirection direction, int destElementsCount)
        {
            int add, mod;
            if (direction == NeighboursCheckDirection.Forward)
            {
                add = 0;
                mod = 1;
            }
            else
            {
                add = destElementsCount - 1;
                mod = -1;
            }

            return new Tuple<int, int>(add, mod);
        }
    }
}