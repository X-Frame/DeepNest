using System;
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
        private static readonly Random _random = new();
        public static List<float> GetAllowedRotations(NFP part)
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

        public static float GetRandomAllowedRotation(NFP part)
        {
            List<float> allowedRotations = GetAllowedRotations(part);
            if (allowedRotations.Count == 0)
            {
                return 0f;
            }
            if (allowedRotations.Count == 1)
            {
                return allowedRotations[0];
            }
            int index = _random.Next(allowedRotations.Count);
            return allowedRotations[index];
        }

    }
}