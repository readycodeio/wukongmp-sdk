using System;

namespace WukongApi
{
    public static class Extensions
    {
        public static bool Equals(this float a, float b, float tolerance)
        {
            return MathF.Abs(a - b) < tolerance;
        }
    }
}