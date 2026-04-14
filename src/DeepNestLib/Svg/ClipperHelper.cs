using ClipperLib;
using System;
using System.Collections.Generic;
using System.Linq;

namespace DeepNestLib.Svg
{
    public class ClipperHelper
    {
        public static IntPoint[] ScaleUpPaths(NFP p, double scale = 1)
        {
            List<IntPoint> ret = new List<IntPoint>();

            for (int i = 0; i < p.Points.Count(); i++)
            {
                //p.Points[i] = new SvgNestPort.SvgPoint((float)Math.Round(p.Points[i].x * scale), (float)Math.Round(p.Points[i].y * scale));
                ret.Add(new IntPoint(
                    (long)Math.Round((decimal)p.Points[i].x * (decimal)scale),
                    (long)Math.Round((decimal)p.Points[i].y * (decimal)scale)
                ));

            }
            return ret.ToArray();
        }
    }
}
