using System.Collections.Generic;

namespace DeepNestLib.Svg
{
    public class PopulationItem
    {
        public object processing = null;

        public double? fitness;

        public float[] Rotation;
        public List<NFP> placements;

        public NFP[] paths;
        public double area;
    }
}
