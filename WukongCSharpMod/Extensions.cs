using System;

namespace WukongCSharpMod
{
    public static class Extensions
    {
        public static bool Equals(this float a, float b, float tolerance)
        {
            return MathF.Abs(a - b) < tolerance;
        }
    }
}