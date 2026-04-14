namespace DeepNestLib.Svg
{
    public class SvgPoint
    {
        public bool exact = true;
        public override string ToString()
        {
            return "x: " + x + "; y: " + y;
        }
        public int id;
        public SvgPoint(double _x, double _y)
        {
            x = _x;
            y = _y;
        }
        public bool marked;
        public double x;
        public double y;

    }
}
