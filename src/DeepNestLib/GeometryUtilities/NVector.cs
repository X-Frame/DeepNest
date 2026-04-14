using DeepNestLib.Svg;

namespace DeepNestLib
{
    public partial class GeometryUtil
    {
        public class NVector
        {
            public SvgPoint start;
            public SvgPoint end;
            public double x;
            public double y;


            public NVector(double v1, double v2, SvgPoint _start, SvgPoint _end)
            {
                this.x = v1;
                this.y = v2;
                this.start = _start;
                this.end = _end;
            }
        }



    }
}
