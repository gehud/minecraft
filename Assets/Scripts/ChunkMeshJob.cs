using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using UnityEngine;
using UnityEngine.Rendering;

namespace Minecraft {
    [BurstCompile]
    public struct ChunkMeshJob : IJob {
        public Entity Entity;
        public ChunkMeshData MeshData;
        public Mesh.MeshDataArray MeshDataArray;
        [ReadOnly]
        public NativeArray<VertexAttributeDescriptor> Descriptors;

        public void Execute() {
            var mesh = MeshDataArray[0];

            mesh.SetVertexBufferParams(MeshData.Vertices.Length, Descriptors);

            var vertices = mesh.GetVertexData<Vertex>();
            for (int i = 0; i < MeshData.Vertices.Length; i++) {
                vertices[i] = MeshData.Vertices[i];
            }

            var opaqueIndicesCount = MeshData.OpaqueIndices.Length;
            var transparentIndicesCount = MeshData.TransparentIndices.Length;
            var indicesCount = opaqueIndicesCount + transparentIndicesCount;
            mesh.SetIndexBufferParams(indicesCount, IndexFormat.UInt16);
            var indices = mesh.GetIndexData<ushort>();

            for (int i = 0; i < opaqueIndicesCount; i++) {
                indices[i] = MeshData.OpaqueIndices[i];
            }

            for (int i = 0; i < transparentIndicesCount; i++) {
                indices[i + opaqueIndicesCount] = MeshData.TransparentIndices[i];
            }

            mesh.subMeshCount = 2;
            mesh.SetSubMesh(0, new SubMeshDescriptor(0, opaqueIndicesCount), ChunkMeshSystem.UpdateFlags);
            mesh.SetSubMesh(1, new SubMeshDescriptor(opaqueIndicesCount, transparentIndicesCount), ChunkMeshSystem.UpdateFlags);
        }
    }
}