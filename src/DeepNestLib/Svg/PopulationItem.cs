using DeepNestLib.Rotation;
using System.Collections.Generic;
using System.Linq;

namespace DeepNestLib.Svg
{
    public class PopulationItem
    {
        public List<NFP> Placements { get; set; } = [];
        /// <summary>
        /// Rotation angles for each placement (one per part)
        /// </summary>
        public float[] Rotation { get; set; }
        public RotationConstraint[] RotationConstraints { get; set; }

        public object Processing { get; set; } = null;

        public double? Fitness { get; set; }
        public int Index { get; set; }

        public PopulationItem() { }
        public PopulationItem(List<NFP> placements, float[] rotations, RotationConstraint[] constraints)
        {
            this.Placements = placements ?? new List<NFP>();
            this.Rotation = rotations;
            this.RotationConstraints = constraints;
        }
        public PopulationItem Clone()
        {
            List<NFP> newPlacements = new List<NFP>(Placements.Count);
            foreach (NFP p in Placements)
            {
                NFP clone = NestingService.Clone(p);
                clone.Rotation = p.Rotation;
                clone.RotationConstraint = p.RotationConstraint;
                clone.Source = p.Source;
                clone.Id = p.Id;
                newPlacements.Add(clone);
            }

            return new PopulationItem
            {
                Placements = newPlacements,
                Rotation = Rotation?.ToArray() ?? [],
                RotationConstraints = RotationConstraints?.ToArray() ?? [],
                Fitness = Fitness,
                Index = Index
            };
        }

    }
}
