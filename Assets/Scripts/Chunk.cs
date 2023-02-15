using System.Collections.Concurrent;
using UnityEngine;

namespace Minecraft {
    [RequireComponent(typeof(ChunkRenderer))]
    public class Chunk : MonoBehaviour {
        public const int SIZE = 16;
        public const int VOLUME = SIZE * SIZE * SIZE;

        public ChunkData Data { get; set; }

        [SerializeField]
        private new ChunkRenderer renderer;

        public void Initialize(ChunkData data) {
            Data = data;
            transform.position = data.Coordinate * SIZE;
        }

        public void UpdateMesh(ConcurrentDictionary<MaterialType, MeshData> meshDatas, MaterialManager materialManager) {
            renderer.UpdateMesh(meshDatas, materialManager);
            Data.IsDirty = false;
            Data.IsComplete = true;
        }
    }
}