using System;

namespace osutostep
{
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