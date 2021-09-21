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
using System.Linq;
using System.Collections.Generic;
using MathNet.Numerics.Distributions;
using JSFDN;


namespace Genome
{
    class ByteGenome
    {
        public List<byte> Genome { get; private set; }

        public byte this[int index]
        { // ByteGenome is modeled as a circular genome. Eg. for g = [0, 0, 0]; g[5] = 3; then g = [0, 0, 3]
            get => Genome[index % Genome.Count];
            set => Genome[index % Genome.Count] = value;
        }
        public ByteGenome(List<byte> genomeBytes)
        {
            Genome = genomeBytes;
        }

        public ByteGenome(string genomeString)
        {
            string[] ByteStrings = genomeString.Split(',');
            Genome = new List<byte>(genomeString.Length / 3); // Approx capacity, see the string: 125,211,15,133,250,2,75 etc.
            for (int i = 0; i < ByteStrings.Length; i++)
            {
                Genome.Add(Convert.ToByte(ByteStrings[i]));
            }
        }

        public static ByteGenome RandByteGenome(int len, int numGates, JSFRng rng)
        {
            byte[] Genome = new byte[len];
            rng.NextBytes(Genome);
            for (int i = 0; i < numGates; i++)
            {
                int idx = rng.Next(len - 2);
                Genome[idx] = 42;
                Genome[idx + 1] = (byte)rng.Next(5);
            }
            return (new ByteGenome(Genome.ToList()));
        }

        public ByteGenome Clone()
        {
            return (new ByteGenome(Genome.ToList()));
        }

        public void SetByte(int idx, byte b)
        {
            Genome[idx] = b;
        }

        public void InsertSequence(int idx, byte[] sequence)
        {
            Genome.InsertRange(idx, sequence);
        }

        public void DeleteSequence(int idx, int amount)
        { // Consider a branch to use removeRange when possible, as   
            for (int i = 0; i < amount; i++)
            {            // it probably reverses the removals to reduce copies
                if (idx < Genome.Count)
                {
                    Genome.RemoveAt(idx); // RemoveAt nukes the current idx , then everying slides down to fill in the gap
                }
                else
                { // If we've removed all the way to the end of the genome, go to the beginning and keep removing
                    Genome.RemoveAt(0);
                }
            }
        }

        public void ReverseSequence(int idx, int amount)
        {
            if (idx + amount < Genome.Count)
            {
                Genome.Reverse(idx, amount);
            }
            else
            {
                List<byte> Buffer = getBuffer(idx, amount);
                Buffer.Reverse();
                for (int i = idx; i < (idx + amount); i++)
                {
                    Genome[i % Genome.Count] = Buffer[i - idx];
                }
            }
        }

        public void invertSequence(int idx, int amount)
        {
            for (int i = idx; i < amount; i++)
            {
                Genome[i] = (byte)~Genome[i];
            }
        }

        public void duplicateSequence(int idx, int amount, int insertIdx)
        {
            if (idx + amount < Genome.Count)
            {
                Genome.InsertRange(insertIdx, Genome.GetRange(idx, amount));
            }
            else
            {
                List<byte> buffer = getBuffer(idx, amount);
                Genome.InsertRange(insertIdx, buffer);
            }

        }

        private List<byte> getBuffer(int idx, int amount)
        {
            List<byte> buffer = new List<byte>();
            for (int i = idx; i < (idx + amount); i++)
            {
                buffer.Add(Genome[i % Genome.Count]);
            }
            return (buffer);
        }

        public int getCount()
        {
            return (Genome.Count);
        }
    }

    class Mutator
    {
        // "Classical" biological mutation rates
        public double PointRatePerGenome { get; }
        public double InsertionRatePerGenome { get; }

        public double InsertionRelSize { get; }
        public double DeletionRatePerGenome { get; }
        public double DeletionRelSize { get; }
        public double InversionRatePerGenome { get; }
        public double InversionRelSize { get; }
        public double ReversalRatePerGenome { get; }
        public double ReversalRelSize { get; }
        public double DuplicationRatePerGenome { get; }
        public double DuplicationRelSize { get; }

        // "Computational" mutation rates
        public double SwapRatePerGenome { get; }
        public double IncrementRatePerGenome { get; }
        public double DecrementRatePerGenome { get; }

        public double CrossoverRate { get; }

        public Mutator(Dictionary<string, string> mutationRateDict)
        {
            PointRatePerGenome = Math.Clamp(Convert.ToDouble(mutationRateDict["pointRatePerGenome"]), 0.0, 1000.0);
            InsertionRatePerGenome = Math.Clamp(Convert.ToDouble(mutationRateDict["insertionRatePerGenome"]), 0.0, 1000.0);
            InsertionRelSize = Math.Clamp(Convert.ToDouble(mutationRateDict["insertionRelSize"]), 0.0, 1.0);
            DeletionRatePerGenome = Math.Clamp(Convert.ToDouble(mutationRateDict["deletionRatePerGenome"]), 0.0, 1000.0);
            DeletionRelSize = Math.Clamp(Convert.ToDouble(mutationRateDict["deletionRelSize"]), 0.0, 1.0);
            InversionRatePerGenome = Math.Clamp(Convert.ToDouble(mutationRateDict["inversionRatePerGenome"]), 0.0, 1000.0);
            InversionRelSize = Math.Clamp(Convert.ToDouble(mutationRateDict["inversionRelSize"]), 0.0, 1.0);
            ReversalRatePerGenome = Math.Clamp(Convert.ToDouble(mutationRateDict["reversalRatePerGenome"]), 0.0, 1000.0);
            ReversalRelSize = Math.Clamp(Convert.ToDouble(mutationRateDict["reversalRelSize"]), 0.0, 1.0);
            DuplicationRatePerGenome = Math.Clamp(Convert.ToDouble(mutationRateDict["duplicationRatePerGenome"]), 0.0, 1000.0);
            DuplicationRelSize = Math.Clamp(Convert.ToDouble(mutationRateDict["duplicationRelSize"]), 0.0, 1.0);

            SwapRatePerGenome = Math.Clamp(Convert.ToDouble(mutationRateDict["swapRatePerGenome"]), 0.0, 1000.0);
            IncrementRatePerGenome = Math.Clamp(Convert.ToDouble(mutationRateDict["incrementRatePerGenome"]), 0.0, 1000.0);
            DecrementRatePerGenome = Math.Clamp(Convert.ToDouble(mutationRateDict["decrementRatePerGenome"]), 0.0, 1000.0);

            CrossoverRate = Math.Clamp(Convert.ToDouble(mutationRateDict["crossoverRate"]), 0.0, 1000.0);
        }

        public void MutateGenome(ByteGenome genome, JSFRng rng)
        {
            int NumPtMuts = Poisson.Sample(rng, this.PointRatePerGenome);
            int NumInsertions = Poisson.Sample(rng, this.InsertionRatePerGenome);
            int NumDeletions = Poisson.Sample(rng, this.DeletionRatePerGenome);
            int NumInversions = Poisson.Sample(rng, this.InversionRatePerGenome);
            int NumReversal = Poisson.Sample(rng, this.ReversalRatePerGenome);
            int NumDuplications = Poisson.Sample(rng, this.DuplicationRatePerGenome);
            int NumSwaps = Poisson.Sample(rng, this.SwapRatePerGenome);
            int NumIncrements = Poisson.Sample(rng, this.IncrementRatePerGenome);
            int NumDecrements = Poisson.Sample(rng, this.DecrementRatePerGenome);

            PointMutation(genome, NumPtMuts, rng);
            Insertion(genome, NumInsertions, rng);
            Deletion(genome, NumDeletions, rng);
            Inversion(genome, NumInversions, rng);
            Reversal(genome, NumReversal, rng);
            Duplication(genome, NumDuplications, rng);
            Swap(genome, NumSwaps, rng);
            Increment(genome, NumIncrements, rng);
            Decrement(genome, NumDecrements, rng);
        }

        private void PointMutation(ByteGenome genome, int numPtMuts, JSFRng rng)
        {
            for (int i = 0; i < numPtMuts; i++)
            {
                int Idx = rng.Next(genome.getCount());
                genome.SetByte(Idx, (byte)rng.Next(256));
            }
        }

        private void Insertion(ByteGenome genome, int numInsertions, JSFRng rng)
        {
            for (int i = 0; i < numInsertions; i++)
            {
                int InsertionIdx = rng.Next(genome.getCount());
                byte[] InsertedSeq = new byte[1 + rng.Next((int)(genome.getCount() * InsertionRelSize))];
                rng.NextBytes(InsertedSeq);
                genome.InsertSequence(InsertionIdx, InsertedSeq);
            }
        }

        private void Deletion(ByteGenome genome, int numDeletions, JSFRng rng)
        {
            for (int i = 0; i < numDeletions; i++)
            {
                int DeletionIdx = rng.Next(genome.getCount());
                int DeletionLength = rng.Next(((int)(genome.getCount() * DeletionRelSize)));
                genome.DeleteSequence(DeletionIdx, DeletionLength);
            }
        }

        private void Inversion(ByteGenome genome, int numInversions, JSFRng rng)
        {
            for (int i = 0; i < numInversions; i++)
            {
                int InversionIdx = rng.Next(genome.getCount());
                int InversionLength = rng.Next((int)(genome.getCount() * InversionRelSize));
                genome.invertSequence(InversionIdx, InversionLength);
            }
        }

        private void Reversal(ByteGenome genome, int numReversals, JSFRng rng)
        {
            for (int i = 0; i < numReversals; i++)
            {
                int ReversalIdx = rng.Next(genome.getCount());
                int ReversalLength = rng.Next((int)(genome.getCount() * ReversalRelSize));
                genome.ReverseSequence(ReversalIdx, ReversalLength);
            }
        }

        private void Duplication(ByteGenome genome, int numDuplications, JSFRng rng)
        {
            for (int i = 0; i < numDuplications; i++)
            {
                int DuplicationIdx = rng.Next(genome.getCount());
                int DuplicationLength = rng.Next((int)(genome.getCount() * DuplicationRelSize));
                int DuplicationInsertIdx = rng.Next(genome.getCount());
                genome.duplicateSequence(DuplicationIdx, DuplicationLength, DuplicationInsertIdx);
            }
        }

        private void Swap(ByteGenome genome, int numSwaps, JSFRng rng)
        {
            for (int i = 0; i < numSwaps; i++)
            {
                int Idx1 = rng.Next(genome.getCount());
                int Idx2 = rng.Next(genome.getCount());
                byte Temp = genome.Genome[Idx1];
                genome.SetByte(Idx1, genome.Genome[Idx2]);
                genome.SetByte(Idx2, Temp);
            }
        }

        private void Increment(ByteGenome genome, int numIncrements, JSFRng rng)
        {
            for (int i = 0; i < numIncrements; i++)
            {
                int IncrementIdx = rng.Next(genome.getCount());
                genome.SetByte(IncrementIdx, (byte)(genome.Genome[IncrementIdx] + 1));
            }
        }

        private void Decrement(ByteGenome genome, int numDecrements, JSFRng rng)
        {
            for (int i = 0; i < numDecrements; i++)
            {
                int DecrementIdx = rng.Next(genome.getCount());
                genome.SetByte(DecrementIdx, (byte)(genome.Genome[DecrementIdx] - 1));
            }
        }

        public ByteGenome sexualReproduction(ByteGenome parent1, ByteGenome parent2, JSFRng rng)
        {
            int NumCrossovers = Poisson.Sample(rng, this.CrossoverRate) + 1; // During sexrepro always at least one crossover
            int MinLength = Math.Min(parent1.getCount(), parent2.getCount());
            HashSet<int> CrossoverPoints = new HashSet<int>((int)this.CrossoverRate * 3); // A sane default that will not re-allocate 99% of the time
            for (int i = 0; i < NumCrossovers; i++)
            {
                int RandIdx = rng.Next(MinLength);
                CrossoverPoints.Add(RandIdx);
            }

            List<byte> Offpsring = new List<byte>(parent1.getCount());
            bool CopyingParent1 = true;
            int j = 0;
            while (true)
            {
                if (CrossoverPoints.Contains(j))
                {
                    CopyingParent1 = !CopyingParent1;
                }

                // terminate when we reach the end of the current strand
                // we intentionally do not switch to the other strand (the idx would be valid due to  conditional),
                // as that adds an additional crossover point and prevents genome shrinkage due to sexrepro
                if (CopyingParent1 && j >= parent1.getCount())
                {
                    break;
                }
                else if (!CopyingParent1 && j >= parent2.getCount())
                {
                    break;
                }

                if (CopyingParent1)
                {
                    Offpsring.Add(parent1.Genome[j]);
                }
                else
                {
                    Offpsring.Add(parent2.Genome[j]);
                }
                j += 1;
            }
            return (new ByteGenome(Offpsring));
        }
    }
}
