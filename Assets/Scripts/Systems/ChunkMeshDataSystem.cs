using Minecraft.Components;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace Minecraft.Systems {
	[UpdateAfter(typeof(ChunkSystem))]
	[BurstCompile]
	public partial struct ChunkMeshDataSystem : ISystem {
		[BurstCompile]
		void ISystem.OnUpdate(ref SystemState state) {
			var entityCommandBuffer = new EntityCommandBuffer(Allocator.TempJob);

			foreach (var (chunk, entity) in SystemAPI.Query<RefRO<Chunk>>().WithAll<DirtyChunk>().WithEntityAccess()) {
				var vertices = new NativeList<Vertex>(Allocator.Persistent);
				var indices = new NativeList<ushort>(Allocator.Persistent);

				for (int x = 0; x < Chunk.SIZE; x++) {
					for (int y = 0; y < Chunk.SIZE; y++) {
						for (int z = 0; z < Chunk.SIZE; z++) {
							if (GetVoxel(ref state, chunk.ValueRO, new int3(x, y, z)).Type == 0) {
								continue;
							}

							var localVoxelCoordinate = new int3(x, y, z);

							// Right face
							if (HasFace(ref state, chunk.ValueRO, localVoxelCoordinate + new int3(1, 0, 0))) {
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
							if (HasFace(ref state, chunk.ValueRO, localVoxelCoordinate + new int3(-1, 0, 0))) {
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
							if (HasFace(ref state, chunk.ValueRO, localVoxelCoordinate + new int3(0, 1, 0))) {
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
							if (HasFace(ref state, chunk.ValueRO, localVoxelCoordinate + new int3(0, -1, 0))) {
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
							if (HasFace(ref state, chunk.ValueRO, localVoxelCoordinate + new int3(0, 0, 1))) {
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
							if (HasFace(ref state, chunk.ValueRO, localVoxelCoordinate + new int3(0, 0, -1))) {
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

				entityCommandBuffer.AddComponent(entity, new ChunkMeshData {
					Vertices = vertices.AsArray(),
					Indices = indices.AsArray()
				});

				entityCommandBuffer.RemoveComponent<DirtyChunk>(entity);
			}

			state.CompleteDependency();

			entityCommandBuffer.Playback(state.EntityManager);

			entityCommandBuffer.Dispose();
		}

		[BurstCompile]
		private Voxel GetVoxel(ref SystemState state, in Chunk chunk, int3 coordinate) {
			var chunkClusterCoordinateDelta = new int3 {
				x = (int)math.floor(coordinate.x / (float)Chunk.SIZE),
				y = (int)math.floor(coordinate.y / (float)Chunk.SIZE),
				z = (int)math.floor(coordinate.z / (float)Chunk.SIZE)
			};

			var chunkClusterCoordinate = chunkClusterCoordinateDelta + new int3(1, 1, 1);
			var chunkClusterIndex = chunkClusterCoordinate.z * 9
				+ chunkClusterCoordinate.y * 3 + chunkClusterCoordinate.x;

			var localVoxelCoordinate = coordinate - chunkClusterCoordinateDelta * Chunk.SIZE;
			var localVoxelIndex = localVoxelCoordinate.z * Chunk.SIZE * Chunk.SIZE
				+ localVoxelCoordinate.y * Chunk.SIZE + localVoxelCoordinate.x;

			var entity = chunk.Claster[chunkClusterIndex];
			if (entity == Entity.Null) {
				return new Voxel(0);
			}

			return state.EntityManager.GetComponentData<Chunk>(entity).Voxels[localVoxelIndex];
		}

		[BurstCompile]
		private bool HasFace(ref SystemState state, in Chunk chunk, int3 coordinate) {
			return GetVoxel(ref state, chunk, coordinate).Type == 0;
		}
	}
}