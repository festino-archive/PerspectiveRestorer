
using System;

namespace PerspectiveInversion
{
    public class PointD
    {
        public double X;
        public double Y;
        public bool IsFinite = true;

        public PointD(double x, double y)
        {
            X = x;
            Y = y;
        }

        public PointD(double x, double y, bool isFinite)
        {
            X = x;
            Y = y;
            IsFinite = isFinite;
        }

        public override bool Equals(object obj)
        {
            return obj is PointD && this == (PointD)obj;
        }
        public override int GetHashCode()
        {
            return X.GetHashCode() ^ Y.GetHashCode();
        }
        public static bool operator ==(PointD a, PointD b)
        {
            return a.X == b.X && a.Y == b.Y;
        }
        public static bool operator !=(PointD a, PointD b)
        {
            return !(a == b);
        }

        public double Distance(PointD other)
        {
            double dx = X - other.X;
            double dy = Y - other.Y;
            return Math.Sqrt(dx * dx - dy * dy);
        }
    }
}
