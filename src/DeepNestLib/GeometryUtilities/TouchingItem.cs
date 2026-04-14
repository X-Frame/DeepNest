namespace DeepNestLib
{
    public partial class GeometryUtil
    {
        public class TouchingItem
        {
            public TouchingItem(int _type, int _a, int _b)
            {
                A = _a;
                B = _b;
                type = _type;
            }
            public int A;
            public int B;
            public int type;

        }



    }
}
