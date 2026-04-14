using System.Collections.Generic;

namespace DeepNestLib.Rotation
{
    public enum RotationConstraint
    {
        Fixed,
        Fixed180,
        Ninety,
        Free
    }

    public static class RotationHelpers
    {
        public static List<float> GetAllowedRotation(NFP part)
        {
            RotationConstraint constraint = part.RotationConstraint;

            return constraint switch
            {
                RotationConstraint.Fixed => [0f],
                RotationConstraint.Fixed180 => [0f, 180f],
                RotationConstraint.Ninety => [0f, 90f],
                _ => [0f, 90f, 180f, 270f]
            };

        }
    }
}
