namespace Minecraft.Extensions {
    public static class FloatExtensions {
        public static float Remap(this float value, float fromMin, float fromMax, float toMin, float toMax) {
            return (value - fromMin) / (fromMax - fromMin) * (toMax - toMin) + toMin;
        }
    }
}