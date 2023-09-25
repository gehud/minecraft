using Minecraft.Components;
using Minecraft.Utilities;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Rendering;

namespace Minecraft.Systems {
	[UpdateAfter(typeof(ChunkGenerationSystem))]
	public partial class ChunkMeshDataSystem : SystemBase {
		private ChunkMeshDataGenerationJob lastJob;
		private JobHandle lastJobHandle;
		private EntityCommandBuffer cmd;

		protected override void OnCreate() {
			EntityManager.AddComponentData(SystemHandle, new ChunkMeshDataSystemData {
				ChunkBufferingSystem = World.GetExistingSystem<ChunkBufferingSystem>()
			});
		}

		[BurstCompile]
		private struct ChunkMeshDataGenerationJob : IJob {
			public EntityCommandBuffer commandBuffer;
			[ReadOnly] public Entity entity;
			[ReadOnly] public int3 chunkCoordinate;
			[ReadOnly, NativeDisableContainerSafetyRestriction] 
			public NativeArray<NativeArray<Voxel>> Claster;

			public void Execute() {
				var vertices = new NativeList<Vertex>(Allocator.Persistent);
				var indices = new NativeList<ushort>(Allocator.Persistent);

				for (int x = 0; x < Chunk.SIZE; x++) {
					for (int y = 0; y < Chunk.SIZE; y++) {
						for (int z = 0; z < Chunk.SIZE; z++) {
							var localVoxelCoordinate = new int3(x, y, z);

							if (GetVoxel(Claster, chunkCoordinate, localVoxelCoordinate).Type == 0) {
								continue;
							}

							// Right face
							if (HasFace(Claster, chunkCoordinate, localVoxelCoordinate + new int3(1, 0, 0))) {
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
							if (HasFace(Claster, chunkCoordinate, localVoxelCoordinate + new int3(-1, 0, 0))) {
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
							if (HasFace(Claster, chunkCoordinate, localVoxelCoordinate + new int3(0, 1, 0))) {
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
							if (HasFace(Claster, chunkCoordinate, localVoxelCoordinate + new int3(0, -1, 0))) {
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
							if (HasFace(Claster, chunkCoordinate, localVoxelCoordinate + new int3(0, 0, 1))) {
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
							if (HasFace(Claster, chunkCoordinate, localVoxelCoordinate + new int3(0, 0, -1))) {
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
			}
		}

		protected override void OnUpdate() {
			Entities.WithAll<DataOnlyChunk>().WithNone<DisableRendering>().ForEach((Entity entity) => {
				EntityManager.AddComponent<DisableRendering>(entity);
			}).WithStructuralChanges().Run();

			Entities.WithAll<DisableRendering>().WithNone<DataOnlyChunk>().ForEach((Entity entity) => {
				EntityManager.RemoveComponent<DisableRendering>(entity);
			}).WithStructuralChanges().Run();

			var querry = new EntityQueryBuilder(Allocator.TempJob).
				WithAll<Chunk>().
				WithAll<DirtyChunk>().
				WithNone<DataOnlyChunk>().
				Build(EntityManager);
			var entities = querry.ToEntityArray(Allocator.TempJob);
			querry.Dispose();
			if (entities.Length != 0 && lastJobHandle.IsCompleted) {
				lastJobHandle.Complete();

				if (cmd.IsCreated) {
					cmd.Playback(EntityManager);
					cmd.Dispose();
				}

				if (lastJob.Claster.IsCreated) {
					lastJob.Claster.Dispose();
				}

				cmd = new EntityCommandBuffer(Allocator.Persistent);

				var entity = entities[0];

				var claster = new NativeArray<NativeArray<Voxel>>(3 * 3 * 3, Allocator.TempJob);
				var chunkCoordinate = EntityManager.GetComponentData<Chunk>(entity).Coordinate;
				var origin = chunkCoordinate - new int3(1, 1, 1);
				var chunkBuffeingSystem = EntityManager.GetComponentData<ChunkMeshDataSystemData>(SystemHandle).ChunkBufferingSystem;
				var chunkBuffer = EntityManager.GetComponentDataRW<ChunkBuffer>(chunkBuffeingSystem);
				for (int i = 0; i < 3 * 3 * 3; i++) {
					var coordinate = Array3DUtility.To3D(i, 3, 3);
					var chunk = ChunkBufferingSystem.GetChunk(chunkBuffer.ValueRO, origin + coordinate);
					claster[i] = EntityManager.Exists(chunk) ? EntityManager.GetComponentData<Chunk>(chunk).Voxels : default;
				}

				lastJob = new ChunkMeshDataGenerationJob {
					chunkCoordinate = chunkCoordinate,
					commandBuffer = cmd,
					entity = entity,
					Claster = claster
				};

				lastJobHandle = lastJob.Schedule();
			}

			entities.Dispose();
		}

		private static Voxel GetVoxel(in NativeArray<NativeArray<Voxel>> claster, in int3 chunkCoordinate, int3 localVoxelCoordinate) {
			var voxelCoordinate = chunkCoordinate * Chunk.SIZE + localVoxelCoordinate;
			var sideChunkCoordinate = new int3 {
				x = (int)math.floor(voxelCoordinate.x / (float)Chunk.SIZE),
				y = (int)math.floor(voxelCoordinate.y / (float)Chunk.SIZE),
				z = (int)math.floor(voxelCoordinate.z / (float)Chunk.SIZE)
			};
			var sideLocalVoxelCoordinate = voxelCoordinate - sideChunkCoordinate * Chunk.SIZE;

			sideChunkCoordinate -= chunkCoordinate;
			sideChunkCoordinate += new int3(1, 1, 1);
			var clasterIndex = Array3DUtility.To1D(sideChunkCoordinate, 3, 3);
			if (!claster[clasterIndex].IsCreated) {
				return new Voxel(0);
			}

			var sideLocalVoxelIndex = Array3DUtility.To1D(sideLocalVoxelCoordinate, Chunk.SIZE, Chunk.SIZE);
			return claster[clasterIndex][sideLocalVoxelIndex];
		}

		private static bool HasFace(in NativeArray<NativeArray<Voxel>> claster, in int3 chunkCoordinate, int3 localVoxelCoordinate) {
			return GetVoxel(claster, chunkCoordinate, localVoxelCoordinate).Type == 0;
		}
	}
}