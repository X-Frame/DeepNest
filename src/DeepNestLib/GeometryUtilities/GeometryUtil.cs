using DeepNestLib.GeometryUtilities;
using DeepNestLib.Svg;
using System;
using System.Collections.Generic;
using System.Linq;

namespace DeepNestLib
{
    public partial class GeometryUtil
    {
        // returns true if points are within the given distance
        public static bool WithinDistance(SvgPoint p1, SvgPoint p2, double distance)
        {
            double dx = p1.x - p2.x;
            double dy = p1.y - p2.y;
            return ((dx * dx + dy * dy) < distance * distance);
        }

        // returns an interior NFP for the special case where A is a rectangle
        public static NFP[] NoFitPolygonRectangle(NFP A, NFP B)
        {
            double minAx = A[0].x;
            double minAy = A[0].y;
            double maxAx = A[0].x;
            double maxAy = A[0].y;

            for (int i = 1; i < A.Length; i++)
            {
                if (A[i].x < minAx)
                {
                    minAx = A[i].x;
                }
                if (A[i].y < minAy)
                {
                    minAy = A[i].y;
                }
                if (A[i].x > maxAx)
                {
                    maxAx = A[i].x;
                }
                if (A[i].y > maxAy)
                {
                    maxAy = A[i].y;
                }
            }

            double minBx = B[0].x;
            double minBy = B[0].y;
            double maxBx = B[0].x;
            double maxBy = B[0].y;
            for (int i = 1; i < B.Length; i++)
            {
                if (B[i].x < minBx)
                {
                    minBx = B[i].x;
                }
                if (B[i].y < minBy)
                {
                    minBy = B[i].y;
                }
                if (B[i].x > maxBx)
                {
                    maxBx = B[i].x;
                }
                if (B[i].y > maxBy)
                {
                    maxBy = B[i].y;
                }
            }

            if (maxBx - minBx > maxAx - minAx)
            {
                return null;
            }
            if (maxBy - minBy > maxAy - minAy)
            {
                return null;
            }


            NFP[] pnts = new NFP[] { new NFP() { Points=new SvgPoint[]{

                new SvgPoint(minAx - minBx + B[0].x, minAy - minBy + B[0].y),
            new SvgPoint(maxAx - maxBx + B[0].x, minAy - minBy + B[0].y),
            new SvgPoint( maxAx - maxBx + B[0].x, maxAy - maxBy + B[0].y),
            new SvgPoint( minAx - minBx + B[0].x, maxAy - maxBy + B[0].y)
            } } };
            return pnts;
        }


        // returns the rectangular bounding box of the given polygon
        public static PolygonBounds GetPolygonBounds(NFP _polygon)
        {
            return GetPolygonBounds(_polygon.Points);
        }
        public static PolygonBounds GetPolygonBounds(List<SvgPoint
            > polygon)
        {
            return GetPolygonBounds(polygon.ToArray());
        }
        public static PolygonBounds GetPolygonBounds(SvgPoint[] polygon)
        {
            if (polygon == null || polygon.Count() < 3)
            {
                throw new ArgumentException("null");
            }

            double xmin = polygon[0].x;
            double xmax = polygon[0].x;
            double ymin = polygon[0].y;
            double ymax = polygon[0].y;

            for (int i = 1; i < polygon.Length; i++)
            {
                if (polygon[i].x > xmax)
                {
                    xmax = polygon[i].x;
                }
                else if (polygon[i].x < xmin)
                {
                    xmin = polygon[i].x;
                }

                if (polygon[i].y > ymax)
                {
                    ymax = polygon[i].y;
                }
                else if (polygon[i].y < ymin)
                {
                    ymin = polygon[i].y;
                }
            }

            double w = xmax - xmin;
            double h = ymax - ymin;
            //return new rectanglef(xmin, ymin, xmax - xmin, ymax - ymin);
            return new PolygonBounds(xmin, ymin, w, h);
        }

        public static bool IsRectangle(NFP poly, double? tolerance = null)
        {
            PolygonBounds bb = GetPolygonBounds(poly);
            if (tolerance == null)
            {
                tolerance = TOL;
            }


            for (int i = 0; i < poly.Points.Length; i++)
            {
                if (!AlmostEqual(poly.Points[i].x, bb.x) && !AlmostEqual(poly.Points[i].x, bb.x + bb.width))
                {
                    return false;
                }
                if (!AlmostEqual(poly.Points[i].y, bb.y) && !AlmostEqual(poly.Points[i].y, bb.y + bb.height))
                {
                    return false;
                }
            }

            return true;
        }

        public static PolygonWithBounds RotatePolygon(NFP polygon, float angle)
        {

            List<SvgPoint> rotated = new List<SvgPoint>();
            angle = (float)(angle * Math.PI / 180.0f);
            for (int i = 0; i < polygon.Points.Length; i++)
            {
                double x = polygon.Points[i].x;
                double y = polygon.Points[i].y;
                float x1 = (float)(x * Math.Cos(angle) - y * Math.Sin(angle));
                float y1 = (float)(x * Math.Sin(angle) + y * Math.Cos(angle));

                rotated.Add(new SvgPoint(x1, y1));
            }

            PolygonWithBounds ret = new PolygonWithBounds()
            {
                Points = rotated.ToArray()
            };
            PolygonBounds bounds = GeometryUtil.GetPolygonBounds(ret);
            ret.x = bounds.x;
            ret.y = bounds.y;
            ret.width = bounds.width;
            ret.height = bounds.height;
            return ret;

        }
        public static bool AlmostEqual(double a, double b, double? tolerance = null)
        {
            if (tolerance == null)
            {
                tolerance = TOL;
            }
            return Math.Abs(a - b) < tolerance;
        }
        public static bool AlmostEqual(double? a, double? b, double? tolerance = null)
        {
            return AlmostEqual(a.Value, b.Value, tolerance);
        }
        // returns true if point already exists in the given nfp
        public static bool inNfp(SvgPoint p, NFP[] nfp)
        {
            if (nfp == null || nfp.Length == 0)
            {
                return false;
            }

            for (int i = 0; i < nfp.Length; i++)
            {
                for (int j = 0; j < nfp[i].Length; j++)
                {
                    if (AlmostEqual(p.x, nfp[i][j].x) && AlmostEqual(p.y, nfp[i][j].y))
                    {
                        return true;
                    }
                }
            }

            return false;
        }
        // normalize vector into a unit vector
        public static SvgPoint NormalizeVector(SvgPoint v)
        {
            if (AlmostEqual(v.x * v.x + v.y * v.y, 1))
            {
                return v; // given vector was already a unit vector
            }
            double len = Math.Sqrt(v.x * v.x + v.y * v.y);
            float inverse = (float)(1 / len);

            return new SvgPoint(v.x * inverse, v.y * inverse
        );
        }
        public static double? PointDistance(SvgPoint p, SvgPoint s1, SvgPoint s2, SvgPoint normal, bool infinite = false)
        {
            normal = NormalizeVector(normal);

            SvgPoint dir = new SvgPoint(normal.y, -normal.x);

            double pdot = p.x * dir.x + p.y * dir.y;
            double s1dot = s1.x * dir.x + s1.y * dir.y;
            double s2dot = s2.x * dir.x + s2.y * dir.y;

            double pdotnorm = p.x * normal.x + p.y * normal.y;
            double s1dotnorm = s1.x * normal.x + s1.y * normal.y;
            double s2dotnorm = s2.x * normal.x + s2.y * normal.y;

            if (!infinite)
            {
                if (((pdot < s1dot || AlmostEqual(pdot, s1dot)) && (pdot < s2dot || AlmostEqual(pdot, s2dot))) || ((pdot > s1dot || AlmostEqual(pdot, s1dot)) && (pdot > s2dot || AlmostEqual(pdot, s2dot))))
                {
                    return null; // dot doesn't collide with segment, or lies directly on the vertex
                }
                if ((AlmostEqual(pdot, s1dot) && AlmostEqual(pdot, s2dot)) && (pdotnorm > s1dotnorm && pdotnorm > s2dotnorm))
                {
                    return Math.Min(pdotnorm - s1dotnorm, pdotnorm - s2dotnorm);
                }
                if ((AlmostEqual(pdot, s1dot) && AlmostEqual(pdot, s2dot)) && (pdotnorm < s1dotnorm && pdotnorm < s2dotnorm))
                {
                    return -Math.Min(s1dotnorm - pdotnorm, s2dotnorm - pdotnorm);
                }
            }

            return -(pdotnorm - s1dotnorm + (s1dotnorm - s2dotnorm) * (s1dot - pdot) / (s1dot - s2dot));
        }
        static double TOL = (float)Math.Pow(10, -9); // Floating point error is likely to be above 1 epsilon
                                                     // returns true if p lies on the line segment defined by AB, but not at any endpoints
                                                     // may need work!
        public static bool OnSegment(SvgPoint A, SvgPoint B, SvgPoint p)
        {

            // vertical line
            if (AlmostEqual(A.x, B.x) && AlmostEqual(p.x, A.x))
            {
                if (!AlmostEqual(p.y, B.y) && !AlmostEqual(p.y, A.y) && p.y < Math.Max(B.y, A.y) && p.y > Math.Min(B.y, A.y))
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }

            // horizontal line
            if (AlmostEqual(A.y, B.y) && AlmostEqual(p.y, A.y))
            {
                if (!AlmostEqual(p.x, B.x) && !AlmostEqual(p.x, A.x) && p.x < Math.Max(B.x, A.x) && p.x > Math.Min(B.x, A.x))
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }

            //range check
            if ((p.x < A.x && p.x < B.x) || (p.x > A.x && p.x > B.x) || (p.y < A.y && p.y < B.y) || (p.y > A.y && p.y > B.y))
            {
                return false;
            }


            // exclude end points
            if ((AlmostEqual(p.x, A.x) && AlmostEqual(p.y, A.y)) || (AlmostEqual(p.x, B.x) && AlmostEqual(p.y, B.y)))
            {
                return false;
            }

            double cross = (p.y - A.y) * (B.x - A.x) - (p.x - A.x) * (B.y - A.y);

            if (Math.Abs(cross) > TOL)
            {
                return false;
            }

            double dot = (p.x - A.x) * (B.x - A.x) + (p.y - A.y) * (B.y - A.y);



            if (dot < 0 || AlmostEqual(dot, 0))
            {
                return false;
            }

            double len2 = (B.x - A.x) * (B.x - A.x) + (B.y - A.y) * (B.y - A.y);



            if (dot > len2 || AlmostEqual(dot, len2))
            {
                return false;
            }

            return true;
        }


        // project each point of B onto A in the given direction, and return the 
        public static double? PolygonProjectionDistance(NFP A, NFP B, SvgPoint direction)
        {
            double Boffsetx = B.offsetx ?? 0;
            double Boffsety = B.offsety ?? 0;

            double Aoffsetx = A.offsetx ?? 0;
            double Aoffsety = A.offsety ?? 0;

            A = A.Slice(0);
            B = B.Slice(0);

            // close the loop for polygons
            if (A[0] != A[A.Length - 1])
            {
                A.Push(A[0]);
            }

            if (B[0] != B[B.Length - 1])
            {
                B.Push(B[0]);
            }

            NFP edgeA = A;
            NFP edgeB = B;

            double? distance = null;
            SvgPoint p, s1, s2;
            double? d;


            for (int i = 0; i < edgeB.Length; i++)
            {
                // the shortest/most negative projection of B onto A
                double? minprojection = null;
                SvgPoint minp = null;
                for (int j = 0; j < edgeA.Length - 1; j++)
                {
                    p = new SvgPoint(edgeB[i].x + Boffsetx, edgeB[i].y + Boffsety);
                    s1 = new SvgPoint(edgeA[j].x + Aoffsetx, edgeA[j].y + Aoffsety);
                    s2 = new SvgPoint(edgeA[j + 1].x + Aoffsetx, edgeA[j + 1].y + Aoffsety);

                    if (Math.Abs((s2.y - s1.y) * direction.x - (s2.x - s1.x) * direction.y) < TOL)
                    {
                        continue;
                    }

                    // project point, ignore edge boundaries
                    d = PointDistance(p, s1, s2, direction);

                    if (d != null && (minprojection == null || d < minprojection))
                    {
                        minprojection = d;
                        minp = p;
                    }
                }
                if (minprojection != null && (distance == null || minprojection > distance))
                {
                    distance = minprojection;
                }
            }

            return distance;
        }

        public static double PolygonArea(NFP polygon)
        {
            double area = 0;
            int i, j;
            for (i = 0, j = polygon.Points.Length - 1; i < polygon.Points.Length; j = i++)
            {
                area += (polygon.Points[j].x + polygon.Points[i].x) * (polygon.Points[j].y
                    - polygon.Points[i].y);
            }
            return 0.5f * area;
        }

        // return true if point is in the polygon, false if outside, and null if exactly on a point or edge
        public static bool? PointInPolygon(SvgPoint point, NFP polygon)
        {
            if (polygon == null || polygon.Points.Length < 3)
            {
                throw new ArgumentException();
            }

            bool inside = false;
            //var offsetx = polygon.offsetx || 0;
            //var offsety = polygon.offsety || 0;
            double offsetx = polygon.offsetx == null ? 0 : polygon.offsetx.Value;
            double offsety = polygon.offsety == null ? 0 : polygon.offsety.Value;

            int i, j;
            for (i = 0, j = polygon.Points.Count() - 1; i < polygon.Points.Length; j = i++)
            {
                double xi = polygon.Points[i].x + offsetx;
                double yi = polygon.Points[i].y + offsety;
                double xj = polygon.Points[j].x + offsetx;
                double yj = polygon.Points[j].y + offsety;

                if (AlmostEqual(xi, point.x) && AlmostEqual(yi, point.y))
                {

                    return null; // no result
                }

                if (OnSegment(new SvgPoint(xi, yi), new SvgPoint(xj, yj), point))
                {
                    return null; // exactly on the segment
                }

                if (AlmostEqual(xi, xj) && AlmostEqual(yi, yj))
                { // ignore very small lines
                    continue;
                }

                bool intersect = ((yi > point.y) != (yj > point.y)) && (point.x < (xj - xi) * (point.y - yi) / (yj - yi) + xi);
                if (intersect)
                {
                    inside = !inside;
                }
            }

            return inside;
        }
        // todo: swap this for a more efficient sweep-line implementation
        // returnEdges: if set, return all edges on A that have intersections

        public static bool Intersect(NFP A, NFP B)
        {
            double Aoffsetx = A.offsetx ?? 0;
            double Aoffsety = A.offsety ?? 0;

            double Boffsetx = B.offsetx ?? 0;
            double Boffsety = B.offsety ?? 0;

            A = A.Slice(0);
            B = B.Slice(0);

            for (int i = 0; i < A.Length - 1; i++)
            {
                for (int j = 0; j < B.Length - 1; j++)
                {
                    SvgPoint a1 = new SvgPoint(A[i].x + Aoffsetx, A[i].y + Aoffsety);
                    SvgPoint a2 = new SvgPoint(A[i + 1].x + Aoffsetx, A[i + 1].y + Aoffsety);
                    SvgPoint b1 = new SvgPoint(B[j].x + Boffsetx, B[j].y + Boffsety);
                    SvgPoint b2 = new SvgPoint(B[j + 1].x + Boffsetx, B[j + 1].y + Boffsety);

                    int prevbindex = (j == 0) ? B.Length - 1 : j - 1;
                    int prevaindex = (i == 0) ? A.Length - 1 : i - 1;
                    int nextbindex = (j + 1 == B.Length - 1) ? 0 : j + 2;
                    int nextaindex = (i + 1 == A.Length - 1) ? 0 : i + 2;

                    // go even further back if we happen to hit on a loop end point
                    if (B[prevbindex] == B[j] || (AlmostEqual(B[prevbindex].x, B[j].x) && AlmostEqual(B[prevbindex].y, B[j].y)))
                    {
                        prevbindex = (prevbindex == 0) ? B.Length - 1 : prevbindex - 1;
                    }

                    if (A[prevaindex] == A[i] || (AlmostEqual(A[prevaindex].x, A[i].x) && AlmostEqual(A[prevaindex].y, A[i].y)))
                    {
                        prevaindex = (prevaindex == 0) ? A.Length - 1 : prevaindex - 1;
                    }

                    // go even further forward if we happen to hit on a loop end point
                    if (B[nextbindex] == B[j + 1] || (AlmostEqual(B[nextbindex].x, B[j + 1].x) && AlmostEqual(B[nextbindex].y, B[j + 1].y)))
                    {
                        nextbindex = (nextbindex == B.Length - 1) ? 0 : nextbindex + 1;
                    }

                    if (A[nextaindex] == A[i + 1] || (AlmostEqual(A[nextaindex].x, A[i + 1].x) && AlmostEqual(A[nextaindex].y, A[i + 1].y)))
                    {
                        nextaindex = (nextaindex == A.Length - 1) ? 0 : nextaindex + 1;
                    }

                    SvgPoint a0 = new SvgPoint(A[prevaindex].x + Aoffsetx, A[prevaindex].y + Aoffsety);
                    SvgPoint b0 = new SvgPoint(B[prevbindex].x + Boffsetx, B[prevbindex].y + Boffsety);

                    SvgPoint a3 = new SvgPoint(A[nextaindex].x + Aoffsetx, A[nextaindex].y + Aoffsety);
                    SvgPoint b3 = new SvgPoint(B[nextbindex].x + Boffsetx, B[nextbindex].y + Boffsety);

                    if (OnSegment(a1, a2, b1) || (AlmostEqual(a1.x, b1.x) && AlmostEqual(a1.y, b1.y)))
                    {
                        // if a point is on a segment, it could intersect or it could not. Check via the neighboring points
                        bool? b0in = PointInPolygon(b0, A);
                        bool? b2in = PointInPolygon(b2, A);
                        if ((b0in == true && b2in == false) || (b0in == false && b2in == true))
                        {
                            return true;
                        }
                        else
                        {
                            continue;
                        }
                    }

                    if (OnSegment(a1, a2, b2) || (AlmostEqual(a2.x, b2.x) && AlmostEqual(a2.y, b2.y)))
                    {
                        // if a point is on a segment, it could intersect or it could not. Check via the neighboring points
                        bool? b1in = PointInPolygon(b1, A);
                        bool? b3in = PointInPolygon(b3, A);

                        if ((b1in == true && b3in == false) || (b1in == false && b3in == true))
                        {
                            return true;
                        }
                        else
                        {
                            continue;
                        }
                    }

                    if (OnSegment(b1, b2, a1) || (AlmostEqual(a1.x, b2.x) && AlmostEqual(a1.y, b2.y)))
                    {
                        // if a point is on a segment, it could intersect or it could not. Check via the neighboring points
                        bool? a0in = PointInPolygon(a0, B);
                        bool? a2in = PointInPolygon(a2, B);

                        if ((a0in == true && a2in == false) || (a0in == false && a2in == true))
                        {
                            return true;
                        }
                        else
                        {
                            continue;
                        }
                    }

                    if (OnSegment(b1, b2, a2) || (AlmostEqual(a2.x, b1.x) && AlmostEqual(a2.y, b1.y)))
                    {
                        // if a point is on a segment, it could intersect or it could not. Check via the neighboring points
                        bool? a1in = PointInPolygon(a1, B);
                        bool? a3in = PointInPolygon(a3, B);

                        if ((a1in == true && a3in == false) || (a1in == false && a3in == true))
                        {
                            return true;
                        }
                        else
                        {
                            continue;
                        }
                    }

                    SvgPoint p = LineIntersect(b1, b2, a1, a2);

                    if (p != null)
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        public static bool IsFinite(object obj)
        {
            return true;
        }
        // returns the intersection of AB and EF
        // or null if there are no intersections or other numerical error
        // if the infinite flag is set, AE and EF describe infinite lines without endpoints, they are finite line segments otherwise
        public static SvgPoint LineIntersect(SvgPoint A, SvgPoint B, SvgPoint E, SvgPoint F, bool infinite = false)
        {
            double a1, a2, b1, b2, c1, c2, x, y;

            a1 = B.y - A.y;
            b1 = A.x - B.x;
            c1 = B.x * A.y - A.x * B.y;
            a2 = F.y - E.y;
            b2 = E.x - F.x;
            c2 = F.x * E.y - E.x * F.y;

            double denom = a1 * b2 - a2 * b1;

            x = (b1 * c2 - b2 * c1) / denom;
            y = (a2 * c1 - a1 * c2) / denom;


            if (!IsFinite(x) || !IsFinite(y))
            {
                return null;
            }

            if (!infinite)
            {
                // coincident points do not count as intersecting
                if (Math.Abs(A.x - B.x) > TOL && ((A.x < B.x) ? x < A.x || x > B.x : x > A.x || x < B.x))
                {
                    return null;
                }

                if (Math.Abs(A.y - B.y) > TOL && ((A.y < B.y) ? y < A.y || y > B.y : y > A.y || y < B.y))
                {
                    return null;
                }

                if (Math.Abs(E.x - F.x) > TOL && ((E.x < F.x) ? x < E.x || x > F.x : x > E.x || x < F.x))
                {
                    return null;
                }

                if (Math.Abs(E.y - F.y) > TOL && ((E.y < F.y) ? y < E.y || y > F.y : y > E.y || y < F.y))
                {
                    return null;
                }
            }

            return new SvgPoint(x, y);
        }

        // searches for an arrangement of A and B such that they do not overlap
        // if an NFP is given, only search for startpoints that have not already been traversed in the given NFP
        public static SvgPoint SearchStartPoint(NFP A, NFP B, bool inside, NFP[] NFP = null)
        {
            // clone arrays
            A = A.Slice(0);
            B = B.Slice(0);

            // close the loop for polygons
            if (A[0] != A[A.Length - 1])
            {
                A.Push(A[0]);
            }

            if (B[0] != B[B.Length - 1])
            {
                B.Push(B[0]);
            }

            for (int i = 0; i < A.Length - 1; i++)
            {
                if (!A[i].marked)
                {
                    A[i].marked = true;
                    for (int j = 0; j < B.Length; j++)
                    {
                        B.offsetx = A[i].x - B[j].x;
                        B.offsety = A[i].y - B[j].y;

                        bool? Binside = null;
                        for (int k = 0; k < B.Length; k++)
                        {
                            bool? inpoly = PointInPolygon(new SvgPoint(B[k].x + B.offsetx.Value,
                                B[k].y + B.offsety.Value), A);
                            if (inpoly != null)
                            {
                                Binside = inpoly;
                                break;
                            }
                        }

                        if (Binside == null)
                        { // A and B are the same
                            return null;
                        }

                        SvgPoint startPoint = new SvgPoint(B.offsetx.Value, B.offsety.Value);
                        if (((Binside.Value && inside) || (!Binside.Value && !inside)) &&
                            !Intersect(A, B) && !inNfp(startPoint, NFP))
                        {
                            return startPoint;
                        }

                        // slide B along vector
                        double vx = A[i + 1].x - A[i].x;
                        double vy = A[i + 1].y - A[i].y;

                        double? d1 = PolygonProjectionDistance(A, B, new SvgPoint(vx, vy));
                        double? d2 = PolygonProjectionDistance(B, A, new SvgPoint(-vx, -vy));

                        double? d = null;

                        // todo: clean this up
                        if (d1 == null && d2 == null)
                        {
                            // nothin
                        }
                        else if (d1 == null)
                        {
                            d = d2;
                        }
                        else if (d2 == null)
                        {
                            d = d1;
                        }
                        else
                        {
                            d = Math.Min(d1.Value, d2.Value);
                        }

                        // only slide until no longer negative
                        if (d != null && !AlmostEqual(d, 0) && d > 0)
                        {

                        }
                        else
                        {
                            continue;
                        }

                        double vd2 = vx * vx + vy * vy;

                        if (d * d < vd2 && !AlmostEqual(d * d, vd2))
                        {
                            float vd = (float)Math.Sqrt(vx * vx + vy * vy);
                            vx *= d.Value / vd;
                            vy *= d.Value / vd;
                        }

                        B.offsetx += vx;
                        B.offsety += vy;

                        for (int k = 0; k < B.Length; k++)
                        {
                            bool? inpoly = PointInPolygon(
                                new SvgPoint(
                                 B[k].x + B.offsetx.Value, B[k].y + B.offsety.Value), A);
                            if (inpoly != null)
                            {
                                Binside = inpoly;
                                break;
                            }
                        }
                        startPoint = new SvgPoint(B.offsetx.Value, B.offsety.Value);
                        if (((Binside.Value && inside) || (!Binside.Value && !inside)) &&
                            !Intersect(A, B) && !inNfp(startPoint, NFP))
                        {
                            return startPoint;
                        }
                    }
                }
            }



            return null;
        }

        public static double? SegmentDistance(SvgPoint A, SvgPoint B, SvgPoint E, SvgPoint F, SvgPoint direction)
        {
            SvgPoint normal = new SvgPoint(
                direction.y,
                -direction.x

            );

            SvgPoint reverse = new SvgPoint(
                    -direction.x,
                     -direction.y
                );

            double dotA = A.x * normal.x + A.y * normal.y;
            double dotB = B.x * normal.x + B.y * normal.y;
            double dotE = E.x * normal.x + E.y * normal.y;
            double dotF = F.x * normal.x + F.y * normal.y;

            double crossA = A.x * direction.x + A.y * direction.y;
            double crossB = B.x * direction.x + B.y * direction.y;
            double crossE = E.x * direction.x + E.y * direction.y;
            double crossF = F.x * direction.x + F.y * direction.y;

            double crossABmin = Math.Min(crossA, crossB);
            double crossABmax = Math.Max(crossA, crossB);

            double crossEFmax = Math.Max(crossE, crossF);
            double crossEFmin = Math.Min(crossE, crossF);

            double ABmin = Math.Min(dotA, dotB);
            double ABmax = Math.Max(dotA, dotB);

            double EFmax = Math.Max(dotE, dotF);
            double EFmin = Math.Min(dotE, dotF);

            // segments that will merely touch at one point
            if (AlmostEqual(ABmax, EFmin, TOL) || AlmostEqual(ABmin, EFmax, TOL))
            {
                return null;
            }
            // segments miss eachother completely
            if (ABmax < EFmin || ABmin > EFmax)
            {
                return null;
            }

            double overlap;

            if ((ABmax > EFmax && ABmin < EFmin) || (EFmax > ABmax && EFmin < ABmin))
            {
                overlap = 1;
            }
            else
            {
                double minMax = Math.Min(ABmax, EFmax);
                double maxMin = Math.Max(ABmin, EFmin);

                double maxMax = Math.Max(ABmax, EFmax);
                double minMin = Math.Min(ABmin, EFmin);

                overlap = (minMax - maxMin) / (maxMax - minMin);
            }

            double crossABE = (E.y - A.y) * (B.x - A.x) - (E.x - A.x) * (B.y - A.y);
            double crossABF = (F.y - A.y) * (B.x - A.x) - (F.x - A.x) * (B.y - A.y);

            // lines are colinear
            if (AlmostEqual(crossABE, 0) && AlmostEqual(crossABF, 0))
            {

                SvgPoint ABnorm = new SvgPoint(B.y - A.y, A.x - B.x);
                SvgPoint EFnorm = new SvgPoint(F.y - E.y, E.x - F.x);

                float ABnormlength = (float)Math.Sqrt(ABnorm.x * ABnorm.x + ABnorm.y * ABnorm.y);
                ABnorm.x /= ABnormlength;
                ABnorm.y /= ABnormlength;

                float EFnormlength = (float)Math.Sqrt(EFnorm.x * EFnorm.x + EFnorm.y * EFnorm.y);
                EFnorm.x /= EFnormlength;
                EFnorm.y /= EFnormlength;

                // segment normals must point in opposite directions
                if (Math.Abs(ABnorm.y * EFnorm.x - ABnorm.x * EFnorm.y) < TOL && ABnorm.y * EFnorm.y + ABnorm.x * EFnorm.x < 0)
                {
                    // normal of AB segment must point in same direction as given direction vector
                    double normdot = ABnorm.y * direction.y + ABnorm.x * direction.x;
                    // the segments merely slide along eachother
                    if (AlmostEqual(normdot, 0, TOL))
                    {
                        return null;
                    }
                    if (normdot < 0)
                    {
                        return 0;
                    }
                }
                return null;
            }

            List<double> distances = new List<double>();

            // coincident points
            if (AlmostEqual(dotA, dotE))
            {
                distances.Add(crossA - crossE);
            }
            else if (AlmostEqual(dotA, dotF))
            {
                distances.Add(crossA - crossF);
            }
            else if (dotA > EFmin && dotA < EFmax)
            {
                double? d = PointDistance(A, E, F, reverse);
                if (d != null && AlmostEqual(d, 0))
                { //  A currently touches EF, but AB is moving away from EF
                    double? dB = PointDistance(B, E, F, reverse, true);
                    if (dB < 0 || AlmostEqual(dB * overlap, 0))
                    {
                        d = null;
                    }
                }
                if (d != null)
                {
                    distances.Add(d.Value);
                }
            }

            if (AlmostEqual(dotB, dotE))
            {
                distances.Add(crossB - crossE);
            }
            else if (AlmostEqual(dotB, dotF))
            {
                distances.Add(crossB - crossF);
            }
            else if (dotB > EFmin && dotB < EFmax)
            {
                double? d = PointDistance(B, E, F, reverse);

                if (d != null && AlmostEqual(d, 0))
                { // crossA>crossB A currently touches EF, but AB is moving away from EF
                    double? dA = PointDistance(A, E, F, reverse, true);
                    if (dA < 0 || AlmostEqual(dA * overlap, 0))
                    {
                        d = null;
                    }
                }
                if (d != null)
                {
                    distances.Add(d.Value);
                }
            }

            if (dotE > ABmin && dotE < ABmax)
            {
                double? d = PointDistance(E, A, B, direction);
                if (d != null && AlmostEqual(d, 0))
                { // crossF<crossE A currently touches EF, but AB is moving away from EF
                    double? dF = PointDistance(F, A, B, direction, true);
                    if (dF < 0 || AlmostEqual(dF * overlap, 0))
                    {
                        d = null;
                    }
                }
                if (d != null)
                {
                    distances.Add(d.Value);
                }
            }

            if (dotF > ABmin && dotF < ABmax)
            {
                double? d = PointDistance(F, A, B, direction);
                if (d != null && AlmostEqual(d, 0))
                { // && crossE<crossF A currently touches EF, but AB is moving away from EF
                    double? dE = PointDistance(E, A, B, direction, true);
                    if (dE < 0 || AlmostEqual(dE * overlap, 0))
                    {
                        d = null;
                    }
                }
                if (d != null)
                {
                    distances.Add(d.Value);
                }
            }

            if (distances.Count == 0)
            {
                return null;
            }

            //return Math.min.apply(Math, distances);
            return distances.Min();
        }
        public static double? PolygonSlideDistance(NFP A, NFP B, NVector direction, bool ignoreNegative)
        {

            SvgPoint A1, A2, B1, B2;
            double Aoffsetx, Aoffsety, Boffsetx, Boffsety;

            Aoffsetx = A.offsetx ?? 0;
            Aoffsety = A.offsety ?? 0;

            Boffsetx = B.offsetx ?? 0;
            Boffsety = B.offsety ?? 0;

            A = A.Slice(0);
            B = B.Slice(0);

            // close the loop for polygons
            if (A[0] != A[A.Length - 1])
            {
                A.Push(A[0]);
            }

            if (B[0] != B[B.Length - 1])
            {
                B.Push(B[0]);
            }

            NFP edgeA = A;
            NFP edgeB = B;

            double? distance = null;
            //var p, s1, s2;
            double? d;


            SvgPoint dir = NormalizeVector(new SvgPoint(direction.x, direction.y));

            SvgPoint normal = new SvgPoint(
                dir.y,
                 -dir.x
            );

            SvgPoint reverse = new SvgPoint(-dir.x, -dir.y);

            for (int i = 0; i < edgeB.Length - 1; i++)
            {
                //var mind = null;
                for (int j = 0; j < edgeA.Length - 1; j++)
                {
                    A1 = new SvgPoint(
                         edgeA[j].x + Aoffsetx, edgeA[j].y + Aoffsety
        );
                    A2 = new SvgPoint(
                        edgeA[j + 1].x + Aoffsetx, edgeA[j + 1].y + Aoffsety
                );
                    B1 = new SvgPoint(edgeB[i].x + Boffsetx, edgeB[i].y + Boffsety);
                    B2 = new SvgPoint(edgeB[i + 1].x + Boffsetx, edgeB[i + 1].y + Boffsety);

                    if ((AlmostEqual(A1.x, A2.x) && AlmostEqual(A1.y, A2.y)) || (AlmostEqual(B1.x, B2.x) && AlmostEqual(B1.y, B2.y)))
                    {
                        continue; // ignore extremely small lines
                    }

                    d = SegmentDistance(A1, A2, B1, B2, dir);

                    if (d != null && (distance == null || d < distance))
                    {
                        if (!ignoreNegative || d > 0 || AlmostEqual(d, 0))
                        {
                            distance = d;
                        }
                    }
                }
            }
            return distance;
        }

        // given a static polygon A and a movable polygon B, compute a no fit polygon by orbiting B about A
        // if the inside flag is set, B is orbited inside of A rather than outside
        // if the searchEdges flag is set, all edges of A are explored for NFPs - multiple 
        public static NFP[] NoFitPolygon(NFP A, NFP B, bool inside, bool searchEdges)
        {
            if (A == null || A.Length < 3 || B == null || B.Length < 3)
            {
                return null;
            }

            A.offsetx = 0;
            A.offsety = 0;

            int i = 0, j = 0;

            double minA = A[0].y;
            int minAindex = 0;

            double maxB = B[0].y;
            int maxBindex = 0;

            for (i = 1; i < A.Length; i++)
            {
                A[i].marked = false;
                if (A[i].y < minA)
                {
                    minA = A[i].y;
                    minAindex = i;
                }
            }

            for (i = 1; i < B.Length; i++)
            {
                B[i].marked = false;
                if (B[i].y > maxB)
                {
                    maxB = B[i].y;
                    maxBindex = i;
                }
            }
            SvgPoint startpoint;
            if (!inside)
            {
                // shift B such that the bottom-most point of B is at the top-most point of A. This guarantees an initial placement with no intersections
                startpoint = new SvgPoint(
                     A[minAindex].x - B[maxBindex].x,
                     A[minAindex].y - B[maxBindex].y);
            }
            else
            {
                // no reliable heuristic for inside
                startpoint = SearchStartPoint(A, B, true);
            }

            List<NFP> NFPlist = new List<NFP>();



            while (startpoint != null)
            {

                B.offsetx = startpoint.x;
                B.offsety = startpoint.y;

                // maintain a list of touching points/edges
                List<TouchingItem> touching = null;

                NVector prevvector = null; // keep track of previous vector
                NFP NFP = new NFP();
                NFP.Push(new SvgPoint(B[0].x + B.offsetx.Value, B[0].y + B.offsety.Value));

                double referencex = B[0].x + B.offsetx.Value;
                double referencey = B[0].y + B.offsety.Value;
                double startx = referencex;
                double starty = referencey;
                int counter = 0;

                while (counter < 10 * (A.Length + B.Length))
                { // sanity check, prevent infinite loop
                    touching = new List<GeometryUtil.TouchingItem>();
                    // find touching vertices/edges
                    for (i = 0; i < A.Length; i++)
                    {
                        int nexti = (i == A.Length - 1) ? 0 : i + 1;
                        for (j = 0; j < B.Length; j++)
                        {
                            int nextj = (j == B.Length - 1) ? 0 : j + 1;
                            if (AlmostEqual(A[i].x, B[j].x + B.offsetx) && AlmostEqual(A[i].y, B[j].y + B.offsety))
                            {
                                touching.Add(new TouchingItem(0, i, j));
                            }
                            else if (OnSegment(A[i], A[nexti],
                                new SvgPoint(B[j].x + B.offsetx.Value, B[j].y + B.offsety.Value)))
                            {
                                touching.Add(new TouchingItem(1, nexti, j));
                            }
                            else if (OnSegment(
                                new SvgPoint(
                                 B[j].x + B.offsetx.Value, B[j].y + B.offsety.Value),
                                new SvgPoint(
                                 B[nextj].x + B.offsetx.Value, B[nextj].y + B.offsety.Value), A[i]))
                            {
                                touching.Add(new TouchingItem(2, i, nextj));
                            }
                        }
                    }

                    // generate translation vectors from touching vertices/edges
                    List<NVector> vectors = new List<NVector>();
                    for (i = 0; i < touching.Count; i++)
                    {
                        SvgPoint vertexA = A[touching[i].A];
                        vertexA.marked = true;

                        // adjacent A vertices
                        int prevAindex = touching[i].A - 1;
                        int nextAindex = touching[i].A + 1;

                        prevAindex = (prevAindex < 0) ? A.Length - 1 : prevAindex; // loop
                        nextAindex = (nextAindex >= A.Length) ? 0 : nextAindex; // loop

                        SvgPoint prevA = A[prevAindex];
                        SvgPoint nextA = A[nextAindex];

                        // adjacent B vertices
                        SvgPoint vertexB = B[touching[i].B];

                        int prevBindex = touching[i].B - 1;
                        int nextBindex = touching[i].B + 1;

                        prevBindex = (prevBindex < 0) ? B.Length - 1 : prevBindex; // loop
                        nextBindex = (nextBindex >= B.Length) ? 0 : nextBindex; // loop

                        SvgPoint prevB = B[prevBindex];
                        SvgPoint nextB = B[nextBindex];

                        if (touching[i].type == 0)
                        {

                            NVector vA1 = new NVector(
                                 prevA.x - vertexA.x,
                                 prevA.y - vertexA.y,
                                 vertexA,
                                 prevA
                                    );

                            NVector vA2 = new NVector(
                                     nextA.x - vertexA.x,
                                     nextA.y - vertexA.y,
                                     vertexA,
                                     nextA
                                        );

                            // B vectors need to be inverted
                            NVector vB1 = new NVector(
                                         vertexB.x - prevB.x,
                                         vertexB.y - prevB.y,
                                         prevB,
                                         vertexB
                                    );

                            NVector vB2 = new NVector(
                                             vertexB.x - nextB.x,
                                            vertexB.y - nextB.y,
                                             nextB,
                                             vertexB
                                        );

                            vectors.Add(vA1);
                            vectors.Add(vA2);
                            vectors.Add(vB1);
                            vectors.Add(vB2);
                        }
                        else if (touching[i].type == 1)
                        {
                            vectors.Add(new NVector(
                                 vertexA.x - (vertexB.x + B.offsetx.Value),
                                 vertexA.y - (vertexB.y + B.offsety.Value),
                                 prevA,
                                 vertexA
        ));

                            vectors.Add(new NVector(
                                 prevA.x - (vertexB.x + B.offsetx.Value),
                                prevA.y - (vertexB.y + B.offsety.Value),
                                 vertexA,
                                 prevA
        ));
                        }
                        else if (touching[i].type == 2)
                        {
                            vectors.Add(new NVector(
                                 vertexA.x - (vertexB.x + B.offsetx.Value),
                                vertexA.y - (vertexB.y + B.offsety.Value),
                                 prevB,
                                 vertexB
                            ));

                            vectors.Add(new NVector(
                                 vertexA.x - (prevB.x + B.offsetx.Value),
                                vertexA.y - (prevB.y + B.offsety.Value),
                                 vertexB,
                                 prevB

                            ));
                        }
                    }

                    NVector translate = null;
                    double maxd = 0;

                    for (i = 0; i < vectors.Count; i++)
                    {
                        if (vectors[i].x == 0 && vectors[i].y == 0)
                        {
                            continue;
                        }

                        // if this vector points us back to where we came from, ignore it.
                        // ie cross product = 0, dot product < 0
                        if (prevvector != null &&
                            vectors[i].y * prevvector.y + vectors[i].x * prevvector.x < 0)
                        {

                            // compare magnitude with unit vectors
                            float vectorlength = (float)Math.Sqrt(vectors[i].x * vectors[i].x + vectors[i].y * vectors[i].y);
                            SvgPoint unitv = new SvgPoint(vectors[i].x / vectorlength, vectors[i].y / vectorlength);

                            float prevlength = (float)Math.Sqrt(prevvector.x * prevvector.x + prevvector.y * prevvector.y);
                            SvgPoint prevunit = new SvgPoint(prevvector.x / prevlength, prevvector.y / prevlength);

                            // we need to scale down to unit vectors to normalize vector length. Could also just do a tan here
                            if (Math.Abs(unitv.y * prevunit.x - unitv.x * prevunit.y) < 0.0001)
                            {
                                continue;
                            }
                        }

                        double? d = PolygonSlideDistance(A, B, vectors[i], true);
                        double vecd2 = vectors[i].x * vectors[i].x + vectors[i].y * vectors[i].y;

                        if (d == null || d * d > vecd2)
                        {
                            float vecd = (float)Math.Sqrt(vectors[i].x * vectors[i].x + vectors[i].y * vectors[i].y);
                            d = vecd;
                        }

                        if (d != null && d > maxd)
                        {
                            maxd = d.Value;
                            translate = vectors[i];
                        }
                    }


                    if (translate == null || AlmostEqual(maxd, 0))
                    {
                        // didn't close the loop, something went wrong here
                        NFP = null;
                        break;
                    }

                    translate.start.marked = true;
                    translate.end.marked = true;

                    prevvector = translate;

                    // trim
                    double vlength2 = translate.x * translate.x + translate.y * translate.y;
                    if (maxd * maxd < vlength2 && !AlmostEqual(maxd * maxd, vlength2))
                    {
                        float scale = (float)Math.Sqrt((maxd * maxd) / vlength2);
                        translate.x *= scale;
                        translate.y *= scale;
                    }

                    referencex += translate.x;
                    referencey += translate.y;

                    if (AlmostEqual(referencex, startx) && AlmostEqual(referencey, starty))
                    {
                        // we've made a full loop
                        break;
                    }

                    // if A and B start on a touching horizontal line, the end point may not be the start point
                    bool looped = false;
                    if (NFP.Length > 0)
                    {
                        for (i = 0; i < NFP.Length - 1; i++)
                        {
                            if (AlmostEqual(referencex, NFP[i].x) && AlmostEqual(referencey, NFP[i].y))
                            {
                                looped = true;
                            }
                        }
                    }

                    if (looped)
                    {
                        // we've made a full loop
                        break;
                    }

                    NFP.Push(new SvgPoint(
                         referencex, referencey
                    ));

                    B.offsetx += translate.x;
                    B.offsety += translate.y;

                    counter++;
                }

                if (NFP != null && NFP.Length > 0)
                {
                    NFPlist.Add(NFP);

                }

                if (!searchEdges)
                {
                    // only get outer NFP or first inner NFP
                    break;
                }
                startpoint = SearchStartPoint(A, B, inside, NFPlist.ToArray());
            }

            return NFPlist.ToArray();
        }
    }
}
