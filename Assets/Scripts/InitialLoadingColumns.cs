using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace Minecraft
{
    public struct InitialLoadingColumns : IComponentData
    {
        public NativeList<int2> Columns;
    }
}