//using System;
//using System.Collections.Generic;

////OH LOL
//namespace osutostep.osubase
//{
//    public enum ManiaConvertType
//    {
//        None = 0,
//        /// <summary>
//        /// Keep the same as last row.
//        /// </summary>
//        ForceStack = 1,
//        /// <summary>
//        /// Keep different from last row.
//        /// </summary>
//        ForceNotStack = 2,
//        /// <summary>
//        /// Keep as single note at its original position.
//        /// </summary>
//        KeepSingle = 4,
//        /// <summary>
//        /// Use a lower random value.
//        /// </summary>
//        LowProbability = 8,
//        /// <summary>
//        /// Reserved.
//        /// </summary>
//        Alternate = 16,
//        /// <summary>
//        /// Ignore the repeat count.
//        /// </summary>
//        ForceSigSlider = 32,
//        /// <summary>
//        /// Convert slider to circle.
//        /// </summary>
//        ForceNotSlider = 64,
//        /// <summary>
//        /// Notes gathered together.
//        /// </summary>
//        Gathered = 128,
//        Mirror = 256,
//        /// <summary>
//        /// Change 0 -> 6.
//        /// </summary>
//        Reverse = 512,
//        /// <summary>
//        /// 1 -> 5 -> 1 -> 5 like reverse.
//        /// </summary>
//        Cycle = 1024,
//        /// <summary>
//        /// Next note will be at column + 1.
//        /// </summary>
//        Stair = 2048,
//        /// <summary>
//        /// Next note will be at column - 1.
//        /// </summary>
//        ReverseStair = 4096,
//        /// <summary>
//        /// For specific beatmaps.
//        /// </summary>
//        NotChange = 8192
//    }

//    public struct Vector2
//    {
//        public Vector2(double x, double y)
//        {
//            X = x;
//            Y = y;
//        }

//        double X;
//        double Y;

//        public static Vector2 operator -(Vector2 a, Vector2 b)
//        {
//            return new Vector2(a.X - b.X, a.Y - b.Y);
//        }

//        public static Vector2 operator +(Vector2 a, Vector2 b)
//        {
//            return new Vector2(a.X + b.X, a.Y + b.Y);
//        }

//        public double Length()
//        {
//            return Math.Sqrt(X * X + Y * Y);
//        }
//    }

//    public class HitCircleBase
//    {
//        public Vector2 Position { get; set; }
//        public double StartTime { get; set; }

//        internal virtual bool IsVisible
//        {
//            get { return true; }
//        }

//        public virtual Vector2 EndPosition
//        {
//            get { return Position; }
//            set { throw new NotImplementedException(); }
//        }
//    }

//    internal class HitCircleManiaRow : HitCircleBase
//    {
//        internal List<HitCircleMania> HitObjects;
//        private ManiaConvertType convertType;
//        private bool[] lastRow;
//        private int lastCount = 0;
//        private int lastColumn = 0; //used when lastCount == 1

//        public HitCircleManiaRow(Vector2 startPosition, double startTime, ManiaConvertType cvtType, bool[] lastRow)
//        {
//            HitObjects = new List<HitCircleMania>();
//            convertType = cvtType;
//            this.lastRow = lastRow;
//            Position = startPosition;
//            StartTime = startTime;

//            for (int i = 0; i < lastRow.Length; i++)
//            {
//                if (lastRow[i])
//                {
//                    lastCount++;
//                    lastColumn = i;
//                }
//            }
//        }

//        internal void GenerateHitObjects(double MapDifficultyBemani)
//        {

//            //sample specific,arranged by priority
//            //don't generate mirror style for 7+1K
//            if ((convertType & ManiaConvertType.KeepSingle) == 0)
//            {
//                if (true)
//                    convertType |= ManiaConvertType.Mirror;
//            }
            

//            if ((convertType & ManiaConvertType.Reverse) > 0 && lastCount > 0)
//            {
//                for (int i = 0; i < lastRow.Length; i++)
//                {
//                    if (lastRow[i])
//                        Add(4 - i - 1);
//                }
//                PostProcessing();
//                return;
//            }
//            if ((convertType & ManiaConvertType.Cycle) > 0 && lastCount == 1)
//            {
//                //make sure last note not in centre column.
//                if (true)
//                {
//                    lastColumn = 4 - lastColumn - 1;
//                    Add(lastColumn);
//                    PostProcessing();
//                    return;
//                }
//            }
//            if ((convertType & ManiaConvertType.ForceStack) > 0 && lastCount > 0)
//            {
//                //keep the same column with last row
//                for (int i = 0; i < lastRow.Length; i++)
//                {
//                    if (lastRow[i])
//                        Add(i);
//                }
//                PostProcessing();
//                return;
//            }
//            if ((convertType & (ManiaConvertType.Stair | ManiaConvertType.ReverseStair)) > 0 && lastCount == 1)
//            {
//                if ((convertType & ManiaConvertType.Stair) > 0)
//                {
//                    lastColumn++;
//                    if (lastColumn == 4)
//                        lastColumn = 0;
//                }
//                else
//                {
//                    lastColumn--;
//                    if (lastColumn == 0 - 1)
//                        lastColumn = 4 - 1;
//                }
//                Add(lastColumn);
//                PostProcessing();
//                return;
//            }
//            if ((convertType & ManiaConvertType.KeepSingle) > 0)
//            {
//                AddRandomNote(1);
//                PostProcessing();
//                return;
//            }

//            if ((convertType & ManiaConvertType.Mirror) > 0)
//            {
//                if (MapDifficultyBemani > 6.5)
//                {
//                    NoteCalculationMirror(0.88f, 0.88f, 0.62f);
//                }
//                else if (MapDifficultyBemani > 4.0)
//                {
//                    NoteCalculationMirror(0.88f, 1f, 0.83f);
//                }
//                else
//                    NoteCalculationMirror(0.88f, 1f, 1f);
//            }
//            else
//            {
//                if (MapDifficultyBemani > 6.5)
//                {
//                    if ((convertType & ManiaConvertType.LowProbability) > ManiaConvertType.None)
//                        NoteCalculationNormal(1f, 1f, 0.58f, 0.22f);
//                    else
//                        NoteCalculationNormal(1f, 1f, 0.38f, 0);
//                }
//                else if (MapDifficultyBemani > 4.0)
//                {
//                    if ((convertType & ManiaConvertType.LowProbability) > ManiaConvertType.None)
//                        NoteCalculationNormal(1f, 1f, 0.92f, 0.65f);
//                    else
//                        NoteCalculationNormal(1f, 1f, 0.85f, 0.48f);
//                }
//                else if (MapDifficultyBemani > 2.0)
//                {
//                    if ((convertType & ManiaConvertType.LowProbability) > ManiaConvertType.None)
//                        NoteCalculationNormal(1f, 1f, 1f, 0.82f);
//                    else
//                        NoteCalculationNormal(1f, 1f, 1f, 0.55f);
//                }
//                else
//                    NoteCalculationNormal(1f, 1f, 1f, 1f);
//            }
//            PostProcessing();
//        }

//        /// <summary>
//        /// calculate how many notes in a row by giving probability
//        /// </summary>
//        /// <param name="noteNeed5"></param>
//        /// <param name="noteNeed4"></param>
//        /// <param name="noteNeed3"></param>
//        /// <param name="noteNeed2"></param>
//        /// <param name="rn"></param>
//        private void NoteCalculationNormal(float noteNeed5, float noteNeed4, float noteNeed3, float noteNeed2)
//        {
//            //reduce probability by Column limit.
//            switch (4)
//            {
//                case 4:
//                    noteNeed5 = 1f;
//                    noteNeed4 = 1f;
//                    noteNeed3 = Math.Max(noteNeed3, 0.96f);
//                    noteNeed2 = Math.Max(noteNeed2, 0.77f);
//                    break;
//            }
//            if (true)
//                noteNeed2 = 0;
//            int noteNeed = HitFactoryMania.GetRandomValue(noteNeed2, noteNeed3, noteNeed4, noteNeed5);
//            AddRandomNote(noteNeed);
//        }

//        /// <summary>
//        /// Decide how many notes put on each side
//        /// turn to NoteCalculationNormal if ForceNotStack in convert indication.
//        /// </summary>
//        /// <param name="centre">probability of putting a note in the centre column</param>
//        /// <param name="noteNeed3"></param>
//        /// <param name="noteNeed2"></param>
//        /// <param name="rn"></param>
//        private void NoteCalculationMirror(float centre, float noteNeed3, float noteNeed2)
//        {
//            if ((convertType & ManiaConvertType.ForceNotStack) > ManiaConvertType.None)
//            {
//                NoteCalculationNormal(noteNeed3, (noteNeed2 + noteNeed3) / 2, noteNeed2, noteNeed2 / 2);
//            }
//            else
//            {
//                        noteNeed2 = Math.Max(0.8f, noteNeed2 * 2);
//                        noteNeed3 = 1f;
//                        centre = 1f;

//                double val = HitFactoryMania.random.NextDouble();
//                bool mid = false;
//                if (val > centre)
//                {
//                    mid = true;
//                }
//                val = HitFactoryMania.random.NextDouble();
//                if (val >= noteNeed3)
//                {
//                    AddMirrorNote(3);
//                    mid = false;
//                }
//                else if (val >= noteNeed2)
//                {
//                    AddMirrorNote(2);
//                }
//                else
//                {
//                    AddMirrorNote(1);
//                }

//            }
//        }

//        //if noteCount==1,note will be placed at it's original column calculated by .ColumnAt
//        private void AddRandomNote(int noteCount)
//        {
//            if ((convertType & ManiaConvertType.ForceNotStack) > ManiaConvertType.None)
//            {
//                int usable = 4;
//                for (int i = 0; i < 4; i++)
//                    usable -= lastRow[i] ? 1 : 0;

//                if (noteCount > usable)
//                    noteCount = usable;
//            }

//            int nextCol =  512 / 4;
//            bool[] currRow = new bool[4];
//            while (noteCount > 0)
//            {
//                while (currRow[nextCol] || (lastRow[nextCol] && (convertType & ManiaConvertType.ForceNotStack) > ManiaConvertType.None))
//                {
//                    if ((convertType & ManiaConvertType.Gathered) > 0)
//                    {
//                        nextCol++;
//                        if (nextCol == 4)
//                            nextCol = 0;
//                    }
//                    else
//                        nextCol = HitFactoryMania.random.Next(0, 4);
//                }
//                Add(nextCol);
//                currRow[nextCol] = true;
//                noteCount--;
//            }
//        }

//        private void AddMirrorNote(int noteCount)
//        {
//            bool[] currRow = new bool[4];
//            int upper = 4 % 2 == 0 ? 4 / 2 : (4 - 1) / 2;
//            int next = HitFactoryMania.random.Next(0, upper);
//            for (int i = 0; i < noteCount; i++)
//            {
//                while (currRow[next])
//                    next = HitFactoryMania.random.Next(0, upper);
//                currRow[next] = true;
//                currRow[4 - next - 1] = true;
//                Add(next);
//                Add(4 - next - 1);
//            }
//        }

//        private void Add(int col)
//        {
//            HitCircleMania note = new HitCircleMania(col, StartTime);
//            HitObjects.Add(note);
//        }

//        private void PostProcessing()
//        {
//            int siblings = HitObjects.Count;
//            if (siblings == 0)
//                return;

//            HitObjects.ForEach(h =>
//            {
//                h.Siblings = siblings;
//            });
//        }

//        internal override bool IsVisible
//        {
//            get { return true; }
//        }
//        public override Vector2 EndPosition
//        {
//            get { return Position; }
//            set { throw new NotImplementedException(); }
//        }

//    }
//}
