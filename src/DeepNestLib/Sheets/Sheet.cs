using DeepNestLib.Svg;

namespace DeepNestLib.Sheets
{
    public class Sheet : NFP
    {
        public double Width;
        public double Height;
    }
    public class RectangleSheet : Sheet
    {
        public void Rebuild()
        {
            Points = [];
            AddPoint(new SvgPoint(x, y));
            AddPoint(new SvgPoint(x + Width, y));
            AddPoint(new SvgPoint(x + Width, y + Height));
            AddPoint(new SvgPoint(x, y + Height));
        }
    }
}
