using DeepNestLib.Rotation;
using DeepNestLib.Svg;
using System;
using System.Collections.Generic;
using System.Linq;

namespace DeepNestLib
{
    public class NFP : IStringify, ICloneable
    {
        public RotationConstraint RotationConstraint { get; set; } = RotationConstraint.Fixed;
        public bool fitted { get { return sheet != null; } }
        public NFP sheet;
        public override string ToString()
        {
            string str1 = (Points != null) ? Points.Count() + "" : "null";
            return $"nfp: id: {id}; source: {source}; rotation: {rotation}; points: {str1}";
        }
        public NFP()
        {
            Points = new SvgPoint[] { };
        }

        public string Name { get; set; }
        public void AddPoint(SvgPoint point)
        {
            List<SvgPoint> list = Points.ToList();
            list.Add(point);
            Points = list.ToArray();
        }

        #region gdi section
        public bool isBin;

        #endregion
        public void reverse()
        {
            Points = Points.Reverse().ToArray();
        }

        public double x { get; set; }
        public double y { get; set; }

        public double WidthCalculated
        {
            get
            {
                double maxx = Points.Max(z => z.x);
                double minx = Points.Min(z => z.x);

                return maxx - minx;
            }
        }

        public double HeightCalculated
        {
            get
            {
                double maxy = Points.Max(z => z.y);
                double miny = Points.Min(z => z.y);
                return maxy - miny;
            }
        }

        public SvgPoint this[int ind]
        {
            get
            {
                return Points[ind];
            }
        }

        public List<NFP> children;




        public int Length
        {
            get
            {
                return Points.Length;
            }
        }

        //public float? width;
        //public float? height;
        public int length
        {
            get
            {
                return Points.Length;
            }
        }

        public int Id;
        public int id
        {
            get
            {
                return Id;
            }
            set
            {
                Id = value;
            }
        }

        public double? offsetx;
        public double? offsety;
        public int? source = null;
        public float Rotation;


        public float rotation
        {
            get
            {
                return Rotation;
            }
            set
            {
                Rotation = value;
            }
        }
        public SvgPoint[] Points;
        public float Area
        {
            get
            {
                float ret = 0;
                if (Points.Length < 3)
                {
                    return 0;
                }

                List<SvgPoint> pp = new List<SvgPoint>();
                pp.AddRange(Points);
                pp.Add(Points[0]);
                for (int i = 1; i < pp.Count; i++)
                {
                    SvgPoint s0 = pp[i - 1];
                    SvgPoint s1 = pp[i];
                    ret += (float)(s0.x * s1.y - s0.y * s1.x);
                }
                return (float)Math.Abs(ret / 2);
            }
        }

        internal void push(SvgPoint svgPoint)
        {
            List<SvgPoint> points = new List<SvgPoint>();
            if (Points == null)
            {
                Points = new SvgPoint[] { };
            }
            points.AddRange(Points);
            points.Add(svgPoint);
            Points = points.ToArray();

        }

        public NFP slice(int v)
        {
            NFP ret = new NFP();
            List<SvgPoint> pp = new List<SvgPoint>();
            for (int i = v; i < length; i++)
            {
                pp.Add(new SvgPoint(this[i].x, this[i].y));

            }
            ret.Points = pp.ToArray();
            return ret;
        }

        public string stringify()
        {
            throw new NotImplementedException();
        }
        // AI
        public NFP Clone()
        {
            NFP clone = new NFP
            {
                id = this.id,
                source = this.source,
                Name = this.Name,
                RotationConstraint = this.RotationConstraint,
                rotation = this.rotation,

                x = this.x,
                y = this.y,
                offsetx = this.offsetx,
                offsety = this.offsety,

                Points = this.Points?.Select(p => new SvgPoint(p.x, p.y) { exact = p.exact }).ToArray()
            };

            if (this.children != null && this.children.Count > 0)
            {
                clone.children = new List<NFP>(this.children.Count);
                foreach (NFP child in this.children)
                {
                    clone.children.Add(child.Clone());
                }
            }

            clone.sheet = this.sheet;

            clone.isBin = this.isBin;

            return clone;
        }

        object ICloneable.Clone()
        {
            return Clone();
        }
    }
}
