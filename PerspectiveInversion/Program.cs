
using System;

namespace PerspectiveInversion
{
    class Program
    {

        static void Main(string[] args)
        {
            double a = 2, b = 1;
            double[][] initPoints = {
                new double[] { -a, -b, 0 },
                new double[] { a, -b, 0 },
                new double[] { -a, b, 0 },
                new double[] { a, b, 0 }
            };
            double[] c = { 0, 0, -1 };
            double yaw = 290, pitch = -30, roll = 40;
            CameraParameters param = new CameraParameters(c, yaw, pitch, roll);
            PointD[] Bpoints = Transform.Straight(initPoints, param);
            CameraParameters restoredParam = Transform.Inversed(Bpoints, a, b);

            // compare
            Console.WriteLine();
            ConsoleUtils.WriteLine(param.C);
            ConsoleUtils.WriteLine(restoredParam.C);
            Console.WriteLine();
            ConsoleUtils.WriteLine(param.R);
            ConsoleUtils.WriteLine(restoredParam.R);
            Console.WriteLine();
            ConsoleUtils.WriteLine(param.Angles);
            ConsoleUtils.WriteLine(restoredParam.Angles);
            Console.WriteLine();
            ConsoleUtils.WriteLine(param.Distances(restoredParam));
            Console.WriteLine();
        }
    }
}
