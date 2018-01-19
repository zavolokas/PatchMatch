namespace Zavolokas.ImageProcessing.PatchMatch
{
    public interface IParallelOptions
    {
        int NotDividableMinAmountElements { get; set; }
        byte ThreadsCount { get; set; }
    }
}