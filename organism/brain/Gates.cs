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
using Genome;

namespace Gates
{
    abstract class Gate
    {
        public static List<String> GateClasses = new List<String> { "deterministic", "probabilistic",
         "copy", "zero", "programming", "cellularAutomata", "simpleConditional", "complexConditional" };

        public int Priority;
        public int RegGroup;
        public List<int> InputIndexes;
        public List<int> OutputIndexes;

        public abstract void Execute(List<byte> memory);
    }

    class DeterministicGate : Gate
    {
        private List<byte> LUT;

        public DeterministicGate(ByteGenome genome, int startIndex, int memoryLength)
        {
            Priority = genome[startIndex + 2];
            RegGroup = genome[startIndex + 3] % 16;
            int NumInputs = (genome[startIndex + 4] % 3) + 1;
            int NumOutputs = (genome[startIndex + 5] % 3) + 1;
            InputIndexes = new List<int>(NumInputs);
            OutputIndexes = new List<int>(NumOutputs);
            int LutSize = ((int)Math.Pow(2, NumInputs)) * NumOutputs;
            LUT = new List<byte>(LutSize);


            int StartInput = (startIndex + 6);
            int EndInput = StartInput + NumInputs;
            int EndOutput = EndInput + NumOutputs;
            int EndLut = EndOutput + LutSize;

            for (int i = StartInput; i < EndInput; i++)
            {
                InputIndexes.Add(genome[i] % memoryLength);
            }
            for (int i = EndInput; i < EndOutput; i++)
            {
                OutputIndexes.Add(genome[i] % memoryLength);
            }
            for (int i = EndOutput; i < EndLut; i++)
            {
                LUT.Add(genome[i]);
            }
        }

        public override void Execute(List<byte> memory)
        {
            int Idx = 0;
            for (int i = 0; i < InputIndexes.Count; i++)
            {
                Idx |= memory[InputIndexes[i]];
            }
            Idx %= (int)Math.Pow(2, InputIndexes.Count);
            for (int i = 0; i < OutputIndexes.Count; i++)
            {
                memory[OutputIndexes[i]] |= LUT[(OutputIndexes.Count * Idx) + i];
            }
        }
    }

    class CopyGate : Gate
    {
        public CopyGate(ByteGenome genome, int startIndex, int memoryLength)
        {
            Priority = genome[startIndex + 2];
            RegGroup = genome[startIndex + 3] % 16;
            int NumNodes = (genome[startIndex + 4] % 3) + 1; // Always need at least one node
            InputIndexes = new List<int>(NumNodes);
            OutputIndexes = new List<int>(NumNodes);
            for (int i = 0; i < NumNodes; i++)
            {
                InputIndexes.Add(genome[startIndex + 5 + i] % memoryLength);
                OutputIndexes.Add(genome[startIndex + 5 + NumNodes + i] % memoryLength);
            }
        }

        public override void Execute(List<byte> memory)
        {
            for (int i = 0; i < InputIndexes.Count; i++)
            {
                memory[OutputIndexes[i]] = memory[InputIndexes[i]];
            }
        }
    }

    class ZeroGate : Gate
    {
        public ZeroGate(ByteGenome genome, int startIndex, int memoryLength)
        {
            Priority = genome[startIndex + 2];
            RegGroup = genome[startIndex + 3] % 16;
            int NumGates = (genome[startIndex + 4] % 3) + 1;
            OutputIndexes = new List<int>();
            for (int i = 0; i < NumGates; i++)
            {
                OutputIndexes.Add(genome[startIndex + 5 + i] % memoryLength);
            }
        }

        public override void Execute(List<byte> memory)
        {
            for (int i = 0; i < OutputIndexes.Count; i++)
            {
                memory[OutputIndexes[i]] = (byte)0;
            }
        }

    }

    class SimpleConditionalGate : Gate
    { // Input 0 LT|EQ fixed cutoff value, input 1, otherwise, input 2.
        private byte Cutoff;
        public SimpleConditionalGate(ByteGenome genome, int startIndex, int memoryLength)
        {
            Priority = genome[startIndex + 2];
            RegGroup = genome[startIndex + 3] % 16;
            Cutoff = genome[startIndex + 4];
            InputIndexes = new List<int> {genome[startIndex + 5] % memoryLength,
                                          genome[startIndex + 6] % memoryLength,
                                          genome[startIndex + 7] % memoryLength};
            OutputIndexes = new List<int> { genome[startIndex + 8] % memoryLength };
        }

        public override void Execute(List<byte> memory)
        {
            if (memory[InputIndexes[0]] <= Cutoff)
            {
                memory[OutputIndexes[0]] = memory[InputIndexes[1]];
            }
            else
            {
                memory[OutputIndexes[0]] = memory[InputIndexes[2]];
            }
        }
    }

    class ComplexConditionalGate : Gate
    { // Input 0 LT|EQ input 1, input 2, else, input 3.
        public ComplexConditionalGate(ByteGenome genome, int startIndex, int memoryLength)
        {
            Priority = genome[startIndex + 2];
            RegGroup = genome[startIndex + 3] % 16;
            InputIndexes = new List<int> {genome[startIndex + 4] % memoryLength,
                                          genome[startIndex + 5] % memoryLength,
                                          genome[startIndex + 6] % memoryLength,
                                          genome[startIndex + 7] % memoryLength};
            OutputIndexes = new List<int> { genome[startIndex + 8] % memoryLength };
        }

        public override void Execute(List<byte> memory)
        {
            if (memory[InputIndexes[0]] <= memory[InputIndexes[1]])
            {
                memory[OutputIndexes[0]] = memory[InputIndexes[2]];
            }
            else
            {
                memory[OutputIndexes[0]] = memory[InputIndexes[3]];
            }
        }
    }

    class ProgrammingGate : Gate
    {
        int Operation;
        public ProgrammingGate(ByteGenome genome, int startIndex, int memoryLength)
        {
            Priority = genome[startIndex + 2];
            RegGroup = genome[startIndex + 3];
            Operation = genome[startIndex + 4] % 4;
            InputIndexes = new List<int> {genome[startIndex + 4] % memoryLength,
                                          genome[startIndex + 5] % memoryLength};
            OutputIndexes = new List<int> { genome[startIndex + 6] % memoryLength };
        }
        public override void Execute(List<byte> memory)
        {
            switch (Operation)
            {
                case 0:
                    memory[OutputIndexes[0]] = (byte)(memory[InputIndexes[0]] + memory[InputIndexes[1]]);
                    break;
                case 1:
                    memory[OutputIndexes[0]] = (byte)(memory[InputIndexes[0]] - memory[InputIndexes[1]]);
                    break;
                case 2:
                    memory[OutputIndexes[0]] = (byte)(memory[InputIndexes[0]] << memory[InputIndexes[1]]);
                    break;
                case 3:
                    memory[OutputIndexes[0]] = (byte)(memory[InputIndexes[0]] >> memory[InputIndexes[1]]);
                    break;
            }
        }
    }

}