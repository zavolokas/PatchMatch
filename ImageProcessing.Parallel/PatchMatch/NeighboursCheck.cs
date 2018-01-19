using System;
using Grapute;
using Zavolokas.ImageProcessing.PatchMatch;

namespace Zavolokas.ImageProcessing.Parallel.PatchMatch
{
    public sealed class NeighboursCheck : Node<PmData, PmData>
    {
        private readonly IPatchMatchNnfBuilder _patchMatchNnfBuilder;
        private readonly ImagePatchDistanceCalculator _calculator;
        private readonly NeighboursCheckDirection _direction;

        public NeighboursCheck(IPatchMatchNnfBuilder patchMatchNnfBuilder, ImagePatchDistanceCalculator calculator,
            NeighboursCheckDirection direction = NeighboursCheckDirection.Forward)
        {
            if (patchMatchNnfBuilder == null)
                throw new ArgumentNullException(nameof(patchMatchNnfBuilder));

            if (calculator == null)
                throw new ArgumentNullException(nameof(calculator));

            _patchMatchNnfBuilder = patchMatchNnfBuilder;
            _calculator = calculator;
            _direction = direction;
        }

        protected override PmData[] Process(PmData data)
        {
            if (data == null)
                throw new ArgumentNullException(nameof(data));

            _patchMatchNnfBuilder.RunBuildNnfIteration(data.Nnf, data.DestImage, data.SrcImage, _direction, data.Settings, _calculator, data.Map, data.DestImagePixelsArea);

            return new[] { data };
        }
    }
}