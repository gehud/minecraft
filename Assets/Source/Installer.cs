using Minecraft.Components;
using Minecraft.Systems;
using System;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;

namespace Minecraft {
    public class Installer : MonoBehaviour {
        [SerializeField]
        private BlockDatabase database;
        [SerializeField]
        private Material material;

        private void Awake() {
            var blockCount = Enum.GetValues(typeof(BlockType)).Length;
            var data = new NativeArray<Block>(blockCount, Allocator.Persistent);
            foreach (var item in database.Data) {
                data[(int)item.Key] = new Block {
                    Texturing = item.Value.Texturing,
                    IsSolid = item.Value.IsSolid,
                    Absorption = item.Value.Absorption,
                    IsTransparent = item.Value.IsTransparent,
                    Emission = item.Value.Emission,
                };
            }

            var world = World.DefaultGameObjectInjectionWorld;

            var blockSystem = world.GetExistingSystem<BlockSystem>();
            world.EntityManager.AddComponentData(blockSystem, new BlockSystemData {
                Blocks = data,
            });

            var chunkSystem = world.GetExistingSystem<ChunkSystem>();
            world.EntityManager.AddComponentObject(chunkSystem, new ChunkSystemData {
                TerrainMaterial = material
            });
        }
    }
}