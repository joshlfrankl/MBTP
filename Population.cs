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
using System.Threading.Tasks;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using Microsoft.Data.Sqlite;
using Organism;
using Genome;
using Global;
using JSFDN;

namespace PopulationNS
{
    class TaskPopulation
    {
        public List<TaskOrganism> Orgs;
        private Mutator MutationSchema;
        public int Generation;

        public TaskPopulation(int populationSize, int genomeLength, Mutator mutator, int randomSeed)
        {
            JSFRng Rng = new JSFRng(randomSeed);
            MutationSchema = mutator;
            Generation = 0;
            int Offset = Rng.Next();

            TaskOrganism[] popArray = new TaskOrganism[populationSize];
            Parallel.For(0, populationSize, i =>  // Consider partitioner to reduce overhead?
            {
                popArray[i] = new TaskOrganism(genomeLength, Offset + i); // Seed wrapping around doesn't matter
            });

            Orgs = popArray.ToList();
        }

        public TaskPopulation(string filepath, Mutator mutator)
        {
            MutationSchema = mutator;
            string DBstring = new SqliteConnectionStringBuilder(String.Format("Data Source={0}", Configuration.Config["loadPopulationPath"])).ToString();
            Orgs = new List<TaskOrganism>();
            using (var SqliteConn = new SqliteConnection(DBstring))
            {
                SqliteConn.Open();
                var ReadCommand = SqliteConn.CreateCommand();
                ReadCommand.CommandText = "SELECT * FROM population";
                var OrgReader = ReadCommand.ExecuteReader();
                bool Toggle = true;

                while (OrgReader.Read())
                {
                    if (Toggle)
                    {
                        Generation = OrgReader.GetInt32(1) - 1;
                        Toggle = false;
                    }
                    ByteGenome Genome = new ByteGenome(OrgReader.GetString(3));
                    TaskOrganism CurrentOrg = new TaskOrganism(Genome, OrgReader.GetString(4));
                    CurrentOrg.SetFitness(OrgReader.GetDouble(2));
                    Orgs.Add(CurrentOrg);
                }
            }
        }

        public void Tick(Action<TaskOrganism, string, bool> w)
        {
            // Consider partitioner to reduce overhead?
            Parallel.ForEach(Orgs, org => w(org, "", false));
        }

        public void ReproTournament(int n, int k, bool sex)
        {
            Stopwatch ReproTimerFull = new Stopwatch();
            ReproTimerFull.Start();

            TaskOrganism[] Chosen = new TaskOrganism[n];

            Stopwatch ReproTimer = new Stopwatch();
            ReproTimer.Start();

            JSFRng Rng = new JSFRng(Orgs[0].GetNext());
            int Offset = Rng.Next();

            // Consider partitioner to reduce overhead?
            Parallel.For<TaskOrganism[]>(0, n, () => new TaskOrganism[k], (i, loopState, buffer) =>
            {
                JSFRng LocalRng = new JSFRng(Offset + i);
                for (int j = 0; j < k; j++)
                {
                    buffer[j] = Orgs[LocalRng.Next(Orgs.Count)];
                }
                Chosen[i] = buffer.Aggregate((o1, o2) => o1.GetFitness() > o2.GetFitness() ? o1 : o2).DeepCopy();
                return (buffer);
            }, buffer => { });

            ReproTimer.Stop();
            TimeSpan TournamentTime = ReproTimer.Elapsed;
            string TournamentTimeString = String.Format("\nTournament selection took: {0:F4}s, ", TournamentTime.TotalSeconds);
            Console.Write(TournamentTimeString);

            ReproTimer.Reset();
            ReproTimer.Start();
            if (sex)
            {
                TaskOrganism[] Offspring = new TaskOrganism[n];
                for (int i = 0; i < n; i++)
                {
                    ByteGenome OffspringGenome = MutationSchema.sexualReproduction(Chosen[i].GetGenome(), Chosen[Rng.Next(n)].GetGenome(), Rng);
                    Offspring[i] = new TaskOrganism(OffspringGenome, Chosen[i].GetNext());
                }
                Chosen = Offspring;
            }
            ReproTimer.Stop();
            TimeSpan SexTime = ReproTimer.Elapsed;
            string SexTimeString = String.Format("Sex took: {0:F4}s, ", SexTime.TotalSeconds);
            Console.Write(SexTimeString);

            ReproTimer.Reset();
            ReproTimer.Start();
            Parallel.ForEach(Orgs, org => Chosen[org.GetNext(n)].copyInto(org, MutationSchema));
            ReproTimer.Stop();
            TimeSpan CopyTime = ReproTimer.Elapsed;
            string CopyTimeString = String.Format("Copying took: {0:F4}s, ", CopyTime.TotalSeconds);
            Console.Write(CopyTimeString);

            ReproTimerFull.Stop();
            TimeSpan TotalTime = ReproTimerFull.Elapsed;
            string TotalTimeString = String.Format("Reproduction took: {0:F4}s\n\n", TotalTime.TotalSeconds);
            Console.WriteLine(TotalTimeString);
        }

        public void ToDB(string outpath)
        {
            Stopwatch DumpTimer = new Stopwatch();
            DumpTimer.Start();
            string DBstring = new SqliteConnectionStringBuilder(String.Format("Data Source={0}", Configuration.Config["dumpPopulationPath"])).ToString();
            using (var SqliteConn = new SqliteConnection(DBstring))
            {
                SqliteConn.Open();
                var InitCommand = SqliteConn.CreateCommand();
                InitCommand.CommandText = @" DROP TABLE IF EXISTS 'population';
                CREATE TABLE population (id INTEGER PRIMARY KEY,
                generation INT NOT NULL,
                fitness REAL NOT NULL,
                genome TEXT NOT NULL,
                randomseed TEXT NOT NULL)";
                InitCommand.ExecuteNonQuery();

                using (var Transaction = SqliteConn.BeginTransaction())
                {
                    var InsertCommand = SqliteConn.CreateCommand();
                    InsertCommand.CommandText = @"INSERT INTO population (generation, fitness, genome, randomseed) VALUES ($generation, $fitness, $genome, $randomseed)";

                    var GenerationParameter = InsertCommand.CreateParameter();
                    GenerationParameter.ParameterName = "$generation";
                    InsertCommand.Parameters.Add(GenerationParameter);
                    GenerationParameter.Value = this.Generation;

                    var FitnessParameter = InsertCommand.CreateParameter();
                    FitnessParameter.ParameterName = "$fitness";
                    InsertCommand.Parameters.Add(FitnessParameter);

                    var GenomeParameter = InsertCommand.CreateParameter();
                    GenomeParameter.ParameterName = "$genome";
                    InsertCommand.Parameters.Add(GenomeParameter);

                    var RngParameter = InsertCommand.CreateParameter();
                    RngParameter.ParameterName = "$randomseed";
                    InsertCommand.Parameters.Add(RngParameter);

                    for (int i = 0; i < Orgs.Count; i++)
                    {
                        GenerationParameter.Value = this.Generation;
                        FitnessParameter.Value = Orgs[i].GetFitness();
                        GenomeParameter.Value = string.Join(",", Orgs[i].GetGenome().Genome);
                        RngParameter.Value = Orgs[i].getRngState();
                        InsertCommand.ExecuteNonQuery();
                    }
                    Transaction.Commit();
                }
            }

            if (Convert.ToBoolean(Configuration.Config["compressDumpedPopulations?"]))
            {
                using (FileStream DbToCompress = File.OpenRead($"{Configuration.Config["dumpPopulationPath"]}")) 
                {
                    using (FileStream CompressedDb = File.Create($"{Configuration.Config["dumpPopulationPath"]}.gz"))
                    {
                        using (GZipStream DbCompressionStream = new GZipStream(CompressedDb, CompressionMode.Compress))
                        {   
                            Console.WriteLine($"Compressing {Configuration.Config["dumpPopulationPath"]}");
                            DbToCompress.CopyTo(DbCompressionStream);
                        }
                    }
                }
                Console.WriteLine($"Deleting {Configuration.Config["dumpPopulationPath"]}");
                File.Delete($"{Configuration.Config["dumpPopulationPath"]}");
            }

            string TotalTimeString = String.Format("Dumping population took: {0:F4}s\n\n", DumpTimer.Elapsed.TotalSeconds);
            Console.WriteLine(TotalTimeString);
        }
    }
}