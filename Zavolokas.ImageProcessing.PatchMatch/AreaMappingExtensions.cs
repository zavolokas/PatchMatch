using System.Linq;
using Zavolokas.Structures;

namespace Zavolokas.ImageProcessing.PatchMatch
{
    internal static class AreaMappingExtensions
    {
        internal static MappedAreasInfo[] ExtractMappedAreasInfo(this IAreasMapping map, int destImageWidth, int srcImageWidth, bool forward)
        {
            var areaAssociations = map.AssociatedAreasAsc.Reverse().ToArray();
            var mapping = new MappedAreasInfo[areaAssociations.Length];
            for (int i = 0; i < areaAssociations.Length; i++)
            {
                var areaAssociation = areaAssociations[i];
                var ass = new MappedAreasInfo
                {
                    DestAreaPointsIndexes = new int[areaAssociation.Item1.ElementsCount],
                    SrcAreaPointsIndexes = new int[areaAssociation.Item2.ElementsCount],
                    SrcBound = areaAssociation.Item2.Bound
                };

                areaAssociation.Item1.FillMappedPointsIndexes(ass.DestAreaPointsIndexes, destImageWidth, forward);
                areaAssociation.Item2.FillMappedPointsIndexes(ass.SrcAreaPointsIndexes, srcImageWidth, forward);

                mapping[i] = ass;
            }
            return mapping;
        }
    }
}