using System.Collections.Generic;

namespace DeepNestLib.Svg
{
    public class DataInfo
    {
        public int index;
        public List<NFP> sheets;
        public int[] sheetids;
        public int[] sheetsources;
        public List<List<NFP>> sheetchildren;
        public PopulationItem individual;
        public SvgNestConfig config;
        public int[] ids;
        public int[] sources;
        public List<List<NFP>> children;
    }
}
