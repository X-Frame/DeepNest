namespace DeepNestLib
{
    public class SvgNestConfig
    {
        public PlacementTypeEnum PlacementType { get; set; } = PlacementTypeEnum.box;
        public double CurveTolerance { get; set; } = 0.72;
        public double Scale { get; set; } = 25;
        public double ClipperScale { get; set; } = 10000000;
        public bool ExploreConcave { get; set; } = false;
        public int MutationRate { get; set; } = 10;
        public int PopulationSize { get; set; } = 10;
        public int Rotations { get; set; } = 4;
        public double Spacing { get; set; } = 100;
        public double SheetSpacing { get; set; } = 500;
        public bool UseHoles { get; set; } = false;
        public double TimeRatio { get; set; } = 0.5;
        public bool MergeLines { get; set; } = false;
        public bool Simplify { get; set; }
    }
}
