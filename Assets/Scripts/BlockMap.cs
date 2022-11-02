namespace Minecraft {
    public class BlockMap : Array3D<BlockType> {
        public BlockMap() : base(Chunk.SIZE, Chunk.SIZE, Chunk.SIZE) { }
    }
}