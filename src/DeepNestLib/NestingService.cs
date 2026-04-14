using ClipperLib;
using DeepNestLib.Background;
using DeepNestLib.GeometryUtilities;
using DeepNestLib.NoFitPolygon;
using DeepNestLib.Rotation;
using DeepNestLib.Sheets;
using DeepNestLib.Svg;
using Minkowski;
using System;
using System.Buffers;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace DeepNestLib
{
    public class NestingService
    {

        public static bool EnableCaches = true;

        public static bool UseParallel { get; set; }

        public static NFP ShiftPolygon(NFP p, PlacementItem shift)
        {
            NFP shifted = new NFP();
            for (int i = 0; i < p.Length; i++)
            {
                shifted.AddPoint(new SvgPoint(p[i].x + shift.x, p[i].y + shift.y) { exact = p[i].exact });
            }
            if (p.children != null /*&& p.children.Count*/)
            {
                shifted.children = new List<NFP>();
                for (int i = 0; i < p.children.Count; i++)
                {
                    shifted.children.Add(ShiftPolygon(p.children[i], shift));
                }
            }

            return shifted;
        }


        // returns the square of the length of any merged lines
        // filter out any lines less than minlength long
        public static MergedResult MergedLength(NFP[] parts, NFP p, double minlength, double tolerance)
        {
            throw new NotImplementedException();
        }

        public class MergedResult
        {
            public double totalLength;
            public object segments;
        }


        public static NFP[] CloneNfp(NFP[] nfp, bool inner = false)
        {

            if (!inner)
            {
                return [Clone(nfp.First())];
            }
            throw new NotImplementedException();
        }
        public static NFP Clone(NFP nfp)
        {
            NFP newnfp = new();
            for (int i = 0; i < nfp.Length; i++)
            {
                newnfp.AddPoint(new SvgPoint(nfp[i].x, nfp[i].y));
            }

            if (nfp.children != null && nfp.children.Count > 0)
            {
                newnfp.children = new List<NFP>();
                for (int i = 0; i < nfp.children.Count; i++)
                {
                    NFP child = nfp.children[i];
                    NFP newchild = new NFP();
                    for (int j = 0; j < child.Length; j++)
                    {
                        newchild.AddPoint(new SvgPoint(child[j].x, child[j].y));
                    }
                    newnfp.children.Add(newchild);
                }
            }

            return newnfp;
        }


        public static int callCounter = 0;

        public static ConcurrentDictionary<string, NFP[]> cacheProcess = new ConcurrentDictionary<string, NFP[]>();
        public static NFP[] Process2(NFP A, NFP B, int type)
        {
            string key = A.Source + ";" + B.Source + ";" + A.Rotation + ";" + B.Rotation;
            bool cacheAllow = type != 1;
            if (cacheAllow && cacheProcess.TryGetValue(key, out NFP[]? cachedValue))
            {
                return cachedValue;
            }

            Stopwatch swg = Stopwatch.StartNew();
            Dictionary<string, List<PointF>> dic1 = new Dictionary<string, List<PointF>>();
            Dictionary<string, List<double>> dic2 = new Dictionary<string, List<double>>();
            dic2.Add("A", new List<double>());
            foreach (SvgPoint item in A.Points)
            {
                List<double> target = dic2["A"];
                target.Add(item.x);
                target.Add(item.y);
            }
            dic2.Add("B", new List<double>());
            foreach (SvgPoint item in B.Points)
            {
                List<double> target = dic2["B"];
                target.Add(item.x);
                target.Add(item.y);
            }

            List<double> hdat = new List<double>();
            foreach (NFP item in A.children)
            {
                foreach (SvgPoint pitem in item.Points)
                {
                    hdat.Add(pitem.x);
                    hdat.Add(pitem.y);
                }
            }

            List<double> aa = dic2["A"];
            List<double> bb = dic2["B"];
            int[] arr1 = A.children.Select(z => z.Points.Count() * 2).ToArray();

            MinkowskiWrapper.setData(aa.Count, aa.ToArray(), A.children.Count, arr1, hdat.ToArray(), bb.Count, bb.ToArray());
            MinkowskiWrapper.calculateNFP();

            callCounter++;

            int[] sizes = new int[2];
            MinkowskiWrapper.getSizes1(sizes);
            int[] sizes1 = new int[sizes[0]];
            int[] sizes2 = new int[sizes[1]];
            MinkowskiWrapper.getSizes2(sizes1, sizes2);

            // Rent arrays from the pool
            double[] dat1 = ArrayPool<double>.Shared.Rent(sizes1.Sum());
            double[] hdat1 = ArrayPool<double>.Shared.Rent(sizes2.Sum());

            NFP ret; // Declare here to be accessible for the return statement

            try
            {
                // Get the results into the rented arrays
                MinkowskiWrapper.getResults(dat1, hdat1);

                // All logic using the rented arrays must be inside this 'try' block.
                if (sizes1.Count() > 1)
                {
                    throw new ArgumentException("sizes1 cnt >1");
                }

                List<PointF> Apts = new List<PointF>();
                for (int i = 0; i < dat1.Length; i += 2)
                {
                    float x1 = (float)dat1[i];
                    float y1 = (float)dat1[i + 1];
                    Apts.Add(new PointF(x1, y1));
                }

                List<List<double>> holesval = new List<List<double>>();
                int index = 0;
                for (int i = 0; i < sizes2.Length; i++)
                {
                    holesval.Add(new List<double>());
                    for (int j = 0; j < sizes2[i]; j++)
                    {
                        holesval.Last().Add(hdat1[index]);
                        index++;
                    }
                }

                List<List<PointF>> holesout = new List<List<PointF>>();
                foreach (List<double> item in holesval)
                {
                    holesout.Add(new List<PointF>());
                    for (int i = 0; i < item.Count; i += 2)
                    {
                        float x = (float)item[i];
                        float y = (float)item[i + 1];
                        holesout.Last().Add(new PointF(x, y));
                    }
                }

                ret = new NFP();
                ret.Points = new SvgPoint[] { };
                foreach (PointF item in Apts)
                {
                    ret.AddPoint(new SvgPoint(item.X, item.Y));
                }

                foreach (List<PointF> item in holesout)
                {
                    ret.children = new List<NFP>();
                    ret.children.Add(new NFP());
                    ret.children.Last().Points = new SvgPoint[] { };
                    foreach (PointF hitem in item)
                    {
                        ret.children.Last().AddPoint(new SvgPoint(hitem.X, hitem.Y));
                    }
                }
            }
            finally
            {
                // This is CRITICAL: always return the arrays to the pool when finished.
                ArrayPool<double>.Shared.Return(dat1);
                ArrayPool<double>.Shared.Return(hdat1);
            }

            // Now 'ret' holds the processed data, and the temporary arrays have been safely returned.

            swg.Stop();
            long msg = swg.ElapsedMilliseconds;
            NFP[] res = new NFP[] { ret };

            if (cacheAllow)
            {
                cacheProcess.TryAdd(key, res);
            }
            return res;
        }

        public static NFP GetFrame(NFP A)
        {
            PolygonBounds bounds = GeometryUtil.GetPolygonBounds(A);

            // expand bounds by 10%
            bounds.width *= 1.1;
            bounds.height *= 1.1;
            bounds.x -= 0.5 * (bounds.width - (bounds.width / 1.1));
            bounds.y -= 0.5 * (bounds.height - (bounds.height / 1.1));

            NFP frame = new NFP();
            frame.Push(new SvgPoint(bounds.x, bounds.y));
            frame.Push(new SvgPoint(bounds.x + bounds.width, bounds.y));
            frame.Push(new SvgPoint(bounds.x + bounds.width, bounds.y + bounds.height));
            frame.Push(new SvgPoint(bounds.x, bounds.y + bounds.height));


            frame.children = new List<NFP>() { (NFP)A };
            frame.Source = A.Source;
            frame.Rotation = 0;

            return frame;
        }

        public static NFP[] GetInnerNfp(NFP A, NFP B, int type, SvgNestConfig config)
        {
            if (A.Source != null && B.Source != null)
            {

                DbCacheKey key = new DbCacheKey()
                {
                    A = A.Source.Value,
                    B = B.Source.Value,
                    ARotation = 0,
                    BRotation = B.Rotation,
                    //Inside =true??
                };
                //var doc = window.db.find({ A: A.source, B: B.source, Arotation: 0, Brotation: B.rotation }, true);
                NFP[] res = window.db.Find(key, true);
                if (res != null)
                {
                    return res;
                }
            }


            NFP frame = GetFrame(A);

            NFP nfp = GetOuterNfp(frame, B, type, true);

            if (nfp == null || nfp.children == null || nfp.children.Count == 0)
            {
                return null;
            }
            List<NFP> holes = new List<NFP>();
            if (A.children != null && A.children.Count > 0)
            {
                for (int i = 0; i < A.children.Count; i++)
                {
                    NFP hnfp = GetOuterNfp(A.children[i], B, 1);
                    if (hnfp != null)
                    {
                        holes.Add(hnfp);
                    }
                }
            }

            if (holes.Count == 0)
            {
                return nfp.children.ToArray();
            }
            IntPoint[][] clipperNfp = InnerNfpToClipperCoordinates(nfp.children.ToArray(), config);
            IntPoint[][] clipperHoles = InnerNfpToClipperCoordinates(holes.ToArray(), config);

            List<List<IntPoint>> finalNfp = new List<List<IntPoint>>();
            Clipper clipper = new ClipperLib.Clipper();

            clipper.AddPaths(clipperHoles.Select(z => z.ToList()).ToList(), ClipperLib.PolyType.ptClip, true);
            clipper.AddPaths(clipperNfp.Select(z => z.ToList()).ToList(), ClipperLib.PolyType.ptSubject, true);

            if (!clipper.Execute(ClipperLib.ClipType.ctDifference, finalNfp, ClipperLib.PolyFillType.pftNonZero, ClipperLib.PolyFillType.pftNonZero))
            {
                return nfp.children.ToArray();
            }

            if (finalNfp.Count == 0)
            {
                return null;
            }

            List<NFP> f = new List<NFP>();
            for (int i = 0; i < finalNfp.Count; i++)
            {
                f.Add(ToNestCoordinates(finalNfp[i].ToArray(), config.ClipperScale));
            }

            if (A.Source != null && B.Source != null)
            {
                // insert into db
                DbCacheKey doc = new DbCacheKey()
                {
                    A = A.Source.Value,
                    B = B.Source.Value,
                    ARotation = 0,
                    BRotation = B.Rotation,
                    nfp = f.ToArray()


                };
                window.db.Insert(doc, true);
            }

            return f.ToArray();

        }
        public static NFP RotatePolygon(NFP polygon, float degrees)
        {
            NFP rotated = new NFP();

            double angle = degrees * Math.PI / 180;
            List<SvgPoint> pp = new List<SvgPoint>(polygon.Length);
            for (int i = 0; i < polygon.Length; i++)
            {
                double x = polygon[i].x;
                double y = polygon[i].y;
                double x1 = (x * Math.Cos(angle) - y * Math.Sin(angle));
                double y1 = (x * Math.Sin(angle) + y * Math.Cos(angle));

                pp.Add(new SvgPoint(x1, y1));
            }
            rotated.Points = pp.ToArray();

            if (polygon.children != null && polygon.children.Count > 0)
            {
                rotated.children = new List<NFP>(); ;
                for (int j = 0; j < polygon.children.Count; j++)
                {
                    rotated.children.Add(RotatePolygon(polygon.children[j], degrees));
                }
            }

            return rotated;
        }

        public static SheetPlacement PlaceParts(NFP[] sheets, NFP[] parts, SvgNestConfig config, int nestindex, CancellationToken cancellationToken)
        {
            if (sheets == null || sheets.Count() == 0)
            {
                return null;
            }

            int i, j, k, m, n;
            double totalsheetarea = 0;

            // total length of merged lines
            double totalMerged = 0;

            // rotate paths by given rotation
            List<NFP> rotatedParts = [];
            for (i = 0; i < parts.Length; i++)
            {
                NFP originalPart = parts[i];
                NFP rotatedPart = RotatePolygon(originalPart, originalPart.Rotation);
                rotatedPart.Rotation = originalPart.Rotation;
                rotatedPart.Source = originalPart.Source;
                rotatedPart.Id = originalPart.Id;
                rotatedPart.RotationConstraint = originalPart.RotationConstraint;
                rotatedParts.Add(rotatedPart);
            }

            parts = rotatedParts.ToArray();

            List<SheetPlacementItem> allplacements = [];

            double fitness = 0;
            NFP nfp;
            double sheetarea = -1;
            int totalPlaced = 0;
            int totalParts = parts.Length;

            while (parts.Length > 0)
            {
                cancellationToken.ThrowIfCancellationRequested(); // Check for cancellation at the start of each sheet placement

                List<NFP> placed = [];
                List<PlacementItem> placements = [];

                NFP sheet = sheets.First();
                sheets = sheets.Skip(1).ToArray();
                sheetarea = Math.Abs(GeometryUtil.PolygonArea(sheet));
                totalsheetarea += sheetarea;
                fitness += sheetarea; // add 1 for each new sheet opened (lower fitness is better)

                string clipkey = "";
                Dictionary<string, ClipCacheItem> clipCache = new Dictionary<string, ClipCacheItem>();
                Clipper clipper = new ClipperLib.Clipper();
                List<List<IntPoint>> combinedNfp = new List<List<ClipperLib.IntPoint>>();
                bool error = false;
                IntPoint[][] clipperSheetNfp = null;
                double? minwidth = null;
                PlacementItem position = null;
                double? minarea = null;

                for (i = 0; i < parts.Length; i++)
                {
                    cancellationToken.ThrowIfCancellationRequested(); // Check for cancellation for each part
                    float prog = 0.66f + 0.34f * (totalPlaced / (float)totalParts);
                    DisplayProgress(prog);

                    NFP part = parts[i];

                    // inner NFP
                    NFP[] sheetNfp = null;
                    bool canPlace = false;

                    List<float> allowedAngles = RotationHelpers.GetAllowedRotation(part);

                    foreach (float angle in allowedAngles)
                    {
                        NFP rotatedPart = RotatePolygon(part, angle);
                        rotatedPart.Rotation = angle;
                        rotatedPart.Source = part.Source;
                        rotatedPart.Id = part.Id;
                        rotatedPart.RotationConstraint = part.RotationConstraint;

                        sheetNfp = GetInnerNfp(sheet, rotatedPart, 0, config);
                        if (sheetNfp != null && sheetNfp.Length > 0 && sheetNfp[0].Length > 0)
                        {
                            part = rotatedPart;           // Use this rotation
                            parts[i] = rotatedPart;       // Update in the array
                            break;
                        }
                    }
                    if (!canPlace || sheetNfp == null || sheetNfp.Length == 0)
                    {
                        continue;
                    }

                    position = null;

                    if (placed.Count == 0)
                    {
                        // first placement, put it on the top left corner
                        for (j = 0; j < sheetNfp.Count(); j++)
                        {
                            for (k = 0; k < sheetNfp[j].Length; k++)
                            {
                                if (position == null ||
                                    ((sheetNfp[j][k].x - part[0].x) < position.x) ||
                                    (
                                    GeometryUtil.AlmostEqual(sheetNfp[j][k].x - part[0].x, position.x)
                                    && ((sheetNfp[j][k].y - part[0].y) < position.y))
                                    )
                                {
                                    position = new PlacementItem()
                                    {
                                        x = sheetNfp[j][k].x - part[0].x,
                                        y = sheetNfp[j][k].y - part[0].y,
                                        id = part.Id,
                                        rotation = part.Rotation,
                                        source = part.Source.Value

                                    };


                                }
                            }
                        }

                        if (position == null)
                        {
                            throw new Exception("position null");
                            //console.log(sheetNfp);
                        }
                        placements.Add(position);
                        placed.Add(part);
                        totalPlaced++;

                        continue;
                    }

                    clipperSheetNfp = InnerNfpToClipperCoordinates(sheetNfp, config);

                    clipper = new ClipperLib.Clipper();
                    combinedNfp = new List<List<ClipperLib.IntPoint>>();

                    error = false;

                    // check if stored in clip cache
                    clipkey = "s:" + part.Source + "r:" + part.Rotation;
                    int startindex = 0;
                    if (EnableCaches && clipCache.ContainsKey(clipkey))
                    {
                        IntPoint[][] prevNfp = clipCache[clipkey].Nfpp;
                        clipper.AddPaths(prevNfp.Select(z => z.ToList()).ToList(), ClipperLib.PolyType.ptSubject, true);
                        startindex = clipCache[clipkey].index;
                    }

                    for (j = startindex; j < placed.Count; j++)
                    {
                        nfp = GetOuterNfp(placed[j], part, 0);
                        // minkowski difference failed. very rare but could happen
                        if (nfp == null)
                        {
                            error = true;
                            break;
                        }
                        // shift to placed location
                        for (m = 0; m < nfp.Length; m++)
                        {
                            nfp[m].x += placements[j].x;
                            nfp[m].y += placements[j].y;
                        }
                        if (nfp.children != null && nfp.children.Count > 0)
                        {
                            for (n = 0; n < nfp.children.Count; n++)
                            {
                                for (int o = 0; o < nfp.children[n].Length; o++)
                                {
                                    nfp.children[n][o].x += placements[j].x;
                                    nfp.children[n][o].y += placements[j].y;
                                }
                            }
                        }

                        IntPoint[][] clipperNfp = NfpToClipperCoordinates(nfp, config);

                        clipper.AddPaths(clipperNfp.Select(z => z.ToList()).ToList(), ClipperLib.PolyType.ptSubject, true);
                    }

                    if (error || !clipper.Execute(ClipperLib.ClipType.ctUnion, combinedNfp, ClipperLib.PolyFillType.pftNonZero, ClipperLib.PolyFillType.pftNonZero))
                    {
                        continue;
                    }


                    if (EnableCaches)
                    {
                        clipCache[clipkey] = new ClipCacheItem()
                        {
                            index = placed.Count - 1,
                            Nfpp = combinedNfp.Select(z => z.ToArray()).ToArray()
                        };
                    }

                    // difference with sheet polygon
                    List<List<IntPoint>> _finalNfp = new List<List<IntPoint>>();
                    clipper = new ClipperLib.Clipper();

                    clipper.AddPaths(combinedNfp, ClipperLib.PolyType.ptClip, true);

                    clipper.AddPaths(clipperSheetNfp.Select(z => z.ToList()).ToList(), ClipperLib.PolyType.ptSubject, true);


                    if (!clipper.Execute(ClipperLib.ClipType.ctDifference, _finalNfp, ClipperLib.PolyFillType.pftEvenOdd, ClipperLib.PolyFillType.pftNonZero))
                    {
                        continue;
                    }

                    if (_finalNfp == null || _finalNfp.Count == 0)
                    {
                        continue;
                    }


                    List<NFP> f = new List<NFP>();
                    for (j = 0; j < _finalNfp.Count; j++)
                    {
                        // back to normal scale
                        f.Add(ToNestCoordinates(_finalNfp[j].ToArray(), config.ClipperScale));
                    }
                    List<NFP> finalNfp = f;

                    // choose placement that results in the smallest bounding box/hull etc
                    // todo: generalize gravity direction
                    minwidth = null;
                    minarea = null;
                    double? minx = null;
                    double? miny = null;
                    NFP nf;
                    double area;
                    PlacementItem shiftvector = null;


                    NFP allpoints = new NFP();
                    for (m = 0; m < placed.Count; m++)
                    {
                        for (n = 0; n < placed[m].Length; n++)
                        {
                            allpoints.AddPoint(
                                new SvgPoint(
                                 placed[m][n].x + placements[m].x, placed[m][n].y + placements[m].y));
                        }
                    }

                    PolygonBounds allbounds = null;
                    PolygonBounds partbounds = null;
                    if (config.PlacementType == PlacementTypeEnum.gravity || config.PlacementType == PlacementTypeEnum.box)
                    {
                        allbounds = GeometryUtil.GetPolygonBounds(allpoints);

                        NFP partpoints = new NFP();
                        for (m = 0; m < part.Length; m++)
                        {
                            partpoints.AddPoint(new SvgPoint(part[m].x, part[m].y));
                        }
                        partbounds = GeometryUtil.GetPolygonBounds(partpoints);
                    }
                    else
                    {
                        allpoints = getHull(allpoints);
                    }
                    for (j = 0; j < finalNfp.Count; j++)
                    {
                        nf = finalNfp[j];
                        for (k = 0; k < nf.Length; k++)
                        {
                            shiftvector = new PlacementItem()
                            {
                                id = part.Id,
                                x = nf[k].x - part[0].x,
                                y = nf[k].y - part[0].y,
                                source = part.Source.Value,
                                rotation = part.Rotation
                            };
                            PolygonBounds rectbounds = null;
                            if (config.PlacementType == PlacementTypeEnum.gravity || config.PlacementType == PlacementTypeEnum.box)
                            {
                                NFP poly = new NFP();
                                poly.AddPoint(new SvgPoint(allbounds.x, allbounds.y));
                                poly.AddPoint(new SvgPoint(allbounds.x + allbounds.width, allbounds.y));
                                poly.AddPoint(new SvgPoint(allbounds.x + allbounds.width, allbounds.y + allbounds.height));
                                poly.AddPoint(new SvgPoint(allbounds.x, allbounds.y + allbounds.height));
                                poly.AddPoint(new SvgPoint(partbounds.x + shiftvector.x, partbounds.y + shiftvector.y));
                                poly.AddPoint(new SvgPoint(partbounds.x + partbounds.width + shiftvector.x, partbounds.y + shiftvector.y));
                                poly.AddPoint(new SvgPoint(partbounds.x + partbounds.width + shiftvector.x, partbounds.y + partbounds.height + shiftvector.y));
                                poly.AddPoint(new SvgPoint(partbounds.x + shiftvector.x, partbounds.y + partbounds.height + shiftvector.y));
                                rectbounds = GeometryUtil.GetPolygonBounds(poly);

                                // weigh width more, to help compress in direction of gravity
                                if (config.PlacementType == PlacementTypeEnum.gravity)
                                {
                                    area = rectbounds.width * 2 + rectbounds.height;
                                }
                                else
                                {
                                    area = rectbounds.width * rectbounds.height;
                                }
                            }
                            else
                            {
                                // must be convex hull
                                NFP localpoints = Clone(allpoints);

                                for (m = 0; m < part.Length; m++)
                                {
                                    localpoints.AddPoint(new SvgPoint(part[m].x + shiftvector.x, part[m].y + shiftvector.y));
                                }

                                area = Math.Abs(GeometryUtil.PolygonArea(getHull(localpoints)));
                                shiftvector.hull = getHull(localpoints);
                                shiftvector.hullsheet = getHull(sheet);
                            }
                            MergedResult merged = null;
                            if (config.MergeLines)
                            {
                                throw new NotImplementedException();
                            }

                            if (
                    minarea == null ||
                    area < minarea ||
                    (GeometryUtil.AlmostEqual(minarea, area) && (minx == null || shiftvector.x < minx)) ||
                    (GeometryUtil.AlmostEqual(minarea, area) && (minx != null && GeometryUtil.AlmostEqual(shiftvector.x, minx) && shiftvector.y < miny))
                    )
                            {
                                minarea = area;

                                minwidth = rectbounds != null ? rectbounds.width : 0;
                                position = shiftvector;
                                if (minx == null || shiftvector.x < minx)
                                {
                                    minx = shiftvector.x;
                                }
                                if (miny == null || shiftvector.y < miny)
                                {
                                    miny = shiftvector.y;
                                }

                                if (config.MergeLines)
                                {
                                    position.mergedLength = merged.totalLength;
                                    position.mergedSegments = merged.segments;
                                }
                            }
                        }

                    }

                    if (position != null)
                    {
                        placed.Add(part);
                        totalPlaced++;
                        placements.Add(position);
                        if (position.mergedLength != null)
                        {
                            totalMerged += position.mergedLength.Value;
                        }
                    }
                    // send placement progress signal
                    int placednum = placed.Count;
                    for (j = 0; j < allplacements.Count; j++)
                    {
                        placednum += allplacements[j].sheetplacements.Count;
                    }
                }
                if (!minwidth.HasValue)
                {
                    fitness = double.NaN;
                }
                else
                {
                    fitness += (minwidth.Value / sheetarea) + minarea.Value;
                }

                for (i = 0; i < placed.Count; i++)
                {


                    int index = Array.IndexOf(parts, placed[i]);
                    if (index >= 0)
                    {
                        parts = parts.splice(index, 1);
                    }
                }
                if (placements != null && placements.Count > 0)
                {
                    allplacements.Add(new SheetPlacementItem()
                    {
                        sheetId = sheet.Id,
                        sheetSource = sheet.Source.Value,
                        sheetplacements = placements
                    });
                }
                else
                {
                    break; // something went wrong
                }

                if (sheets.Count() == 0)
                {
                    break;
                }
            }

            // there were parts that couldn't be placed
            // scale this value high - we really want to get all the parts in, even at the cost of opening new sheets
            for (i = 0; i < parts.Count(); i++)
            {
                fitness += 100000000 * (Math.Abs(GeometryUtil.PolygonArea(parts[i])) / totalsheetarea);
            }

            return new SheetPlacement()
            {
                placements = new[] { allplacements.ToList() },
                fitness = fitness,
                //  paths = paths,
                area = sheetarea,
                mergedLength = totalMerged


            };
        }

        // jsClipper uses X/Y instead of x/y...
        public DataInfo data;
        NFP[] parts;



        int index;
        // run the placement synchronously


        public static WindowUnk window = new();

        public Action<SheetPlacement> ResponseAction;

        public static long LastPlacePartTime = 0;
        public void Sync(CancellationToken cancellationToken)
        {
            int c = 0;
            foreach (KeyValuePair<NfpCacheKey, List<NFP>> key in window.nfpCache)
            {
                c++;
            }
            Stopwatch sw = Stopwatch.StartNew();
            SheetPlacement placement = PlaceParts(data.sheets.ToArray(), parts, data.config, index, cancellationToken);
            sw.Stop();
            LastPlacePartTime = sw.ElapsedMilliseconds;

            placement.index = data.index;
            ResponseAction(placement);
        }
        public void BackgroundStart(DataInfo data, CancellationToken cancellationToken)
        {
            this.data = data;
            int index = data.index;
            PopulationItem individual = data.individual;

            List<NFP> parts = individual.placements;

            List<NfpPair> pairs = new List<NfpPair>();

            if (UseParallel)
            {
                ConcurrentBag<NfpPair> concurrentPairs = new ConcurrentBag<NfpPair>();

                Parallel.For(0, parts.Count, (Action<int>)(i =>
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    NFP B = parts[i];
                    for (int j = 0; j < i; j++)
                    {
                        NFP A = parts[j];
                        NfpPair key = new NfpPair()
                        {
                            A = A,
                            B = B,
                            ARotation = A.Rotation,
                            BRotation = B.Rotation,
                            Asource = A.Source.Value,
                            Bsource = B.Source.Value
                        };
                        DbCacheKey doc = new DbCacheKey()
                        {
                            A = A.Source.Value,
                            B = B.Source.Value,
                            ARotation = A.Rotation,
                            BRotation = B.Rotation
                        };

                        if (!window.db.Has(doc))
                        {
                            concurrentPairs.Add(key);
                        }
                    }
                }));

                pairs = concurrentPairs
                    .GroupBy(p => new { p.Asource, p.Bsource, p.ARotation, p.BRotation })
                    .Select(g => g.First())
                    .ToList();
            }
            else
            {
                for (int i = 0; i < parts.Count; i++)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    NFP B = parts[i];
                    for (int j = 0; j < i; j++)
                    {
                        NFP A = parts[j];
                        NfpPair key = new NfpPair()
                        {
                            A = A,
                            B = B,
                            ARotation = A.Rotation,
                            BRotation = B.Rotation,
                            Asource = A.Source.Value,
                            Bsource = B.Source.Value

                        };
                        DbCacheKey doc = new DbCacheKey()
                        {
                            A = A.Source.Value,
                            B = B.Source.Value,

                            ARotation = A.Rotation,
                            BRotation = B.Rotation

                        };
                        if (!Inpairs(key, pairs.ToArray()) && !window.db.Has(doc))
                        {
                            pairs.Add(key);
                        }
                    }
                }
            }

            this.parts = parts.ToArray();
            if (pairs.Count > 0)
            {
                NfpPair[] ret1 = PmapDeepNest(pairs, cancellationToken);
                ThenDeepNest(ret1, parts, cancellationToken);
            }
            else
            {
                Sync(cancellationToken);
            }
        }

        public NFP GetPart(int source, List<NFP> parts)
        {
            for (int k = 0; k < parts.Count; k++)
            {
                if (parts[k].Source == source)
                {
                    return parts[k];
                }
            }
            return null;
        }

        public void ThenIterate(NfpPair processed, List<NFP> parts)
        {

            // returned data only contains outer nfp, we have to account for any holes separately in the synchronous portion
            // this is because the c++ addon which can process interior nfps cannot run in the worker thread					
            NFP A = GetPart(processed.Asource, parts);
            NFP B = GetPart(processed.Bsource, parts);

            List<NFP> Achildren = new List<NFP>();


            if (A.children != null)
            {
                for (int j = 0; j < A.children.Count; j++)
                {
                    Achildren.Add(RotatePolygon(A.children[j], processed.ARotation));
                }
            }

            if (Achildren.Count > 0)
            {
                NFP Brotated = RotatePolygon(B, processed.BRotation);
                PolygonBounds bbounds = GeometryUtil.GetPolygonBounds(Brotated);
                List<NFP> cnfp = new List<NFP>();

                for (int j = 0; j < Achildren.Count; j++)
                {
                    PolygonBounds cbounds = GeometryUtil.GetPolygonBounds(Achildren[j]);
                    if (cbounds.width > bbounds.width && cbounds.height > bbounds.height)
                    {
                        NFP[] n = GetInnerNfp(Achildren[j], Brotated, 1, data.config);
                        if (n != null && n.Count() > 0)
                        {
                            cnfp.AddRange(n);
                        }
                    }
                }

                processed.nfp.children = cnfp;
            }
            DbCacheKey doc = new DbCacheKey()
            {
                A = processed.Asource,
                B = processed.Bsource,
                ARotation = processed.ARotation,
                BRotation = processed.BRotation,
                nfp = new[] { processed.nfp }
            };

            window.db.Insert(doc);
        }

        public static Action<float> displayProgress;
        public static void DisplayProgress(float p)
        {
            if (displayProgress != null)
            {
                displayProgress(p);
            }
        }
        public void ThenDeepNest(NfpPair[] processed, List<NFP> parts, CancellationToken token)
        {
            int cnt = 0;
            if (UseParallel)
            {
                Parallel.ForEach(processed, (item) =>
                {
                    float progress = 0.33f + 0.33f * (cnt / (float)processed.Count());
                    cnt++;
                    DisplayProgress(progress);
                    ThenIterate(item, parts);
                });
            }
            else
            {
                for (int i = 0; i < processed.Length; i++)
                {
                    float progress = 0.33f + 0.33f * (cnt / (float)processed.Count());
                    cnt++;
                    DisplayProgress(progress);
                    ThenIterate(processed[i], parts);
                }
            }

            Sync(token);
        }


        public bool Inpairs(NfpPair key, NfpPair[] p)
        {
            for (int i = 0; i < p.Length; i++)
            {
                if (p[i].Asource == key.Asource && p[i].Bsource == key.Bsource && p[i].ARotation == key.ARotation && p[i].BRotation == key.BRotation)
                {
                    return true;
                }
            }
            return false;
        }

        public NfpPair[] PmapDeepNest(List<NfpPair> pairs, CancellationToken cancellationToken)
        {


            NfpPair[] ret = new NfpPair[pairs.Count()];
            int cnt = 0;
            if (UseParallel)
            {
                Parallel.For(0, pairs.Count, (i, loopState) =>
                {
                    if (cancellationToken.IsCancellationRequested)
                    {
                        loopState.Stop();
                        return;
                    }

                    ret[i] = Process(pairs[i], cancellationToken);
                    float progress = 0.33f * (cnt / (float)pairs.Count);
                    cnt++;
                    DisplayProgress(progress);
                });
            }
            else
            {
                for (int i = 0; i < pairs.Count; i++)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    NfpPair item = pairs[i];
                    ret[i] = Process(item, cancellationToken);
                    float progress = 0.33f * (cnt / (float)pairs.Count);
                    cnt++;
                    DisplayProgress(progress);
                }
            }
            cancellationToken.ThrowIfCancellationRequested();
            return ret.ToArray();
        }

        public NfpPair Process(NfpPair pair, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            NFP A = RotatePolygon(pair.A, pair.ARotation);
            NFP B = RotatePolygon(pair.B, pair.BRotation);

            IntPoint[] Ac = ClipperHelper.ScaleUpPaths(A, 10000000);

            IntPoint[] Bc = ClipperHelper.ScaleUpPaths(B, 10000000);
            for (int i = 0; i < Bc.Length; i++)
            {
                Bc[i].X *= -1;
                Bc[i].Y *= -1;
            }
            List<List<IntPoint>> solution = ClipperLib.Clipper.MinkowskiSum(new List<IntPoint>(Ac), new List<IntPoint>(Bc), true);
            NFP clipperNfp = null;

            double? largestArea = null;
            for (int i = 0; i < solution.Count(); i++)
            {
                NFP n = ToNestCoordinates(solution[i].ToArray(), 10000000);
                double sarea = -GeometryUtil.PolygonArea(n);
                if (largestArea == null || largestArea < sarea)
                {
                    clipperNfp = n;
                    largestArea = sarea;
                }
            }

            for (int i = 0; i < clipperNfp.Length; i++)
            {
                clipperNfp[i].x += B[0].x;
                clipperNfp[i].y += B[0].y;
            }

            pair.A = null;
            pair.B = null;
            pair.nfp = clipperNfp;
            return pair;


        }
        public static NFP ToNestCoordinates(IntPoint[] polygon, double scale)
        {
            List<SvgPoint> clone = new List<SvgPoint>();

            for (int i = 0; i < polygon.Count(); i++)
            {
                clone.Add(new SvgPoint(
                     polygon[i].X / scale,
                             polygon[i].Y / scale
                        ));
            }
            return new NFP() { Points = clone.ToArray() };
        }
        public static NFP getHull(NFP polygon)
        {
            double[][] points = new double[polygon.Length][];
            for (int i = 0; i < polygon.Length; i++)
            {
                points[i] = (new double[] { polygon[i].x, polygon[i].y });
            }

            double[][] hullpoints = D3.polygonHull(points);

            if (hullpoints == null)
            {
                return polygon;
            }

            NFP hull = new NFP();
            for (int i = 0; i < hullpoints.Count(); i++)
            {
                hull.AddPoint(new SvgPoint(hullpoints[i][0], hullpoints[i][1]));
            }
            return hull;
        }


        // returns clipper nfp. Remember that clipper nfp are a list of polygons, not a tree!
        public static IntPoint[][] NfpToClipperCoordinates(NFP nfp, SvgNestConfig config)
        {

            List<IntPoint[]> clipperNfp = new List<IntPoint[]>();

            // children first
            if (nfp.children != null && nfp.children.Count > 0)
            {
                for (int j = 0; j < nfp.children.Count; j++)
                {
                    if (GeometryUtil.PolygonArea(nfp.children[j]) < 0)
                    {
                        nfp.children[j].Reverse();
                    }
                    //var childNfp = SvgNest.toClipperCoordinates(nfp.children[j]);
                    IntPoint[] childNfp = ClipperHelper.ScaleUpPaths(nfp.children[j], config.ClipperScale);
                    clipperNfp.Add(childNfp);
                }
            }

            if (GeometryUtil.PolygonArea(nfp) > 0)
            {
                nfp.Reverse();
            }

            // clipper js defines holes based on orientation

            IntPoint[] outerNfp = ClipperHelper.ScaleUpPaths(nfp, config.ClipperScale);

            clipperNfp.Add(outerNfp);

            return clipperNfp.ToArray();
        }

        // inner nfps can be an array of nfps, outer nfps are always singular
        public static IntPoint[][] InnerNfpToClipperCoordinates(NFP[] nfp, SvgNestConfig config)
        {
            List<IntPoint[]> clipperNfp = new List<IntPoint[]>();
            for (int i = 0; i < nfp.Count(); i++)
            {
                IntPoint[][] clip = NfpToClipperCoordinates(nfp[i], config);
                clipperNfp.AddRange(clip);
                //clipperNfp = clipperNfp.Concat(new[] { clip }).ToList();
            }

            return clipperNfp.ToArray();
        }

        public static NFP GetOuterNfp(NFP A, NFP B, int type, bool inside = false)
        {
            NFP[] nfp = null;


            DbCacheKey key = new DbCacheKey()
            {
                A = A.Source,
                B = B.Source,
                ARotation = A.Rotation,
                BRotation = B.Rotation,
                //Type = type
            };

            NFP[] doc = window.db.Find(key);
            if (doc != null)
            {
                return doc.First();
            }

            if (inside || (A.children != null && A.children.Count > 0))
            {
                nfp = Process2(A, B, type);
            }
            else
            {
                IntPoint[] Ac = ClipperHelper.ScaleUpPaths(A, 10000000);

                IntPoint[] Bc = ClipperHelper.ScaleUpPaths(B, 10000000);
                for (int i = 0; i < Bc.Length; i++)
                {
                    Bc[i].X *= -1;
                    Bc[i].Y *= -1;
                }
                List<List<IntPoint>> solution = ClipperLib.Clipper.MinkowskiSum(new List<IntPoint>(Ac), new List<IntPoint>(Bc), true);
                NFP clipperNfp = null;

                double? largestArea = null;
                for (int i = 0; i < solution.Count(); i++)
                {
                    NFP n = ToNestCoordinates(solution[i].ToArray(), 10000000);
                    double sarea = GeometryUtil.PolygonArea(n);
                    if (largestArea == null || largestArea > sarea)
                    {
                        clipperNfp = n;
                        largestArea = sarea;
                    }
                }

                for (int i = 0; i < clipperNfp.Length; i++)
                {
                    clipperNfp[i].x += B[0].x;
                    clipperNfp[i].y += B[0].y;
                }
                nfp = new NFP[] { new NFP() { Points = clipperNfp.Points } };


            }

            if (nfp == null || nfp.Length == 0)
            {
                return null;
            }

            NFP nfps = nfp.First();
            if (nfps == null || nfps.Length == 0)
            {
                return null;
            }
            if (!inside && A.Source != null && B.Source != null)
            {
                DbCacheKey doc2 = new DbCacheKey()
                {
                    A = A.Source.Value,
                    B = B.Source.Value,
                    ARotation = A.Rotation,
                    BRotation = B.Rotation,
                    nfp = nfp
                };
                window.db.Insert(doc2);


            }
            return nfps;
        }
    }
}
