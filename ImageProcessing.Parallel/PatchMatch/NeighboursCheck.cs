using System;
using Grapute;
using Zavolokas.ImageProcessing.PatchMatch;

namespace Zavolokas.ImageProcessing.Parallel.PatchMatch
{
    public sealed class NeighboursCheck : Node<PmData, PmData>
    {
        private readonly ImagePatchDistanceCalculator _calculator;
        private readonly NeighboursCheckDirection _direction;

        public NeighboursCheck(ImagePatchDistanceCalculator calculator,
            NeighboursCheckDirection direction = NeighboursCheckDirection.Forward)
        {
            if (calculator == null)
                throw new ArgumentNullException(nameof(calculator));

            _calculator = calculator;
            _direction = direction;
        }

        protected override PmData[] Process(PmData data)
        {
            if (data == null)
                throw new ArgumentNullException(nameof(data));

            PatchMatchNnfBuilder.RunBuildNnfIteration(data.Nnf, data.Map, data.DestImage, data.SrcImage, data.DestImagePixelsArea , _calculator, _direction, data.Settings);

            return new[] { data };
        }
    }
}