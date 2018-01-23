namespace Zavolokas.ImageProcessing.PatchMatch
{
    public class ParallelOptions
    {
        public int NotDividableMinAmountElements { get; set; } = 80;
        public byte ThreadsCount { get; set; } = 4;
    }
}