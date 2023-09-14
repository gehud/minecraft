using Minecraft.Components;
using Minecraft.Utilities;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Rendering;

namespace Minecraft.Systems {
	[UpdateAfter(typeof(ChunkGenerationSystem))]
	public partial class ChunkMeshDataSystem : SystemBase {
		protected override void OnCreate() {
			EntityManager.AddComponentData(SystemHandle, new ChunkMeshDataSystemData {
				ChunkBufferingSystem = World.GetExistingSystem<ChunkBufferingSystem>()
			});
		}

		protected override void OnUpdate() {
			Entities.WithAll<DataOnlyChunk>().WithNone<DisableRendering>().ForEach((Entity entity) => {
				EntityManager.AddComponent<DisableRendering>(entity);
			}).WithStructuralChanges().Run();

			Entities.WithAll<DisableRendering>().WithNone<DataOnlyChunk>().ForEach((Entity entity) => {
				EntityManager.RemoveComponent<DisableRendering>(entity);
			}).WithStructuralChanges().Run();

			var entityManager = EntityManager;
			var commandBuffer = new EntityCommandBuffer(Allocator.Persistent);
			var chunkBufferingSystem = EntityManager.GetComponentData<ChunkMeshDataSystemData>(SystemHandle).ChunkBufferingSystem;

			Chunk chunk = default;
			Entities.WithNativeDisableContainerSafetyRestriction(chunk).WithAll<DirtyChunk>().WithNone<DataOnlyChunk>().ForEach((Entity entity) => {
				chunk = entityManager.GetComponentData<Chunk>(entity);
				var vertices = new NativeList<Vertex>(Allocator.Persistent);
				var indices = new NativeList<ushort>(Allocator.Persistent);

				for (int x = 0; x < Chunk.SIZE; x++) {
					for (int y = 0; y < Chunk.SIZE; y++) {
						for (int z = 0; z < Chunk.SIZE; z++) {
							var localVoxelCoordinate = new int3(x, y, z);

							if (GetVoxel(entityManager, chunkBufferingSystem, chunk, localVoxelCoordinate).Type == 0) {
								continue;
							}

							// Right face
							if (HasFace(entityManager, chunkBufferingSystem, chunk, localVoxelCoordinate + new int3(1, 0, 0))) {
								var vertexCount = vertices.Length;
								indices.Add((ushort)(vertexCount + 0));
								indices.Add((ushort)(vertexCount + 1));
								indices.Add((ushort)(vertexCount + 2));
								indices.Add((ushort)(vertexCount + 0));
								indices.Add((ushort)(vertexCount + 2));
								indices.Add((ushort)(vertexCount + 3));

								vertices.Add(new Vertex(x + 1.0f, y + 0.0f, z + 0.0f));
								vertices.Add(new Vertex(x + 1.0f, y + 1.0f, z + 0.0f));
								vertices.Add(new Vertex(x + 1.0f, y + 1.0f, z + 1.0f));
								vertices.Add(new Vertex(x + 1.0f, y + 0.0f, z + 1.0f));
							}

							// Left face
							if (HasFace(entityManager, chunkBufferingSystem, chunk, localVoxelCoordinate + new int3(-1, 0, 0))) {
								var vertexCount = vertices.Length;
								indices.Add((ushort)(vertexCount + 0));
								indices.Add((ushort)(vertexCount + 1));
								indices.Add((ushort)(vertexCount + 2));
								indices.Add((ushort)(vertexCount + 0));
								indices.Add((ushort)(vertexCount + 2));
								indices.Add((ushort)(vertexCount + 3));

								vertices.Add(new Vertex(x + 0.0f, y + 0.0f, z + 1.0f));
								vertices.Add(new Vertex(x + 0.0f, y + 1.0f, z + 1.0f));
								vertices.Add(new Vertex(x + 0.0f, y + 1.0f, z + 0.0f));
								vertices.Add(new Vertex(x + 0.0f, y + 0.0f, z + 0.0f));
							}

							// Top face
							if (HasFace(entityManager, chunkBufferingSystem, chunk, localVoxelCoordinate + new int3(0, 1, 0))) {
								var vertexCount = vertices.Length;
								indices.Add((ushort)(vertexCount + 0));
								indices.Add((ushort)(vertexCount + 1));
								indices.Add((ushort)(vertexCount + 2));
								indices.Add((ushort)(vertexCount + 0));
								indices.Add((ushort)(vertexCount + 2));
								indices.Add((ushort)(vertexCount + 3));

								vertices.Add(new Vertex(x + 0.0f, y + 1.0f, z + 0.0f));
								vertices.Add(new Vertex(x + 0.0f, y + 1.0f, z + 1.0f));
								vertices.Add(new Vertex(x + 1.0f, y + 1.0f, z + 1.0f));
								vertices.Add(new Vertex(x + 1.0f, y + 1.0f, z + 0.0f));
							}

							// Buttom face
							if (HasFace(entityManager, chunkBufferingSystem, chunk, localVoxelCoordinate + new int3(0, -1, 0))) {
								var vertexCount = vertices.Length;
								indices.Add((ushort)(vertexCount + 0));
								indices.Add((ushort)(vertexCount + 1));
								indices.Add((ushort)(vertexCount + 2));
								indices.Add((ushort)(vertexCount + 0));
								indices.Add((ushort)(vertexCount + 2));
								indices.Add((ushort)(vertexCount + 3));

								vertices.Add(new Vertex(x + 1.0f, y + 0.0f, z + 0.0f));
								vertices.Add(new Vertex(x + 1.0f, y + 0.0f, z + 1.0f));
								vertices.Add(new Vertex(x + 0.0f, y + 0.0f, z + 1.0f));
								vertices.Add(new Vertex(x + 0.0f, y + 0.0f, z + 0.0f));
							}

							// Front face
							if (HasFace(entityManager, chunkBufferingSystem, chunk, localVoxelCoordinate + new int3(0, 0, 1))) {
								var vertexCount = vertices.Length;
								indices.Add((ushort)(vertexCount + 0));
								indices.Add((ushort)(vertexCount + 1));
								indices.Add((ushort)(vertexCount + 2));
								indices.Add((ushort)(vertexCount + 0));
								indices.Add((ushort)(vertexCount + 2));
								indices.Add((ushort)(vertexCount + 3));

								vertices.Add(new Vertex(x + 1.0f, y + 0.0f, z + 1.0f));
								vertices.Add(new Vertex(x + 1.0f, y + 1.0f, z + 1.0f));
								vertices.Add(new Vertex(x + 0.0f, y + 1.0f, z + 1.0f));
								vertices.Add(new Vertex(x + 0.0f, y + 0.0f, z + 1.0f));
							}

							// Back face
							if (HasFace(entityManager, chunkBufferingSystem, chunk, localVoxelCoordinate + new int3(0, 0, -1))) {
								var vertexCount = vertices.Length;
								indices.Add((ushort)(vertexCount + 0));
								indices.Add((ushort)(vertexCount + 1));
								indices.Add((ushort)(vertexCount + 2));
								indices.Add((ushort)(vertexCount + 0));
								indices.Add((ushort)(vertexCount + 2));
								indices.Add((ushort)(vertexCount + 3));

								vertices.Add(new Vertex(x + 0.0f, y + 0.0f, z + 0.0f));
								vertices.Add(new Vertex(x + 0.0f, y + 1.0f, z + 0.0f));
								vertices.Add(new Vertex(x + 1.0f, y + 1.0f, z + 0.0f));
								vertices.Add(new Vertex(x + 1.0f, y + 0.0f, z + 0.0f));
							}
						}
					}
				}

				commandBuffer.AddComponent(entity, new ChunkMeshData {
					Vertices = vertices.AsArray(),
					Indices = indices.AsArray()
				});

				commandBuffer.RemoveComponent<DirtyChunk>(entity);
			}).ScheduleParallel();

			CompleteDependency();
			
			commandBuffer.Playback(EntityManager);
			commandBuffer.Dispose();
		}

		private static Voxel GetVoxel(EntityManager entityManager, in SystemHandle chunkBufferingSystem, in Chunk chunk, int3 coordinate) {
			var voxelCoordinate = chunk.Coordinate * Chunk.SIZE + coordinate;
			var chunkCoordinate = new int3 {
				x = (int)math.floor(voxelCoordinate.x / (float)Chunk.SIZE),
				y = (int)math.floor(voxelCoordinate.y / (float)Chunk.SIZE),
				z = (int)math.floor(voxelCoordinate.z / (float)Chunk.SIZE)
			};
			var localVoxelCoordinate = voxelCoordinate - chunkCoordinate * Chunk.SIZE;
			var localVoxelIndex = Array3DUtility.To1D(
				localVoxelCoordinate.x,
				localVoxelCoordinate.y,
				localVoxelCoordinate.z,
				Chunk.SIZE,
				Chunk.SIZE);

			var chunkBuffer = entityManager.GetComponentDataRW<ChunkBuffer>(chunkBufferingSystem);
			var chunkEntity = ChunkBufferingSystem.GetChunk(chunkBuffer.ValueRO, chunkCoordinate);
			return chunkEntity == Entity.Null ? new Voxel(0) : entityManager.GetComponentData<Chunk>(chunkEntity).Voxels[localVoxelIndex];
		}

		private static bool HasFace(EntityManager entityManager, in SystemHandle chunkBufferingSystem, in Chunk chunk, int3 coordinate) {
			return GetVoxel(entityManager, chunkBufferingSystem, chunk, coordinate).Type == 0;
		}
	}
}