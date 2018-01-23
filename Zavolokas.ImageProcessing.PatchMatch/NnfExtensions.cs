using System;
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
    }
}