using DeepNestLib.Rotation;
using DeepNestLib.Svg;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace DeepNestLib
{
    // Mostly AI
    public class GeneticAlgorithm
    {
        public SvgNestConfig Config { get; private set; }

        public List<PopulationItem> Population { get; private set; } = [];
        private readonly Random _random = new();

        public GeneticAlgorithm(NFP[] adam, SvgNestConfig config, CancellationToken cancellationToken)
        {
            Config = config;
            foreach (NFP part in adam)
            {
                part.AllowedAngles = RotationHelpers.GetAllowedRotation(part);
            }
            float[] angles = new float[adam.Length];
            for (int i = 0; i < adam.Length; i++)
            {
                angles[i] = ChooseRandomLegalRotation(adam[i]);
            }
            Population = [new PopulationItem
            {
                Placements = adam.ToList(),
                Rotation = angles
            }];
            while (Population.Count < config.PopulationSize)
            {
                cancellationToken.ThrowIfCancellationRequested();
                PopulationItem mutant = this.Mutate(Population[0]);
                Population.Add(mutant);
            }
        }
        /// <summary>
        /// Creates one individual (set of placements + rotations) while respecting each part's RotationConstraint
        /// </summary>
        private PopulationItem CreateIndividual(NFP[] originalParts)
        {
            List<NFP> placements = new(originalParts.Length);
            List<float> rotations = new(originalParts.Length);
            List<RotationConstraint> constraints = new(originalParts.Length);

            for (int i = 0; i < originalParts.Length; i++)
            {
                NFP original = originalParts[i];

                // Choose a legal rotation for this specific part
                float angle = ChooseRandomLegalRotation(original);

                // Rotate the geometry
                NFP rotated = NestingService.RotatePolygon(original, angle);

                rotated.Rotation = angle;
                rotated.RotationConstraint = original.RotationConstraint;
                rotated.Source = original.Source;
                rotated.Id = original.Id;

                placements.Add(rotated);
                rotations.Add(angle);
                constraints.Add(original.RotationConstraint);
            }

            return new PopulationItem
            {
                Placements = placements,
                Rotation = rotations.ToArray(),
                RotationConstraints = constraints.ToArray()
            };
        }

        /// <summary>
        /// Returns a random legal rotation angle for a part based on its constraint
        /// </summary>
        private float ChooseRandomLegalRotation(NFP part)
        {
            List<float> allowed = RotationHelpers.GetAllowedRotation(part);

            if (allowed.Count == 0)
            {
                return 0f;
            }

            if (allowed.Count == 1)
            {
                return allowed[0];
            }

            int index = _random.Next(allowed.Count);
            return allowed[index];
        }

        public PopulationItem Mutate(PopulationItem p)
        {
            PopulationItem clone = p.Clone();

            for (int i = 0; i < clone.Placements.Count(); i++)
            {
                NFP part = clone.Placements[i];
                if (_random.NextDouble() < 0.08 * Config.MutationRate)
                {
                    int j = (i + 1) % clone.Placements.Count;
                    (clone.Placements[i], clone.Placements[j]) = (clone.Placements[j], clone.Placements[i]);
                }
                if (_random.NextDouble() < 0.99)
                {
                    clone.Rotation[i] = ChooseRandomLegalRotation(part);
                }
            }
            return clone;
        }

        private void RepairRotations(PopulationItem individual)
        {
            for (int i = 0; i < individual.Placements.Count; i++)
            {
                NFP part = individual.Placements[i];
                if (part is null)
                {
                    continue;
                }
                if (part.AllowedAngles == null || part.AllowedAngles.Count == 0)
                {
                    part.AllowedAngles = RotationHelpers.GetAllowedRotation(part);
                }

                List<float> allowed = part.AllowedAngles;

                // If current rotation is not allowed, replace it
                if (!allowed.Contains(individual.Rotation[i]))
                {
                    individual.Rotation[i] = ChooseRandomLegalRotation(part);
                }
            }
        }

        // returns a random individual from the population, weighted to the front of the list (lower fitness value is more likely to be selected)
        public PopulationItem RandomWeightedIndividual(PopulationItem exclude = null)
        {
            //var pop = this.population.slice(0);
            PopulationItem[] pop = this.Population.ToArray();

            if (exclude != null && Array.IndexOf(pop, exclude) >= 0)
            {
                pop = pop.Where((x, idx) => idx != Array.IndexOf(pop, exclude)).ToArray();
                //pop.Splice(Array.IndexOf(pop, exclude), 1);
            }

            double rand = _random.NextDouble();

            float lower = 0;
            float weight = 1 / (float)pop.Length;
            float upper = weight;

            for (int i = 0; i < pop.Length; i++)
            {
                // if the random number falls between lower and upper bounds, select this individual
                if (rand > lower && rand < upper)
                {
                    return pop[i];
                }
                lower = upper;
                upper += 2 * weight * ((pop.Length - i) / (float)pop.Length);
            }

            return pop[0];
        }

        // single point crossover
        public PopulationItem[] Mate(PopulationItem male, PopulationItem female)
        {
            int cutpoint = (int)Math.Round(Math.Min(Math.Max(_random.NextDouble(), 0.1), 0.9) * (male.Placements.Count - 1));

            List<NFP> gene1 = new List<NFP>(male.Placements.Take(cutpoint).ToArray());
            List<float> rot1 = new List<float>(male.Rotation.Take(cutpoint).ToArray());

            List<NFP> gene2 = new List<NFP>(female.Placements.Take(cutpoint).ToArray());
            List<float> rot2 = new List<float>(female.Rotation.Take(cutpoint).ToArray());

            int i = 0;

            for (i = 0; i < female.Placements.Count; i++)
            {
                if (!gene1.Any(z => z.Id == female.Placements[i].Id))
                {
                    gene1.Add(female.Placements[i]);
                    rot1.Add(female.Rotation[i]);
                }
            }

            for (i = 0; i < male.Placements.Count; i++)
            {
                if (!gene2.Any(z => z.Id == male.Placements[i].Id))
                {
                    gene2.Add(male.Placements[i]);
                    rot2.Add(male.Rotation[i]);
                }
            }
            PopulationItem child1 = new PopulationItem { Placements = gene1, Rotation = rot1.ToArray() };
            PopulationItem child2 = new PopulationItem { Placements = gene2, Rotation = rot2.ToArray() };

            RepairRotations(child1);
            RepairRotations(child2);

            return [child1, child2];
        }

        public void generation()
        {
            // Individuals with higher fitness are more likely to be selected for mating
            Population = Population.OrderBy(z => z.Fitness).ToList();

            // fittest individual is preserved in the new generation (elitism)
            List<PopulationItem> newpopulation = new List<PopulationItem>();
            newpopulation.Add(this.Population[0].Clone());
            while (newpopulation.Count() < this.Population.Count)
            {
                PopulationItem male = RandomWeightedIndividual();
                PopulationItem female = RandomWeightedIndividual(male);

                // each mating produces two children
                PopulationItem[] children = Mate(male, female);

                // slightly mutate children
                newpopulation.Add(this.Mutate(children[0]));

                if (newpopulation.Count < this.Population.Count)
                {
                    newpopulation.Add(this.Mutate(children[1]));
                }
            }

            this.Population = newpopulation;
        }
    }


}
