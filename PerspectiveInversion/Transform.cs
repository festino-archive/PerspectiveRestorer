using System;
using System.Numerics;
using MathNet.Numerics.LinearAlgebra;
using MathNet.Numerics.LinearAlgebra.Factorization;

namespace PerspectiveInversion
{
    public class Transform
    {
        private static MatrixBuilder<double> M = Matrix<double>.Build;
        private static VectorBuilder<double> V = MathNet.Numerics.LinearAlgebra.Vector<double>.Build;

        public static PointD[] Straight(double[][] initPoints, CameraParameters param)
        {
            int N = initPoints.Length;
            MathNet.Numerics.LinearAlgebra.Vector<double> C = param.Pos;
            Matrix<double> R = param.R;
            R = M.DenseOfDiagonalArray(new double[] { 1, -1, -1 }) * R;

            MathNet.Numerics.LinearAlgebra.Vector<double>[] Apoints = new MathNet.Numerics.LinearAlgebra.Vector<double>[N];
            for (int i = 0; i < N; i++)
                Apoints[i] = V.DenseOfArray(initPoints[i]);

            MathNet.Numerics.LinearAlgebra.Vector<double>[] Cpoints = new MathNet.Numerics.LinearAlgebra.Vector<double>[N];
            for (int i = 0; i < N; i++)
                Cpoints[i] = R * (Apoints[i] - C);

            PointD[] Bpoints = new PointD[N];
            for (int i = 0; i < N; i++)
                Bpoints[i] = new PointD(Cpoints[i][0] / Cpoints[i][2], Cpoints[i][1] / Cpoints[i][2]);

            return Bpoints;
        }

        public static CameraParameters Inversed(PointD[] Bpoints, double a, double b)
        {
            PointD[] Bmirrored = new PointD[Bpoints.Length];
            for (int i = 0; i < Bpoints.Length; i++)
                Bmirrored[i] = new PointD(-Bpoints[i].X, Bpoints[i].Y);
            Bpoints = Bmirrored;

            PointD[] origPoints = {
                new PointD(-a, -b),
                new PointD(a, -b),
                new PointD(-a, b),
                new PointD(a, b)
            };
            // https://github.com/opencv/opencv/blob/master/modules/imgproc/src/imgwarp.cpp
            double[,] system = new double[8, 8];
            for (int i = 0; i < 4; i++)
            {
                int row = i;
                system[row, 0] = origPoints[i].X;
                system[row, 1] = origPoints[i].Y;
                system[row, 2] = 1;
                system[row, 6] = -origPoints[i].X * Bpoints[i].X;
                system[row, 7] = -origPoints[i].Y * Bpoints[i].X;
                system[row + 4, 3] = origPoints[i].X;
                system[row + 4, 4] = origPoints[i].Y;
                system[row + 4, 5] = 1;
                system[row + 4, 6] = -origPoints[i].X * Bpoints[i].Y;
                system[row + 4, 7] = -origPoints[i].Y * Bpoints[i].Y;
            }
            double[] B = {
                Bpoints[0].X, Bpoints[1].X, Bpoints[2].X, Bpoints[3].X,
                Bpoints[0].Y, Bpoints[1].Y, Bpoints[2].Y, Bpoints[3].Y
            };

            Matrix<double> mathSystem = Matrix<double>.Build.DenseOfArray(system);
            Evd<double> eigen = mathSystem.Evd();
            if (!eigen.IsFullRank)
            {
                Console.WriteLine("no solution");
                return null;
            }
            var systemSol = mathSystem.Solve(V.DenseOfArray(B));

            double[] Cval = { -systemSol[2], -systemSol[5], -1 };
            double[,] R = new double[3, 3];

            for (int i = 0; i < 3; i++)
            {
                R[i, 0] = systemSol[3 * i];
                R[i, 1] = systemSol[3 * i + 1];
            }
            // norm R - the same for all columns
            double norm2 = 0;
            for (int i = 0; i < 3; i++)
                norm2 += R[i, 0] * R[i, 0];
            double norm = 1 / Math.Sqrt(norm2);
            if (R[1, 1] < 0)
                norm = -norm;
            for (int j = 0; j < 2; j++)
                for (int i = 0; i < 3; i++)
                    R[i, j] *= norm;
            for (int i = 0; i < 3; i++)
                Cval[i] *= norm;

            // R[i, 2]
            CompleteRotationMatrix(R);

            var Csol = M.DenseOfArray(R).Solve(V.DenseOfArray(Cval));
            double[] C = Csol.ToArray();

            return new CameraParameters(C, R);
        }

        /* Restore third column using first two */
        public static void CompleteRotationMatrix(double[,] R)
        {
            R[0, 2] = R[1, 0] * R[2, 1] - R[1, 1] * R[2, 0];
            R[1, 2] = -(R[0, 0] * R[2, 1] - R[0, 1] * R[2, 0]);
            R[2, 2] = R[0, 0] * R[1, 1] - R[0, 1] * R[1, 0];
        }

        /* Restore third column using first two */
        public static void CompleteRotationMatrix_V1(double[,] R)
        {
            double s01 = -(R[0, 0] * R[1, 0] + R[0, 1] * R[1, 1]);
            double s02 = -(R[0, 0] * R[2, 0] + R[0, 1] * R[2, 1]);
            double s12 = -(R[1, 0] * R[2, 0] + R[1, 1] * R[2, 1]);
            for (int i = 0; i < 3; i++)
            {
                double t = 1 - R[i, 0] * R[i, 0] - R[i, 1] * R[i, 1];
                if (IsZero(t))
                    t = 0;
                R[i, 2] = Math.Sqrt(t);
            }
            if (!IsZero(R[1, 2]))
                if (!IsZero(R[0, 2]))
                    R[1, 2] *= Math.Sign(s01);
            if (!IsZero(R[2, 2]))
                if (!IsZero(R[0, 2]))
                    R[1, 2] *= Math.Sign(s02);
                else if (!IsZero(R[1, 2]))
                    R[1, 2] *= Math.Sign(s12);
        }

        public static CameraParameters Inversed_V1(PointD[] Bpoints, double a, double b)
        {
            PointD[] Bmirrored = new PointD[Bpoints.Length];
            for (int i = 0; i < Bpoints.Length; i++)
                Bmirrored[i] = new PointD(-Bpoints[i].X, Bpoints[i].Y);
            Bpoints = Bmirrored;
            Line diag1 = new Line(Bpoints[0], Bpoints[3]);
            Line diag2 = new Line(Bpoints[1], Bpoints[2]);
            Line sideHB = new Line(Bpoints[0], Bpoints[1]);
            Line sideHT = new Line(Bpoints[2], Bpoints[3]);
            Line sideVL = new Line(Bpoints[0], Bpoints[2]);
            Line sideVR = new Line(Bpoints[1], Bpoints[3]);
            PointD center = diag1.Intersect(diag2);
            PointD perspectiveH = sideHB.Intersect(sideHT);
            PointD perspectiveV = sideVL.Intersect(sideVR);
            Line middleH = new Line(perspectiveH, center);
            Line middleV = new Line(perspectiveV, center);
            PointD[] points = new PointD[9];
            for (int i = 0; i < 4; i++)
                points[i] = Bpoints[i];
            points[4] = center;
            points[5] = middleH.Intersect(sideVL);
            points[6] = middleH.Intersect(sideVR);
            points[7] = middleV.Intersect(sideHB);
            points[8] = middleV.Intersect(sideHT);

            PointD[] origPoints = {
                new PointD(-a, -b),
                new PointD(a, -b),
                new PointD(-a, b),
                new PointD(a, b),
                new PointD(0, 0),
                new PointD(-a, 0),
                new PointD(a, 0),
                new PointD(0, -b),
                new PointD(0, b),
            };

            // r11, r12, r21, r22, r31, r32, Cxr11, Cyr12, Czr13, ..., Czr33
            double[,] system = new double[15, 15];
            for (int i = 0; i < 9; i++)
            {
                int Xrow = 2 * i;
                if (Xrow >= 15)
                    break;
                system[Xrow, 0] = origPoints[i].X;
                system[Xrow, 1] = origPoints[i].Y;
                system[Xrow, 4] = origPoints[i].X * points[i].X;
                system[Xrow, 5] = origPoints[i].Y * points[i].X;
                system[Xrow, 6] = 1;
                system[Xrow, 7] = 1;
                system[Xrow, 8] = 1;
                system[Xrow, 12] = -points[i].X;
                system[Xrow, 13] = -points[i].X;
                system[Xrow, 14] = -points[i].X;

                int Yrow = 2 * i + 1;
                if (Yrow >= 15)
                    break;
                system[Yrow, 2] = origPoints[i].X;
                system[Yrow, 3] = origPoints[i].Y;
                system[Yrow, 4] = origPoints[i].X * points[i].Y;
                system[Yrow, 5] = origPoints[i].Y * points[i].Y;
                system[Yrow, 9] = 1;
                system[Yrow, 10] = 1;
                system[Yrow, 11] = 1;
                system[Yrow, 12] = -points[i].Y;
                system[Yrow, 13] = -points[i].Y;
                system[Yrow, 14] = -points[i].Y;
            }

            Matrix<double> mathSystem = Matrix<double>.Build.DenseOfArray(system);
            Evd<double> eigen = mathSystem.Evd();
            Complex[] values = eigen.EigenValues.ToArray();
            double[,] vectors = eigen.EigenVectors.ToArray();
            int index = -1;
            for (int i = 0; i < values.Length; i++)
            {
                if (IsZero(values[i].Magnitude))
                    index = i;
                Console.WriteLine(values[i].ToString("f3"));
            }
            ConsoleUtils.WriteLine(vectors);
            if (index < 0)
            {
                Console.WriteLine("no solution");
                return null;
            }
            double[,] R = new double[3, 3];
            double[] Cval = new double[9];
            double[] C = new double[3];
            for (int i = 0; i < 9; i++)
                Cval[i] = vectors[i, index];
            for (int i = 0; i < 3; i++)
            {
                R[i, 0] = vectors[2 * i, index];
                R[i, 1] = vectors[2 * i + 1, index];
            }
            // norm R
            for (int j = 0; j < 2; j++)
            {
                double norm2 = 0;
                for (int i = 0; i < 3; i++)
                    norm2 += R[i, j] * R[i, j];
                double norm = 1 / Math.Sqrt(norm2);
                for (int i = 0; i < 3; i++)
                    R[i, j] *= norm;
            }
            // R[i, 2]
            CompleteRotationMatrix(R);

            // C[j] R[i, j]
            for (int j = 0; j < 3; j++)
            {
                int denomIndex = 0;
                for (int i = 0; i < 3; i++)
                    if (Math.Abs(R[i, j]) > 0.05)
                    {
                        denomIndex = i;
                        break;
                    }
                C[j] = Cval[denomIndex * 3 + j] / R[denomIndex, j];
            }

            return new CameraParameters(C, R);
        }

        public static bool IsZero(double d)
        {
            return d < 0.0001;
        }
    }
}
