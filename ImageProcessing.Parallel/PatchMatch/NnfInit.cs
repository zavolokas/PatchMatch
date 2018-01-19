using System;
using Grapute;
using Zavolokas.ImageProcessing.PatchMatch;

namespace Zavolokas.ImageProcessing.Parallel.PatchMatch
{
    public class NnfInit : Node<PmData, PmData>
    {
        private readonly IPatchMatchNnfBuilder _patchMatchNnfBuilder;
        private readonly ImagePatchDistanceCalculator _calculator;

        public NnfInit(IPatchMatchNnfBuilder patchMatchNnfBuilder, ImagePatchDistanceCalculator calculator)
        {
            if (patchMatchNnfBuilder == null)
                throw new ArgumentNullException(nameof(patchMatchNnfBuilder));

            if (calculator == null)
                throw new ArgumentNullException(nameof(calculator));

            _patchMatchNnfBuilder = patchMatchNnfBuilder;
            _calculator = calculator;
        }

        protected override PmData[] Process(PmData data)
        {
            if (data == null)
                throw new ArgumentNullException(nameof(data));

            _patchMatchNnfBuilder.RunRandomNnfInitIteration(data.Nnf, data.DestImage, data.SrcImage, _calculator, data.Settings, data.Map, data.DestImagePixelsArea);
            
            return new[] {data};
        }
    }
}