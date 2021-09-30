using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;

namespace Global
{
    static class Configuration
    {
        private static Dictionary<string, string> Config = null;

        public static void InitializeConfiguration(Dictionary<string, string> inDict) {
            if (Config is not null) // Only init once
            {
                throw new Exception("Attempted to re-initialize the configuration dictionary.");
            }
            if (inDict is null) // Must init with non-null.
            {
                throw new Exception("Attempted to initialize configuration dictionary to null.");
            }
            Config = inDict;
        }

        public static void Print()
        {
            Console.WriteLine(string.Join(Environment.NewLine, Config.Select(kv => $"{kv.Key} = {kv.Value}")));
        }

        public static string GetStringValue(string target) {
            try
            {
                return(Config[target]);
            }
            catch (System.ArgumentNullException n) {
                throw new Exception($"Could not find {target} in settings.yaml. Did you accidentally delete it from your settings.yaml file?", n);
            }
        }

        public static bool GetBoolValue(string target) {
            try
            {
                return(Convert.ToBoolean(Config[target]));
            }
            catch (System.ArgumentNullException n)
            {
                throw new Exception($"Could not find {target} in settings.yaml. Did you accidentally delete it from your settings.yaml file?", n);
            }
            catch (FormatException f)
            {
                throw new Exception($"Could not convert the value of {target} into a boolean. The current value is {Config[target]}. The value must be \"True\" or \"False\" in settings.yaml", f);
            }
        }

        public static int GetInt32Value(string target) {
            try
            {
                return(Convert.ToInt32(Config[target]));
            }
            catch (System.ArgumentNullException n)
            {
                throw new Exception($"Could not find {target} in settings.yaml. Did you accidentally delete it from your settings.yaml file?", n);
            }
            catch (FormatException f)
            {
                throw new Exception($"Could not convert the value of {target} into a 32-bit integer. The current value is {Config[target]}.", f);
            }
        }

        public static double GetDoubleValue(string target) {
            try
            {
                return(Convert.ToDouble(Config[target]));
            }
            catch (System.ArgumentNullException n)
            {
                throw new Exception($"Could not find {target} in settings.yaml. Did you accidentally delete it from your settings.yaml file?", n);
            }
            catch (FormatException f)
            {
                throw new Exception($"Could not convert the value of {target} into a double. The current value is {Config[target]}.", f);
            }
        }

        public static string DumpCurrentConfiguration() {
            return(string.Join("\n", Config));
        }

        public static void WriteReferenceSettings(string path) {
            string referenceSettings = @"# Mutation settings
pointRatePerGenome: 0.5           # Rates are lambda parameter of poisson distributionf
insertionRatePerGenome: 0.01      
insertionRelSize: 0.05            # Proportion of genome size
deletionRatePerGenome: 0.01
deletionRelSize: 0.05
inversionRatePerGenome: 0.01
inversionRelSize: 0.05
reversalRatePerGenome: 0.01
reversalRelSize: 0.05
duplicationRatePerGenome: 0.01
duplicationRelSize: 0.05
swapRatePerGenome: 0.01
incrementRatePerGenome: 0.01
decrementRatePerGenome: 0.01
crossoverRate: 1                   # + 1 required crossover during sexual reproduction

# Brain settings
memorySize: 8                      # How many nodes in the brain?
seedGates: 3                       # Initial genomes with fewer gates than this will have gates inserted to reach this number

# Organism settings
genomeLength: 512                  # Initial number of bytes in each genome

# Population settings
populationSize: 1000000            # Number of organisms in the population
kProportion: 0.001                 # Proportion of population in each tournament
nProportion: 0.05                  # How many tournaments occur as a proportion of population size

# Experiment settings
experimentName: ""HomingTaskTest""
DBPath: ""/tmp/out_db.db""                                    # Path to statistics DB
dumpFinalPopulation?: ""False""
dumpPopulationPath: ""/tmp/population_dump.db""               # Path to save the population after execution
loadPopulationPath: ""/tmp/population_dump.db""               # Path to load a population from a file
compressDumpedPopulations?: ""False""                         # Gzip the dumped population to reduce size by ~90%? Deletes the uncompressed file.
loadPopulationFromFile?: ""False""                            # Load a saved population from a file? If not, a randomly generated population is used
generations: 100                                            # Number of generations before stopping
randomSeed: 0                                               # 0 for a randomized seed, which will be printed on startup
dumpImages?: ""False""                                         # Save images for the task?
renderPath: ""/tmp/image_dump""                               # Path to save the images

# Task selection
taskName: ""HomingTask""
# Homing task settings
homingSpeedOutput?: ""False""				# Set speed using node 1? Speed on each update will be [value_of_node_1]/255 * orgSpeed
homingClockInput?: ""False""				# Set node 2 to the current step number?
homingTargetX: 25.0					# X-coordinate of target
homingTargetY: 25.0					# Y-coordinate of target
homingNumUpdates: 100					# Number of task updates
homingOrgSpeed: 1.0					# How far does the organism move on each tick?
homingStartingAngle: 0.0				# What direction is the organism pointing at the start? (Radians counter-clockwise from the positive x-axis)
homingBrainTicksPerUpdate: 1				# How many times does the brain tick on each task update?

# Chemotaxis task settings
ctUsePopulationConsistentSeed?: ""True""		# Use the same seed across all organisms within the population? All orgs will get the same random numbers.
ctNumUpdates: 100					# Number of task updates
ctNumReps: 5						# How many times is the task repeated? Scores are averaged to determine the final fitness.
ctMaxXYDist: 50					# How far might the target be positioned from the origin along the X or Y axes?
ctBrainTicksPerUpdate: 1				# How many times does the brain tick on each task update?
ctMinDist: 15.0					# What is the minimum distance of the target from the origin?

#Sum task settings
sumNumBrainTicks: 1					# How many times does the Markov Brain tick before finishing the task?

# Block catching task settings
blockCatchUsePopulationConsistentSeed?: ""True""	# Use the same seed across all organisms within the population? All orgs will get the same random numbers.
blockCatchSimHeight: 32				# How tall is the arena? Blocks fall one unit per update. (Integer)
blockCatchSimWidth: 16					# How wide is the arena? The org can move left or right one unit per update.(Integer)
blockCatchNumUpdates: 128				# How many updates?
blockCatchNumTrials: 5					# How many repeated trials?";

            File.WriteAllText($"{path}/settings.yaml", referenceSettings);
        }
    }
}