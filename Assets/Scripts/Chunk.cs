using System.Collections.Generic;
using UnityEngine;

namespace Minecraft {
    [RequireComponent(typeof(ChunkRenderer))]
    [RequireComponent(typeof(ChunkCollider))]
    public class Chunk : MonoBehaviour {
        public const int SIZE = 16;
        public const int VOLUME = SIZE * SIZE * SIZE;

        public ChunkData Data { get; set; }

        [SerializeField]
        private new ChunkRenderer renderer;
        [SerializeField]
        private new ChunkCollider collider;

        public void Initialize(ChunkData data) {
            Data = data;
            transform.position = data.Coordinate * SIZE;
        }

        public void UpdateMesh(IDictionary<MaterialType, MeshData> meshDatas, MaterialManager materialManager) {
            renderer.UpdateMesh(meshDatas, materialManager);
            collider.UpdateMesh(meshDatas);
            Data.IsDirty = false;
            Data.IsComplete = true;
        }
    }
}