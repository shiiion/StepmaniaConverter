//using System;

//namespace osutostep.osubase
//{

//    internal enum ManiaNoteType
//    {
//        Normal,
//        Long,
//        Special
//    }

//    internal class HitCircleMania : HitCircleBase
//    {
//        internal int LogicalColumn;
//        internal ManiaNoteType ManiaType = ManiaNoteType.Normal;
//        internal double TimePress;
//        internal double TimeRelease;
//        internal bool Pressed;
//        internal int Siblings = 1;
//        internal double Length = 0;
//        internal bool IsMissing;
//        internal bool IsFinished;
//        internal double EndTime;

//        public int Col { get; set; }

//        public HitCircleMania(int column, double startTime)
//        {
//            StartTime = startTime;
//            Col = column;
//            LogicalColumn = column;

//            Position = new Vector2((512 / 8) * (Col + 1), -100);
//            StartTime = startTime;
//            EndTime = startTime;
            
//        }

//        public int ComboNumber
//        {
//            get { return 0; }
//            set { }
//        }

//        public bool NewCombo
//        {
//            get;
//            set;
//        }

//        public override Vector2 EndPosition
//        {
//            get { return Position; }
//            set { throw new NotImplementedException(); }
//        }
//    }
//}
