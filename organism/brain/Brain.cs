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
using Gates;
using Genome;

namespace BrainNS
{
    class Brain
    {
        public List<byte> Memory { get; set; }
        public List<Gate> Gates { get; private set; }

        public Dictionary<String, float> GateCounts { get; private set; }

        public Brain(int memoryLength, ByteGenome genome)
        {
            Memory = new byte[memoryLength].ToList<byte>();
            Gates = new List<Gate>();
            GateCounts = Gate.GateClasses.ToDictionary(name => name, _ => 0.0f);
            BuildGateList(genome, memoryLength);
        }

        public Dictionary<String, float> GetGateCounts()
        {
            return (new Dictionary<String, float>(GateCounts)); // A copy to avoid issues w/ rebuild. Shallow copy fine for value types.
        }

        public void Execute(int updates)
        {
            for (int i = 0; i < updates; i++)
            {
                for (int j = 0; j < Gates.Count; j++)
                {
                    Gates[j].Execute(Memory);
                }
            }
        }

        private void BuildGateList(ByteGenome genome, int memoryLength)
        {
            for (int i = 0; i < genome.getCount(); i++)
            {
                if (genome[i] == 42)
                {
                    switch (genome[i + 1])
                    {
                        case 0:
                            Gates.Add(new ZeroGate(genome, i, memoryLength));
                            GateCounts["zero"] += 1;
                            break;
                        case 1:
                            Gates.Add(new CopyGate(genome, i, memoryLength));
                            GateCounts["copy"] += 1;
                            break;
                        case 2:
                            Gates.Add(new DeterministicGate(genome, i, memoryLength));
                            GateCounts["deterministic"] += 1;
                            break;
                        case 3:
                            Gates.Add(new SimpleConditionalGate(genome, i, memoryLength));
                            GateCounts["simpleConditional"] += 1;
                            break;
                        case 4:
                            Gates.Add(new ComplexConditionalGate(genome, i, memoryLength));
                            GateCounts["complexConditional"] += 1;
                            break;
                        case 5:
                            Gates.Add(new ProgrammingGate(genome, i, memoryLength));
                            GateCounts["programming"] += 1;
                            break;
                        default:
                            break;
                    }
                }
            }
            Gates.Sort((x, y) => x.Priority.CompareTo(y.Priority));
        }

        public void ZeroMemory()
        {
            for (int i = 0; i < Memory.Count; i++)
            {
                Memory[i] = 0;
            }
        }

        public void Rebuild(ByteGenome genome)
        {
            Gates.Clear();
            ZeroMemory();
            foreach (string key in GateCounts.Keys)
            {
                GateCounts[key] = 0.0f;
            }
            BuildGateList(genome, Memory.Count);
        }

        public int GetMemoryLength()
        {
            return (Memory.Count);
        }

        public int GetNumGates()
        {
            return (Gates.Count);
        }

    }
}