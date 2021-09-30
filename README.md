# MBTP
A Markov Brain Task Playground 

MBTP is a basic framework for evolving Markov Brains. You can read more about Markov Brains here: https://arxiv.org/pdf/1709.05601.pdf.  

# Building
Download and install dotnet 5 (https://dotnet.microsoft.com/download/dotnet/5.0) for your system. Clone the MBTP repository, then run the following in the MBTP directory:

```bash
dotnet build -c Release
cp -r ./fonts ./bin/Release/net5.0
```
You can copy/rename the net5.0 folder containing the MBTP executable.

# Configuration
The settings.yaml file holds the configuration for MBTP. By editing the file, you can control the population size, mutation rates, number of generations, task, task parameters, etc. MBTP will print the configuration file before beginning the evolution process, so if you dump MBTP to stdout you will know the exact settings used during execution. If you delete your settings.yaml file, a new one will be written to the current directory the next time MBTP is run.

# Writing your own task
Tasks must be of the type Action<TaskOrganism, int, string, bool>. 
The first parameter is a TaskOrganism containing a Markov Brain. This is what you will evaluate to determine a fitness value.
The int is a seed which can be used to have population-consistent randomness. This is helpful e.g. randomly setting a target on the XY plane, so that any "luck" is consistent across the population. Otherwise, you can use a per-organism random seed, but when you have 100k-1m organisms in your population, some are bound to get very "lucky" each generation.
The third parameter is the name of the directory which will be used to save any rendered images or other files associated with this generation of organisms.
The fourth parameter is a boolean which determines whether on not stats about this run should be saved into the organism's stats attribute (org.SetStats).

HomingTask is a good example of a basic, but non-trivial task to use as a guide.

# Copyright
GNU GPLv3 applies to all files in this repository except JSFDN.cs, which is in the public domain, and RobotoMono-Light.ttf which is licensed under the Apache-2.0 license (see LICENSE.APACHE2).  

This repository depends on, but does not distribute, the following dependencies (dotnet will download them for you when running dotnet build):  
Math.NET Numerics - MIT license  
Microsoft.Data.Sqlite - Apache-2.0 license  
SixLabors ImageSharp - Apache-2.0 license  
SixLabors ImageSharp.Drawing - Apache-2.0 license  
YamlDotNet - MIT license  

