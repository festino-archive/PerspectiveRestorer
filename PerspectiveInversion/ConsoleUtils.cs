using MathNet.Numerics.LinearAlgebra;
using System;

namespace PerspectiveInversion
{
    static class ConsoleUtils
    {
        public static void WriteLine(PointD[] arg)
        {
            string text = "[ ";
            for (int i = 0; i < arg.Length; i++)
            {
                if (i > 0)
                    text += ", ";
                text += "(";
                text += arg[i].X.ToString("f2");
                text += "; ";
                text += arg[i].Y.ToString("f2");
                text += ")";
            }
            text += " ]";
            Console.WriteLine(text);
        }
        public static void WriteLine(double[] arg)
        {
            string text = "[ ";
            for (int i = 0; i < arg.Length; i++)
            {
                if (i > 0)
                    text += ", ";
                text += arg[i].ToString("f3");
            }
            text += " ]";
            Console.WriteLine(text);
        }
        public static void WriteLine(double[,] arg)
        {
            int M = arg.GetLength(0);
            int N = arg.GetLength(1);
            for (int i = 0; i < M; i++)
            {
                string text = "";
                if (i == 0)
                    text += "Г ";
                else if (i == M - 1)
                    text += "L ";
                else
                    text += "| ";

                for (int j = 0; j < N; j++)
                {
                    if (j > 0)
                        text += ", ";
                    text += arg[i, j].ToString("f3");
                }

                /*if (i == 0)
                    text += " ꓶ";
                else if (i == M - 1)
                    text += " ⅃";
                else
                    text += " |";*/
                Console.WriteLine(text);
            }
        }
        public static void WriteLine(Matrix<double> arg)
        {
            WriteLine(arg.ToArray());
        }
    }
}
