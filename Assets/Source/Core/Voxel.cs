namespace Minecraft {
    public struct Voxel {
        public BlockType Type;
        public Light Light;

        public Voxel(BlockType type) {
            Type = type;
            Light = default;
        }

        public Voxel(BlockType type, Light light) {
            Type = type;
            Light = light;
        }

        public override string ToString() {
            return $"Block: {Type}, {Light}";
        }
    }
}