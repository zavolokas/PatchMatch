using System;

namespace Zavolokas.ImageProcessing.PatchMatch
{

    public sealed class PatchMatchSettings : ParallelOptions, ICloneable
    {
        public byte PatchSize = 11;
        public byte IterationsAmount = 5;
        public byte PremergeIterationsAmount = 2;
        public double RandomSearchRadius = 2;
        public double RandomSearchAlpha = 0.5;
        public int MinSearchWindowSize = 240;
        public double StablePixelsProcent = 1;
        public byte MarkupIterationsAmount = 5;
        public int MaxAllowedTuIterations = int.MaxValue;
        public bool RestrictTuIterations = false;

        public PatchMatchSettings()
        {
            ThreadsCount = (byte)Environment.ProcessorCount;
            NotDividableMinAmountElements = 80;
        }

        public object Clone()
        {
            return new PatchMatchSettings
            {
                PatchSize = PatchSize,
                IterationsAmount = IterationsAmount,
                PremergeIterationsAmount = PremergeIterationsAmount,
                RandomSearchRadius = RandomSearchRadius,
                RandomSearchAlpha = RandomSearchAlpha,
                MinSearchWindowSize = MinSearchWindowSize,
                ThreadsCount = ThreadsCount,
                NotDividableMinAmountElements = NotDividableMinAmountElements,
                StablePixelsProcent = StablePixelsProcent,
                MarkupIterationsAmount = MarkupIterationsAmount,
                MaxAllowedTuIterations = MaxAllowedTuIterations,
                RestrictTuIterations = RestrictTuIterations,
            };
        }
    }
}