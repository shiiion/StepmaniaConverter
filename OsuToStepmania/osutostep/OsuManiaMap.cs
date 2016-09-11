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

        public HitObject(HitObjectType type, int col, double start, double end = 0)
        {
            Type = type;
            Col = col;
            Start = start;
            End = end;
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
        public bool AnchorPoint;

        public TimingPoint(double start, double beatLength, bool anchorPoint)
        {
            Start = start;
            BeatLength = beatLength;
            AnchorPoint = anchorPoint;
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
                    Contents.AudioLeadIn = double.Parse(keyValuePair[1]);
                    break;
                case "PreviewTime":
                    Contents.SampleLength = double.Parse(keyValuePair[1]);
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
                    Contents.HP = double.Parse(keyValuePair[1]);
                    break;
                case "OverallDifficulty":
                    Contents.OD = double.Parse(keyValuePair[1]);
                    break;
                case "ApproachRate":
                    Contents.AR = double.Parse(keyValuePair[1]);
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
            if (parameters.Length != 5 || int.Parse(parameters[0]) != 0)
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

            int col = int.Parse(parameters[0]) / (512 / 4);
            double time = double.Parse(parameters[2]);
            HitObjectType type = (HitObjectType)Int32.Parse(parameters[3]);
            double endTime = 0;

            if (type == HitObjectType.Hold)
            {
                endTime = int.Parse(parameters[5].Split(':')[0]);
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

            double start = double.Parse(parameters[0]);
            double newLength = double.Parse(parameters[1]);
            bool anchorPoint = (newLength > 0);

            if (!anchorPoint)
            {
                return;
            }

            TimingPoint newPoint = new TimingPoint(start, newLength, anchorPoint);

            if(Contents.TimingPoints.Count == 0 && newPoint.Start > 0)
            {
                Contents.TimingPoints.Add(new TimingPoint(0, newLength, anchorPoint));
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