using System;
using Unity.Collections;
using UnityEngine;

namespace Minecraft {
    public class StaticBlockDatabase : MonoBehaviour {
        public static NativeArray<BlockDescription> Data => data;

        private static NativeArray<BlockDescription> data;

        public static Material TerrainMaterial => terrainMaterial;

        private static Material terrainMaterial;

        [SerializeField]
        private BlockDatabase database;
        [SerializeField]
        private Material material;

        private void Awake() {
            terrainMaterial = material;

            var blockCount = Enum.GetValues(typeof(BlockType)).Length;
            data = new NativeArray<BlockDescription>(blockCount, Allocator.Persistent);
            foreach (var item in database.Data) {
                data[(int)item.Key] = new BlockDescription {
                    Texturing = item.Value.Texturing
                };
            }
        }

        private void OnDestroy() {
            data.Dispose();
        }
    }
}