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
using System.Diagnostics;
using YamlDotNet.Serialization;
using PopulationNS;
using Organism;
using Genome;
using AuditorNS;
using Tasks;
using Global;

namespace MBTP
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine($"Executing from {AppDomain.CurrentDomain.BaseDirectory}");
            string ConfigString = System.IO.File.ReadAllText(@"./settings.yaml");
            var Deserializer = new DeserializerBuilder().Build();
            Dictionary<string, string> Config = Deserializer.Deserialize<Dictionary<string, string>>(ConfigString);
            Config["UUID"] = System.Guid.NewGuid().ToString();

            Configuration.Config = Config;

            RunExperimentFromScratch(Convert.ToBoolean(Configuration.Config["loadPopulationFromFile?"]));
        }

        static void RunExperimentFromScratch(bool loadPopulation)
        {
            Console.WriteLine("Beginning simulation using the following settings:\n");
            Configuration.Print();
            Console.WriteLine("\n");

            int Seed = 0;

            if (Convert.ToInt32(Configuration.Config["randomSeed"]) == 0)
            {
                Random Rng = new Random();
                Seed = Rng.Next();
            }
            else
            {
                Seed = Convert.ToInt32(Configuration.Config["randomSeed"]);
            }

            Mutator Mutator = new Mutator(Configuration.Config);
            int PopulationSize = Convert.ToInt32(Configuration.Config["populationSize"]);

            Stopwatch CodeTimer = new Stopwatch();
            TaskPopulation p;
            CodeTimer.Start();
            if (loadPopulation)
            {
                Console.WriteLine(String.Format("Loading population from {0}.", Configuration.Config["loadPopulationPath"]));
                p = new TaskPopulation(Configuration.Config["loadPopulationPath"], Mutator);
            }
            else
            {
                Console.WriteLine(String.Format($"Initializing new random population with seed: {Seed}"));
                p = new TaskPopulation(PopulationSize, Convert.ToInt32(Configuration.Config["genomeLength"]), Mutator, Seed);
            }

            CodeTimer.Stop();
            TimeSpan RunTime = CodeTimer.Elapsed;
            string RunTimeString = String.Format("Population initialization took: {0:F4}s", RunTime.TotalSeconds);
            Console.WriteLine(RunTimeString);

            Action<TaskOrganism, string, bool> theTask = MoveTask.Run;

            int NumGenerations = Convert.ToInt32(Configuration.Config["generations"]);
            Auditor PopAuditor = new Auditor(p, String.Format("Data Source={0}", Configuration.Config["DBPath"]), theTask);

            CodeTimer.Reset();
            CodeTimer.Start();
            Stopwatch LoopTimer = new Stopwatch();

            while (p.Generation < NumGenerations)
            {
                LoopTimer.Start();
                p.Tick(theTask);
                LoopTimer.Stop();
                double TickTime = LoopTimer.Elapsed.TotalSeconds;

                LoopTimer.Reset();
                LoopTimer.Start();
                PopAuditor.Audit(p.Generation);
                LoopTimer.Stop();

                Console.WriteLine(String.Format(@"Generation: {0}; Max fitness: {1:F5}; Avg fitness {2:F5}; Max gates:{3};
Avg gates: {4:F2}; Avg length: {5:F2}; Tick time: {6:F4}s; Audit time: {7:F4}s",
                 p.Generation, PopAuditor.Stats["maxFitness"], PopAuditor.Stats["meanFitness"], PopAuditor.Stats["maxGates"],
                  PopAuditor.Stats["meanGates"], PopAuditor.Stats["meanLength"], TickTime, LoopTimer.Elapsed.TotalSeconds));
                LoopTimer.Reset();

                p.Generation += 1;
                if (p.Generation < NumGenerations)
                { // On the final tick, no repro so the final population is dumped
                    p.ReproTournament((int)(PopulationSize * Convert.ToDouble(Configuration.Config["kProportion"])),
                    (int)(PopulationSize * Convert.ToDouble(Configuration.Config["nProportion"])), true);
                }
                else
                {
                    Console.WriteLine("\nNo reproduction on the final generation.");
                }

                System.GC.Collect();
            }
            p.ToDB(Configuration.Config["dumpPopulationPath"]);
            CodeTimer.Stop();
            Console.WriteLine(String.Format("{0} generations took: {1:F4}s", NumGenerations, CodeTimer.Elapsed.TotalSeconds));
        }
    }
}

namespace Global
{
    static class Configuration
    {
        public static Dictionary<string, string> Config;

        public static void Print()
        {
            Console.WriteLine(string.Join(Environment.NewLine, Config.Select(kv => $"{kv.Key} = {kv.Value}")));
        }
    }
}