using ClipperLib;
using DeepNestLib.Sheets;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace DeepNestLib.Svg
{
    public partial class SvgNest
    {
        public static SvgNestConfig Config = new SvgNestConfig();

        public NestingService background = new NestingService();

        public PolygonTreeItem[] tree;

        PopulationItem Individual = null;
        GeneticAlgorithm ga;

        public bool useHoles;
        public bool searchEdges;

        public List<SheetPlacement> nests = new List<SheetPlacement>();

        public SvgNest() { }
        public SvgNest(SvgNestConfig config)
        {
            Config = config;
        }

        public static SvgPoint GetTarget(SvgPoint o, NFP simple, double tol)
        {
            List<InrangeItem> inrange = new List<InrangeItem>();
            // find closest points within 2 offset deltas
            for (int j = 0; j < simple.length; j++)
            {
                SvgPoint s = simple[j];
                double d2 = (o.x - s.x) * (o.x - s.x) + (o.y - s.y) * (o.y - s.y);
                if (d2 < tol * tol)
                {
                    inrange.Add(new InrangeItem() { point = s, distance = d2 });
                }
            }

            SvgPoint target = null;
            if (inrange.Count > 0)
            {
                List<InrangeItem> filtered = inrange.Where((p) =>
                {
                    return p.point.exact;
                }).ToList();

                // use exact points when available, normal points when not
                inrange = filtered.Count > 0 ? filtered : inrange;


                inrange = inrange.OrderBy((b) =>
  {
      return b.distance;
  }).ToList();

                target = inrange[0].point;
            }
            else
            {
                double? mind = null;
                for (int j = 0; j < simple.length; j++)
                {
                    SvgPoint s = simple[j];
                    double d2 = (o.x - s.x) * (o.x - s.x) + (o.y - s.y) * (o.y - s.y);
                    if (mind == null || d2 < mind)
                    {
                        target = s;
                        mind = d2;
                    }
                }
            }

            return target;
        }

        public static NFP Clone(NFP p)
        {
            NFP newp = new NFP();
            for (int i = 0; i < p.length; i++)
            {
                newp.AddPoint(new SvgPoint(

                     p[i].x,
                     p[i].y

                ));
            }

            return newp;
        }

        public static bool PointInPolygon(SvgPoint point, NFP polygon)
        {
            // scaling is deliberately coarse to filter out points that lie *on* the polygon
            IntPoint[] p = SvgToClipper2(polygon, 1000);
            IntPoint pt = new IntPoint(1000 * point.x, 1000 * point.y);

            return Clipper.PointInPolygon(pt, p.ToList()) > 0;
        }

        // returns true if any complex vertices fall outside the simple polygon
        public static bool Exterior(NFP simple, NFP complex, bool inside)
        {
            // find all protruding vertices
            for (int i = 0; i < complex.length; i++)
            {
                SvgPoint v = complex[i];
                if (!inside && !PointInPolygon(v, simple) && Find(v, simple) == null)
                {
                    return true;
                }
                if (inside && PointInPolygon(v, simple) && Find(v, simple) != null)
                {
                    return true;
                }
            }
            return false;
        }

        public static NFP SimplifyFunction(NFP polygon, bool inside)
        {
            double tolerance = 4 * Config.CurveTolerance;

            // give special treatment to line segments above this length (squared)
            double fixedTolerance = 40 * Config.CurveTolerance * 40 * Config.CurveTolerance;
            int i, j;


            if (Config.Simplify)
            {
                NFP hull = NestingService.getHull(polygon);
                if (hull != null)
                {
                    return hull;
                }
                else
                {
                    return polygon;
                }
            }

            NFP cleaned = CleanPolygon2(polygon);
            if (cleaned != null && cleaned.length > 1)
            {
                polygon = cleaned;
            }
            else
            {
                return polygon;
            }
            // polygon to polyline
            NFP copy = polygon.slice(0);
            copy.push(copy[0]);
            // mark all segments greater than ~0.25 in to be kept
            // the PD simplification algo doesn't care about the accuracy of long lines, only the absolute distance of each point
            // we care a great deal
            for (i = 0; i < copy.length - 1; i++)
            {
                SvgPoint p1 = copy[i];
                SvgPoint p2 = copy[i + 1];
                double sqd = (p2.x - p1.x) * (p2.x - p1.x) + (p2.y - p1.y) * (p2.y - p1.y);
                if (sqd > fixedTolerance)
                {
                    p1.marked = true;
                    p2.marked = true;
                }
            }

            NFP simple = Simplify.simplify(copy, tolerance, true);
            // now a polygon again
            simple.Points = simple.Points.Take(simple.Points.Count() - 1).ToArray();

            // could be dirty again (self intersections and/or coincident points)
            simple = CleanPolygon2(simple);

            // simplification process reduced poly to a line or point
            if (simple == null)
            {
                simple = polygon;
            }



            NFP[] offsets = PolygonOffsetDeepNest(simple, inside ? -tolerance : tolerance);

            NFP offset = null;
            double offsetArea = 0;
            List<NFP> holes = new List<NFP>();
            for (i = 0; i < offsets.Length; i++)
            {
                double area = GeometryUtil.polygonArea(offsets[i]);
                if (offset == null || area < offsetArea)
                {
                    offset = offsets[i];
                    offsetArea = area;
                }
                if (area > 0)
                {
                    holes.Add(offsets[i]);
                }
            }

            // mark any points that are exact
            for (i = 0; i < simple.length; i++)
            {
                NFP seg = new NFP();
                seg.AddPoint(simple[i]);
                seg.AddPoint(simple[i + 1 == simple.length ? 0 : i + 1]);

                int? index1 = Find(seg[0], polygon);
                int? index2 = Find(seg[1], polygon);

                if (index1 + 1 == index2 || index2 + 1 == index1 || index1 == 0 && index2 == polygon.length - 1 || index2 == 0 && index1 == polygon.length - 1)
                {
                    seg[0].exact = true;
                    seg[1].exact = true;
                }
            }
            int numshells = 4;
            NFP[] shells = new NFP[numshells];

            for (j = 1; j < numshells; j++)
            {
                double delta = j * (tolerance / numshells);
                delta = inside ? -delta : delta;
                NFP[] shell = PolygonOffsetDeepNest(simple, delta);
                if (shell.Count() > 0)
                {
                    shells[j] = shell.First();
                }
            }

            if (offset == null)
            {
                return polygon;
            }
            // selective reversal of offset
            for (i = 0; i < offset.length; i++)
            {
                SvgPoint o = offset[i];
                SvgPoint target = GetTarget(o, simple, 2 * tolerance);

                // reverse point offset and try to find exterior points
                NFP test = Clone(offset);
                test.Points[i] = new SvgPoint(target.x, target.y);

                if (!Exterior(test, polygon, inside))
                {
                    o.x = target.x;
                    o.y = target.y;
                }
                else
                {
                    // a shell is an intermediate offset between simple and offset
                    for (j = 1; j < numshells; j++)
                    {
                        if (shells[j] != null)
                        {
                            NFP shell = shells[j];
                            double delta = j * (tolerance / numshells);
                            target = GetTarget(o, shell, 2 * delta);
                            test = Clone(offset);
                            test.Points[i] = new SvgPoint(target.x, target.y);
                            if (!Exterior(test, polygon, inside))
                            {
                                o.x = target.x;
                                o.y = target.y;
                                break;
                            }
                        }
                    }
                }
            }

            for (i = 0; i < offset.length; i++)
            {
                SvgPoint p1 = offset[i];
                SvgPoint p2 = offset[i + 1 == offset.length ? 0 : i + 1];

                double sqd = (p2.x - p1.x) * (p2.x - p1.x) + (p2.y - p1.y) * (p2.y - p1.y);

                if (sqd < fixedTolerance)
                {
                    continue;
                }
                for (j = 0; j < simple.length; j++)
                {
                    SvgPoint s1 = simple[j];
                    SvgPoint s2 = simple[j + 1 == simple.length ? 0 : j + 1];

                    double sqds = (p2.x - p1.x) * (p2.x - p1.x) + (p2.y - p1.y) * (p2.y - p1.y);

                    if (sqds < fixedTolerance)
                    {
                        continue;
                    }

                    if ((GeometryUtil._almostEqual(s1.x, s2.x) || GeometryUtil._almostEqual(s1.y, s2.y)) && // we only really care about vertical and horizontal lines
                    GeometryUtil._withinDistance(p1, s1, 2 * tolerance) &&
                    GeometryUtil._withinDistance(p2, s2, 2 * tolerance) &&
                    (!GeometryUtil._withinDistance(p1, s1, Config.CurveTolerance / 1000) ||
                    !GeometryUtil._withinDistance(p2, s2, Config.CurveTolerance / 1000)))
                    {
                        p1.x = s1.x;
                        p1.y = s1.y;
                        p2.x = s2.x;
                        p2.y = s2.y;
                    }
                }
            }

            IntPoint[] Ac = ClipperHelper.ScaleUpPaths(offset, 10000000);
            IntPoint[] Bc = ClipperHelper.ScaleUpPaths(polygon, 10000000);

            List<List<IntPoint>> combined = new List<List<IntPoint>>();
            Clipper clipper = new Clipper();

            clipper.AddPath(Ac.ToList(), PolyType.ptSubject, true);
            clipper.AddPath(Bc.ToList(), PolyType.ptSubject, true);

            // the line straightening may have made the offset smaller than the simplified
            if (clipper.Execute(ClipType.ctUnion, combined, PolyFillType.pftNonZero, PolyFillType.pftNonZero))
            {
                double? largestArea = null;
                for (i = 0; i < combined.Count; i++)
                {
                    NFP n = NestingService.ToNestCoordinates(combined[i].ToArray(), 10000000);
                    double sarea = -GeometryUtil.polygonArea(n);
                    if (largestArea == null || largestArea < sarea)
                    {
                        offset = n;
                        largestArea = sarea;
                    }
                }
            }

            cleaned = CleanPolygon2(offset);
            if (cleaned != null && cleaned.length > 1)
            {
                offset = cleaned;
            }

            // mark any points that are exact (for line merge detection)
            for (i = 0; i < offset.length; i++)
            {
                SvgPoint[] seg = new SvgPoint[] { offset[i], offset[i + 1 == offset.length ? 0 : i + 1] };
                int? index1 = Find(seg[0], polygon);
                int? index2 = Find(seg[1], polygon);
                if (index1 == null)
                {
                    index1 = 0;
                }
                if (index2 == null)
                {
                    index2 = 0;
                }
                if (index1 + 1 == index2 || index2 + 1 == index1
                    || index1 == 0 && index2 == polygon.length - 1 ||
                    index2 == 0 && index1 == polygon.length - 1)
                {
                    seg[0].exact = true;
                    seg[1].exact = true;
                }
            }

            if (!inside && holes != null && holes.Count > 0)
            {
                offset.children = holes;
            }

            return offset;

        }
        public static int? Find(SvgPoint v, NFP p)
        {
            for (int i = 0; i < p.length; i++)
            {
                if (GeometryUtil._withinDistance(v, p[i], Config.CurveTolerance / 1000))
                {
                    return i;
                }
            }
            return null;
        }

        public static void OffsetTree(NFP t, double offset, SvgNestConfig config, CancellationToken cancellationToken, bool? inside = null)
        {
            cancellationToken.ThrowIfCancellationRequested();

            NFP simple = t;

            simple = SimplifyFunction(t, inside == null ? false : inside.Value);

            NFP[] offsetpaths = new NFP[] { simple };
            if (offset > 0)
            {
                offsetpaths = PolygonOffsetDeepNest(simple, offset);
            }

            if (offsetpaths.Count() > 0)
            {

                List<SvgPoint> rett = new List<SvgPoint>();
                rett.AddRange(offsetpaths[0].Points);
                rett.AddRange(t.Points.Skip(t.length));
                t.Points = rett.ToArray();
            }

            if (simple.children != null && simple.children.Count > 0)
            {
                if (t.children == null)
                {
                    t.children = new List<NFP>();
                }

                for (int i = 0; i < simple.children.Count; i++)
                {
                    t.children.Add(simple.children[i]);
                }
            }

            if (t.children != null && t.children.Count > 0)
            {
                for (int i = 0; i < t.children.Count; i++)
                {

                    OffsetTree(t.children[i], -offset, config, cancellationToken, inside == null ? true : !inside);
                }
            }
        }


        // use the clipper library to return an offset to the given polygon. Positive offset expands the polygon, negative contracts
        // note that this returns an array of polygons
        public static NFP[] PolygonOffsetDeepNest(NFP polygon, double offset)
        {

            if (offset == 0 || GeometryUtil._almostEqual(offset, 0))
            {
                return new[] { polygon };
            }

            List<IntPoint> p = SvgToClipper(polygon).ToList();

            int miterLimit = 4;
            ClipperOffset co = new ClipperOffset(miterLimit, Config.CurveTolerance * Config.ClipperScale);
            co.AddPath(p.ToList(), JoinType.jtMiter, EndType.etClosedPolygon);

            List<List<IntPoint>> newpaths = new List<List<IntPoint>>();
            co.Execute(ref newpaths, offset * Config.ClipperScale);


            List<NFP> result = new List<NFP>();
            for (int i = 0; i < newpaths.Count; i++)
            {
                result.Add(ClipperToSvg(newpaths[i]));
            }


            return result.ToArray();
        }

        // converts a polygon from normal float coordinates to integer coordinates used by clipper, as well as x/y -> X/Y
        public static IntPoint[] SvgToClipper2(NFP polygon, double? scale = null)
        {


            IntPoint[] d = ClipperHelper.ScaleUpPaths(polygon, scale == null ? Config.ClipperScale : scale.Value);
            return d.ToArray();

        }

        // converts a polygon from normal float coordinates to integer coordinates used by clipper, as well as x/y -> X/Y
        public static IntPoint[] SvgToClipper(NFP polygon)
        {



            IntPoint[] d = ClipperHelper.ScaleUpPaths(polygon, Config.ClipperScale);
            return d.ToArray();
        }

        // returns a less complex polygon that satisfies the curve tolerance
        public static NFP CleanPolygon(NFP polygon)
        {
            IntPoint[] p = SvgToClipper2(polygon);
            // remove self-intersections and find the biggest polygon that's left
            List<List<IntPoint>> simple = Clipper.SimplifyPolygon(p.ToList(), PolyFillType.pftNonZero);

            if (simple == null || simple.Count == 0)
            {
                return null;
            }

            List<IntPoint> biggest = simple[0];
            double biggestarea = Math.Abs(Clipper.Area(biggest));
            for (int i = 1; i < simple.Count; i++)
            {
                double area = Math.Abs(Clipper.Area(simple[i]));
                if (area > biggestarea)
                {
                    biggest = simple[i];
                    biggestarea = area;
                }
            }

            // clean up singularities, coincident points and edges
            List<IntPoint> clean = Clipper.CleanPolygon(biggest, 0.01 *
                Config.CurveTolerance * Config.ClipperScale);

            if (clean == null || clean.Count == 0)
            {
                return null;
            }
            return ClipperToSvg(clean);

        }

        public static NFP CleanPolygon2(NFP polygon)
        {
            IntPoint[] p = SvgToClipper(polygon);
            // remove self-intersections and find the biggest polygon that's left
            List<List<IntPoint>> simple = Clipper.SimplifyPolygon(p.ToList(), PolyFillType.pftNonZero);

            if (simple == null || simple.Count == 0)
            {
                return null;
            }

            List<IntPoint> biggest = simple[0];
            double biggestarea = Math.Abs(Clipper.Area(biggest));
            for (int i = 1; i < simple.Count; i++)
            {
                double area = Math.Abs(Clipper.Area(simple[i]));
                if (area > biggestarea)
                {
                    biggest = simple[i];
                    biggestarea = area;
                }
            }

            // clean up singularities, coincident points and edges
            List<IntPoint> clean = Clipper.CleanPolygon(biggest, 0.01 *
                Config.CurveTolerance * Config.ClipperScale);

            if (clean == null || clean.Count == 0)
            {
                return null;
            }
            NFP cleaned = ClipperToSvg(clean);

            // remove duplicate endpoints
            SvgPoint start = cleaned[0];
            SvgPoint end = cleaned[cleaned.length - 1];
            if (start == end || GeometryUtil._almostEqual(start.x, end.x)
                && GeometryUtil._almostEqual(start.y, end.y))
            {
                cleaned.Points = cleaned.Points.Take(cleaned.Points.Count() - 1).ToArray();
            }

            return cleaned;

        }
        public static NFP ClipperToSvg(IList<IntPoint> polygon)
        {
            List<SvgPoint> ret = new List<SvgPoint>();

            for (int i = 0; i < polygon.Count; i++)
            {
                ret.Add(new SvgPoint(polygon[i].X / Config.ClipperScale, polygon[i].Y / Config.ClipperScale));
            }

            return new NFP() { Points = ret.ToArray() };
        }


        public static int ToTree(PolygonTreeItem[] list, int idstart = 0)
        {
            List<PolygonTreeItem> parents = new List<PolygonTreeItem>();
            int i, j;

            // assign a unique id to each leaf
            int id = idstart;

            for (i = 0; i < list.Length; i++)
            {
                PolygonTreeItem p = list[i];

                bool ischild = false;
                for (j = 0; j < list.Length; j++)
                {
                    if (j == i)
                    {
                        continue;
                    }
                    if (GeometryUtil.pointInPolygon(p.Polygon.Points[0], list[j].Polygon).Value)
                    {
                        if (list[j].Childs == null)
                        {
                            list[j].Childs = new List<PolygonTreeItem>();
                        }
                        list[j].Childs.Add(p);
                        p.Parent = list[j];
                        ischild = true;
                        break;
                    }
                }

                if (!ischild)
                {
                    parents.Add(p);
                }
            }

            for (i = 0; i < list.Length; i++)
            {
                if (parents.IndexOf(list[i]) < 0)
                {
                    list = list.Skip(i).Take(1).ToArray();
                    i--;
                }
            }

            for (i = 0; i < parents.Count; i++)
            {
                parents[i].Polygon.Id = id;
                id++;
            }

            for (i = 0; i < parents.Count; i++)
            {
                if (parents[i].Childs != null)
                {
                    id = ToTree(parents[i].Childs.ToArray(), id);
                }
            }

            return id;
        }

        public static NFP CloneTree(NFP tree)
        {
            NFP newtree = new NFP();
            foreach (SvgPoint t in tree.Points)
            {
                newtree.AddPoint(new SvgPoint(t.x, t.y) { exact = t.exact });
            }


            if (tree.children != null && tree.children.Count > 0)
            {
                newtree.children = new List<NFP>();
                foreach (NFP c in tree.children)
                {
                    newtree.children.Add(CloneTree(c));
                }
            }
            return newtree;
        }


        public void ResponseProcessor(SheetPlacement payload)
        {
            if (ga == null)
            {
                return;
            }
            ga.Population[payload.index].processing = null;
            ga.Population[payload.index].fitness = payload.fitness;

            // render placement
            if (nests.Count == 0 || nests[0].fitness > payload.fitness)
            {
                nests.Insert(0, payload);

                if (nests.Count > Config.PopulationSize)
                {
                    nests.RemoveAt(nests.Count - 1);
                }
            }
        }

        public void LaunchWorkers(NestItem[] parts, CancellationToken cancellationToken)
        {

            background.ResponseAction = ResponseProcessor;
            if (ga == null)
            {
                List<NFP> adam = new List<NFP>();
                int id = 0;
                for (int i = 0; i < parts.Count(); i++)
                {
                    if (!parts[i].IsSheet)
                    {

                        for (int j = 0; j < parts[i].Quanity; j++)
                        {
                            NFP poly = CloneTree(parts[i].Polygon); // deep copy
                            poly.id = id; // id is the unique id of all parts that will be nested, including cloned duplicates
                            poly.source = i; // source is the id of each unique part from the main part list

                            adam.Add(poly);
                            id++;
                        }
                    }
                }

                adam = adam.OrderByDescending(z => Math.Abs(GeometryUtil.polygonArea(z))).ToList();
                ga = new GeneticAlgorithm(adam.ToArray(), Config, cancellationToken);
            }
            Individual = null;

            // check if current generation is finished
            bool finished = true;
            for (int i = 0; i < ga.Population.Count; i++)
            {
                if (ga.Population[i].fitness == null)
                {
                    finished = false;
                    break;
                }
            }
            if (finished)
            {
                // all individuals have been evaluated, start next generation
                ga.generation();
            }

            int running = ga.Population.Where((p) =>
            {
                return p.processing != null;
            }).Count();

            List<NFP> sheets = new List<NFP>();
            List<int> sheetids = new List<int>();
            List<int> sheetsources = new List<int>();
            List<List<NFP>> sheetchildren = new List<List<NFP>>();
            int sid = 0;
            for (int i = 0; i < parts.Count(); i++)
            {
                if (parts[i].IsSheet)
                {
                    NFP poly = parts[i].Polygon;
                    for (int j = 0; j < parts[i].Quanity; j++)
                    {
                        NFP cln = CloneTree(poly);
                        cln.id = sid; // id is the unique id of all parts that will be nested, including cloned duplicates
                        cln.source = poly.source; // source is the id of each unique part from the main part list

                        sheets.Add(cln);
                        sheetids.Add(sid);
                        sheetsources.Add(i);
                        sheetchildren.Add(poly.children);
                        sid++;
                    }
                }
            }
            for (int i = 0; i < ga.Population.Count; i++)
            {
                //if(running < config.threads && !GA.population[i].processing && !GA.population[i].fitness){
                // only one background window now...
                if (running < 1 && ga.Population[i].processing == null && ga.Population[i].fitness == null)
                {
                    ga.Population[i].processing = true;

                    // hash values on arrays don't make it across ipc, store them in an array and reassemble on the other side....
                    List<int> ids = new List<int>();
                    List<int> sources = new List<int>();
                    List<List<NFP>> children = new List<List<NFP>>();

                    for (int j = 0; j < ga.Population[i].placements.Count; j++)
                    {
                        int id = ga.Population[i].placements[j].id;
                        int? source = ga.Population[i].placements[j].source;
                        List<NFP> child = ga.Population[i].placements[j].children;
                        ids.Add(id);
                        sources.Add(source.Value);
                        children.Add(child);
                    }

                    DataInfo data = new DataInfo()
                    {
                        index = i,
                        sheets = sheets,
                        sheetids = sheetids.ToArray(),
                        sheetsources = sheetsources.ToArray(),
                        sheetchildren = sheetchildren,
                        individual = ga.Population[i],
                        config = Config,
                        ids = ids.ToArray(),
                        sources = sources.ToArray(),
                        children = children

                    };

                    background.BackgroundStart(data, cancellationToken);
                    running++;
                }
            }
        }

        public static IntPoint[] ToClipperCoordinates(NFP polygon)
        {
            List<IntPoint> clone = new List<IntPoint>();
            for (int i = 0; i < polygon.length; i++)
            {
                clone.Add
                    (new IntPoint(
                     polygon[i].x,
                             polygon[i].y

                        ));
            }

            return clone.ToArray();
        }
    }
}
