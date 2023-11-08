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
        [SerializeField, Min(0)]
        private int HeightOffset = 32;
        [SerializeField]
        private NoiseSettings continentalness;
        [SerializeField]
        private NoiseSettings erosion;
        [SerializeField]
        private NoiseSettings peaksAndValleys;

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

            var chunkSystem = world.GetExistingSystem<ChunkMeshSystem>();
            world.EntityManager.AddComponentObject(chunkSystem, new ChunkMeshSystemData {
                Material = material
            });

            var chunkGenerationSystem = world.GetExistingSystem<ChunkGenerationSystem>();
            world.EntityManager.AddComponentData(chunkGenerationSystem, new ChunkGenerationSystemData {
                HeightOffset = HeightOffset,
                Continentalness = new Noise(continentalness, Allocator.Persistent),
                Erosion = new Noise(erosion, Allocator.Persistent),
                PeaksAndValleys = new Noise(peaksAndValleys, Allocator.Persistent)
            });
        }
    }
}