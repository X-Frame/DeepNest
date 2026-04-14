using ClipperLib;
using System;
using System.Collections.Generic;

namespace DeepNestLib.Svg
{
    public static class SvgExtensions
    {
        public static double DistTo(this SvgPoint p, SvgPoint p2)
        {
            return Math.Sqrt(Math.Pow(p.x - p2.x, 2) + Math.Pow(p.y - p2.y, 2));
        }

        public static T[] Splice<T>(this T[] p, int a, int b)
        {
            List<T> ret = new List<T>();
            for (int i = 0; i < p.Length; i++)
            {
                if (i >= a && i < a + b)
                {
                    continue;
                }

                ret.Add(p[i]);
            }
            return ret.ToArray();
        }

        public static List<List<IntPoint>> splice(this List<List<IntPoint>> p, int a, int b)
        {
            List<List<IntPoint>> ret = new List<List<IntPoint>>();
            for (int i = a; i < a + b; i++)
            {
                if (i >= a && i < a + b)
                {
                    continue;
                }

                ret.Add(p[i]);
            }
            return ret;
        }

        public static NFP[] splice(this NFP[] p, int a, int b)
        {
            List<NFP> ret = new List<NFP>();
            for (int i = 0; i < p.Length; i++)
            {
                if (i >= a && i < a + b)
                {
                    continue;
                }

                ret.Add(p[i]);
            }

            return ret.ToArray();
        }
    }
}
