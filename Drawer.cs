using MathNet.Numerics.LinearAlgebra;
using PerspectiveInversion;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;

namespace PerspectiveRestorer
{
    class Drawer
    {
        private ParamManager pm;
        private Canvas canvas;

        public Drawer(ParamManager pm, Canvas canvas)
        {
            this.pm = pm;
            this.canvas = canvas;
        }

        public void Redraw(CropInfo info, Solver solver, int selectedPoint, bool planeDetails, bool axes, bool zLinesFlag)
        {
            if (solver == null)
                return;
            canvas.Children.Clear();

            bool validPoints = solver.HasSolution;
            Brush color = Brushes.GreenYellow;
            if (!validPoints)
                color = Brushes.Red;

            Point[] ScreenPoints = new Point[4];
            double normW = canvas.ActualWidth / info.ImageOrig.PixelWidth / info.ScaleFactor;
            double normH = canvas.ActualHeight / info.ImageOrig.PixelHeight / info.ScaleFactor;
            for (int i = 0; i < 4; i++)
            {
                double x = (solver.ImagePoints[i].X - (int)info.MinCorner.X) * normW;
                double y = (solver.ImagePoints[i].Y - (int)info.MinCorner.Y) * normH;
                ScreenPoints[i] = new Point(x, y);
            }

            int[] pointOrder = { 0, 1, 3, 2, 0 };
            Line[] RectSides = new Line[4];
            for (int k = 0; k < 4; k++)
            {
                int i = pointOrder[k];
                int j = pointOrder[k + 1];
                Line ijLine = new Line()
                {
                    Stroke = color,
                    StrokeThickness = 2
                };
                ijLine.X1 = ScreenPoints[i].X;
                ijLine.Y1 = ScreenPoints[i].Y;
                ijLine.X2 = ScreenPoints[j].X;
                ijLine.Y2 = ScreenPoints[j].Y;
                canvas.Children.Add(ijLine);
                RectSides[k] = ijLine;
            }
            if (!validPoints)
            {
                DrawPoints(ScreenPoints, selectedPoint);
                return;
            }

            if (planeDetails)
            {
                for (int i = 0; i < RectSides.Length; i++)
                {
                    Line fullLine = Extend(RectSides[i], canvas.ActualWidth, canvas.ActualHeight);
                    canvas.Children.Add(fullLine);
                }
            }

            double a = pm.GetParam("1->2 length")[0, 0] / 2;
            double b = pm.GetParam("1->3 length")[0, 0] / 2;
            double width = info.ImageOrig.PixelWidth, height = info.ImageOrig.PixelHeight;
            double reToImg = width / CameraParameters.GetFovWidth(pm.GetParam("vert_fov")[0, 0]);
            double reToCanvasX = canvas.ActualWidth / info.ImageCropped.PixelWidth;
            double reToCanvasY = canvas.ActualHeight / info.ImageCropped.PixelHeight;
            if (zLinesFlag)
            {
                // dx^2 + dy^2 != 0
                double[][] basePoints = new double[4][];
                basePoints[0] = new double[] { -a, -b, 0 };
                basePoints[1] = new double[] { a, -b, 0 };
                basePoints[2] = new double[] { -a, b, 0 };
                basePoints[3] = new double[] { a, b, 0 };
                var transMatrix = Matrix<double>.Build.DenseOfArray(pm.GetFaceTrans());
                for (int i = 0; i < basePoints.Length; i++)
                {
                    Line zLine = new Line()
                    {
                        Stroke = color,
                        StrokeThickness = 2
                    };
                    double[] basePoint = basePoints[i];
                    double[] zPoint = new double[3];
                    basePoint.CopyTo(zPoint, 0);
                    zPoint[2] = 1;
                    basePoint = (transMatrix * Vector<double>.Build.DenseOfArray(basePoint)).ToArray();
                    zPoint = (transMatrix * Vector<double>.Build.DenseOfArray(zPoint)).ToArray();
                    PointD[] proj = PerspectiveInversion.Transform.Straight(new double[][] { basePoint, zPoint }, solver.Solution);
                    for (int j = 0; j < 2; j++)
                    {
                        proj[j] = new PointD(width / 2 + reToImg * proj[j].X, height / 2 + reToImg * proj[j].Y);
                        proj[j] = new PointD((proj[j].X - (int)info.MinCorner.X) * normW,
                                (proj[j].Y - (int)info.MinCorner.Y) * normH);
                    }
                    zLine.X1 = proj[0].X;
                    zLine.Y1 = proj[0].Y;
                    zLine.X2 = proj[1].X;
                    zLine.Y2 = proj[1].Y;
                    Line fullLine = Extend(zLine, canvas.ActualWidth, canvas.ActualHeight);
                    canvas.Children.Add(fullLine);
                }
            }
            if (axes)
            {
                var transMatrix = Matrix<double>.Build.DenseOfArray(pm.GetFaceTrans());
                double[][] initPoints = new double[4][];
                initPoints[0] = new double[] { -a, -b, 0 };
                initPoints[0] = (transMatrix * Vector<double>.Build.DenseOfArray(initPoints[0])).ToArray();
                initPoints[1] = new double[] { initPoints[0][0] + 1, initPoints[0][1] + 0, initPoints[0][2] + 0 };
                initPoints[2] = new double[] { initPoints[0][0] + 0, initPoints[0][1] + 1, initPoints[0][2] + 0 };
                initPoints[3] = new double[] { initPoints[0][0] + 0, initPoints[0][1] + 0, initPoints[0][2] + 1 };
                PointD[] axesPoints = PerspectiveInversion.Transform.Straight(initPoints, solver.Solution);
                for (int i = 0; i < axesPoints.Length; i++)
                {
                    axesPoints[i] = new PointD(width / 2 + reToImg * axesPoints[i].X, height / 2 + reToImg * axesPoints[i].Y);
                    /*axesPoints[i] = info.ImageToCrop(axesPoints[i]);
                    axesPoints[i] = new PointD(reToCanvasX * axesPoints[i].X, reToCanvasY * axesPoints[i].Y);*/
                    axesPoints[i] = new PointD((axesPoints[i].X - (int)info.MinCorner.X) * normW,
                            (axesPoints[i].Y - (int)info.MinCorner.Y) * normH);
                }
                Brush[] colors = { Brushes.Red, Brushes.Blue, Brushes.Green };
                for (int i = 0; i < 3; i++)
                {
                    Line axis = new Line()
                    {
                        Stroke = colors[i],
                        StrokeThickness = 2,
                        X1 = axesPoints[0].X,
                        Y1 = axesPoints[0].Y,
                        X2 = axesPoints[i + 1].X,
                        Y2 = axesPoints[i + 1].Y,
                    };
                    canvas.Children.Add(axis);
                }
            }

            DrawPoints(ScreenPoints, selectedPoint);
        }

        private void DrawPoints(Point[] ScreenPoints, int selectedPoint)
        {
            for (int i = 0; i < 4; i++)
            {
                Ellipse circle = GetCircle(ScreenPoints[i], 4, Brushes.Green);
                if (i == selectedPoint)
                    circle = GetCircle(ScreenPoints[i], 12, Brushes.White);
                canvas.Children.Add(circle);
            }
        }
        private Ellipse GetCircle(Point p, double r, Brush color)
        {
            return GetCircle(p.X, p.Y, r, color);
        }
        private Ellipse GetCircle(double x, double y, double r, Brush color)
        {
            double r2 = r / 2;
            Ellipse circle = new Ellipse()
            {
                Width = r2,
                Height = r2,
                StrokeThickness = r2,
                Stroke = color,
            };
            circle.SetValue(Canvas.LeftProperty, x - r2 / 2);
            circle.SetValue(Canvas.TopProperty, y - r2 / 2);
            return circle;
        }

        private Line Extend(Line line, double width, double height)
        {
            Line newLine = new Line()
            {
                Stroke = line.Stroke,
                StrokeThickness = line.StrokeThickness - 1,
            };
            double X1 = line.X1, Y1 = line.Y1,
                    X2 = line.X2, Y2 = line.Y2;
            double dx = X1 - X2;
            double dy = Y1 - Y2;

            if (IsNear(dx, 0))
            {
                Y1 = 0;
                Y2 = height;
            }
            else if (IsNear(dy, 0))
            {
                X1 = 0;
                X2 = width;
            }
            else
            {
                double x = X1, y = Y1;
                if (IsNear(X1, 0) || IsNear(X1, width) || IsNear(Y1, 0) || IsNear(Y1, height))
                {
                    x = X2;
                    y = Y2;
                }
                if (IsNear(X2, 0) || IsNear(X2, width) || IsNear(Y2, 0) || IsNear(Y2, height))
                {
                    x = (X1 + X2) * 0.5;
                    y = (Y1 + Y2) * 0.5;
                }
                double[] points = new double[4];
                points[0] = -x / dx;
                points[1] = -y / dy;
                points[2] = (width - x) / dx;
                points[3] = (height - y) / dy;
                Array.Sort(points);
                X1 = x + points[1] * dx;
                Y1 = y + points[1] * dy;
                X2 = x + points[2] * dx;
                Y2 = y + points[2] * dy;
            }

            newLine.X1 = X1;
            newLine.Y1 = Y1;
            newLine.X2 = X2;
            newLine.Y2 = Y2;
            return newLine;
        }

        private bool IsNear(double x, double y)
        {
            return Math.Abs(x - y) < 0.0001;
        }
    }
}
