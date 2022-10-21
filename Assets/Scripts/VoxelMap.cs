namespace Minecraft
{
    public class VoxelMap : Array3D<VoxelType>
    {
        public VoxelMap() : base(Chunk.SIZE, Chunk.SIZE, Chunk.SIZE) { }
    }
}