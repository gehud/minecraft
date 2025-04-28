using Minecraft.Utilities;
using Unity.Collections;
using UnityEngine.Rendering;

namespace Minecraft
{
    public struct Vertex
    {
        public uint A;
        public uint B;

        public static NativeArray<VertexAttributeDescriptor> GetDescriptors(Allocator allocator)
        {
            var descriptors = new NativeArray<VertexAttributeDescriptor>(1, allocator);
            descriptors[0] = new VertexAttributeDescriptor(VertexAttribute.TexCoord0, VertexAttributeFormat.UInt32, 2);
            return descriptors;
        }

        public static Vertex Create(int x, int y, int z, int u, int v, int r, int g, int b, int s)
        {
            var i = IndexUtility.ToIndex(u, v, 17);

            // Layout:
            // A
            // x - 5bit, y - 5bit, z - 5bit, i - 9bit
            // B
            // r - 6bit, g - 6bit, b - 6bit, s - 6bit 

            // A
            var yBit = 5;
            var zBit = 5;
            var iBit = 9;

            var ziBit = zBit + iBit;
            var yziBit = yBit + ziBit;

            // B
            var gBit = 6;
            var bBit = 6;
            var sBit = 6;

            var bsBit = bBit + sBit;
            var gbsBit = gBit + bsBit;

            return new Vertex
            {
                A = (uint)(x << yziBit | y << ziBit | z << iBit | i),
                B = (uint)(r << gbsBit | g << bsBit | b << sBit | s)
            };
        }
    }
}