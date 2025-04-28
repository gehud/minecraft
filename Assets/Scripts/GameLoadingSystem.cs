using Minecraft.Lighting;
using Unity.Entities;
using Unity.Mathematics;

namespace Minecraft
{
    public partial class GameLoadingSystem : SystemBase
    {
        public delegate void GameLoadedHandler();
        public static event GameLoadedHandler GameLoaded;

        public delegate void GameLoadingHandler(float progress);
        public static event GameLoadingHandler GameLoading;

        private const int loadedAreaExpected = 7 * 7;

        private bool isLoaded = false;

        protected override void OnUpdate()
        {
            if (isLoaded)
            {
                return;
            }

            if (!SystemAPI.HasSingleton<InitialLoadingColumns>())
            {
                return;
            }

            var initialLoadingColumns = SystemAPI.GetSingleton<InitialLoadingColumns>().Columns;

            var chunkBufferingSystemData = SystemAPI.GetSingletonRW<ChunkBufferingSystemData>();

            var readyColumns = 0;
            for (int i = initialLoadingColumns.Length - 1; i >= 0; i--)
            {
                var column = initialLoadingColumns[i];

                var fail = false;

                for (int y = 0; y < chunkBufferingSystemData.ValueRO.Height; y++)
                {
                    var coordinate = new int3(column.x, y, column.y);
                    ChunkBufferingSystem.GetEntity
                    (
                        chunkBufferingSystemData.ValueRO,
                        coordinate,
                        out var chunkEntity
                    );

                    var isReadyChunk = EntityManager.HasComponent<Sunlight>(chunkEntity)
                        && !EntityManager.IsComponentEnabled<DirtyChunk>(chunkEntity)
                        && !EntityManager.HasComponent<RawChunk>(chunkEntity)
                        && !EntityManager.HasComponent<IncompleteLighting>(chunkEntity);

                    if (!isReadyChunk)
                    {
                        fail = true;
                        break;
                    }
                }

                if (fail)
                {
                    break;
                }
                else
                {
                    ++readyColumns;
                    GameLoading?.Invoke(readyColumns / (float)loadedAreaExpected);
                }
            }

            if (readyColumns == loadedAreaExpected || readyColumns == initialLoadingColumns.Length)
            {
                GameLoading?.Invoke(1f);
                GameLoaded?.Invoke();

                var entity = EntityManager.CreateEntity();
                EntityManager.AddComponent<GameLoadingMarker>(entity);

                isLoaded = true;
            }
        }
    }
}