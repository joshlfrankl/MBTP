// Copyright (C) 2021 Joshua Franklin

// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.

// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.

// You should have received a copy of the GNU General Public License
// along with this program.  If not, see <https://www.gnu.org/licenses/>.

using System;
using System.Collections.Generic;
using BrainNS;
using Genome;
using Global;
using JSFDN;

namespace Organism
{

    class TaskOrganism
    {
        private JSFRng Rng;
        private ByteGenome Genome;
        private Brain Brain;

        private double Fitness;

        private string Stats;

        public static List<String> StatKeys = new List<String> { "GenomeLength", "BrainSize", "Fitness" };
        public TaskOrganism(int genomeLength, int randomSeed)
        {
            this.Rng = new JSFRng(randomSeed);
            this.Genome = ByteGenome.RandByteGenome(genomeLength, Convert.ToInt32(Configuration.Config["seedGates"]), Rng);
            this.Brain = new Brain(Convert.ToInt32(Configuration.Config["memorySize"]), this.Genome);
            this.Fitness = 0.0;
            this.Stats = "";
        }

        public TaskOrganism(TaskOrganism parent, Mutator mutator, int randomSeed)
        {
            this.Rng = new JSFRng(randomSeed);
            this.Genome = parent.Genome.Clone();
            mutator.MutateGenome(this.Genome, this.Rng);
            this.Brain = new Brain(Convert.ToInt32(Configuration.Config["memorySize"]), this.Genome);
            this.Fitness = 0.0;
            this.Stats = "";
        }

        public TaskOrganism(ByteGenome g, string rngState)
        { // For seeding from DB with rng state string
            this.Rng = new JSFRng(rngState);
            this.Genome = g.Clone();
            this.Brain = new Brain(Convert.ToInt32(Configuration.Config["memorySize"]), this.Genome);
            this.Fitness = 0.0;
            this.Stats = "";
        }

        public TaskOrganism(ByteGenome g, int randomSeed)
        {
            this.Rng = new JSFRng(randomSeed);
            this.Genome = g.Clone();
            this.Brain = new Brain(Convert.ToInt32(Configuration.Config["memorySize"]), this.Genome);
            this.Fitness = 0.0;
            this.Stats = "";
        }

        public void Run(int updates)
        {
            Brain.Execute(updates);
        }

        public TaskOrganism Reproduce(Mutator mutator)
        {
            return (new TaskOrganism(this, mutator, this.Rng.Next()));
        }

        public TaskOrganism DeepCopy()
        {
            return (new TaskOrganism(this.Genome, this.Rng.Next()));
        }

        public void copyInto(TaskOrganism target, Mutator mutator)
        {
            target.Genome = this.Genome.Clone();
            mutator.MutateGenome(target.Genome, target.Rng);
            target.Brain.Rebuild(target.Genome);
            target.Fitness = 0.0;
            target.Stats = "";
        }

        public void setRng(int seed)
        {
            Rng = new JSFRng(seed);
        }

        public string getRngState()
        {
            return (Rng.ExportStateString());
        }

                public int GetNext(int upperBound)
        {
            return (Rng.Next(upperBound));
        }

        public int GetNext()
        {
            return (Rng.Next());
        }

        public double GetDouble()
        {
            return (Rng.NextDouble());
        }

        public ByteGenome GetGenome()
        {
            return this.Genome;
        }

        public float GetGenomeLength()
        {
            return this.Genome.getCount();
        }

        public float GetBrainSize()
        {
            return (Brain.GetNumGates());
        }

        public byte GetMemory(int idx)
        {
            return (Brain.Memory[idx]);
        }

        public void SetMemory(int idx, byte b)
        {
            Brain.Memory[idx] = b;
        }

        public int GetMemoryLength()
        {
            return (Brain.GetMemoryLength());
        }

        public string GetStats()
        {
            return (Stats);
        }

        public void SetStats(string st) {
            Stats = st;
        }

        public void SetFitness(double f)
        {
            this.Fitness = f;
        }

        public double GetFitness()
        {
            return (this.Fitness);
        }

        public void ZeroMemory()
        {
            Brain.ZeroMemory();
        }
    }
}