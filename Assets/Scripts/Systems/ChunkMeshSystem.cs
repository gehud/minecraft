using Minecraft.Components;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Rendering;
using UnityEngine;
using UnityEngine.Rendering;

namespace Minecraft.Systems {
    [UpdateAfter(typeof(ChunkMeshDataSystem))]
    public partial class ChunkMeshSystem : SystemBase {
        private const MeshUpdateFlags MESH_UPDATE_FLAGS
            = MeshUpdateFlags.DontRecalculateBounds
            | MeshUpdateFlags.DontResetBoneBounds
            | MeshUpdateFlags.DontNotifyMeshUsers
#if !DEBUG
			| MeshUpdateFlags.DontValidateIndices;
#else
            ;
#endif

        [BurstCompile]
        private struct MeshJob : IJob {
            public ChunkMeshData chunkMeshData;
            public Mesh.MeshDataArray meshDataArray;

            public void Execute() {
                var mesh = meshDataArray[0];

                var descriptors = new NativeArray<VertexAttributeDescriptor>(1, Allocator.Temp);
                descriptors[0] = new VertexAttributeDescriptor(VertexAttribute.Position, VertexAttributeFormat.Float32, 3);
                mesh.SetVertexBufferParams(chunkMeshData.Vertices.Length, descriptors);
                var vertices = mesh.GetVertexData<Vertex>();
                for (int i = 0; i < chunkMeshData.Vertices.Length; i++) {
                    vertices[i] = chunkMeshData.Vertices[i];
                }

                mesh.SetIndexBufferParams(chunkMeshData.Indices.Length, IndexFormat.UInt16);
                var indices = mesh.GetIndexData<ushort>();
                for (int i = 0; i < chunkMeshData.Indices.Length; i++) {
                    indices[i] = chunkMeshData.Indices[i];
                }

                mesh.subMeshCount = 1;
                mesh.SetSubMesh(0, new SubMeshDescriptor(0, chunkMeshData.Indices.Length), MESH_UPDATE_FLAGS);
            }
        }

        protected override void OnUpdate() {
            Entities.ForEach((Entity entity, in ChunkMeshData chunkMeshData, in RenderMeshArray renderMeshArray) => {
                var job = new MeshJob {
                    chunkMeshData = chunkMeshData,
                    meshDataArray = Mesh.AllocateWritableMeshData(1)
                };

                job.Schedule().Complete();

                var mesh = new Mesh();
                Mesh.ApplyAndDisposeWritableMeshData(job.meshDataArray, mesh, MESH_UPDATE_FLAGS);
                EntityManager.GetSharedComponentManaged<RenderMeshArray>(entity).Meshes[0] = mesh;
                EntityManager.RemoveComponent<ChunkMeshData>(entity);
            }).WithStructuralChanges().Run();
        }
    }
}