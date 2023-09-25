using Minecraft.Components;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Rendering;
using UnityEngine;
using UnityEngine.Rendering;

namespace Minecraft.Systems {
	[UpdateAfter(typeof(ChunkMeshDataSystem))]
	public partial class ChunkMeshSystem : SystemBase {
		private MeshJob lastJob;
		private JobHandle lastJobHandle;

		private const MeshUpdateFlags MESH_UPDATE_FLAGS =
			MeshUpdateFlags.DontRecalculateBounds |
			MeshUpdateFlags.DontResetBoneBounds |
			MeshUpdateFlags.DontNotifyMeshUsers
#if !DEBUG
			| MeshUpdateFlags.DontValidateIndices;
#else
			;
#endif

		private struct MeshJob : IJob {
			public Entity entity;
			public ChunkMeshData chunkMeshData;
			public Mesh.MeshDataArray meshDataArray;

			public void Execute() {
				var mesh = meshDataArray[0];

				mesh.SetVertexBufferParams(chunkMeshData.Vertices.Length,
					new VertexAttributeDescriptor(VertexAttribute.Position, VertexAttributeFormat.Float32, 3));
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
			var querry = new EntityQueryBuilder(Allocator.TempJob).
				WithAll<ChunkMeshData>().
				WithAll<RenderMeshArray>().
				Build(EntityManager);
			var entities = querry.ToEntityArray(Allocator.TempJob);
			querry.Dispose();
			if (entities.Length != 0 && lastJobHandle.IsCompleted) {
				lastJobHandle.Complete();

				var entity = entities[0];

				if (lastJob.entity != null && EntityManager.Exists(lastJob.entity)) {
					var mesh = new Mesh();
					Mesh.ApplyAndDisposeWritableMeshData(lastJob.meshDataArray, mesh, MESH_UPDATE_FLAGS);
					EntityManager.GetSharedComponentManaged<RenderMeshArray>(lastJob.entity).Meshes[0] = mesh;
					EntityManager.RemoveComponent<ChunkMeshData>(lastJob.entity);
				}

				if (entity == lastJob.entity && entities.Length > 1) {
					entity = entities[1];
				}

				if (entity != lastJob.entity) {
					lastJob = new MeshJob {
						entity = entity,
						chunkMeshData = EntityManager.GetComponentData<ChunkMeshData>(entity),
						meshDataArray = Mesh.AllocateWritableMeshData(1)
					};

					lastJobHandle = lastJob.Schedule();
				}
			}

			entities.Dispose();
		}
	}
}