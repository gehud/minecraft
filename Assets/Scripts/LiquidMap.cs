using UnityEngine;

namespace Minecraft
{
    public struct Liquid
    {
        public static Liquid Empty => default;

        public VoxelType Type;
        public byte Amount;
        public LiquidFlowDirection FlowDirection;

        public Liquid(VoxelType type, byte amount, LiquidFlowDirection flowDirection = LiquidFlowDirection.None)
        {
            Type = type;
            Amount = amount;
            FlowDirection = flowDirection;
        }
    }

    public class LiquidMap : Array3D<Liquid>
    {
        public const int MIN = 0;
        public const int MAX = 7;

        public LiquidMap() : base(Chunk.SIZE, Chunk.SIZE, Chunk.SIZE) { }

        public byte Get(int x, int y, int z, VoxelType type)
        {
            var unit = this[x, y, z];
            return (byte)(unit.Type == type ? unit.Amount : 0);
        }

        public byte Get(Vector3Int coordinate, VoxelType type)
        {
            return Get(coordinate.x, coordinate.y, coordinate.z, type);
        }

        public void Set(int x, int y, int z, VoxelType type, byte value)
        {
            this[x, y, z] = new Liquid(type, value);
        }

        public void Set(Vector3Int coordinate, VoxelType type, byte value)
        {
            Set(coordinate.x, coordinate.y, coordinate.z, type, value);
        }
    }
}