using System;

namespace PerspectiveInversion
{
    class Line
    {
        // ax + by + c = 0
        public double a { get; private set; }
        public double b { get; private set; }
        public double c { get; private set; }

        public Line(PointD p1, PointD p2) : this(p1.X, p1.Y, p2.X, p2.Y)
        {
            if (!p1.IsFinite && !p2.IsFinite)
            {
                throw new Exception("Couldn't create line from 2 infinite points");
            }
            else if (p1.IsFinite && p2.IsFinite)
            {
                CalcParams(p1.X, p1.Y, p2.X, p2.Y);
            }
            else
            {
                if (!p1.IsFinite)
                {
                    PointD temp = p2;
                    p2 = p1;
                    p1 = temp;
                }
                a = p2.Y;
                b = -p2.X;
                c = -(a * p1.X + b * p1.Y);
                NormSigns();
            }
        }
        public Line(double x1, double y1, double x2, double y2)
        {
            CalcParams(x1, y1, x2, y2);
        }

        private void CalcParams(double x1, double y1, double x2, double y2)
        {
            a = y1 - y2;
            b = -(x1 - x2);
            c = -(a * x1 + b * y1);
            NormSigns();
        }
        private void NormSigns()
        {
            if (a < 0 || a == 0 && b < 0)
            {
                a = -a;
                b = -b;
                c = -c;
            }
        }

        public PointD Intersect(Line line)
        {
            double denominator = b * line.a - a * line.b;
            double numeratorX = -(b * line.c - c * line.b);
            double numeratorY = -(c * line.a - a * line.c);
            if (Math.Abs(denominator) < 0.00001)
                return new PointD(numeratorX, numeratorY, false);
            return new PointD(numeratorX / denominator, numeratorY / denominator);
        }
    }
}
