using System;
using System.Collections.Generic;
using System.Text;

namespace osutostep
{
    public class Measure
    {
        //4 = 1/4 note, 8 = 1/8 note, etc
        private int beatDivision;
        public string[] Rows { get; set; }

        public Measure(int beatDivision)
        {
            this.beatDivision = beatDivision;
            Rows = new string[beatDivision];
            for (int a = 0; a < Rows.Length; a++)
            {
                Rows[a] = "0000";
            }
        }

        //take least common multiple of this beat division and the new beat division
        //copy each row LCM/beatdivision times into a new array containing LCM rows
        public void ChangeDivision(int newBeatDivision)
        {
            if (newBeatDivision <= beatDivision)
            {
                //no need to change, accuracy loss is bad
                return;
            }

            //faster if larger # is 2nd
            int resultBeatDivision = MathUtil.LCM(beatDivision, newBeatDivision);

            int previousInterval = resultBeatDivision / beatDivision;

            string[] resultRows = new string[resultBeatDivision];

            for (int a = 0; a < resultBeatDivision; a++)
            {
                if (a % previousInterval == 0)
                {
                    resultRows[a] = Rows[a / previousInterval];
                }
                else
                {
                    resultRows[a] = "0000";
                }
            }

            Rows = resultRows;
            beatDivision = resultBeatDivision;
        }

        public void AddStep(int col, double division, StepArrowType type)
        {
            double row, beatDiv;
            MathUtil.GetRichardsFraction(division, out row, out beatDiv);

            if ((int)beatDiv != beatDivision)
            {
                if (beatDiv > beatDivision)
                {
                    //scale up our beat division to fit the required precision 
                    //ex: 1/4 -> 1/8
                    ChangeDivision((int)beatDiv);
                }
                else
                {
                    //scale our row up to the higher precision beat division
                    row = (row * beatDivision) / beatDiv;
                }
            }

            if (row < Rows.Length && col < 4)
            {
                Rows[(int)row] = Rows[(int)row].Remove(col, 1).Insert(col, ((int)type).ToString());
            }
        }

        public override string ToString()
        {
            StringBuilder measureBuilder = new StringBuilder();
            foreach (string row in Rows)
            {
                measureBuilder.AppendLine(row);
            }
            return measureBuilder.ToString();
        }
    }

    public class BPM
    {
        public double Beat;
        public double BeatsPerMin;

        public BPM(double beat, double BPM)
        {
            Beat = beat;
            BeatsPerMin = BPM;
        }

        public override string ToString()
        {
            return $"{Beat}={BeatsPerMin}";
        }
    }

    public enum StepArrowType
    {
        Normal = 1, HoldBegin = 2, HoldEnd = 3
    }

    public class StepManiaMap
    {

        public class StepManiaHeaders
        {
            public string Title;
            public string Artist;
            public string TitleTranslate;
            public string ArtistTranslate;
            public string Credit;
            public string Banner;
            public string Background;
            public string SongPath;
            public double StartOffset;
            public List<BPM> TimingPoints = new List<BPM>();
            public double SampleStart;
            public double SampleLength;


            public override string ToString()
            {
                StringBuilder sb = new StringBuilder();
                sb.AppendLine($"#TITLE:{Title};");
                sb.AppendLine($"#ARTIST:{Artist};");
                sb.AppendLine($"#TITLETRANSLIT:{TitleTranslate};");
                sb.AppendLine($"#ARTISTTRANSLIT:{ArtistTranslate};");
                sb.AppendLine($"#GENRE:From-Osu;");
                sb.AppendLine($"#CREDIT:{Credit};");
                sb.AppendLine($"#BANNER:"/*{Banner}*/+";");
                sb.AppendLine($"#BACKGROUND:{Background};");
                sb.AppendLine($"#MUSIC:{SongPath};");
                sb.AppendLine($"#OFFSET:{StartOffset};");
                string BPMString = "#BPMS:";
                foreach (BPM point in TimingPoints)
                {
                    BPMString += point.ToString() + ",";
                }
                sb.AppendLine(BPMString);
                sb.AppendLine($"#SAMPLESTART:{SampleStart};");
                sb.AppendLine($"#SAMPLELENGTH:{SampleLength};");
                sb.AppendLine("#SELECTABLE:YES;");
                sb.AppendLine();
                return sb.ToString();
            }
        }

        public StepManiaHeaders Header { get; set; }

        public string CurrentDirectory { get; set; }

        private List<Measure> measures;

        //scale me up by 4!!!
        public double DifficultyValue { get; set; }

        public StepManiaMap(string path)
        {
            CurrentDirectory = path;
            Header = new StepManiaHeaders();
            measures = new List<Measure>();
        }

        public void AddBPM(double Beat, double BPM)
        {
            Header.TimingPoints.Add(new BPM(Beat, BPM));
        }

        //4 beats = 1 measure
        public void AddObject(int col, double beat, StepArrowType type)
        {
            int measure = (int)(beat / 4);

            //auto-resize measure array to fit the new object in
            if (measure >= measures.Count)
            {
                int newMeasureCount = (measure + 1) - measures.Count;
                for (int a = 0; a < newMeasureCount; a++)
                {
                    measures.Add(new Measure(4));
                }
            }

            Measure m = measures[measure];
            m.AddStep(col, (beat - (measure * 4)) / 4.0, type);
        }

        private string getDiffName()
        {
            if(DifficultyValue <= 2)
            {
                return "Beginner";
            }
            if(DifficultyValue <= 4)
            {
                return "Easy";
            }
            if (DifficultyValue <= 7)
            {
                return "Normal";
            }
            if (DifficultyValue < 10)
            {
                return "Hard";
            }
                return "Challenge";
        }

        public override string ToString()
        {
            StringBuilder scBuilder = new StringBuilder();

            scBuilder.Append(Header.ToString());

            scBuilder.AppendLine("#NOTES:");
            scBuilder.AppendLine("dance-single:");
            scBuilder.AppendLine(":");
            //!!RENAME ME LATER!!
            scBuilder.AppendLine($"{getDiffName()}:");
            scBuilder.AppendLine($"{Math.Round(DifficultyValue)}:");
            scBuilder.AppendLine("0.000000,0.000000,0.000000,0.000000,0.000000,0.000000,0.000000,0.000000,0.000000,0.000000,0.000000,0.000000,0.000000,0.000000,0.000000,0.000000,0.000000,0.000000,0.000000,0.000000,0.000000,0.000000:");
            for (int a = 0; a < measures.Count; a++)
            {
                scBuilder.Append(measures[a].ToString());
                if (a != (measures.Count - 1))
                {
                    scBuilder.AppendLine(",");
                }
            }
            scBuilder.AppendLine(";");

            return scBuilder.ToString();
        }
    }
}