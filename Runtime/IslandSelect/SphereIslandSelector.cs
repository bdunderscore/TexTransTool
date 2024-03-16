using System.Collections;
using System.Collections.Generic;
using System.Linq;
using net.rs64.TexTransCore.Island;

namespace net.rs64.TexTransTool.IslandSelector
{
    public class SphereIslandSelector : AbstractIslandSelector
    {
        public float SphereSize = 0.1f;

        internal override BitArray IslandSelect(Island[] islands, IslandDescription[] islandDescription)
        {
            var bitArray = new BitArray(islands.Length);
            var matrix = transform.worldToLocalMatrix;

            var sqrMagMax = SphereSize * SphereSize;
            for (var islandIndex = 0; islands.Length > islandIndex; islandIndex += 1)
            {
                var description = islandDescription[islandIndex];

                foreach (var tri in islands[islandIndex].triangles)
                {
                    for (var vi = 0; 3 > vi; vi += 1)
                    {
                        if (matrix.MultiplyPoint3x4(description.Position[tri[vi]]).sqrMagnitude < sqrMagMax) { bitArray[islandIndex] = true; break; }
                    }
                    if (bitArray[islandIndex]) { break; }
                }
            }

            return bitArray;
        }
    }
}
