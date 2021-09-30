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
using System.IO;
using System.Collections.Generic;
using System.Diagnostics;
using YamlDotNet.Serialization;
using PopulationNS;
using Organism;
using Genome;
using AuditorNS;
using Global;

namespace MBTP
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine($"Executing from {AppDomain.CurrentDomain.BaseDirectory}");
            if (!File.Exists(@"./settings.yaml"))
            {   // Die if no settings.yaml and write out a new reference settings.yaml. Could contiue execution, but bigger risk of confusion/wasted time.
                Configuration.WriteReferenceSettings(".");
                throw new Exception("Did not find settings.yaml in the current directory. Wrote a new reference settings.yaml in the current directory.");
            }
            string ConfigString = System.IO.File.ReadAllText(@"./settings.yaml");
            var Deserializer = new DeserializerBuilder().Build();
            Dictionary<string, string> Config = Deserializer.Deserialize<Dictionary<string, string>>(ConfigString);
            Config["UUID"] = System.Guid.NewGuid().ToString();

            Configuration.InitializeConfiguration(Config);

            RunExperimentFromScratch(Configuration.GetBoolValue("loadPopulationFromFile?"));
        }

        static void RunExperimentFromScratch(bool loadPopulation)
        {
            Console.WriteLine("Beginning simulation using the following settings:\n");
            Configuration.Print();
            Console.WriteLine("\n");

            int Seed = 0;

            if (Configuration.GetInt32Value("randomSeed") == 0)
            {
                Random Rng = new Random();
                Seed = Rng.Next();
            }
            else
            {
                Seed = Configuration.GetInt32Value("randomSeed");
            }

            Mutator Mutator = new Mutator();
            int PopulationSize = Configuration.GetInt32Value("populationSize");

            Stopwatch CodeTimer = new Stopwatch();
            TaskPopulation p;
            CodeTimer.Start();
            if (loadPopulation)
            {
                Console.WriteLine(String.Format("Loading population from {0}.", Configuration.GetStringValue("loadPopulationPath")));
                p = new TaskPopulation(Configuration.GetStringValue("loadPopulationPath"), Mutator);
            }
            else
            {
                Console.WriteLine(String.Format($"Initializing new random population with seed: {Seed}"));
                p = new TaskPopulation(PopulationSize, Configuration.GetInt32Value("genomeLength"), Mutator, Seed);
            }

            CodeTimer.Stop();
            TimeSpan RunTime = CodeTimer.Elapsed;
            string RunTimeString = String.Format("Population initialization took: {0:F4}s\n", RunTime.TotalSeconds);
            Console.WriteLine(RunTimeString);

            Dictionary<string, Action<TaskOrganism, int, string, bool>> taskDict = Tasks.TaskUtils.GetTasks();
            
            Action<TaskOrganism, int, string, bool> theTask = null;

            try
            {
            theTask = taskDict[Configuration.GetStringValue("taskName")];
            }
            catch (KeyNotFoundException k)
            {   
                throw new Exception("taskName not found in task dictionary. Did you make a typo or forget to add your task to the dictionary?", k);
            }

            int NumGenerations = Configuration.GetInt32Value("generations");
            Auditor PopAuditor = new Auditor(p, $"Data Source={Configuration.GetStringValue("DBPath")}", theTask);

            CodeTimer.Reset();
            CodeTimer.Start();
            Stopwatch LoopTimer = new Stopwatch();

            while (p.Generation < NumGenerations)
            {   
                Console.WriteLine($"Running generation {p.Generation} tick...");
                LoopTimer.Start();
                p.Tick(theTask, Seed + p.Generation);
                LoopTimer.Stop();
                double TickTime = LoopTimer.Elapsed.TotalSeconds;

                LoopTimer.Reset();
                LoopTimer.Start();
                PopAuditor.Audit(p.Generation, Seed);
                LoopTimer.Stop();

                Console.WriteLine(String.Format(@"Generation: {0}; Max fitness: {1:F5}; Avg fitness {2:F5}; Max gates:{3};
Avg gates: {4:F2}; Avg length: {5:F2}; Tick time: {6:F4}s; Audit time: {7:F4}s",
                 p.Generation, PopAuditor.Stats["maxFitness"], PopAuditor.Stats["meanFitness"], PopAuditor.Stats["maxGates"],
                  PopAuditor.Stats["meanGates"], PopAuditor.Stats["meanLength"], TickTime, LoopTimer.Elapsed.TotalSeconds));
                LoopTimer.Reset();

                p.Generation += 1;
                if (p.Generation < NumGenerations)
                { // On the final tick, no repro so the final population is dumped
                    p.ReproTournament((int)(PopulationSize * Configuration.GetDoubleValue("kProportion")),
                    (int)(PopulationSize * Configuration.GetDoubleValue("nProportion")), true);
                }
                else
                {
                    Console.WriteLine("\nNo reproduction on the final generation.");
                }

                System.GC.Collect();
            }
            if (Configuration.GetBoolValue("dumpFinalPopulation?"))
            {
                p.ToDB(Configuration.GetStringValue("dumpPopulationPath"));
            }
            
            CodeTimer.Stop();
            Console.WriteLine(String.Format("{0} generations took: {1:F4}s", NumGenerations, CodeTimer.Elapsed.TotalSeconds));
        }
    }
}
