using Minecraft.Components;
using Unity.Collections;
using Unity.Entities;
using Unity.Rendering;
using UnityEngine;
using UnityEngine.Rendering;

namespace Minecraft.Systems {
	[UpdateAfter(typeof(ChunkMeshDataSystem))]
	public partial class ChunkMeshSystem : SystemBase {
		private const MeshUpdateFlags MESH_UPDATE_FLAGS =
			MeshUpdateFlags.DontRecalculateBounds |
			MeshUpdateFlags.DontResetBoneBounds | 
			MeshUpdateFlags.DontNotifyMeshUsers
#if !DEBUG
			| MeshUpdateFlags.DontValidateIndices;
#else
			;
#endif

		private Bounds Bounds => new() {
			center = Vector3.one * 8.0f,
			extents = Vector3.one * 8.0f
		};

		protected override void OnUpdate() {
			var commandBuffer = new EntityCommandBuffer(Allocator.TempJob);

			Entities.ForEach((Entity entity, in ChunkMeshData chunkMeshData, in RenderMeshArray renderMeshArray) => {
				var mesh = new Mesh();

				mesh.SetVertexBufferParams(chunkMeshData.Vertices.Length,
					new VertexAttributeDescriptor(VertexAttribute.Position, VertexAttributeFormat.Float32, 3));
				mesh.SetVertexBufferData(chunkMeshData.Vertices, 0, 0, chunkMeshData.Vertices.Length, 0, MESH_UPDATE_FLAGS);

				mesh.SetIndexBufferParams(chunkMeshData.Indices.Length, IndexFormat.UInt16);
				mesh.SetIndexBufferData(chunkMeshData.Indices, 0, 0, chunkMeshData.Indices.Length, MESH_UPDATE_FLAGS);

				mesh.subMeshCount = 1;
				mesh.SetSubMesh(0, new SubMeshDescriptor(0, chunkMeshData.Indices.Length), MESH_UPDATE_FLAGS);

				mesh.bounds = Bounds;

				renderMeshArray.Meshes[0] = mesh;

				commandBuffer.RemoveComponent<ChunkMeshData>(entity);
			}).WithoutBurst().Run();

			CompleteDependency();

			commandBuffer.Playback(EntityManager);

			commandBuffer.Dispose();
		}
	}
}