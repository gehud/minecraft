﻿using Minecraft.Components;
using Minecraft.Utilities;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace Minecraft.Systems {
	[UpdateAfter(typeof(ChunkGenerationSystem))]
	public partial struct ChunkMeshDataSystem : ISystem {
		private SystemHandle chunkBufferingSystem;

		void ISystem.OnCreate(ref SystemState state) {
			chunkBufferingSystem = state.WorldUnmanaged.GetExistingUnmanagedSystem<ChunkBufferingSystem>();
		}

		void ISystem.OnUpdate(ref SystemState state) {
			var entityCommandBuffer = new EntityCommandBuffer(Allocator.TempJob);

			foreach (var (chunk, entity) in SystemAPI.
				Query<RefRO<Chunk>>().
				WithAll<DirtyChunk>().
				WithNone<DataOnlyChunk>().
				WithEntityAccess()) {

				var vertices = new NativeList<Vertex>(Allocator.Persistent);
				var indices = new NativeList<ushort>(Allocator.Persistent);

				for (int x = 0; x < Chunk.SIZE; x++) {
					for (int y = 0; y < Chunk.SIZE; y++) {
						for (int z = 0; z < Chunk.SIZE; z++) {
							var localVoxelCoordinate = new int3(x, y, z);

							if (GetVoxel(ref state, chunk.ValueRO, localVoxelCoordinate).Type == 0) {
								continue;
							}

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

			entityCommandBuffer.Playback(state.EntityManager);

			entityCommandBuffer.Dispose();
		}

		private Voxel GetVoxel(ref SystemState state, in Chunk chunk, int3 coordinate) {
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

			var chunkBuffer = state.EntityManager.GetComponentDataRW<ChunkBuffer>(chunkBufferingSystem);
			var chunkEntity = ChunkBufferingSystem.GetChunk(chunkBuffer.ValueRO, chunkCoordinate);
			return chunkEntity == Entity.Null ? new Voxel(0) : state.EntityManager.GetComponentData<Chunk>(chunkEntity).Voxels[localVoxelIndex];
		}

		private bool HasFace(ref SystemState state, in Chunk chunk, int3 coordinate) {
			return GetVoxel(ref state, chunk, coordinate).Type == 0;
		}
	}
}