using DeepNestLib.Background;
using DeepNestLib.Sheets;
using DeepNestLib.Svg;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace DeepNestLib
{
    public class NestingContext
    {
        public NestingContext() { _config = new SvgNestConfig(); }
        public NestingContext(SvgNestConfig config) { _config = config; }
        public List<NFP> Polygons { get; private set; } = new List<NFP>();
        public List<NFP> Sheets { get; private set; } = new List<NFP>();
        public double MaterialUtilization { get; private set; } = 0;
        public int PlacedPartsCount { get; private set; } = 0;


        SheetPlacement current = null;
        public SheetPlacement Current { get { return current; } }
        public SvgNest Nest { get; private set; }

        public int Iterations { get; private set; } = 0;

        private SvgNestConfig _config;

        public void StartNest()
        {
            current = null;
            Nest = new SvgNest(_config);
            NestingService.cacheProcess = new ConcurrentDictionary<string, NFP[]>();
            NestingService.window = new WindowUnk();
            NestingService.callCounter = 0;
            Iterations = 0;
            NestingService.UseParallel = _config.UseParallel;
        }

        bool offsetTreePhase = true;
        public void NestIterate(CancellationToken cancellationToken)
        {
            List<NFP> lsheets = new List<NFP>();
            List<NFP> lpoly = new List<NFP>();

            for (int i = 0; i < Polygons.Count; i++)
            {
                Polygons[i].id = i;
            }
            for (int i = 0; i < Sheets.Count; i++)
            {
                Sheets[i].id = i;
            }
            foreach (NFP item in Polygons)
            {
                cancellationToken.ThrowIfCancellationRequested();
                NFP clone = item.Clone();
                lpoly.Add(clone);
            }


            foreach (NFP item in Sheets)
            {
                cancellationToken.ThrowIfCancellationRequested();
                NFP clone = item.Clone();
                lsheets.Add(clone);
            }

            if (offsetTreePhase)
            {
                IGrouping<int?, NFP>[] grps = lpoly.GroupBy(z => z.source).ToArray();
                if (NestingService.UseParallel)
                {
                    Parallel.ForEach(grps, (item) =>
                    {
                        cancellationToken.ThrowIfCancellationRequested();
                        SvgNest.OffsetTree(item.First(), 0.5 * SvgNest.Config.Spacing, SvgNest.Config, cancellationToken);
                        foreach (NFP? zitem in item)
                        {
                            cancellationToken.ThrowIfCancellationRequested();
                            zitem.Points = item.First().Points.ToArray();
                        }

                    });

                }
                else
                {

                    foreach (IGrouping<int?, NFP>? item in grps)
                    {
                        cancellationToken.ThrowIfCancellationRequested();
                        SvgNest.OffsetTree(item.First(), 0.5 * SvgNest.Config.Spacing, SvgNest.Config, cancellationToken);
                        foreach (NFP? zitem in item)
                        {
                            cancellationToken.ThrowIfCancellationRequested();
                            zitem.Points = item.First().Points.ToArray();
                        }
                    }
                }

                foreach (NFP item in lsheets)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    SvgNest.OffsetTree(item, SvgNest.Config.SheetEdgeOffset, SvgNest.Config, cancellationToken, true);
                }
            }



            List<NestItem> partsLocal = new List<NestItem>();
            IEnumerable<NestItem> p1 = lpoly.GroupBy(z => z.source).Select(z => new NestItem()
            {
                Polygon = z.First(),
                IsSheet = false,
                Quanity = z.Count()
            });

            IEnumerable<NestItem> p2 = lsheets.GroupBy(z => z.source).Select(z => new NestItem()
            {
                Polygon = z.First(),
                IsSheet = true,
                Quanity = z.Count()
            });


            partsLocal.AddRange(p1);
            partsLocal.AddRange(p2);
            int srcc = 0;
            foreach (NestItem item in partsLocal)
            {
                item.Polygon.source = srcc++;
            }

            cancellationToken.ThrowIfCancellationRequested();
            Nest.LaunchWorkers(partsLocal.ToArray(), cancellationToken);
            SheetPlacement plcpr = Nest.nests.First();

            if (current == null || plcpr.fitness < current.fitness)
            {
                AssignPlacement(plcpr, cancellationToken);
            }
            Iterations++;
        }

        public void ExportSvg(string v)
        {
            SvgParser.Export(v, Polygons, Sheets);
        }


        public void AssignPlacement(SheetPlacement plcpr, CancellationToken cancellationToken)
        {
            current = plcpr;
            double totalSheetsArea = 0;
            double totalPartsArea = 0;

            PlacedPartsCount = 0;
            List<NFP> placed = new List<NFP>();
            foreach (NFP item in Polygons)
            {
                cancellationToken.ThrowIfCancellationRequested();
                item.sheet = null;
            }
            List<int> sheetsIds = new List<int>();

            foreach (List<SheetPlacementItem> item in plcpr.placements)
            {
                cancellationToken.ThrowIfCancellationRequested();
                foreach (SheetPlacementItem zitem in item)
                {
                    int sheetid = zitem.sheetId;
                    if (!sheetsIds.Contains(sheetid))
                    {
                        sheetsIds.Add(sheetid);
                    }

                    NFP sheet = Sheets.First(z => z.id == sheetid);
                    totalSheetsArea += GeometryUtil.PolygonArea(sheet);

                    foreach (PlacementItem ssitem in zitem.sheetplacements)
                    {
                        cancellationToken.ThrowIfCancellationRequested();

                        PlacedPartsCount++;
                        NFP poly = Polygons.First(z => z.id == ssitem.id);
                        totalPartsArea += GeometryUtil.PolygonArea(poly);
                        placed.Add(poly);
                        poly.sheet = sheet;
                        poly.x = ssitem.x + sheet.x;
                        poly.y = ssitem.y + sheet.y;
                        poly.rotation = ssitem.rotation;
                    }
                }
            }

            NFP[] emptySheets = Sheets.Where(z => !sheetsIds.Contains(z.id)).ToArray();

            MaterialUtilization = Math.Abs(totalPartsArea / totalSheetsArea);

            IEnumerable<NFP> ppps = Polygons.Where(z => !placed.Contains(z));
            foreach (NFP? item in ppps)
            {
                item.x = -1000;
                item.y = 0;
            }
        }

        public void ReorderSheets()
        {
            double x = 0;
            double y = 0;
            int gap = _config.SheetSpacing;
            for (int i = 0; i < Sheets.Count; i++)
            {
                Sheets[i].x = x;
                Sheets[i].y = y;
                if (Sheets[i] is Sheet)
                {
                    Sheet? r = Sheets[i] as Sheet;
                    x += r.Width + gap;
                }
                else
                {
                    double maxx = Sheets[i].Points.Max(z => z.x);
                    double minx = Sheets[i].Points.Min(z => z.x);
                    double w = maxx - minx;
                    x += w + gap;
                }
            }
        }

        public void AddSheet(int w, int h, int src)
        {
            RectangleSheet tt = new RectangleSheet();
            tt.Name = "sheet" + (Sheets.Count + 1);
            Sheets.Add(tt);

            tt.source = src;
            tt.Height = h;
            tt.Width = w;
            tt.Rebuild();
            ReorderSheets();
        }

        Random r = new Random();


        public void LoadSampleData()
        {
            Console.WriteLine("Adding sheets..");
            //add sheets
            for (int i = 0; i < 5; i++)
            {
                AddSheet(3000, 1500, 0);
            }

            Console.WriteLine("Adding parts..");
            //add parts
            int src1 = GetNextSource();
            for (int i = 0; i < 200; i++)
            {
                AddRectanglePart(src1, 250, 220);
            }

        }
        public void LoadInputData(string path, int count)
        {
            DirectoryInfo dir = new DirectoryInfo(path);
            foreach (FileInfo item in dir.GetFiles("*.svg"))
            {
                try
                {
                    int src = GetNextSource();
                    for (int i = 0; i < count; i++)
                    {
                        ImportFromRawDetail(SvgParser.LoadSvg(item.FullName), src);
                    }
                }
                catch (Exception)
                {
                    Console.WriteLine("Error loading " + item.FullName + ". skip");
                }
            }
        }
        public NFP ImportFromRawDetail(RawDetail raw, int src)
        {
            NFP po = null;
            List<NFP> nfps = new List<NFP>();
            foreach (LocalContour item in raw.Outers)
            {
                NFP nn = new NFP();
                nfps.Add(nn);
                foreach (System.Drawing.PointF pitem in item.Points)
                {
                    nn.AddPoint(new SvgPoint(pitem.X, pitem.Y));
                }
            }

            if (nfps.Any())
            {
                NFP tt = nfps.OrderByDescending(z => z.Area).First();
                po = tt;
                po.Name = raw.Name;

                foreach (NFP r in nfps)
                {
                    if (r == tt)
                    {
                        continue;
                    }

                    if (po.children == null)
                    {
                        po.children = new List<NFP>();
                    }
                    po.children.Add(r);
                }

                po.source = src;
                Polygons.Add(po);
            }
            return po;
        }
        public int GetNextSource()
        {
            if (Polygons.Any())
            {
                return Polygons.Max(z => z.source.Value) + 1;
            }
            return 0;
        }
        public int GetNextSheetSource()
        {
            if (Sheets.Any())
            {
                return Sheets.Max(z => z.source.Value) + 1;
            }
            return 0;
        }
        public void AddRectanglePart(int src, int ww = 50, int hh = 80)
        {
            int xx = 0;
            int yy = 0;
            NFP pl = new NFP();

            Polygons.Add(pl);
            pl.source = src;
            pl.Points = new SvgPoint[] { };
            pl.AddPoint(new SvgPoint(xx, yy));
            pl.AddPoint(new SvgPoint(xx + ww, yy));
            pl.AddPoint(new SvgPoint(xx + ww, yy + hh));
            pl.AddPoint(new SvgPoint(xx, yy + hh));
        }
        public void LoadXml(string v)
        {
            XDocument d = XDocument.Load(v);
            XElement f = d.Descendants().First();
            int gap = int.Parse(f.Attribute("gap").Value);
            SvgNest.Config.Spacing = gap;

            foreach (XElement item in d.Descendants("sheet"))
            {
                int src = GetNextSheetSource();
                int cnt = int.Parse(item.Attribute("count").Value);
                int ww = int.Parse(item.Attribute("width").Value);
                int hh = int.Parse(item.Attribute("height").Value);

                for (int i = 0; i < cnt; i++)
                {
                    AddSheet(ww, hh, src);
                }
            }
            foreach (XElement item in d.Descendants("part"))
            {
                int cnt = int.Parse(item.Attribute("count").Value);
                string path = item.Attribute("path").Value;
                RawDetail r = SvgParser.LoadSvg(path);
                int src = GetNextSource();

                for (int i = 0; i < cnt; i++)
                {
                    ImportFromRawDetail(r, src);
                }
            }
        }
    }
}
