using MathNet.Numerics.LinearAlgebra;
using MathNet.Spatial.Euclidean;
using MathNet.Spatial.Units;
using System;

namespace PerspectiveInversion
{
    public class CameraParameters
    {
        public double Yaw {
            get {
                TryFillAngles();
                return yaw;
            }
        }
        public double Pitch {
            get {
                TryFillAngles();
                return pitch;
            }
        }
        public double Roll {
            get {
                TryFillAngles();
                return roll;
            }
        }
        public double[] Angles {
            get {
                TryFillAngles();
                return angles;
            }
        }
        private double yaw;
        private double pitch;
        private double roll;
        private double[] angles;
        public readonly double[] C;
        public readonly Matrix<double> R;
        private static readonly double DEGREES_IN_RADIAN = 180 / Math.PI;

        public Vector<double> Pos { get => Vector<double>.Build.DenseOfArray(C); }

        public CameraParameters(double[] c, double yaw, double pitch, double roll)
        {
            C = c;
            double revol = 0.5 * (yaw / Math.PI + 1);
            this.yaw = yaw - 2 * Math.PI * Math.Floor(revol);
            this.pitch = pitch;
            this.roll = roll;
            angles = new double[] { Yaw, Pitch, Roll };
            R = RotationMatrix(Yaw, Pitch, Roll);
        }
        public CameraParameters(double[] c, Matrix<double> R)
        {
            C = c;
            this.R = R;
        }
        public CameraParameters(double[] c, double[,] R)
                : this(c, Matrix<double>.Build.DenseOfArray(R)) { }

        public static Matrix<double> RotationMatrix(double yaw, double pitch, double roll)
        {
            Matrix<double> R = Matrix3D.RotationAroundYAxis(Angle.FromDegrees(yaw));
            R = Matrix3D.RotationAroundZAxis(Angle.FromDegrees(roll)) * R;
            R = Matrix3D.RotationAroundXAxis(Angle.FromDegrees(pitch)) * R;
            return R;
        }

        /* C, R, angles */
        public double[] Distances(CameraParameters other)
        {
            double distC = Distance(C, other.C);
            double distR = Distance(R.ToArray(), other.R.ToArray());
            double distAngles = Distance(Angles, other.Angles);

            return new double[] { distC, distR, distAngles };
        }

        public static double Distance(double[] a, double[] b)
        {
            if (a.Length != b.Length)
                throw new ArgumentException("Length exception: " + a.Length + " != " + b.Length);
            double sum2 = 0;
            for (int i = 0; i < a.Length; i++)
                sum2 += (a[i] - b[i]) * (a[i] - b[i]);
            return Math.Sqrt(sum2);
        }

        public static double Distance(double[,] a, double[,] b)
        {
            if (a.GetLength(0) != b.GetLength(0))
                throw new ArgumentException("Length(0) exception: " + a.GetLength(0) + " != " + b.GetLength(0));
            if (a.GetLength(1) != b.GetLength(1))
                throw new ArgumentException("Length(1) exception: " + a.GetLength(1) + " != " + b.GetLength(1));
            double sum2 = 0;
            for (int i = 0; i < a.GetLength(0); i++)
                for (int j = 0; j < a.GetLength(1); j++)
                    sum2 += (a[i, j] - b[i, j]) * (a[i, j] - b[i, j]);
            return Math.Sqrt(sum2);
        }

        /* x width on z=1 plane, fov in degrees */
        public static double GetFovWidth(double fov)
        {
            return 2 * Math.Tan(fov / 2 / 180 * Math.PI);
        }

        private void TryFillAngles()
        {
            if (angles == null)
            {
                if (R == null)
                {
                    roll = double.NaN;
                    yaw = double.NaN;
                    pitch = double.NaN;
                }
                else
                {
                    roll = DEGREES_IN_RADIAN * Math.Asin(-R[0, 1]);
                    yaw = DEGREES_IN_RADIAN * Math.Atan2(R[0, 2], R[0, 0]);
                    pitch = DEGREES_IN_RADIAN * Math.Atan2(R[2, 1], R[1, 1]);
                }
                angles = new double[] { yaw, pitch, roll };
            }
        }
    }
}
