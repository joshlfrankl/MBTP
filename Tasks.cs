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
using SixLabors.Fonts;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;
using SixLabors.ImageSharp.Drawing;
using SixLabors.ImageSharp.Drawing.Processing;
using Organism;
using Global;
using JSFDN;

namespace Tasks
{
    class TaskUtils
    {
        public static Dictionary<string, Action<TaskOrganism, int, string, bool>> GetTasks() {
            var availableTasks = new Dictionary<string, Action<TaskOrganism, int, string, bool>>()
            {
                {"HomingTask", HomingTask.Run},
                {"ChemotaxisTask", ChemotaxisTask.Run},
                {"SumTask", SumTask.Run},
                {"BlockCatchingTask", BlockCatchingTask.Run},
            };
            return(availableTasks);
        }
    }

    class BrainUtils
    {
        public static FontCollection Collection = new FontCollection();
        public static FontFamily FontFam = Collection.Install($"{AppDomain.CurrentDomain.BaseDirectory}/fonts/RobotoMono-Light.ttf");
        public static Font BrainFont = FontFam.CreateFont(12);

        public static void RenderBrain(Image frame, TaskOrganism org)
        {
            int MemBoxSide = 32;

            frame.Mutate(frameCtx =>
            {
                for (int i = 0; i < org.GetMemoryLength(); i++)
                {
                    var box = new RectangularPolygon(i * MemBoxSide, MemBoxSide, MemBoxSide, MemBoxSide);
                    frameCtx.Fill(Color.DarkGray, box);
                    frameCtx.DrawText($"{org.GetMemory(i)}", BrainFont, Color.White, new PointF(i * MemBoxSide + 8, MemBoxSide + 8));
                }
            });
        }
    }

    class HomingTask
    {
        public static void Run(TaskOrganism org, int seed, string dirName, bool logData)
        {   
            int NumUpdates = Global.Configuration.GetInt32Value("homingNumUpdates");
            int NumBrainTicks = Global.Configuration.GetInt32Value("homingBrainTicksPerUpdate");
            
            double TargetX = Global.Configuration.GetDoubleValue("homingTargetX");
            double TargetY = Global.Configuration.GetDoubleValue("homingTargetY");
            double OrgMaxSpeed = Global.Configuration.GetDoubleValue("homingOrgSpeed");
            double OrgAngle = Global.Configuration.GetDoubleValue("homingStartingAngle");
            double OrgSpeed = OrgMaxSpeed;

            bool ClockInput = Global.Configuration.GetBoolValue("homingClockInput?");
            bool SpeedOutput = Global.Configuration.GetBoolValue("homingSpeedOutput?");

            List<string> Log = new List<string>(NumUpdates);

            double OrgX = 0.0; 
            double OrgY = 0.0; 

            if (dirName != "")
            {
                System.IO.Directory.CreateDirectory($"{Global.Configuration.GetStringValue("renderPath")}/{dirName}");
            }

            org.ZeroMemory();

            for (int i = 0; i < NumUpdates; i++)
            {   
                if (ClockInput)
                {
                    org.SetMemory(2, (byte) i);
                }
                
                org.Run(NumBrainTicks);

                int turn = org.GetMemory(0);
                if (turn < 100)
                {
                    OrgAngle += 0.1;
                } else if (turn < 200) {
                    OrgAngle -= 0.1;
                } else {
                    // Straight ahead
                }

                if (SpeedOutput)
                {
                    OrgSpeed = (org.GetMemory(1) / 255.0) * OrgMaxSpeed;
                }
                
                OrgX += Math.Cos(OrgAngle) * OrgSpeed;
                OrgY += Math.Sin(OrgAngle) * OrgSpeed;

                if (dirName != "")
                {
                    render(org, dirName, i, OrgX, OrgY, OrgAngle, TargetX, TargetY);
                }

                if (logData)
                {
                    Log.Add($"{OrgX},{OrgY},{OrgAngle},{OrgSpeed}");
                }
            }
            org.SetFitness((1.0 / Math.Sqrt(Math.Pow(OrgX - TargetX, 2.0) + Math.Pow(OrgY - TargetY, 2.0))));
            org.SetStats(String.Join(";", Log));
        }

        private static void render(TaskOrganism org, string dirName, int i, double orgX, double orgY, double orgAngle, double targetX, double targetY)
        {
            int imgX = 512;
            int imgY = 512;

            float scaleX = (float)imgX / (150.0f);
            float scaleY = (float)imgY / (150.0f);

            using (var frame = new Image<SixLabors.ImageSharp.PixelFormats.Rgb24>(imgX, imgY))
            {
                frame.Mutate(frameCtx =>
                {
                    frameCtx.BackgroundColor(Color.Black);
                    var target = new EllipsePolygon(((float)targetX) * scaleX + (imgX / 2),
                                                    ((float)targetY) * scaleY + (imgY / 2),
                                                    5.0f, 5.0f);
                    var o = new EllipsePolygon(((float)orgX) * scaleX + (imgX / 2),
                                                 ((float)orgY) * scaleY + (imgY / 2),
                                                 10.0f, 10.0f);
                    frameCtx.Fill(Color.Wheat, o);
                    frameCtx.Fill(Color.RoyalBlue, target);
                    BrainUtils.RenderBrain(frame, org);
                });
                frame.SaveAsPng($"{Global.Configuration.GetStringValue("renderPath")}/{dirName}/{i}.png");
            }
        }
    }

    class ChemotaxisTask
    {
        public static double dist(double x1, double y1, double x2, double y2)
        {
            return (Math.Sqrt(Math.Pow(x1 - x2, 2.0) + Math.Pow(y1 - y2, 2.0)));
        }

        public static void Run(TaskOrganism org, int seed, string dirName, bool logData)
        {
            int NumUpdates = Global.Configuration.GetInt32Value("ctNumUpdates");
            int NumReps = Global.Configuration.GetInt32Value("ctNumReps");
            int MaxXYDistance = Global.Configuration.GetInt32Value("ctMaxXYDist");
            int NumBrainTicks = Global.Configuration.GetInt32Value("ctBrainTicksPerUpdate");

            double MinDistance = Global.Configuration.GetDoubleValue("ctMinDist");

            bool usePopConsistSeed = Global.Configuration.GetBoolValue("ctUsePopulationConsistentSeed?");
            JSFRng PopConsistentRNG;

            if (usePopConsistSeed)
            {
                PopConsistentRNG = new JSFRng(seed); // Use the same seed across whole population, to avoid lucky/unlucky orgs.
            } else
            {
                PopConsistentRNG = new JSFRng(org.GetNext());
            }

            double[] scores = new double[NumReps];
            for (int n = 0; n < NumReps; n++)
            {
                double orgX = PopConsistentRNG.NextDouble();
                double orgY = PopConsistentRNG.NextDouble();
                double orgAngle = PopConsistentRNG.NextDouble() * 2.0 * Math.PI;

                double targetX = 0;
                double targetY = 0;

                while (dist(0, 0, targetX, targetY) < MinDistance)
                {
                    targetX = PopConsistentRNG.Next(MaxXYDistance) + PopConsistentRNG.NextDouble();
                    targetY = PopConsistentRNG.Next(MaxXYDistance) + PopConsistentRNG.NextDouble();
                    if (PopConsistentRNG.Next() % 2 == 0)
                    {
                        targetX = -targetX;
                    }

                    if (PopConsistentRNG.Next() % 2 == 0)
                    {
                        targetY = -targetY;
                    }
                }

                int finalUpdate = NumUpdates;

                if (dirName != "")
                {
                    System.IO.Directory.CreateDirectory($"{Global.Configuration.GetStringValue("renderPath")}/{dirName}");
                }

                org.ZeroMemory();

                for (int i = 0; i < NumUpdates; i++)
                {
                    double shift = org.GetMemory(2) + 1.0;
                    double growth = org.GetMemory(3) + 1.0;
                    double probSig = 1.0 / (1.0 + shift * Math.Exp(-growth / dist(orgX, orgY, targetX, targetY)));

                    for (int j = 5; j < 8; j++)
                    {
                        if (PopConsistentRNG.NextDouble() < probSig)
                        {
                            org.SetMemory(j, 10);
                        }
                        else
                        {
                            org.SetMemory(j, 0);
                        }
                    }

                    org.Run(NumBrainTicks);

                    int turn = org.GetMemory(0);
                    if (turn > 200)
                    {
                        orgAngle = PopConsistentRNG.NextDouble() * 2.0 * Math.PI;
                    }
                    else
                    {
                        double speed = 1.0;
                        orgX += Math.Cos(orgAngle) * speed;
                        orgY += Math.Sin(orgAngle) * speed;
                    }

                    if (dirName != "" && n == 0)
                    {
                        render(org, dirName, i, orgX, orgY, orgAngle, targetX, targetY);
                    }

                }
                scores[n] = (1 / dist(orgX, orgY, targetX, targetY));
            }
            double score = 0.0;
            for (int i = 0; i < NumReps; i++)
            {
                score += scores[i];
            }
            org.SetFitness(score / NumReps);
        }

        private static void render(TaskOrganism org, string dirName, int i, double orgX, double orgY, double orgAngle, double targetX, double targetY)
        {
            int imgX = 512;
            int imgY = 512;

            float scaleX = (float)imgX / (150.0f);
            float scaleY = (float)imgY / (150.0f);

            using (var frame = new Image<SixLabors.ImageSharp.PixelFormats.Rgb24>(imgX, imgY))
            {
                frame.Mutate(frameCtx =>
                {
                    frameCtx.BackgroundColor(Color.Black);
                    var target = new EllipsePolygon(((float)targetX) * scaleX + (imgX / 2),
                                                    ((float)targetY) * scaleY + (imgY / 2),
                                                    5.0f, 5.0f);
                    var o = new EllipsePolygon(((float)orgX) * scaleX + (imgX / 2),
                                                 ((float)orgY) * scaleY + (imgY / 2),
                                                 10.0f, 10.0f);
                    frameCtx.Fill(Color.Wheat, o);
                    frameCtx.Fill(Color.RoyalBlue, target);
                    BrainUtils.RenderBrain(frame, org);
                });
                frame.SaveAsPng($"{Global.Configuration.GetStringValue("renderPath")}/{dirName}/{i}.png");
            }
        }
    }

    class SumTask
    {
        public static void Run(TaskOrganism org, int seed, string dirName, bool logData)
        {   
            int NumBrainTicks = Global.Configuration.GetInt32Value("sumNumBrainTicks");
            org.Run(NumBrainTicks);
            int t = 0;
            for (int i = 0; i < org.GetMemoryLength(); i++)
            {
                t += org.GetMemory(i);
            }
            org.SetFitness(t);
        }
    }


    class BlockCatchingTask
    {
        public static void Run(TaskOrganism org, int seed, string dirName, bool logData)
        {   
            int simHeight = Global.Configuration.GetInt32Value("blockCatchSimHeight");
            int simWidth = Global.Configuration.GetInt32Value("blockCatchSimWidth");
            int numIters = Global.Configuration.GetInt32Value("blockCatchNumUpdates");
            int numRounds = Global.Configuration.GetInt32Value("blockCatchNumTrials");

            int totalScore = 0;
            int totalBlocks = 0;

            bool usePopConsistSeed = Global.Configuration.GetBoolValue("blockCatchUsePopulationConsistentSeed?");
            JSFRng PopConsistentRNG;

            if (usePopConsistSeed)
            {
                PopConsistentRNG = new JSFRng(seed); // Use the same seed across whole population, to avoid lucky/unlucky orgs.
            } else
            {
                PopConsistentRNG = new JSFRng(org.GetNext());
            }

            for (int j = 0; j < numRounds; j++)
            {
                int orgX = 0;
                int blockX = PopConsistentRNG.Next(simWidth);
                int blockY = simHeight;
                totalBlocks += 1;
                int score = 0;
                org.ZeroMemory();

                for (int i = 0; i < numIters; i++)
                {
                    org.SetMemory(0, (byte)i);
                    if (blockY == 0)
                    {
                        if (blockX == orgX)
                        {
                            score += 1;
                            org.SetMemory(1, 255);
                        }
                        blockY = simHeight;
                        blockX = PopConsistentRNG.Next(simWidth);
                        totalBlocks += 1;
                    }
                    else
                    {
                        blockY -= 1;
                        org.SetMemory(1, 0);
                    }

                    if (blockX == orgX)
                    {
                        org.SetMemory(2, 255);
                    }

                    if (orgX == 0)
                    {
                        org.SetMemory(3, 128);
                    }
                    else if (orgX == (simWidth - 1))
                    {
                        org.SetMemory(3, 255);
                    }
                    else
                    {
                        org.SetMemory(3, 0);
                    }

                    org.Run(1);

                    if (org.GetMemory(4) > 200 && orgX < simWidth)
                    {
                        orgX += 1;
                    }
                    else if (org.GetMemory(4) < 100 && orgX > 0)
                    {
                        orgX -= 1;
                    }
                }
                totalScore += score;
            }
            // Fitness is the proportion of blocks caught.
            org.SetFitness((double) totalScore / (double) totalBlocks);
        }
    }
}