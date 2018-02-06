using Grapute;

namespace Zavolokas.ImageProcessing.Parallel.PatchMatch
{
    internal class NnfSplit : Node<PmData, PmData>
    {
        protected override PmData[] Process(PmData data)
        {
            return new[] { data };
        }
    }
}