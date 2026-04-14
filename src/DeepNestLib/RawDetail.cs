using DeepNestLib.Svg;
using System.Collections.Generic;

namespace DeepNestLib
{
    public class RawDetail
    {
        public List<LocalContour> Outers = new List<LocalContour>();
        public List<LocalContour> Holes = new List<LocalContour>();

        public string Name { get; set; }
    }
}
