using DeepNestLib.Rotation;

namespace DeepNestLib
{
    public class DbCacheKey
    {
        public int? A { get; set; }
        public int? B { get; set; }
        public float ARotation { get; set; }
        public float BRotation { get; set; }
        public RotationConstraint ARotationConstraint { get; set; } = RotationConstraint.Fixed;
        public RotationConstraint BRotationConstraint { get; set; } = RotationConstraint.Fixed;
        public NFP[] Nfp { get; set; }
        public int Type { get; set; } = 0;
    }
}
