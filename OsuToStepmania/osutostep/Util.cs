using System;
using System.Collections.Generic;

namespace osutostep
{
    //ripped from osu client
    class DifficultyCalculatorUtil
    {
        private static readonly double MANIA_STAR_SCALE = 0.018;
        private static readonly double STRAIN_STEP = 400;
        private static readonly double INDIVIDUAL_DECAY_BASE = 0.125;
        private static readonly double OVERALL_DECAY_BASE = 0.30;
        private static readonly double DECAY_WEIGHT = 0.9;

        public static void CalculateStrains(List<HitObject> objects)
        {
            List<HitObject>.Enumerator enumerator = objects.GetEnumerator();

            if (!enumerator.MoveNext()) return;

            HitObject cur = enumerator.Current;
            HitObject next;

            while(enumerator.MoveNext())
            {
                next = enumerator.Current;
                CalculateStrain(next, cur);
                cur = next;
            }
        }

        private static void CalculateStrain(HitObject obj, HitObject prev)
        {
            double addition = 1;
            double timeElapsed = (obj.Start - prev.Start);
            double individualDecay = Math.Pow(INDIVIDUAL_DECAY_BASE, timeElapsed / 1000);
            double overallDecay = Math.Pow(OVERALL_DECAY_BASE, timeElapsed / 1000);

            double holdFactor = 1.0; // Factor to all additional strains in case something else is held
            double holdAddition = 0; // Addition to the current note in case it's a hold and has to be released awkwardly

            // Fill up the heldUntil array
            // 4 columns
            for (int i = 0; i < 4; ++i)
            {
                obj.heldUntil[i] = prev.heldUntil[i];

                // If there is at least one other overlapping end or note, then we get an addition, buuuuuut...
                if (obj.Start < obj.heldUntil[i] && obj.End > obj.heldUntil[i])
                {
                    holdAddition = 1.0;
                }

                // ... this addition only is valid if there is _no_ other note with the same ending. Releasing multiple notes at the same time is just as easy as releasing 1
                if (obj.End == obj.heldUntil[i])
                {
                    holdAddition = 0;
                }

                // We give a slight bonus to everything if something is held meanwhile
                if (obj.heldUntil[i] > obj.End)
                {
                    holdFactor = 1.25;
                }

                // Decay individual strains
                obj.individualStrains[i] = prev.individualStrains[i] * individualDecay;
            }

            obj.heldUntil[obj.Col] = obj.End;

            // Increase individual strain in own column
            obj.IndividualStrain += (2.0/* + (double)SpeedMania.Column / 8.0*/) * holdFactor;

            obj.OverallStrain = prev.OverallStrain * overallDecay + (addition + holdAddition) * holdFactor;
        }

        public static double GetDifficultyMania(List<HitObject> objects)
        {

            List<double> highestStrains = new List<double>();

            //TimeRate never changes from 1 (no HT or DT)
            double intervalEndTime = STRAIN_STEP;
            double maximumStrain = 0;
            HitObject previous = null;

            foreach (HitObject ho in objects)
            {
                while(ho.Start > intervalEndTime)
                {
                    highestStrains.Add(maximumStrain);
                    if(previous == null)
                    {
                        maximumStrain = 0;
                    }
                    else
                    {
                        double individualDecay = Math.Pow(INDIVIDUAL_DECAY_BASE, (intervalEndTime - previous.Start) / 1000);
                        double overallDecay = Math.Pow(OVERALL_DECAY_BASE, (intervalEndTime - previous.Start) / 1000);
                        maximumStrain = previous.IndividualStrain * individualDecay + previous.OverallStrain * overallDecay;
                    }

                    intervalEndTime += STRAIN_STEP;
                }

                double strain = ho.IndividualStrain + ho.OverallStrain;
                maximumStrain = Math.Max(strain, maximumStrain);

                previous = ho;
            }

            double difficulty = 0;
            double weight = 1;
            highestStrains.Sort((a, b) => b.CompareTo(a));

            foreach(double strain in highestStrains)
            {
                difficulty += weight * strain;
                weight *= DECAY_WEIGHT;
            }

            return difficulty * MANIA_STAR_SCALE;
        }
    }

    class MathUtil
    {
        //MOVEME
        public static int LCM(int a, int b)
        {
            int num1, num2;
            if (a > b)
            {
                num1 = a; num2 = b;
            }
            else
            {
                num1 = b; num2 = a;
            }

            for (int i = 1; i <= num2; i++)
            {
                if ((num1 * i) % num2 == 0)
                {
                    return i * num1;
                }
            }
            return num2;
        }

        //https://www.maa.org/sites/default/files/pdf/upload_library/22/Allendoerfer/1982/0025570x.di021121.02p0002y.pdf
        public static int GetRichardsFraction(double dec, out double num, out double den)
        {
            //exclude sign
            double g = dec;
            long a = 0;
            long b = 1;
            long c = 1;
            long d = 0;
            long s;
            int iter = 0;

            do
            {
                s = (long)Math.Floor(g);
                num = a + s * c;
                den = b + s * d;
                a = c;
                b = d;
                c = (long)num;
                d = (long)den;
                g = 1.0 / (g - s);
                if ((1e-10) > Math.Abs(num / den - dec))
                {
                    return iter;
                }
            }
            while (iter++ < 1e6);
            return iter;
        }
    }
}