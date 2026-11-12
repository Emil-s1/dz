using System;

namespace GeometryLibrary
{
    public class Geometry
    {
        public static double SquareArea(double side) => side < 0 ? throw new ArgumentException() : side * side;
        public static double RectangleArea(double w, double h) => (w < 0 || h < 0) ? throw new ArgumentException() : w * h;
        public static double TriangleArea(double a, double b, double c)
        {
            if (a <= 0 || b <= 0 || c <= 0) return 0;
            double p = (a + b + c) / 2;
            return Math.Sqrt(p * (p - a) * (p - b) * (p - c));
        }
    }
}