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
                Command.CommandText = @" CREATE TABLE IF NOT EXISTS 'max_orgs' (id INTEGER PRIMARY KEY,
                generation INTEGER NOT NULL,
                fitness REAL NOT NULL,
                length INTEGER NOT NULL,
                gates INTEGER NOT NULL,
                genome TEXT NOT NULL,
                datalog TEXT NOT NULL,
                experiment_id TEXT NOT NULL)";
                Command.ExecuteNonQuery();

                Command.CommandText = @" CREATE TABLE IF NOT EXISTS 'avg_orgs' (id INTEGER PRIMARY KEY,
                generation INTEGER NOT NULL,
                fitness REAL NOT NULL,
                length REAL NOT NULL,
                gates REAL NOT NULL,
                experiment_id TEXT NOT NULL)";
                Command.ExecuteNonQuery();

                Command.CommandText = @" CREATE TABLE IF NOT EXISTS 'experiments' (experiment_id TEXT PRIMARY KEY,
                config TEXT NOT NULL)";
                Command.ExecuteNonQuery();

                // Insert the experiment info into the experiments table
                using (var transaction = SqliteConn.BeginTransaction()) 
                {
                var InsertCommand = SqliteConn.CreateCommand();
                InsertCommand.CommandText = $@" INSERT INTO experiments (experiment_id, config) VALUES ($uuid, $exp_config)";
                var ExperimentIdParameter = InsertCommand.CreateParameter();
                ExperimentIdParameter.ParameterName = "$uuid";
                InsertCommand.Parameters.Add(ExperimentIdParameter);
                ExperimentIdParameter.Value = Configuration.Config["UUID"];

                var ExperimentConfigParameter = InsertCommand.CreateParameter();
                ExperimentConfigParameter.ParameterName = "$exp_config";
                InsertCommand.Parameters.Add(ExperimentConfigParameter);
                ExperimentConfigParameter.Value = string.Join("\n", Configuration.Config);
                
                InsertCommand.ExecuteNonQuery();
                transaction.Commit();
                }



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
                    Command.CommandText = @"INSERT INTO max_orgs (generation, fitness, length, gates, genome, datalog, experiment_id)
                    VALUES ($generation, $fitness, $length, $gates, $genome, $log, $uuid)";

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

                    var UuidParameter = Command.CreateParameter();
                    UuidParameter.ParameterName = "$uuid";
                    Command.Parameters.Add(UuidParameter);
                    UuidParameter.Value = Configuration.Config["UUID"];

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
                    var Command = sqliteConn.CreateCommand();
                    Command.CommandText = @"INSERT INTO avg_orgs (generation, fitness, length, gates, experiment_id) 
                    VALUES ($generation, $fitness, $length, $gates, $uuid)";

                    var generationParameter = Command.CreateParameter();
                    generationParameter.ParameterName = "$generation";
                    Command.Parameters.Add(generationParameter);
                    generationParameter.Value = Population.Generation;

                    var fitnessParameter = Command.CreateParameter();
                    fitnessParameter.ParameterName = "$fitness";
                    Command.Parameters.Add(fitnessParameter);
                    fitnessParameter.Value = Stats["meanFitness"];

                    var lengthParameter = Command.CreateParameter();
                    lengthParameter.ParameterName = "$length";
                    Command.Parameters.Add(lengthParameter);
                    lengthParameter.Value = Stats["meanLength"];

                    var gatesParameter = Command.CreateParameter();
                    gatesParameter.ParameterName = "$gates";
                    Command.Parameters.Add(gatesParameter);
                    gatesParameter.Value = Stats["meanGates"];

                    var UuidParameter = Command.CreateParameter();
                    UuidParameter.ParameterName = "$uuid";
                    Command.Parameters.Add(UuidParameter);
                    UuidParameter.Value = Configuration.Config["UUID"];

                    Command.ExecuteNonQuery();

                    transaction.Commit();
                }
            }
        }
    }
}