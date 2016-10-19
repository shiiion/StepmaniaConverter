using System;
using System.Text;
using System.IO;
using System.Collections.Generic;

namespace osutostep
{
    public class ManiaConverter
    {
        public static string Reason { get; private set; }

        public static void WriteStepchart(StepManiaMap outputMap, OsuManiaMap from)
        {
            string smString = outputMap.ToString();
            CopyResourceFiles(from, outputMap);

            string stepchartPath = outputMap.CurrentDirectory + $"\\{outputMap.Header.ArtistTranslate} - {outputMap.Header.TitleTranslate}.sm";

            File.WriteAllText(stepchartPath, smString);
        }

        //todo: if i fail
        public static bool CopyResourceFiles(OsuManiaMap ommap, StepManiaMap smmap)
        {
            try
            {
                File.Copy(ommap.CurrentDirectory + $"\\{ommap.Contents.AudioPath}", smmap.CurrentDirectory + $"\\{ommap.Contents.AudioPath}");
            }
            catch (Exception e)
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine(e.Message);
            }
            try
            {
                File.Copy(ommap.CurrentDirectory + $"\\{ommap.Contents.BgPath}", smmap.CurrentDirectory + $"\\{ommap.Contents.BgPath}");
            }
            catch (Exception e)
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine(e.Message);
            }
            try
            {
                Banner bannerGenerated = new Banner(ommap.CurrentDirectory + $"\\{ommap.Contents.BgPath}");
                bannerGenerated.BannerArtist = smmap.Header.Artist;
                bannerGenerated.BannerTitle = smmap.Header.Title;
                bannerGenerated.FontColor = System.Drawing.Color.White;
                bannerGenerated.BannerFont = "Moon Light.otf";
                bannerGenerated.GenerateFinalImage().Save(smmap.CurrentDirectory + $"\\{smmap.Header.Banner}");
            }
            catch (Exception e)
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine(e.Message);
            }

            Console.ResetColor();


            return true;
        }

        public static double getOsuBeatsnapOffset(TimingPoint firstTimingPoint)
        {
            double curMS = firstTimingPoint.Start;

            if(curMS < 0)
            {
                while (curMS < 0)
                {
                    curMS += firstTimingPoint.BeatLength;
                }

                return -Math.Min(Math.Abs(curMS), curMS - firstTimingPoint.BeatLength) / 1000.0;
            }

            while(curMS > 0)
            {
                curMS -= firstTimingPoint.BeatLength;
            }

            return Math.Min(Math.Abs(curMS), curMS + firstTimingPoint.BeatLength) / 1000.0;
        }

        public static bool Convert(OsuManiaMap ommap, ref StepManiaMap smmap)
        {
            string outputDirectory = smmap.CurrentDirectory;

            smmap.DifficultyValue = ommap.Contents.DifficultyRating * 4;

            smmap.Header.Title = ommap.Contents.TitleUnicode;
            smmap.Header.TitleTranslate = ommap.Contents.Title;
            smmap.Header.Artist = ommap.Contents.ArtistUnicode;
            smmap.Header.ArtistTranslate = ommap.Contents.Artist;
            smmap.Header.Credit = ommap.Contents.Source;
            smmap.Header.Background = ommap.Contents.BgPath;
            smmap.Header.Banner = ommap.Contents.BgPath.Substring(0, ommap.Contents.BgPath.IndexOf('.')) + "_bn" 
                + ommap.Contents.BgPath.Substring(ommap.Contents.BgPath.IndexOf('.'));
            smmap.Header.SongPath = ommap.Contents.AudioPath;
            smmap.Header.StartOffset = 0;
            smmap.Header.SampleStart = (double)ommap.Contents.AudioLeadIn / 1000.0;
            smmap.Header.SampleLength = (double)ommap.Contents.SampleLength / 1000.0;

            foreach (TimingPoint point in ommap.Contents.TimingPoints)
            {
                if (smmap.Header.TimingPoints.Count > 0)
                {
                    smmap.Header.TimingPoints.Add(convertBPM(false, point, ommap.Contents.TimingPoints));
                }
                else
                {
                    smmap.Header.TimingPoints.Add(convertBPM(true, point, ommap.Contents.TimingPoints));
                }
            }
            double beatOffset = 0;
            double offset = getMillisFromBeat(beatOffset = getNoteSnapOffset(ommap.Contents.Objects[0], ommap.Contents.TimingPoints), smmap.Header.TimingPoints);


            double bullshitOffset;
            if (ommap.Contents.TimingPoints.Count > 1)
            {
                if (ommap.Contents.TimingPoints[0].Start < 0)
                {
                    bullshitOffset = getOsuBeatsnapOffset(ommap.Contents.TimingPoints[0]);
                }
                else
                {
                    bullshitOffset = getOsuBeatsnapOffset(ommap.Contents.TimingPoints[1]);
                }
            }
            else
            {
                bullshitOffset = getOsuBeatsnapOffset(ommap.Contents.TimingPoints[0]);
            }

            smmap.Header.StartOffset = offset + bullshitOffset;

            foreach (HitObject ho in ommap.Contents.Objects)
            {
                if (ho.Type == HitObjectType.Hit)
                {
                    smmap.AddObject(ho.Col, getBeatToPoint(ho.Start, ommap.Contents.TimingPoints) + beatOffset, StepArrowType.Normal);
                }
                else
                {
                    smmap.AddObject(ho.Col, getBeatToPoint(ho.Start, ommap.Contents.TimingPoints) + beatOffset, StepArrowType.HoldBegin);
                    smmap.AddObject(ho.Col, getBeatToPoint(ho.End, ommap.Contents.TimingPoints) + beatOffset, StepArrowType.HoldEnd);
                }
            }

            return true;
        }

        private static double getMillisFromBeat(double beat, List<BPM> bpms)
        {
            int a = 1;
            double seconds = 0;
            BPM curBPM = bpms[0];

            double nextBeat = (bpms.Count >= 2 ? bpms[1].Beat : beat);

            while (a < bpms.Count && nextBeat < beat)
            {
                seconds += ((nextBeat - curBPM.Beat) / curBPM.BeatsPerMin) * 60;
                curBPM = bpms[a];
                a++;
                nextBeat = (a < bpms.Count ? bpms[a].Beat : beat);
            }
            
            return seconds + (((beat - curBPM.Beat) / curBPM.BeatsPerMin)) * 60;
        }

        private static double getNoteSnapOffset(HitObject ho, List<TimingPoint> points)
        {
            const double SNAP_DENOM = 1;
            double row, beatDiv;

            double beatSubDiv = getBeatToPoint(ho.Start, points);

            beatSubDiv = beatSubDiv - ((int)beatSubDiv);

            MathUtil.GetRichardsFraction(beatSubDiv, out row, out beatDiv);

            if (beatDiv <= SNAP_DENOM)
            {
                return 0;
            }

            double scale = (beatDiv / SNAP_DENOM);
            double nearest = 0;
            for (; nearest < row; nearest += scale) ;
            return (nearest - row) / beatDiv;
        }

        private static double getBeatToPoint(double time, List<TimingPoint> points)
        {
            int a = 1;
            double beats = 0;
            TimingPoint curPoint = points[0];
            double nextTime = (points.Count >= 2 ? points[1].Start : time);
            
            while (a < points.Count && nextTime < time) 
            {
                beats += (nextTime - curPoint.Start) / curPoint.BeatLength;
                curPoint = points[a];
                a++;
                nextTime = (a < points.Count  ? points[a].Start : time);
            }

            double roundOff = Math.Round((beats + ((time - curPoint.Start) / curPoint.BeatLength)) * 32) / 32;
            return roundOff;
        }

        private static BPM convertBPM(bool first, TimingPoint osuBPM, List<TimingPoint> points)
        {
            double BPM = 60 / (osuBPM.BeatLength / 1000);

            if (first)
            {
                return new BPM(0, BPM);
            }

            return new BPM(getBeatToPoint(osuBPM.Start - points[0].Start, points), BPM);
        }
    }
}