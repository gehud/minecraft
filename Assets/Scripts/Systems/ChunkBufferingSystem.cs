using Minecraft.Components;
using Minecraft.Utilities;
using System.Linq;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace Minecraft.Systems {
	[UpdateBefore(typeof(ChunkSystem))]
	public partial struct ChunkBufferingSystem : ISystem {
		public static int ChunkToIndex(in ChunkBuffer chunkBuffer, int3 coordinate) {
			var arrayCoordinate = new int3 {
				x = coordinate.x - chunkBuffer.Center.x + chunkBuffer.DrawDistance + 1,
				y = coordinate.y,
				z = coordinate.z - chunkBuffer.Center.y + chunkBuffer.DrawDistance + 1
			};

			return Array3DUtility.To1D(arrayCoordinate.x, arrayCoordinate.y, 
				arrayCoordinate.z, chunkBuffer.ChunksSize, ChunkBuffer.HEIGHT);
		}

		public static bool HasChunk(in ChunkBuffer chunkBuffer, int3 coordinate) {
#if DEBUG // With bounds check.
			var arrayCoordinate = new int3 {
				x = coordinate.x - chunkBuffer.Center.x + chunkBuffer.DrawDistance + 1,
				y = coordinate.y,
				z = coordinate.z - chunkBuffer.Center.y + chunkBuffer.DrawDistance + 1
			};

			if (arrayCoordinate.x < 0 || 
				arrayCoordinate.y < 0 || 
				arrayCoordinate.z < 0 ||
				arrayCoordinate.x >= chunkBuffer.ChunksSize ||
				arrayCoordinate.y >= chunkBuffer.ChunksSize || 
				arrayCoordinate.z >= chunkBuffer.ChunksSize) {
				return false;
			}

			return chunkBuffer.Chunks[ChunkToIndex(chunkBuffer, coordinate)] != Entity.Null;
#else
			var index = ChunkToIndex(chunkBuffer, coordinate);
			if (index < 0 || index >= chunkBuffer.Chunks.Length) {
				return false;
			}

			return chunkBuffer.Chunks[index] != Entity.Null;
#endif
		}

		public static Entity GetChunk(in ChunkBuffer chunkBuffer, int3 coordinate) {
#if DEBUG // With bounds check.
			var arrayCoordinate = new int3 {
				x = coordinate.x - chunkBuffer.Center.x + chunkBuffer.DrawDistance + 1,
				y = coordinate.y,
				z = coordinate.z - chunkBuffer.Center.y + chunkBuffer.DrawDistance + 1
			};

			if (arrayCoordinate.x < 0 ||
				arrayCoordinate.y < 0 ||
				arrayCoordinate.z < 0 ||
				arrayCoordinate.x >= chunkBuffer.ChunksSize ||
				arrayCoordinate.y >= chunkBuffer.ChunksSize ||
				arrayCoordinate.z >= chunkBuffer.ChunksSize) {
				return Entity.Null;
			}

			return chunkBuffer.Chunks[ChunkToIndex(chunkBuffer, coordinate)];
#else
			var index = ChunkToIndex(chunkBuffer, coordinate);
			if (index < 0 || index >= chunkBuffer.Chunks.Length) {
				return Entity.Null;
			}

			return chunkBuffer.Chunks[index];
#endif
		}

		public static void SetChunk(ref ChunkBuffer chunkBuffer, int3 coordinate, Entity chunk) {
			chunkBuffer.Chunks[ChunkToIndex(chunkBuffer, coordinate)] = chunk;
		}

		public static bool HasRenderedChunk(EntityManager entityManager, in ChunkBuffer chunkBuffer, int3 coordinate) {
			var index = ChunkToIndex(chunkBuffer, coordinate);
			if (index < 0 || index >= chunkBuffer.Chunks.Length) {
				return false;
			}

			var entity = chunkBuffer.Chunks[index];
			return entity != Entity.Null && !entityManager.HasComponent<DataOnlyChunk>(entity);
		}

		private static void UpdateMetrics(ref ChunkBuffer chunkBuffer, int newDrawDistance) {
			var oldChunksSize = chunkBuffer.ChunksSize;
			var oldChunks = chunkBuffer.Chunks;
			chunkBuffer.DrawDistance = newDrawDistance;
			chunkBuffer.ChunksSize = chunkBuffer.DrawDistance * 2 + 3;
			var chunksVolume = chunkBuffer.ChunksSize * chunkBuffer.ChunksSize * ChunkBuffer.HEIGHT;
			
			chunkBuffer.Chunks = new NativeArray<Entity>(chunksVolume, Allocator.Persistent);
			if (chunkBuffer.ChunksBuffer.IsCreated) {
				chunkBuffer.ChunksBuffer.Dispose();
			}
			chunkBuffer.ChunksBuffer = new NativeArray<Entity>(chunksVolume, Allocator.Persistent);

			if (oldChunks.IsCreated) {
				var d = chunkBuffer.ChunksSize - oldChunksSize;
				for (int x = 0; x < oldChunksSize; x++) {
					for (int z = 0; z < oldChunksSize; z++) {
						for (int y = 0; y < ChunkBuffer.HEIGHT; y++) {
							var index = Array3DUtility.To1D(x, y, z, oldChunksSize, ChunkBuffer.HEIGHT);
							var chunk = oldChunks[index];
							if (chunk == Entity.Null)
								continue;

							int nx = x + d / 2;
							int nz = z + d / 2;
							if (nx < 0 || nz < 0 || nx >= chunkBuffer.ChunksSize || nz >= chunkBuffer.ChunksSize)
								continue;

							chunkBuffer.Chunks[Array3DUtility.To1D(nx, y, nz, chunkBuffer.ChunksSize, ChunkBuffer.HEIGHT)] = chunk;
						}
					}
				}

				oldChunks.Dispose();
			}
		}

		private static void UpdateBuffer(EntityManager entityManager, 
			ref ChunkBuffer chunkBuffer, int2 newCenter)  {

			for (int i = 0; i < chunkBuffer.ChunksBuffer.Length; i++) {
				chunkBuffer.ChunksBuffer[i] = Entity.Null;
			}

			var centerDelta = newCenter - chunkBuffer.Center;
			for (int x = 0; x < chunkBuffer.ChunksSize; x++) {
				for (int z = 0; z < chunkBuffer.ChunksSize; z++) {
					for (int y = 0; y < ChunkBuffer.HEIGHT; y++) {
						var index = Array3DUtility.To1D(x, y, z, chunkBuffer.ChunksSize, 
							ChunkBuffer.HEIGHT);

						var chunk = chunkBuffer.Chunks[index];
						if (chunk == Entity.Null)
							continue;

						int nx = x - centerDelta.x;
						int nz = z - centerDelta.y;
						if (nx < 0 || nz < 0 || nx >= chunkBuffer.ChunksSize || 
							nz >= chunkBuffer.ChunksSize) {

							entityManager.DestroyEntity(chunkBuffer.Chunks[index]);
							chunkBuffer.Chunks[index] = Entity.Null;
							continue;
						}

						chunkBuffer.ChunksBuffer[Array3DUtility.To1D(nx, y, nz, 
							chunkBuffer.ChunksSize, ChunkBuffer.HEIGHT)] = chunk;
					}
				}
			}

			(chunkBuffer.Chunks, chunkBuffer.ChunksBuffer) = 
				(chunkBuffer.ChunksBuffer, chunkBuffer.Chunks);

			chunkBuffer.Center = newCenter;
		}

		private void GenerateLoadData(ref ChunkLoadData loadData, in ChunkBuffer chunkBuffer, int2 center, int zone) {
			loadData.Chunks = new NativeList<int3>(Allocator.TempJob);
			loadData.Renderers = new NativeList<int3>(Allocator.TempJob);

			int startX = center.x - zone - 1;
			int endX = center.x + zone + 1;
			int startZ = center.y - zone - 1;
			int endZ = center.y + zone + 1;
			for (int x = startX; x <= endX; x++) {
				for (int z = startZ; z <= endZ; z++) {
					for (int y = 0; y < ChunkBuffer.HEIGHT; y++) {
						var chunkCoordinate = new int3(x, y, z);

						if (!HasChunk(chunkBuffer, chunkCoordinate)) {
							loadData.Chunks.Add(chunkCoordinate);
						}

						if (x != startX && x != endX && z != startZ && z != endZ) {
							loadData.Renderers.Add(chunkCoordinate);
						}
					}
				}
			}
		}

		void ISystem.OnCreate(ref SystemState state) {
			state.EntityManager.AddComponent<ChunkBuffer>(state.SystemHandle);
			state.EntityManager.AddComponent<ChunkLoadData>(state.SystemHandle);

			state.EntityManager.AddComponentData(state.SystemHandle, new ChunkBufferingResizeRequest {
				NewDrawDistance = 2,
			});

			state.EntityManager.AddComponentData(state.SystemHandle, new ChunkBufferingRequest {
				NewCenter = int2.zero
			});
		}

		void ISystem.OnUpdate(ref SystemState state) {
			if (state.EntityManager.HasComponent<ChunkBufferingResizeRequest>(state.SystemHandle)) {
				var request = state.EntityManager.GetComponentData<ChunkBufferingResizeRequest>(state.SystemHandle);
				var buffer = state.EntityManager.GetComponentDataRW<ChunkBuffer>(state.SystemHandle);
				UpdateMetrics(ref buffer.ValueRW, request.NewDrawDistance);
				state.EntityManager.RemoveComponent<ChunkBufferingResizeRequest>(state.SystemHandle);
			}

			if (state.EntityManager.HasComponent<ChunkBufferingRequest>(state.SystemHandle)) {
				var request = state.EntityManager.GetComponentData<ChunkBufferingRequest>(state.SystemHandle);

				var chunkBuffer = state.EntityManager.GetComponentDataRW<ChunkBuffer>(state.SystemHandle);
				UpdateBuffer(state.EntityManager, ref chunkBuffer.ValueRW, request.NewCenter);

				var buffer = state.EntityManager.GetComponentData<ChunkBuffer>(state.SystemHandle);
				for (int zone = 0; zone <= buffer.DrawDistance; zone++) {
					var loadData = state.EntityManager.GetComponentDataRW<ChunkLoadData>(state.SystemHandle);
					chunkBuffer = state.EntityManager.GetComponentDataRW<ChunkBuffer>(state.SystemHandle);
					GenerateLoadData(ref loadData.ValueRW, chunkBuffer.ValueRO, request.NewCenter, zone);

					loadData = state.EntityManager.GetComponentDataRW<ChunkLoadData>(state.SystemHandle);
					foreach (var item in loadData.ValueRO.Chunks) {
						var newChunk = state.EntityManager.CreateEntity();
						state.EntityManager.AddComponentData(newChunk, new ChunkInitializer {
							Coordinate = item,
							HasRenderer = true
						});

						chunkBuffer = state.EntityManager.GetComponentDataRW<ChunkBuffer>(state.SystemHandle);
						chunkBuffer.ValueRW.Chunks[ChunkToIndex(state.EntityManager.GetComponentDataRW<ChunkBuffer>(state.SystemHandle).ValueRO, item)] = newChunk;
					}

					loadData = state.EntityManager.GetComponentDataRW<ChunkLoadData>(state.SystemHandle);
					foreach (var item in loadData.ValueRO.Renderers) {
						chunkBuffer = state.EntityManager.GetComponentDataRW<ChunkBuffer>(state.SystemHandle);
						var chunk = GetChunk(chunkBuffer.ValueRO, item);
						if (state.EntityManager.HasComponent<ChunkInitializer>(chunk)) {
							var initializer = state.EntityManager.GetComponentData<ChunkInitializer>(chunk);
							initializer.HasRenderer = true;
							state.EntityManager.SetComponentData(chunk, initializer);
						} else if (state.EntityManager.HasComponent<DataOnlyChunk>(chunk)) {
							state.EntityManager.RemoveComponent<DataOnlyChunk>(chunk);
						}
					}

					loadData = state.EntityManager.GetComponentDataRW<ChunkLoadData>(state.SystemHandle);
					loadData.ValueRW.Chunks.Dispose();
					loadData = state.EntityManager.GetComponentDataRW<ChunkLoadData>(state.SystemHandle);
					loadData.ValueRW.Renderers.Dispose();
				}

				state.EntityManager.RemoveComponent<ChunkBufferingRequest>(state.SystemHandle);
			}
		}
	}
}