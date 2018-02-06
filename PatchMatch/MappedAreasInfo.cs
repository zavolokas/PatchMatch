using System.Collections.Generic;
using Zavolokas.Structures;

namespace Zavolokas.ImageProcessing.PatchMatch
{
    internal class MappedAreasInfo
    {
        public HashSet<int> DestAreaPointsIndexesSet;
        public HashSet<int> SrcAreaPointsIndexesSet;
        public int[] SrcAreaPointsIndexes;
        public int[] DestAreaPointsIndexes;
        public Rectangle SrcBound;

        public MappedAreasInfo Clone()
        {
            return new MappedAreasInfo
            {
                DestAreaPointsIndexes = DestAreaPointsIndexes,
                DestAreaPointsIndexesSet = new HashSet<int>(DestAreaPointsIndexes),
                SrcAreaPointsIndexes = SrcAreaPointsIndexes,
                SrcAreaPointsIndexesSet = new HashSet<int>(SrcAreaPointsIndexes),
                SrcBound = SrcBound
            };
        }
    }
}