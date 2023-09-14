using Minecraft.Components;
using Minecraft.Utilities;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

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

		private static void UpdateBuffer(EntityCommandBuffer commandBuffer, 
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

							commandBuffer.DestroyEntity(chunkBuffer.Chunks[index]);
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

		private void GenerateLoadData(ref ChunkLoadData loadData, int2 center, int zone) {
			loadData.Data = new NativeList<ChunkLoadDescription>(Allocator.TempJob);

			int startX = center.x - zone - 1;
			int endX = center.x + zone + 1;
			int startZ = center.y - zone - 1;
			int endZ = center.y + zone + 1;
			for (int x = startX; x <= endX; x++) {
				for (int z = startZ; z <= endZ; z++) {
					for (int y = 0; y < ChunkBuffer.HEIGHT; y++) {
						var chunkCoordinate = new int3(x, y, z);

						bool isRendered = x != startX && x != endX && z != startZ && z != endZ;
						loadData.Data.Add(new ChunkLoadDescription {
							Coordinate = chunkCoordinate,
							IsRendered = isRendered
						});
					}
				}
			}
		}

		void ISystem.OnCreate(ref SystemState state) {
			state.EntityManager.AddComponent<ChunkBuffer>(state.SystemHandle);
			state.EntityManager.AddComponent<ChunkLoadData>(state.SystemHandle);

			var requestEntity = state.EntityManager.CreateEntity();
			state.EntityManager.AddComponentData(requestEntity, new ChunkBufferingResizeRequest {
				NewDrawDistance = 2,
			});

			requestEntity = state.EntityManager.CreateEntity();
			state.EntityManager.AddComponentData(requestEntity, new ChunkBufferingRequest {
				NewCenter = int2.zero
			});
		}

		void ISystem.OnUpdate(ref SystemState state) {
			var commandBuffer = new EntityCommandBuffer(Allocator.TempJob);

			foreach (var transform in SystemAPI.Query<RefRO<LocalToWorld>>().WithAll<PlayerMovement>()) {
				var position = transform.ValueRO.Position;
				var column = new int2 {
					x = (int)math.floor(position.x / Chunk.SIZE),
					y = (int)math.floor(position.z / Chunk.SIZE)
				};

				var loadData = state.EntityManager.GetComponentDataRW<ChunkLoadData>(state.SystemHandle);
				var lastPlayerColumn = loadData.ValueRO.LastPlayerColumn;
				if (lastPlayerColumn.x != column.x || lastPlayerColumn.y != column.y) {
					loadData.ValueRW.LastPlayerColumn = column;
					var requestEntity = state.EntityManager.CreateEntity();
					commandBuffer.AddComponent(requestEntity, new ChunkBufferingRequest {
						NewCenter = column
					});
				}
			}

			foreach (var (request, entity) in SystemAPI.
				Query<RefRO<ChunkBufferingResizeRequest>>().
				WithEntityAccess()) {

				var buffer = state.EntityManager.GetComponentDataRW<ChunkBuffer>(state.SystemHandle);
				UpdateMetrics(ref buffer.ValueRW, request.ValueRO.NewDrawDistance);
				commandBuffer.DestroyEntity(entity);
			}

			foreach (var (request, entity) in SystemAPI.
				Query<RefRO<ChunkBufferingRequest>>().
				WithEntityAccess()) {

				var buffer = state.EntityManager.GetComponentDataRW<ChunkBuffer>(state.SystemHandle);
				UpdateBuffer(commandBuffer, ref buffer.ValueRW, request.ValueRO.NewCenter);

				var loadData = state.EntityManager.GetComponentDataRW<ChunkLoadData>(state.SystemHandle);

				GenerateLoadData(ref loadData.ValueRW, request.ValueRO.NewCenter, buffer.ValueRO.DrawDistance);

				foreach (var item in loadData.ValueRO.Data) {
					if (!HasChunk(buffer.ValueRO, item.Coordinate)) {
						var newChunk = state.EntityManager.CreateEntity();
						commandBuffer.AddComponent(newChunk, new ChunkInitializer {
							Coordinate = item.Coordinate,
							HasRenderer = item.IsRendered
						});

						buffer.ValueRW.Chunks[ChunkToIndex(buffer.ValueRO, item.Coordinate)] = newChunk;
					} else {
						var chunk = GetChunk(buffer.ValueRO, item.Coordinate);

						if (item.IsRendered) {
							if (state.EntityManager.HasComponent<DataOnlyChunk>(chunk)) {
								commandBuffer.RemoveComponent<DataOnlyChunk>(chunk);
							} 
						} else {
							if (!state.EntityManager.HasComponent<DataOnlyChunk>(chunk)) {
								commandBuffer.AddComponent<DataOnlyChunk>(chunk);
							}
						}
					}
				}

				loadData.ValueRW.Data.Dispose();

				commandBuffer.DestroyEntity(entity);
			}

			commandBuffer.Playback(state.EntityManager);
			commandBuffer.Dispose();
		}
	}
}