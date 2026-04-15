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
        SvgNestConfig Config;

        public List<PopulationItem> Population;

        public static bool StrictAngles = false;

        float[] DefaultAngles = [0, 0, 90, 0, 0, 270, 180, 180, 180, 90];

        Random R = new();

        public GeneticAlgorithm(NFP[] adam, SvgNestConfig config, CancellationToken cancellationToken)
        {
            List<float> ang2 = new List<float>();
            for (int i = 0; i < adam.Length; i++)
            {
                ang2.Add((i * 90) % 360);
            }
            DefaultAngles = ang2.ToArray();
            Config = config;

            List<float> angles = new List<float>();
            for (int i = 0; i < adam.Length; i++)
            {
                if (StrictAngles)
                {
                    angles.Add(DefaultAngles[i]);
                }
                else
                {
                    float angle = RotationHelpers.GetRandomAllowedRotation(adam[i]);
                    //float angle = (float)Math.Floor(R.NextDouble() * Config.Rotations) * (360f / Config.Rotations);
                    angles.Add(angle);
                }

            }
            Population = [new PopulationItem() { placements = adam.ToList(), Rotation = angles.ToArray() }];
            while (Population.Count < config.PopulationSize)
            {
                cancellationToken.ThrowIfCancellationRequested();
                PopulationItem mutant = this.Mutate(Population[0]);
                Population.Add(mutant);
            }
        }


        public PopulationItem Mutate(PopulationItem p)
        {
            PopulationItem clone = new PopulationItem();

            clone.placements = p.placements.ToArray().ToList();
            clone.Rotation = p.Rotation.Clone() as float[];
            for (int i = 0; i < clone.placements.Count(); i++)
            {
                double rand = R.NextDouble();
                if (rand < 0.01 * Config.MutationRate)
                {
                    int j = i + 1;
                    if (j < clone.placements.Count)
                    {
                        NFP temp = clone.placements[i];
                        clone.placements[i] = clone.placements[j];
                        clone.placements[j] = temp;
                    }
                }
                rand = R.NextDouble();
                if (rand < 0.01 * Config.MutationRate)
                {
                    clone.Rotation[i] = RotationHelpers.GetRandomAllowedRotation(clone.placements[i]); //(float)Math.Floor(R.NextDouble() * Config.Rotations) * (360f / Config.Rotations);
                }
            }


            return clone;
        }

        public float[] ShuffleArray(float[] array)
        {
            for (int i = array.Length - 1; i > 0; i--)
            {
                int j = (int)Math.Floor(R.NextDouble() * (i + 1));
                float temp = array[i];
                array[i] = array[j];
                array[j] = temp;
            }
            return array;
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

            double rand = R.NextDouble();

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
            int cutpoint = (int)Math.Round(Math.Min(Math.Max(R.NextDouble(), 0.1), 0.9) * (male.placements.Count - 1));

            List<NFP> gene1 = new List<NFP>(male.placements.Take(cutpoint).ToArray());
            List<float> rot1 = new List<float>(male.Rotation.Take(cutpoint).ToArray());

            List<NFP> gene2 = new List<NFP>(female.placements.Take(cutpoint).ToArray());
            List<float> rot2 = new List<float>(female.Rotation.Take(cutpoint).ToArray());

            int i = 0;

            for (i = 0; i < female.placements.Count; i++)
            {
                if (!gene1.Any(z => z.id == female.placements[i].id))
                {
                    gene1.Add(female.placements[i]);
                    rot1.Add(female.Rotation[i]);
                }
            }

            for (i = 0; i < male.placements.Count; i++)
            {
                if (!gene2.Any(z => z.id == male.placements[i].id))
                {
                    gene2.Add(male.placements[i]);
                    rot2.Add(male.Rotation[i]);
                }
            }

            return new[] {new  PopulationItem() {
                placements= gene1, Rotation= rot1.ToArray()},
                new PopulationItem(){ placements= gene2, Rotation= rot2.ToArray()}};
        }

        public void generation()
        {
            // Individuals with higher fitness are more likely to be selected for mating
            Population = Population.OrderBy(z => z.fitness).ToList();

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
