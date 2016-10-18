//using System;
//using System.Collections.Generic;

//namespace osutostep.osubase
//{

//    class HitFactoryMania
//    {
//        internal static FastRandom random = new FastRandom(1337);
//        internal static FastRandom randomSync = new FastRandom(1337);
//        internal static FastRandom randomSpecial = new FastRandom(10086);//for non-conversion random.

//        private static Vector2 prevPos;
//        private static double prevTime;
//        private static double prevDelta;
//        private static bool[] prevRow;
//        private static List<double> prevNotes;
//        private static readonly int prevCount = 7;
//        private static double noteDensity = int.MaxValue;
//        private static ManiaConvertType currentStair = ManiaConvertType.Stair;

//        public static void Init(double HP, double CS, double OD, double AR)
//        {

//            int seed = (int)Math.Round(HP + CS)
//                * 20 + (int)(OD * 41.2) + (int)Math.Round(AR);
//            random.Reinitialise(seed);
//            randomSync.Reinitialise(seed);

//            prevNotes = new List<double>(prevCount);
//            record(new Vector2(0, 0), 0);
//            prevRow = new bool[4];
//        }

//        public static int GetRandomValue(float noteNeed2, float noteNeed3, float noteNeed4 = 1, float noteNeed5 = 1, float noteNeed6 = 1)
//        {
//            double val = random.NextDouble();
//            if (val >= noteNeed6)
//                return 6;
//            else if (val >= noteNeed5)
//                return 5;
//            else if (val >= noteNeed4)
//                return 4;
//            else if (val >= noteNeed3)
//                return 3;
//            else if (val >= noteNeed2)
//                return 2;
//            else
//                return 1;
//        }

//        private static void densityUpdate(double Time)
//        {
//            if (prevNotes.Count == prevCount)
//                prevNotes.RemoveAt(0);
//            prevNotes.Add(Time);
//            if (prevNotes.Count >= 2)
//            {
//                noteDensity = (double)(prevNotes[prevNotes.Count - 1] - prevNotes[0]) / prevNotes.Count;
//            }
//        }

//        private static void record(Vector2 position, double Time)
//        {
//            prevDelta = Time - prevTime;
//            prevTime = Time;
//            prevPos = position;
//        }

//        internal static HitCircleManiaRow CreateHitCircle(Vector2 startPosition, double startTime, bool newCombo, int comboOffset)
//        {
//            HitCircleManiaRow r;
//            {
//                densityUpdate(startTime);//update before generate notes

//                double delta = startTime - prevTime;
//                double beatIntv = hitObjectManager.Beatmap.BeatLengthAt(startTime, false);

//                ManiaConvertType ctype = ManiaConvertType.None;
//                if (delta <= 80)// more than 187bpm
//                    ctype = ManiaConvertType.ForceNotStack | ManiaConvertType.KeepSingle;
//                else if (delta <= 95)  //more than 157bpm
//                    ctype = ManiaConvertType.ForceNotStack | ManiaConvertType.KeepSingle | currentStair;
//                else if (delta <= 105)//140bpm
//                    ctype = ManiaConvertType.ForceNotStack | ManiaConvertType.LowProbability;
//                else if (delta <= 125)//120bpm
//                    ctype = ManiaConvertType.ForceNotStack | ManiaConvertType.None;
//                else
//                {
//                    double deltaPos = (startPosition - prevPos).Length();

//                    if (delta <= 135 && deltaPos < 20)  //111bpm
//                        ctype = ManiaConvertType.Cycle | ManiaConvertType.KeepSingle;
//                    else if (delta <= 150 && deltaPos < 20)  //100bpm stream, forceStack
//                        ctype = ManiaConvertType.ForceStack | ManiaConvertType.LowProbability;
//                    else if (deltaPos < 20 && noteDensity >= beatIntv / 2.5)
//                        ctype = ManiaConvertType.Reverse | ManiaConvertType.LowProbability;
//                    else if (noteDensity < beatIntv / 2.5)  //high note density
//                        ctype = ManiaConvertType.None;
//                    else   //low note density
//                        ctype = ManiaConvertType.LowProbability;
//                }

//                r = new HitCircleManiaRow(startPosition, startTime, ctype, prevRow);

//                r.GenerateHitObjects(bemani);

//                prevRow = new bool[4];
//                foreach (HitCircleMania note in r.HitObjects)
//                {
//                    prevRow[note.LogicalColumn] = true;
//                    if ((ctype & ManiaConvertType.Stair) > 0 && note.LogicalColumn == 3)
//                        currentStair = ManiaConvertType.ReverseStair;
//                    else if ((ctype & ManiaConvertType.ReverseStair) > 0 && note.LogicalColumn == 0)
//                        currentStair = ManiaConvertType.Stair;
//                }

//                record(startPosition, startTime);
//            }
//            return r;
//        }

//        internal static Slider CreateSlider(Vector2 startPosition, double startTime, bool newCombo, HitObjectSoundType soundType,
//                                              CurveTypes curveType, int repeatCount, double sliderLength, List<Vector2> sliderPoints, List<HitObjectSoundType> soundTypes, int comboOffset,
//                                              SampleSet sampleSet, SampleSet sampleSetAddition, List<SampleSet> sampleSets, List<SampleSet> sampleSetAdditions, CustomSampleSet customSampleSet,
//                                              int volume, string sampleFile)
//        {
//            SliderMania s;
//            if (hitObjectManager.Beatmap.PlayMode == PlayModes.OsuMania)
//            {
//                s = new SliderMania(hitObjectManager, startPosition, startTime, soundType, repeatCount, sliderLength, sliderPoints, soundTypes, ManiaConvertType.NotChange, prevRow)
//                {
//                    SampleSet = sampleSet,
//                    SampleSetAdditions = sampleSetAddition,
//                    CustomSampleSet = customSampleSet,
//                    SampleVolume = volume
//                };

//                s.GenerateHitObjects();
//                s.HitObjects.ForEach(n => n.ProcessSampleFile(sampleFile));

//                record(startPosition, s.EndTime);
//            }
//            else
//            {
//                ControlPoint cp = hitObjectManager.Beatmap.ControlPointAt(startTime);
//                ManiaConvertType ctype = ManiaConvertType.None;
//                if (cp != null && !cp.KiaiMode)
//                    ctype = ManiaConvertType.LowProbability;

//                s = new SliderMania(hitObjectManager, startPosition, startTime, soundType, repeatCount, sliderLength, sliderPoints, soundTypes, ctype, prevRow)
//                {
//                    SampleSet = sampleSet,
//                    SampleSetAdditions = sampleSetAddition,
//                    SampleSetList = sampleSets,
//                    SampleSetAdditionList = sampleSetAdditions,
//                    CustomSampleSet = customSampleSet,
//                    SampleVolume = volume
//                };

//                s.GenerateHitObjects();

//                prevRow = new bool[hitObjectManager.ManiaStage.Columns.Count];
//                if (s.HitObjects.Count > 1)
//                {
//                    foreach (HitCircleMania hb in s.HitObjects.FindAll(h => h.EndTime == s.EndTime))
//                        prevRow[hb.LogicalColumn] = true;
//                }
//                else
//                {
//                    prevRow[s.HitObjects[0].LogicalColumn] = true;
//                }

//                s.HitObjects.ForEach(n => n.ProcessSampleFile(sampleFile));

//                double intv = (s.EndTime - startTime) / repeatCount;
//                while (repeatCount-- >= 0)
//                {
//                    record(startPosition, startTime);
//                    densityUpdate(startTime);
//                    startTime += intv;
//                }
//            }
//            return s;
//        }

//        internal static Spinner CreateSpinner(double startTime, double endTime, HitObjectSoundType soundType,
//                                                SampleSet sampleSet, SampleSet sampleSetAddition, CustomSampleSet customSampleSet,
//                                                int volume, string sampleFile)
//        {
//            SpinnerMania s = new SpinnerMania(hitObjectManager, startTime, endTime, soundType, ManiaConvertType.ForceNotStack, prevRow)
//            {
//                SampleSet = sampleSet,
//                SampleSetAdditions = sampleSetAddition,
//                CustomSampleSet = customSampleSet,
//                SampleVolume = volume
//            };

//            s.GenerateHitObjects();
//            s.HitObjects.ForEach(n => n.ProcessSampleFile(sampleFile));

//            record(new Vector2(256, 192), endTime);
//            densityUpdate(endTime);
//            return s;
//        }

//        internal static HitCircle CreateSpecial(Vector2 startPosition, double startTime, double endTime, bool newCombo,
//                                                  HitObjectSoundType soundType, int comboOffset, SampleSet sampleSet, SampleSet sampleSetAddition, CustomSampleSet customSampleSet, int volume, string sampleFile)
//        {
//            //notice:this type of note is ONLY generated by bms converter or future editor version
//            HitCircleManiaHold h = new HitCircleManiaHold(hitObjectManager, hitObjectManager.ManiaStage.ColumnAt(startPosition), startTime, endTime, soundType)
//            {
//                SampleSet = sampleSet,
//                SampleSetAdditions = sampleSetAddition,
//                CustomSampleSet = customSampleSet,
//                SampleVolume = volume
//            };

//            h.ProcessSampleFile(sampleFile);

//            record(startPosition, endTime);
//            densityUpdate(endTime);
//            return h;
//        }
//    }
//}
