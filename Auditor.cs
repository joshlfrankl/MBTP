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
using Microsoft.Data.Sqlite;
using System.Collections.Generic;
using Organism;
using PopulationNS;
using Global;

namespace AuditorNS
{
    class Auditor
    {
        private string DbString;
        private TaskPopulation Population;
        public Dictionary<String, double> Stats;

        public Action<TaskOrganism, string, bool> Task;
        public Auditor(TaskPopulation population, String outpath, Action<TaskOrganism, string, bool> task)
        {
            this.Population = population;
            this.Task = task;
            Stats = new Dictionary<String, double>();
            this.DbString = new SqliteConnectionStringBuilder(outpath).ToString();
            using (var SqliteConn = new SqliteConnection(this.DbString))
            {
                SqliteConn.Open();
                var Command = SqliteConn.CreateCommand();
                Command.CommandText = @" DROP TABLE IF EXISTS 'maxOrgs';
                CREATE TABLE maxOrgs (id INTEGER PRIMARY KEY,
                generation INTEGER NOT NULL,
                fitness REAL NOT NULL,
                length INTEGER NOT NULL,
                gates INTEGER NOT NULL,
                genome TEXT NOT NULL,
                datalog TEXT NOT NULL)";
                Command.ExecuteNonQuery();

                Command.CommandText = @" DROP TABLE IF EXISTS 'avgOrgs';
                CREATE TABLE avgOrgs (id INTEGER PRIMARY KEY,
                generation INTEGER NOT NULL,
                fitness REAL NOT NULL,
                length REAL NOT NULL,
                gates REAL NOT NULL)";
                Command.ExecuteNonQuery();
            }
        }

        public void Audit(int generation)
        {
            UpdateStats(generation);
        }

        public void UpdateStats(int generation)
        { // Returns idx of highest fitness org
            double MaxFitness = Single.NegativeInfinity;
            double FitnessAcc = 0.0;
            double MaxGates = 0.0;
            double GatesAcc = 0.0;
            double MaxLength = 0.0;
            double LengthAcc = 0.0;
            int MaxIdx = 0;

            //Dictionary<String, float> gateCounts = Gate.gateClasses.ToDictionary(name => name, _ => 0.0f);

            for (int i = 0; i < Population.Orgs.Count; i++)
            {
                double fit = Population.Orgs[i].GetFitness();
                float brainSize = Population.Orgs[i].GetBrainSize();
                float length = Population.Orgs[i].GetGenomeLength();

                FitnessAcc += fit;
                GatesAcc += brainSize;
                LengthAcc += length;

                if (MaxFitness < fit)
                {
                    MaxFitness = fit;
                    MaxIdx = i;
                }

                if (MaxGates < brainSize)
                {
                    MaxGates = brainSize;
                }

                if (MaxLength < length)
                {
                    MaxLength = length;
                }
            }

            Stats["meanFitness"] = FitnessAcc / Population.Orgs.Count;
            Stats["meanGates"] = GatesAcc / Population.Orgs.Count;
            Stats["meanLength"] = LengthAcc / Population.Orgs.Count;
            Stats["maxFitness"] = MaxFitness;
            Stats["maxGates"] = MaxGates;
            Stats["maxLength"] = MaxLength;

            RecordAvgOrg();
            RecordMaxOrg(Population.Orgs[MaxIdx]);

            if (Convert.ToBoolean(Configuration.Config["dumpImages?"]))
            {
                Console.WriteLine("Rendering max org...");
                Task(Population.Orgs[MaxIdx], $"gen{generation}", true);
            }
        }

        private void RecordMaxOrg(TaskOrganism org)
        {
            using (var SqliteConn = new SqliteConnection(DbString))
            {
                SqliteConn.Open();
                using (var transaction = SqliteConn.BeginTransaction())
                {
                    var Command = SqliteConn.CreateCommand();
                    Command.CommandText = @"INSERT INTO maxOrgs (generation, fitness, length, gates, genome, datalog) VALUES ($generation, $fitness, $length, $gates, $genome, $log)";

                    var GenerationParameter = Command.CreateParameter();
                    GenerationParameter.ParameterName = "$generation";
                    Command.Parameters.Add(GenerationParameter);
                    GenerationParameter.Value = Population.Generation;

                    var FitnessParameter = Command.CreateParameter();
                    FitnessParameter.ParameterName = "$fitness";
                    Command.Parameters.Add(FitnessParameter);
                    FitnessParameter.Value = org.GetFitness();

                    var LengthParameter = Command.CreateParameter();
                    LengthParameter.ParameterName = "$length";
                    Command.Parameters.Add(LengthParameter);
                    LengthParameter.Value = org.GetGenomeLength();

                    var GatesParameter = Command.CreateParameter();
                    GatesParameter.ParameterName = "$gates";
                    Command.Parameters.Add(GatesParameter);
                    GatesParameter.Value = org.GetBrainSize();

                    var GenomeParameter = Command.CreateParameter();
                    GenomeParameter.ParameterName = "$genome";
                    Command.Parameters.Add(GenomeParameter);
                    GenomeParameter.Value = string.Join(",", org.GetGenome().Genome);

                    var LogParameter = Command.CreateParameter();
                    LogParameter.ParameterName = "$log";
                    Command.Parameters.Add(LogParameter);
                    LogParameter.Value = org.GetStats();

                    Command.ExecuteNonQuery();

                    transaction.Commit();
                }
            }
        }

        private void RecordAvgOrg()
        {
            using (var sqliteConn = new SqliteConnection(DbString))
            {
                sqliteConn.Open();
                using (var transaction = sqliteConn.BeginTransaction())
                {
                    var command = sqliteConn.CreateCommand();
                    command.CommandText = @"INSERT INTO avgOrgs (generation, fitness, length, gates) VALUES ($generation, $fitness, $length, $gates)";

                    var generationParameter = command.CreateParameter();
                    generationParameter.ParameterName = "$generation";
                    command.Parameters.Add(generationParameter);
                    generationParameter.Value = Population.Generation;

                    var fitnessParameter = command.CreateParameter();
                    fitnessParameter.ParameterName = "$fitness";
                    command.Parameters.Add(fitnessParameter);
                    fitnessParameter.Value = Stats["meanFitness"];

                    var lengthParameter = command.CreateParameter();
                    lengthParameter.ParameterName = "$length";
                    command.Parameters.Add(lengthParameter);
                    lengthParameter.Value = Stats["meanLength"];

                    var gatesParameter = command.CreateParameter();
                    gatesParameter.ParameterName = "$gates";
                    command.Parameters.Add(gatesParameter);
                    gatesParameter.Value = Stats["meanGates"];

                    command.ExecuteNonQuery();

                    transaction.Commit();
                }
            }
        }
    }
}