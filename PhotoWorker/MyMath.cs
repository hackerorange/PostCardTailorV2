using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PhotoWorker
{
    class MyMath
    {
        public static float MMtoInch(double value)
        {
            return (float)value * 0.039382716049382716049382716049383f;
        }
        public static float MMtoPix(float value)
        {
            return 72 * MMtoInch(value);
        }

    }
}
