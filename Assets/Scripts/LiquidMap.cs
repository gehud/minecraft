namespace Minecraft {
	public class LiquidMap : Array3D<byte> {
        public const byte MIN = 0;
        public const byte MAX = 8;

        public LiquidMap() : base(Chunk.SIZE, Chunk.SIZE, Chunk.SIZE) { }
	}
}