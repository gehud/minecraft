using Minecraft.Utilities;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;

namespace Minecraft.Lighting {
    [BurstCompile]
    [UpdateAfter(typeof(ChunkGenerationSystem))]
    public partial struct LightingSystem : ISystem {
        public const int ChanelCount = 4;

        private static readonly int3[] blockSides = {
            new( 0,  0,  1),
            new( 0,  0, -1),
            new( 0,  1,  0),
            new( 0, -1,  0),
            new( 1,  0,  0),
            new(-1,  0,  0),
        };

        [BurstCompile]
        public static void AddLight(in LightingSystemData systemData, in ChunkBufferingSystemData chunkBufferingSystemData, in EntityManager entityManager, in EntityCommandBuffer commandBuffer, in int3 voxelCoordinate, LightChanel chanel, byte level) {
            if (level <= 1) {
                return;
            }

            var chunkCoordinate = CoordinateUtility.ToChunk(voxelCoordinate);

            ChunkBufferingSystem.GetEntity(chunkBufferingSystemData, chunkCoordinate, out var entity);
            if (entity == Entity.Null || !entityManager.HasComponent<Chunk>(entity)) {
                return;
            }

            var localVoxelCoordinate = CoordinateUtility.ToLocal(chunkCoordinate, voxelCoordinate);
            var index = IndexUtility.ToIndex(localVoxelCoordinate, Chunk.Size, Chunk.Size);
            var voxels = entityManager.GetComponentData<Chunk>(entity).Voxels;
            var voxel = voxels[index];
            voxel.Light.Set(chanel, level);
            voxels[index] = voxel;

            if (!entityManager.HasComponent<DirtyChunk>(entity)) {
                commandBuffer.AddComponent<DirtyChunk>(entity);
            }

            if (!entityManager.HasComponent<ImmediateChunk>(entity)) {
                commandBuffer.AddComponent<ImmediateChunk>(entity);
            }

            ChunkBufferingSystem.MarkDirtyIfNeededImmediate(chunkBufferingSystemData, entityManager, commandBuffer, chunkCoordinate, localVoxelCoordinate);

            var entry = new LightingEntry(voxelCoordinate, level);
            systemData.AddQueues[(int)chanel].Enqueue(entry);
        }

        [BurstCompile]
        public static void AddLight(in LightingSystemData systemData, in ChunkBufferingSystemData chunkBufferingSystemData, in EntityManager entityManager, in int3 voxelCoordinate, LightChanel chanel) {
            var chunkCoordinate = CoordinateUtility.ToChunk(voxelCoordinate);
            ChunkBufferingSystem.GetEntity(chunkBufferingSystemData, chunkCoordinate, out var entity);
            if (entity == Entity.Null || !entityManager.HasComponent<Chunk>(entity)) {
                return;
            }

            var localVoxelCoordinate = CoordinateUtility.ToLocal(chunkCoordinate, voxelCoordinate);
            var index = IndexUtility.ToIndex(localVoxelCoordinate, Chunk.Size, Chunk.Size);
            var voxels = entityManager.GetComponentData<Chunk>(entity).Voxels;
            var level = voxels[index].Light.Get(chanel);
            if (level <= 1) {
                return;
            }

            var entry = new LightingEntry(voxelCoordinate, level);
            systemData.AddQueues[(int)chanel].Enqueue(entry);
        }

        [BurstCompile]
        public static void RemoveLight(in LightingSystemData systemData, in ChunkBufferingSystemData chunkBufferingSystemData, in EntityManager entityManager, in EntityCommandBuffer commandBuffer, in int3 voxelCoordinate, LightChanel chanel) {
            var chunkCoordinate = CoordinateUtility.ToChunk(voxelCoordinate);
            ChunkBufferingSystem.GetEntity(chunkBufferingSystemData, chunkCoordinate, out var entity);
            if (entity == Entity.Null || !entityManager.HasComponent<Chunk>(entity)) {
                return;
            }

            var localVoxelCoordinate = CoordinateUtility.ToLocal(chunkCoordinate, voxelCoordinate);
            var index = IndexUtility.ToIndex(localVoxelCoordinate, Chunk.Size, Chunk.Size);
            var voxels = entityManager.GetComponentData<Chunk>(entity).Voxels;
            var voxel = voxels[index];
            byte level = voxel.Light.Get(chanel);
            if (level <= 1) {
                return;
            }

            voxel.Light.Set(chanel, Light.Min);
            voxels[index] = voxel;

            if (!entityManager.HasComponent<DirtyChunk>(entity)) {
                commandBuffer.AddComponent<DirtyChunk>(entity);
            }

            if (!entityManager.HasComponent<ImmediateChunk>(entity)) {
                commandBuffer.AddComponent<ImmediateChunk>(entity);
            }

            ChunkBufferingSystem.MarkDirtyIfNeededImmediate(chunkBufferingSystemData, entityManager, commandBuffer, chunkCoordinate, localVoxelCoordinate);

            var entry = new LightingEntry(voxelCoordinate, level);
            systemData.RemoveQueues[(int)chanel].Enqueue(entry);
        }

        [BurstCompile]
        void ISystem.OnCreate(ref SystemState state) {
            var addQueues = new NativeArray<NativeQueue<LightingEntry>>(ChanelCount, Allocator.Persistent);
            for (int i = 0; i < addQueues.Length; i++) {
                addQueues[i] = new NativeQueue<LightingEntry>(Allocator.Persistent);
            }

            var removeQueues = new NativeArray<NativeQueue<LightingEntry>>(ChanelCount, Allocator.Persistent);
            for (int i = 0; i < removeQueues.Length; i++) {
                removeQueues[i] = new NativeQueue<LightingEntry>(Allocator.Persistent);
            }

            state.EntityManager.AddComponentData(state.SystemHandle, new LightingSystemData {
                AddQueues = addQueues,
                RemoveQueues = removeQueues
            });
        }

        [BurstCompile]
        public static void Calculate(in LightingSystemData systemData, in BlockSystemData blockSystemData, in ChunkBufferingSystemData chunkBufferingSystemData, in EntityManager entityManager, in EntityCommandBuffer commandBuffer, LightChanel chanel) {
            while (systemData.RemoveQueues[(int)chanel].TryDequeue(out var entry)) {
                for (int i = 0; i < blockSides.Length; i++) {
                    var voxelCoordinate = entry.Coordinate + blockSides[i];
                    var chunkCoordinate = CoordinateUtility.ToChunk(voxelCoordinate);
                    ChunkBufferingSystem.GetEntity(chunkBufferingSystemData, chunkCoordinate, out var entity);
                    if (entity != Entity.Null && entityManager.HasComponent<Chunk>(entity)) {
                        var localVoxelCoordinate = CoordinateUtility.ToLocal(chunkCoordinate, voxelCoordinate);
                        var index = IndexUtility.ToIndex(localVoxelCoordinate, Chunk.Size, Chunk.Size);
                        var voxels = entityManager.GetComponentData<Chunk>(entity).Voxels;
                        var voxel = voxels[index];
                        var level = voxel.Light.Get(chanel);
                        var blockType = voxels[index].Type;
                        var absorption = blockSystemData.Blocks[(int)blockType].Absorption;
                        if (level != 0 && level == entry.Level - absorption - 1) {
                            var removeEntry = new LightingEntry(voxelCoordinate, level);
                            systemData.RemoveQueues[(int)chanel].Enqueue(removeEntry);
                            voxel.Light.Set(chanel, Light.Min);
                            voxels[index] = voxel;
                            if (!entityManager.HasComponent<DirtyChunk>(entity)) {
                                commandBuffer.AddComponent<DirtyChunk>(entity);
                            }
                            if (!entityManager.HasComponent<ImmediateChunk>(entity)) {
                                commandBuffer.AddComponent<ImmediateChunk>(entity);
                            }
                            ChunkBufferingSystem.MarkDirtyIfNeededImmediate(chunkBufferingSystemData, entityManager, commandBuffer, chunkCoordinate, localVoxelCoordinate);
                        } else if (level >= entry.Level) {
                            var addEntry = new LightingEntry(voxelCoordinate, level);
                            systemData.AddQueues[(int)chanel].Enqueue(addEntry);
                        }
                    }
                }
            }

            while (systemData.AddQueues[(int)chanel].TryDequeue(out var entry)) {
                if (entry.Level <= 1) {
                    continue;
                }

                for (int i = 0; i < blockSides.Length; i++) {
                    var voxelCoordinate = entry.Coordinate + blockSides[i];
                    var chunkCoordinate = CoordinateUtility.ToChunk(voxelCoordinate);
                    ChunkBufferingSystem.GetEntity(chunkBufferingSystemData, chunkCoordinate, out var entity);
                    if (entity != Entity.Null && entityManager.HasComponent<Chunk>(entity)) {
                        var localVoxelCoordinate = CoordinateUtility.ToLocal(chunkCoordinate, voxelCoordinate);
                        var index = IndexUtility.ToIndex(localVoxelCoordinate, Chunk.Size, Chunk.Size);
                        var voxels = entityManager.GetComponentData<Chunk>(entity).Voxels;
                        var voxel = voxels[index];
                        var level = voxel.Light.Get(chanel);
                        var blockType = voxels[index].Type;
                        var absorption = blockSystemData.Blocks[(int)blockType].Absorption;
                        if (blockSystemData.Blocks[(int)blockType].IsTransparent && level + absorption + 1 < entry.Level) {
                            var newLevel = (byte)(entry.Level - absorption - 1);
                            voxel.Light.Set(chanel, newLevel);
                            voxels[index] = voxel;
                            var addEntry = new LightingEntry(voxelCoordinate, newLevel);
                            systemData.AddQueues[(int)chanel].Enqueue(addEntry);
                            if (!entityManager.HasComponent<DirtyChunk>(entity)) {
                                commandBuffer.AddComponent<DirtyChunk>(entity);
                            }
                            if (!entityManager.HasComponent<ImmediateChunk>(entity)) {
                                commandBuffer.AddComponent<ImmediateChunk>(entity);
                            }
                            ChunkBufferingSystem.MarkDirtyIfNeededImmediate(chunkBufferingSystemData, entityManager, commandBuffer, chunkCoordinate, localVoxelCoordinate);
                        }
                    }
                }
            }
        }

        [BurstCompile]
        void ISystem.OnUpdate(ref SystemState state) {
            var chunkBufferingSystemData = SystemAPI.GetSingletonRW<ChunkBufferingSystemData>();

            var commandBuffer = new EntityCommandBuffer(Allocator.Temp);

            foreach (var (request, entity) in SystemAPI
                .Query<SunlightRequest>()
                .WithEntityAccess()) {

                var systemData = state.EntityManager.GetComponentData<LightingSystemData>(state.SystemHandle);
                var blockSystemData = SystemAPI.GetSingleton<BlockSystemData>();

                var clasterHeight = chunkBufferingSystemData.ValueRO.Height + 2;
                var claster = new NativeArray<NativeArray<Voxel>>(3 * 3 * clasterHeight, Allocator.TempJob);
                var origin = new int3 {
                    x = request.Column.x - 1,
                    y = -1,
                    z = request.Column.y - 1
                };

                bool isValidClaster = true;
                for (int j = 0; j < 3 * 3 * clasterHeight; j++) {
                    var coordinate = origin + IndexUtility.ToCoordinate(j, 3, clasterHeight);
                    ChunkBufferingSystem.GetEntity(chunkBufferingSystemData.ValueRO, coordinate, out var clasterEntity);
                    bool isValidChunk = state.EntityManager.Exists(clasterEntity)
                        && state.EntityManager.HasComponent<Chunk>(clasterEntity)
                        && !state.EntityManager.HasComponent<DirtyChunk>(clasterEntity)
                        && !state.EntityManager.HasComponent<RawChunk>(clasterEntity);

                    if (isValidChunk) {
                        claster[j] = state.EntityManager.GetComponentData<Chunk>(clasterEntity).Voxels;
                    } else if (!isValidChunk 
                        && coordinate.y != -1 
                        && coordinate.y != chunkBufferingSystemData.ValueRO.Height) {
                        isValidClaster = false;
                        break;
                    }
                }

                if (!isValidClaster) {
                    claster.Dispose();
                    continue;
                }

                var job = new SunlightCalculationJob {
                    Blocks = blockSystemData.Blocks,
                    Chanel = LightChanel.Sun,
                    Column = request.Column,
                    BufferHeight = chunkBufferingSystemData.ValueRO.Height,
                    ClasterHeight = clasterHeight,
                    Claster = claster,

                    AddQueues = systemData.AddQueues,
                    RemoveQueues = systemData.RemoveQueues,
                };

                job.Schedule().Complete();
                job.Claster.Dispose();

                commandBuffer.DestroyEntity(entity);

                for (int y = 0; y < chunkBufferingSystemData.ValueRO.Height; y++) {
                    var chunkCoordinate = new int3(job.Column.x, y, job.Column.y);
                    ChunkBufferingSystem.GetEntity(chunkBufferingSystemData.ValueRO, chunkCoordinate, out var chunkEntity);
                    if (!state.EntityManager.Exists(chunkEntity)
                        || !state.EntityManager.HasComponent<Chunk>(chunkEntity)
                        || state.EntityManager.HasComponent<RawChunk>(chunkEntity)) {
                        break;
                    }

                    commandBuffer.AddComponent<Sunlight>(chunkEntity);
                    commandBuffer.AddComponent<IncompleteLighting>(chunkEntity);
                }
            }

            commandBuffer.Playback(state.EntityManager);
            commandBuffer.Dispose();

            commandBuffer = new EntityCommandBuffer(Allocator.Temp);

            chunkBufferingSystemData = SystemAPI.GetSingletonRW<ChunkBufferingSystemData>();

            foreach (var (chunk, entity) in SystemAPI
                .Query<RefRO<Chunk>>()
                .WithAll<Sunlight>()
                .WithAll<IncompleteLighting>()
                .WithNone<DirtyChunk>()
                .WithEntityAccess()) {

                var chunkCoordinate = chunk.ValueRO.Coordinate;
                var origin = chunkCoordinate - new int3(1, 1, 1);

                var isValidClaster = true;
                for (int j = 0; j < 3 * 3 * 3; j++) {
                    var coordinate = origin + IndexUtility.ToCoordinate(j, 3, 3);
                    ChunkBufferingSystem.GetEntity(chunkBufferingSystemData.ValueRO, coordinate, out var sideChunk);
                    bool isValidChunk = state.EntityManager.Exists(sideChunk)
                        && state.EntityManager.HasComponent<Chunk>(sideChunk)
                        && !state.EntityManager.HasComponent<RawChunk>(sideChunk)
                        && state.EntityManager.HasComponent<Sunlight>(sideChunk);

                    if (coordinate.y != -1 && coordinate.y != chunkBufferingSystemData.ValueRO.Height && !isValidChunk) {
                        isValidClaster = false;
                        break;
                    }
                }

                if (isValidClaster) {
                    commandBuffer.AddComponent<DirtyChunk>(entity);
                    commandBuffer.RemoveComponent<IncompleteLighting>(entity);
                }
            }

            commandBuffer.Playback(state.EntityManager);
            commandBuffer.Dispose();
        }
    }
}