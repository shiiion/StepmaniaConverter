//this should be complete 9/6/16
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace osutostep
{
    public enum HitObjectType
    {
        Hit = 1,
        Hold = 128,
    }

    public class HitObject : IComparable<HitObject>
    {
        //from 0 to 3
        public int Col;
        //in millisecomds
        public double Start;
        public HitObjectType Type;
        //in milliseconds (if Type == HitObjectType.Hold)
        public double End;
        public double IndividualStrain
        {
            get
            {
                return individualStrains[Col];
            }
            set
            {
                individualStrains[Col] = value;
            }
        }
        public double OverallStrain;

        public double[] heldUntil;
        public double[] individualStrains;

        public HitObject(HitObjectType type, int col, double start, double end = 0)
        {
            Type = type;
            Col = col;
            Start = start;
            End = end;
            OverallStrain = 1;

            heldUntil = new double[4] { 0, 0, 0, 0 };
            individualStrains= new double[4] { 0, 0, 0, 0 };
        }

        public int CompareTo(HitObject other)
        {
            if(other.Start == Start)
            {
                return 0;
            }
            else if(other.Start < Start)
            {
                return 1;
            }
            return -1;
        }
    }

    //all other parts of TimingPoint are ignored (they are osu-specific)
    public class TimingPoint
    {
        public double Start;
        public double BeatLength;

        public TimingPoint(double start, double beatLength)
        {
            Start = start;
            BeatLength = beatLength;
        }
    }



    public class OsuManiaMap
    {
        public class OsuContents
        {
            //General
            public string AudioPath;
            public double AudioLeadIn;
            public double SampleLength;

            //Metadata
            public string Title;
            public string TitleUnicode;
            public string Artist;
            public string ArtistUnicode;
            public string Creator;
            public string DifficultyName;
            public string Source;

            public double DifficultyRating;

            //Difficulty
            public double HP;
            //CS should be 4
            public double OD;
            public double AR;

            //Background info
            //if no bg is provided (ex: storyboarding) use default bg
            public string BgPath;

            //Timing points
            public List<TimingPoint> TimingPoints = new List<TimingPoint>();

            //HitObjects
            public List<HitObject> Objects = new List<HitObject>();
        }

        public delegate void GetLineDelegate(string line);

        public bool Loaded { get; set; }

        public string Reason { get; private set; }

        public string CurrentDirectory { get; set; }

        public OsuContents Contents { get; private set; }

        public string FormatFolderName
        {
            get
            {
                if(!Loaded)
                {
                    return ".badfolder";
                }
                return $"{Contents.Artist} - {Contents.Title}";
            }
        }

        public OsuManiaMap(string path, string folderPath)
        {
            Contents = new OsuContents();
            CurrentDirectory = folderPath;
            Loaded = LoadOsuMap(path);
        }

        private bool LoadOsuMap(string path)
        {
            string allText = "";
            try
            {
                allText = File.ReadAllText(path);
            }
            catch (Exception e)
            {
                Reason = e.Message;
                return false;
            }


            string hitObjectText = getFileSection(allText, "HitObjects");
            string timingPointText = getFileSection(allText, "TimingPoints");
            string difficultyText = getFileSection(allText, "Difficulty");
            string bgText = getFileSection(allText, "Events");
            string metadataText = getFileSection(allText, "Metadata");
            string generalText = getFileSection(allText, "General");

            try
            {
                getLineAndRun(hitObjectText, parseHitObject);
                getLineAndRun(timingPointText, parseTimingPoint);
                getLineAndRun(difficultyText, parseDifficulty);
                getLineAndRun(bgText, parseBackground);
                getLineAndRun(metadataText, parseMetadata);
                getLineAndRun(generalText, parseGeneral);

                DifficultyCalculatorUtil.CalculateStrains(Contents.Objects);
                Contents.DifficultyRating = DifficultyCalculatorUtil.GetDifficultyMania(Contents.Objects);
            }
            catch (Exception e)
            {
                Reason = e.Message;
                return false;
            }

            return true;
        }

        private void getLineAndRun(string lines, GetLineDelegate callback)
        {
            using (StringReader reader = new StringReader(lines))
            {
                string line;
                while ((line = reader.ReadLine()) != null)
                {
                    line = line.Trim();

                    if (string.IsNullOrWhiteSpace(line))
                    {
                        continue;
                    }

                    callback(line);
                }
            }
        }

        #region ~~PARSING CODE~~

        private void parseGeneral(string line)
        {
            //all lines split by ":"
            string[] keyValuePair = line.Split(':');

            if (keyValuePair.Length != 2)
            {
                return;
            }

            switch (keyValuePair[0])
            {
                case "AudioFilename":
                    Contents.AudioPath = keyValuePair[1].Trim();
                    break;
                case "AudioLeadIn":
                    if (!double.TryParse(keyValuePair[1], out Contents.AudioLeadIn))
                    {
                        throw new Exception($"Failed to parse AudioLeadIn as type double (check [General] in .osu)\n\tline={line}");
                    }
                    break;
                case "PreviewTime":
                    if (!double.TryParse(keyValuePair[1], out Contents.SampleLength))
                    {
                        throw new Exception($"Failed to parse SampleLength as type double (check [General] in .osu)\n\tline={line}");
                    }
                    break;
            }
        }

        private void parseMetadata(string line)
        {
            //all lines split by ":"
            string[] keyValuePair = line.Split(':');

            if (keyValuePair.Length != 2)
            {
                return;
            }

            switch (keyValuePair[0])
            {
                case "Title":
                    Contents.Title = keyValuePair[1];
                    break;
                case "TitleUnicode":
                    Contents.TitleUnicode = keyValuePair[1];
                    break;
                case "Artist":
                    Contents.Artist = keyValuePair[1];
                    break;
                case "ArtistUnicode":
                    Contents.ArtistUnicode = keyValuePair[1];
                    break;
                case "Creator":
                    Contents.Creator = keyValuePair[1];
                    break;
                case "Version":
                    Contents.DifficultyName = keyValuePair[1];
                    break;
                case "Source":
                    Contents.Source = keyValuePair[1];
                    break;
            }
        }

        private void parseDifficulty(string line)
        {
            //all lines split by ":"
            string[] keyValuePair = line.Split(':');

            if (keyValuePair.Length != 2)
            {
                return;
            }

            switch (keyValuePair[0])
            {
                case "HPDrainRate":
                    if(!double.TryParse(keyValuePair[1], out Contents.HP))
                    {
                        throw new Exception($"Failed to parse HPDrainRate as type double (check [Difficulty] in .osu)\n\tline={line}");
                    }
                    break;
                case "OverallDifficulty":
                    if (!double.TryParse(keyValuePair[1], out Contents.OD))
                    {
                        throw new Exception($"Failed to parse OverallDifficulty as type double (check [Difficulty] in .osu)\n\tline={line}");
                    }
                    break;
                case "ApproachRate":
                    if (!double.TryParse(keyValuePair[1], out Contents.AR))
                    {
                        throw new Exception($"Failed to parse ApproachRate as type double (check [Difficulty] in .osu)\n\tline={line}");
                    }
                    break;
            }
        }

        private void parseBackground(string line)
        {
            if (line.StartsWith("//"))
            {
                return;
            }
            string[] parameters = line.Split(',');

            int eventType;

            if (parameters.Length != 5 || !int.TryParse(parameters[0], out eventType) || eventType != 0)
            {
                return;
            }
            //		....\path\to\osu            +    \bgfile
            Contents.BgPath = parameters[2].Replace("\"", "");
        }

        private void parseHitObject(string line)
        {
            string[] parameters = line.Split(',');
            //parameters should have 6 elements
            if (parameters.Length != 6)
            {
                return;
            }

            int col;
            if (!int.TryParse(parameters[0], out col))
            {
                throw new Exception($"Failed to parse HitObject X location as type int (check [HitObjects] in .osu)\n\tline={line}");
            }
            col /= (512 / 4);
            double time;
            if(!double.TryParse(parameters[2], out time))
            {
                throw new Exception($"Failed to parse HitObject time location as type int (check [HitObjects] in .osu)\n\tline={line}");
            }
            int typeInt;
            if(!int.TryParse(parameters[3], out typeInt))
            {
                throw new Exception($"Failed to parse HitObject type as type int (check [HitObjects] in .osu)\n\tline={line}");
            }

            HitObjectType type = (HitObjectType)typeInt;

            double endTime = 0;

            if (type == HitObjectType.Hold)
            {
                if(!double.TryParse(parameters[5].Split(':')[0], out endTime))
                {
                    throw new Exception($"Failed to parse HitObject endTime as type double (check [HitObjects] in .osu)\n\tline={line}");
                }
            }

            HitObject newObject = new HitObject(type, col, time, endTime);

            Contents.Objects.Add(newObject);
        }

        private void parseTimingPoint(string line)
        {
            //ingnore 3, 4, 5, 7
            //0 = offset, 1 = beatlength, 2 = time signature, 6 = timingchange
            //beatlength = bpms
            string[] parameters = line.Split(',');

            if (parameters.Length != 8)
            {
                return;
            }

            double start;
            if(!double.TryParse(parameters[0], out start))
            {
                throw new Exception($"Failed to parse TimingPoint start as type double (check [TimingPoints] in .osu)\n\tline={line}");
            }
            double newLength;
            if(!double.TryParse(parameters[1], out newLength))
            {
                throw new Exception($"Failed to parse TimingPoint beatLength as type double (check [TimingPoints] in .osu)\n\tline={line}");
            }

            if (!(newLength > 0))
            {
                return;
            }

            TimingPoint newPoint = new TimingPoint(start, newLength);

            if(Contents.TimingPoints.Count == 0 && newPoint.Start > 0)
            {
                Contents.TimingPoints.Add(new TimingPoint(0, newLength));
            }

            Contents.TimingPoints.Add(newPoint);
        }

        #endregion

        private string getFileSection(string fileText, string sectionName)
        {
            StringBuilder builder = new StringBuilder();
            StringReader reader = new StringReader(fileText);
            string line = null;
            bool isReading = false;
            while ((line = reader.ReadLine()) != null)
            {
                line = line.Trim();
                if (line.StartsWith("["))
                {
                    if (line.Equals($"[{sectionName}]"))
                    {
                        isReading = true;
                    }
                    else
                    {
                        isReading = false;
                    }
                }
                else
                {
                    if (isReading)
                    {
                        builder.AppendLine(line);
                    }
                }
            }
            reader.Dispose();
            return builder.ToString();
        }
    }
}