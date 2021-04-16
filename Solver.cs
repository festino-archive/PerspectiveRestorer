
using System;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using MathNet.Numerics.LinearAlgebra;
using PerspectiveInversion;

namespace PerspectiveRestorer
{
    public class Solver
    {
        public PointD[] ImagePoints;
        public bool HasSolution { get => CheckPoints(); }
        public CameraParameters Solution {
            get
            {
                if (HasSolution && solution == null)
                    Solve();
                return solution;
            }
        }
        private CameraParameters solution;

        private ParamManager pm;
        private BitmapImage ImageOrig;
        private TextBox MinecraftCommand;

        public Solver(ParamManager pm, TextBox minecraftCommand, BitmapImage imageOrig)
        {
            this.pm = pm;
            ImageOrig = imageOrig;
            MinecraftCommand = minecraftCommand;
        }

        private bool CheckPoints()
        {
            ImagePoints = new PointD[4];
            for (int i = 1; i <= 4; i++)
            {
                double[,] point = pm.GetParam(i.ToString());
                ImagePoints[i - 1] = new PointD(point[0, 0], point[1, 0]);
            }
            double fov = pm.GetParam("vert_fov")[0, 0];
            if (fov < 1 || 170 < fov)
                return false;
            //true if 1,4 and 2,3 intersects and points have some distance

            for (int i = 0; i < 4; i++)
                for (int j = i + 1; j < 4; j++)
                {
                    double dx = ImagePoints[i].X - ImagePoints[j].X;
                    double dy = ImagePoints[i].Y - ImagePoints[j].Y;
                    if (dx * dx + dy * dy < 0.1)
                        return false;
                }
            bool valid = true;
            PointD[] points = new PointD[4];
            points[0] = ImagePoints[0];
            points[1] = ImagePoints[1];
            points[2] = ImagePoints[3];
            points[3] = ImagePoints[2];
            for (int i = 0; i < 4; i++)
                valid &= RayIntersects(points[i], points[(i + 2) % 4],
                             points[(i + 1) % 4], points[(i + 3) % 4]);
            return valid;
        }

        private bool RayIntersects(PointD r1, PointD r2, PointD segm1, PointD segm2)
        {
            PointD ray = new PointD(r2.X - r1.X, r2.Y - r1.Y);
            PointD v1 = new PointD(segm1.X - r1.X, segm1.Y - r1.Y);
            PointD v2 = new PointD(segm2.X - r1.X, segm2.Y - r1.Y);
            return CrossProduct(ray, v1) * CrossProduct(ray, v2) < 0;
        }
        private double CrossProduct(PointD a, PointD b)
        {
            return a.X * b.Y - a.Y * b.X;
        }

        public void Solve()
        {
            if (!HasSolution)
                return;

            double a = pm.GetParam("1->2 length")[0, 0] / 2;
            double b = pm.GetParam("1->3 length")[0, 0] / 2;
            PointD[] zPoints = new PointD[4];
            double width = ImageOrig.PixelWidth, height = ImageOrig.PixelHeight;
            double norm = CameraParameters.GetFovWidth(pm.GetParam("vert_fov")[0, 0]) / width;
            for (int i = 0; i < 4; i++)
            {
                double zWidth = norm * (ImagePoints[i].X - width / 2);
                double zHeight = norm * (ImagePoints[i].Y - height / 2);
                zPoints[i] = new PointD(zWidth, zHeight);
            }

            CameraParameters restoredParam = null;
            try
            {
                restoredParam = Transform.Inversed(zPoints, a, b);
            }
            catch (Exception) { }
            if (restoredParam == null)
            {
                double[,] C_error = new double[3, 1];
                for (int i = 0; i < 3; i++)
                    C_error[i, 0] = double.NaN;
                pm.SetRes("C", C_error);
                return;
            }

            double[,] C = new double[3, 1];
            for (int i = 0; i < 3; i++)
                C[i, 0] = restoredParam.C[i];
            pm.SetRes("C", C);
            pm.SetRes("R", restoredParam.R.ToArray());

            double[,] trans = pm.GetFaceTrans();
            var transMatrix = Matrix<double>.Build.DenseOfArray(trans);
            double[] newC = (transMatrix * Vector<double>.Build.DenseOfArray(restoredParam.C)).ToArray();
            double[,] newR = (restoredParam.R * transMatrix.Transpose()).ToArray();

            restoredParam = new CameraParameters(newC, newR);
            solution = restoredParam;

            double[,] Angles = new double[3, 1];
            for (int i = 0; i < 3; i++)
                Angles[i, 0] = restoredParam.Angles[i];
            Angles[0, 0] += 180;
            if (Angles[0, 0] > 180)
                Angles[0, 0] -= 360;
            pm.SetRes("Angles", Angles);

            if (Math.Abs(Angles[2, 0]) > 1)
            {
                MinecraftCommand.Text = $"Too non-zero roll={Angles[2, 0]} to use in minecraft command.";
                return;
            }
            double[] worldCoord = new double[3];
            double[] dir = pm.GetFace();
            for (int i = 0; i < 3; i++)
                worldCoord[i] = pm.GetParam("block")[i, 0] + 0.5 + 0.5 * dir[i] + newC[i];
            string command = "/execute in overworld? run tp @s";
            command += $" {worldCoord[0]} {worldCoord[1]} {worldCoord[2]}";
            command += $" {Angles[0, 0]} {Angles[1, 0]}";
            MinecraftCommand.Text = command;
        }
    }
}
