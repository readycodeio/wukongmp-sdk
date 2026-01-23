namespace WukongMp.Api.WukongUtils
{
    internal static class MathUtils
    {
        public static float LerpAngle(float current, float target, float alpha)
        {
            float delta = ((target - current + 540f) % 360f) - 180f;
            return current + delta * alpha;
        }
    }
}
