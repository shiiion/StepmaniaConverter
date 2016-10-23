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
                bannerGenerated.BannerArtist = smmap.Header.ArtistTranslate;
                bannerGenerated.BannerTitle = smmap.Header.TitleTranslate;
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
        
        public static double getOsuBeatsnapOffset(double startOffset, double beatLength)
        {
            double curMS = startOffset;

            if(curMS < 0)
            {
                while (curMS < 0)
                {
                    curMS += beatLength / 4;
                }

                return (curMS - (beatLength / 4)) / 1000.0;
            }

            while(curMS > 0)
            {
                curMS -= (beatLength / 4);
            }

            return curMS / 1000.0;
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
            if (ommap.Contents.BgPath != null)
            {
                smmap.Header.Banner = ommap.Contents.BgPath.Substring(0, ommap.Contents.BgPath.IndexOf('.')) + "_bn"
                    + ommap.Contents.BgPath.Substring(ommap.Contents.BgPath.IndexOf('.'));
            }
            smmap.Header.SongPath = ommap.Contents.AudioPath;
            smmap.Header.StartOffset = 0;
            smmap.Header.SampleStart = (double)ommap.Contents.AudioLeadIn / 1000.0;
            smmap.Header.SampleLength = (double)ommap.Contents.SampleLength / 1000.0;


            double bullshitOffset = 0;
            if (ommap.Contents.TimingPoints.Count > 1)
            {
                if (ommap.Contents.TimingPoints[0].Start < 0)
                {
                    bullshitOffset = getOsuBeatsnapOffset(ommap.Contents.TimingPoints[0].Start, ommap.Contents.TimingPoints[0].BeatLength);
                    bullshitOffset -= (ommap.Contents.TimingPoints[0].Start / 1000.0);
                }
                else
                {
                    bullshitOffset = getOsuBeatsnapOffset(ommap.StartingPointOffset, ommap.Contents.TimingPoints[0].BeatLength);
                }
            }
            else
            {
                if (ommap.Contents.TimingPoints[0].Start < 0)
                {
                    bullshitOffset = getOsuBeatsnapOffset(ommap.Contents.TimingPoints[0].Start, ommap.Contents.TimingPoints[0].BeatLength);
                    bullshitOffset += ommap.Contents.TimingPoints[0].Start / 1000.0;
                }
                else
                {
                    bullshitOffset = getOsuBeatsnapOffset(ommap.StartingPointOffset, ommap.Contents.TimingPoints[0].BeatLength);
                }
            }

            for(int a=0;a<ommap.Contents.TimingPoints.Count;a++)
            {
                smmap.Header.TimingPoints.Add(convertBPM(ommap.Contents.TimingPoints[a], ommap.Contents.TimingPoints));
            }

            double cumulativeBeatOffset = 0;
            double nextTPStart = ommap.Contents.TimingPoints.Count == 1 ? double.MaxValue : ommap.Contents.TimingPoints[1].Start;
            int tpIndex = 1;

            smmap.Header.StartOffset = bullshitOffset - ommap.StartingPointOffset / 1000.0;

            foreach (HitObject ho in ommap.Contents.Objects)
            {
                //if (ho.Start > nextTPStart)
                //{
                //    double offset;
                //    cumulativeBeatOffset += (offset = getNoteSnapOffset(ho, ommap.Contents.TimingPoints));
                //    smmap.Header.Stops.Add(new Stop(getBeatToPoint(nextTPStart, ommap.Contents.TimingPoints), 
                //        getMillisFromBeat(-offset, smmap.Header.TimingPoints[tpIndex].BeatsPerMin)));
                //    nextTPStart = (++tpIndex == ommap.Contents.TimingPoints.Count ? double.MaxValue : ommap.Contents.TimingPoints[tpIndex].Start);
                //}

                if (ho.Type == HitObjectType.Hit)
                {
                    smmap.AddObject(ho.Col, getBeatToPoint(ho.Start, ommap.Contents.TimingPoints) + cumulativeBeatOffset, StepArrowType.Normal);
                }
                else
                {
                    smmap.AddObject(ho.Col, getBeatToPoint(ho.Start, ommap.Contents.TimingPoints) + cumulativeBeatOffset, StepArrowType.HoldBegin);
                    smmap.AddObject(ho.Col, getBeatToPoint(ho.End, ommap.Contents.TimingPoints) + cumulativeBeatOffset, StepArrowType.HoldEnd);
                }
            }

            return true;
        }

        private static TimingPoint getNoteTimingPoint(HitObject ho, List<TimingPoint> timingPoints)
        {
            if(timingPoints.Count == 1)
            {
                return timingPoints[0];
            }

            TimingPoint prevTP = timingPoints[0];

            foreach(TimingPoint tp in timingPoints)
            {
                if(ho.Start < tp.Start)
                {
                    return prevTP;
                }
                prevTP = tp;
            }
            return null;
        }

        private static double getMillisFromBeat(double beat, double bpm)
        {
            return ((beat) / bpm) * 60;
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

            while (a < points.Count && nextTime <= time)
            {
                beats += (nextTime - curPoint.Start) / curPoint.BeatLength;
                curPoint = points[a];
                a++;
                nextTime = (a < points.Count ? points[a].Start : time);
            }

            double roundOff = Math.Round((beats + ((time - curPoint.Start) / curPoint.BeatLength)) * 32) / 32;
            return roundOff;
        }

        private static BPM convertBPM(TimingPoint osuBPM, List<TimingPoint> points)
        {
            double BPM = 60 / (osuBPM.BeatLength / 1000);

            if (osuBPM.Start < 0)
            {
                return new BPM(0, BPM);
            }

            return new BPM(getBeatToPoint(osuBPM.Start, points), BPM);
        }
    }
}