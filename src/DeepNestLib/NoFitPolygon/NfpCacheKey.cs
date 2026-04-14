using DeepNestLib.Rotation;
using System;

namespace DeepNestLib.NoFitPolygon
{
    public readonly struct NfpCacheKey : IEquatable<NfpCacheKey>
    {
        public readonly int A, B, Type;
        public readonly int ARotation, BRotation; // Use scaled integers for rotation
        public readonly RotationConstraint ARotationConstraint;
        public readonly RotationConstraint BRotationConstraint;

        public NfpCacheKey(DbCacheKey obj)
        {
            A = obj.A.GetValueOrDefault();
            B = obj.B.GetValueOrDefault();
            Type = obj.Type;
            ARotation = (int)Math.Round(obj.ARotation * 10000);
            BRotation = (int)Math.Round(obj.BRotation * 10000);
            ARotationConstraint = obj.ARotationConstraint;
            BRotationConstraint = obj.BRotationConstraint;
        }

        public bool Equals(NfpCacheKey other) =>
            A == other.A &&
            B == other.B &&
            ARotation == other.ARotation &&
            BRotation == other.BRotation &&
            Type == other.Type &&
            ARotationConstraint == other.ARotationConstraint &&
            BRotationConstraint == other.BRotationConstraint;

        public override bool Equals(object obj) => obj is NfpCacheKey other && Equals(other);

        public override int GetHashCode()
        {
            unchecked
            {
                int hash = 17;
                hash = hash * 23 + A.GetHashCode();
                hash = hash * 23 + B.GetHashCode();
                hash = hash * 23 + ARotation.GetHashCode();
                hash = hash * 23 + BRotation.GetHashCode();
                hash = hash * 23 + ARotationConstraint.GetHashCode();
                hash = hash * 23 + BRotationConstraint.GetHashCode();
                hash = hash * 23 + Type.GetHashCode();
                return hash;
            }
        }
    }
}
