using System.Collections.Generic;

namespace DeepNestLib.Sheets
{
    public class SheetPlacement
    {
        public double? fitness;

        public float[] Rotation;
        public List<SheetPlacementItem>[] placements;

        public NFP[] paths;
        public double area;
        public double mergedLength;
        internal int index;
    }
}
