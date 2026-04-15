using DeepNestLib.Rotation;
using DeepNestLib.Svg;
using System;
using System.Collections.Generic;
using System.Linq;

namespace DeepNestLib
{
    public class NFP : IStringify
    {
        public string Name { get; set; }
        public SvgPoint this[int index] => Points[index];
        public List<NFP> children = null;

        public double? offsetx;
        public double? offsety;
        public int Length => Points?.Length ?? 0;
        public int? Source { get; set; }
        public int Id { get; set; }
        public float Rotation { get; set; } = 0f;
        public RotationConstraint RotationConstraint { get; set; } = RotationConstraint.Fixed;
        public SvgPoint[] Points;
        public bool fitted { get { return sheet != null; } }
        public NFP sheet;
        public List<float> AllowedAngles { get; set; }
        public override string ToString()
        {
            string str1 = (Points != null) ? Points.Count() + "" : "null";
            return $"nfp: id: {Id}; source: {Source}; rotation: {Rotation}; points: {str1}";
        }
        public NFP()
        {
            Points = [];
        }

        public void AddPoint(SvgPoint point)
        {
            List<SvgPoint> list = Points.ToList();
            list.Add(point);
            Points = list.ToArray();
        }

        #region gdi section
        public bool isBin;

        #endregion
        public void Reverse()
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

        internal void Push(SvgPoint svgPoint)
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

        public NFP Slice(int v)
        {
            NFP ret = new NFP();
            List<SvgPoint> pp = new List<SvgPoint>();
            for (int i = v; i < Length; i++)
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
        /// <summary>
        /// Creates a deep clone of the NFP (including Points array, children, AllowedAngles, etc.)
        /// Uses DeepNestLib.Rotation utilities where appropriate for rotation-related fields.
        /// </summary>
        public NFP Clone(bool applyCurrentRotation = false)
        {
            NFP clone = new NFP
            {
                Name = this.Name,
                offsetx = this.offsetx,
                offsety = this.offsety,
                Source = this.Source,
                Id = this.Id,
                Rotation = this.Rotation,
                RotationConstraint = this.RotationConstraint,
                x = this.x,
                y = this.y,
                isBin = this.isBin,
                sheet = this.sheet,
                AllowedAngles = this.AllowedAngles?.ToList()
            };
            if (applyCurrentRotation && Math.Abs(this.Rotation) > 0.001f)
            {
                clone.Rotate(this.Rotation);
                clone.Rotation = this.Rotation;
            }

            // Deep copy Points array
            if (this.Points != null)
            {
                clone.Points = new SvgPoint[this.Points.Length];
                for (int i = 0; i < this.Points.Length; i++)
                {
                    clone.Points[i] = new SvgPoint(this.Points[i].x, this.Points[i].y);
                }
            }

            // Deep clone children recursively
            if (this.children != null)
            {
                clone.children = new List<NFP>(this.children.Count);
                foreach (NFP child in this.children)
                {
                    clone.children.Add(child?.Clone());
                }
            }

            return clone;
        }
        public NFP Rotate(float degrees)
        {
            return NestingService.RotatePolygon(this, degrees);
        }

        public void ApplyRotation(float degrees)
        {
            if (Math.Abs(degrees) < 0.001f)
            {
                return;
            }

            NFP rotated = NestingService.RotatePolygon(this, degrees);

            this.Points = rotated.Points;
            this.Rotation = (this.Rotation + degrees) % 360f;   // accumulate rotation

            if (rotated.children != null)
            {
                this.children = rotated.children;
            }
        }
    }
}
