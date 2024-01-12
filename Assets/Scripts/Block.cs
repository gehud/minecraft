using Minecraft.Lighting;

namespace Minecraft {
    public struct Block {
        public Texturing Texturing;
        public bool IsSolid;
        public bool IsTransparent;
        public int Absorption;
        public LightColor Emission;
    }
}