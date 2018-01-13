using System;
using Grapute;
using Zavolokas.ImageProcessing.PatchMatch;

namespace Zavolokas.ImageProcessing.Parallel.PatchMatch
{
    public class NnfInit : Node<PmData, PmData>
    {
        private readonly ImagePatchDistanceCalculator _calculator;

        public NnfInit(ImagePatchDistanceCalculator calculator)
        {
            if (calculator == null)
                throw new ArgumentNullException(nameof(calculator));

            _calculator = calculator;
        }

        protected override PmData[] Process(PmData data)
        {
            if (data == null)
                throw new ArgumentNullException(nameof(data));

            PatchMatchNnfBuilder.RunRandomNnfInitIteration(data.Nnf, data.Map, data.DestImage, data.SrcImage, data.DestImagePixelsArea, _calculator, data.Settings);
            
            return new[] {data};
        }
    }
}