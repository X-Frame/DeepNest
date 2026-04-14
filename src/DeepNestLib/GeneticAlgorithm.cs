using DeepNestLib.Rotation;
using DeepNestLib.Svg;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace DeepNestLib
{
    public class GeneticAlgorithm
    {
        public SvgNestConfig Config { get; private set; }

        public List<PopulationItem> Population { get; private set; } = [];
        private readonly Random _random = new();

        public GeneticAlgorithm(NFP[] adam, SvgNestConfig config, CancellationToken cancellationToken)
        {
            Config = config;
            Population = new List<PopulationItem>();
            for (int i = 0; i < config.PopulationSize; i++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                Population.Add(CreateIndividual(adam));
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

            return new PopulationItem(placements, [.. rotations], [.. constraints]);
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

        /// <summary>
        ///// Main evolution step - called each generation
        ///// </summary>
        //public void Evolve(CancellationToken cancellationToken)
        //{
        //    if (Population.Count == 0)
        //    {
        //        return;
        //    }

        //    // Sort by fitness (lower is better)
        //    Population = Population.OrderBy(p => p.Fitness).ToList();

        //    List<PopulationItem> newPopulation = new List<PopulationItem>();

        //    // Elitism: keep the best individuals
        //    int eliteCount = Math.Max(1, (int)(Config.PopulationSize * 0.1));
        //    for (int i = 0; i < eliteCount && i < Population.Count; i++)
        //    {
        //        newPopulation.Add(Population[i].Clone());
        //    }

        //    // Breed the rest
        //    while (newPopulation.Count < Config.PopulationSize)
        //    {
        //        cancellationToken.ThrowIfCancellationRequested();

        //        PopulationItem parent1 = SelectParent();
        //        PopulationItem parent2 = SelectParent();

        //        PopulationItem child = Crossover(parent1, parent2);
        //        Mutate(child, Config.MutationRate);

        //        newPopulation.Add(child);
        //    }

        //    Population = newPopulation;
        //}

        //private PopulationItem SelectParent()
        //{
        //    // Tournament selection (simple & effective)
        //    int tournamentSize = 5;
        //    PopulationItem best = null;

        //    for (int i = 0; i < tournamentSize; i++)
        //    {
        //        PopulationItem candidate = Population[_random.Next(Population.Count)];
        //        if (best == null || candidate.Fitness < best.Fitness)
        //        {
        //            best = candidate;
        //        }
        //    }
        //    return best;
        //}

        //private PopulationItem Crossover(PopulationItem parent1, PopulationItem parent2)
        //{
        //    int count = parent1.placements.Count;
        //    List<NFP> placements = new List<NFP>(count);
        //    List<float> rotations = new List<float>(count);
        //    List<RotationConstraint> constraints = new List<RotationConstraint>(count);

        //    for (int i = 0; i < count; i++)
        //    {
        //        // Randomly choose rotation from one of the parents (respecting constraint)
        //        bool takeFromParent1 = _random.NextDouble() < 0.5;
        //        PopulationItem source = takeFromParent1 ? parent1 : parent2;

        //        float angle = source.Rotation[i];
        //        RotationConstraint constraint = source.RotationConstraints[i];

        //        NFP original = source.placements[i]; // Note: this is already rotated, but we re-apply for safety

        //        NFP childPart = NestingService.RotatePolygon(original, angle);
        //        childPart.rotation = angle;
        //        childPart.RotationConstraint = constraint;
        //        childPart.source = original.source;
        //        childPart.Id = original.Id;

        //        placements.Add(childPart);
        //        rotations.Add(angle);
        //        constraints.Add(constraint);
        //    }

        //    return new PopulationItem(placements, rotations.ToArray(), constraints.ToArray());
        //}

        //private void Mutate(PopulationItem individual, double mutationRate)
        //{
        //    for (int i = 0; i < individual.placements.Count; i++)
        //    {
        //        if (_random.NextDouble() < mutationRate)
        //        {
        //            NFP part = individual.placements[i];
        //            float newAngle = ChooseRandomLegalRotation(part, Config);

        //            // Re-rotate with new legal angle
        //            NFP mutated = NestingService.RotatePolygon(part, newAngle);
        //            mutated.rotation = newAngle;
        //            mutated.RotationConstraint = part.RotationConstraint;
        //            mutated.source = part.source;
        //            mutated.Id = part.Id;

        //            individual.placements[i] = mutated;
        //            individual.Rotation[i] = newAngle;
        //            // RotationConstraints[i] stays the same (constraint doesn't change)
        //        }
        //    }
        //}

        //public PopulationItem GetBest() => Population.OrderBy(p => p.Fitness).FirstOrDefault();


        public PopulationItem Mutate(PopulationItem p)
        {
            PopulationItem clone = p.Clone();

            for (int i = 0; i < clone.Placements.Count(); i++)
            {
                if (_random.NextDouble() < 0.01 * Config.MutationRate)
                {
                    int j = (i + 1) % clone.Placements.Count;
                    (clone.Placements[i], clone.Placements[j]) = (clone.Placements[j], clone.Placements[i]);
                }
                if (_random.NextDouble() < 0.01 * Config.MutationRate)
                {
                    float newAngle = ChooseRandomLegalRotation(clone.Placements[i]);
                    NFP mutated = NestingService.RotatePolygon(clone.Placements[i], newAngle);
                    mutated.Rotation = newAngle;
                    mutated.RotationConstraint = clone.Placements[i].RotationConstraint;
                    mutated.Source = clone.Placements[i].Source;
                    mutated.Id = clone.Placements[i].Id;

                    clone.Placements[i] = mutated;
                    clone.Rotation[i] = newAngle;
                }
            }
            return clone;
        }

        // returns a random individual from the population, weighted to the front of the list (lower fitness value is more likely to be selected)
        public PopulationItem RandomWeightedIndividual(PopulationItem exclude = null)
        {
            //var pop = this.population.slice(0);
            PopulationItem[] pop = this.Population.ToArray();

            if (exclude != null && Array.IndexOf(pop, exclude) >= 0)
            {
                pop.Splice(Array.IndexOf(pop, exclude), 1);
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
            List<RotationConstraint> con1 = new List<RotationConstraint>(male.RotationConstraints.Take(cutpoint));

            List<NFP> gene2 = new List<NFP>(female.Placements.Take(cutpoint).ToArray());
            List<float> rot2 = new List<float>(female.Rotation.Take(cutpoint).ToArray());
            List<RotationConstraint> con2 = new List<RotationConstraint>(female.RotationConstraints.Take(cutpoint));

            int i = 0;

            for (i = 0; i < female.Placements.Count; i++)
            {
                if (!gene1.Any(z => z.Id == female.Placements[i].Id))
                {
                    gene1.Add(female.Placements[i]);
                    rot1.Add(female.Rotation[i]);
                    con1.Add(female.RotationConstraints[i]);
                }
            }

            for (i = 0; i < male.Placements.Count; i++)
            {
                if (!gene2.Any(z => z.Id == male.Placements[i].Id))
                {
                    gene2.Add(male.Placements[i]);
                    rot2.Add(male.Rotation[i]);
                    con2.Add(male.RotationConstraints[i]);
                }
            }

            return new[] {new  PopulationItem() {
                Placements= gene1, Rotation= rot1.ToArray(), RotationConstraints = con1.ToArray()},
                new PopulationItem(){ Placements= gene2, Rotation= rot2.ToArray(), RotationConstraints = con2.ToArray()}};
        }

        public void generation()
        {
            // Individuals with higher fitness are more likely to be selected for mating
            Population = Population.OrderBy(z => z.Fitness).ToList();

            // fittest individual is preserved in the new generation (elitism)
            List<PopulationItem> newpopulation = new List<PopulationItem>();
            newpopulation.Add(this.Population[0]);
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
