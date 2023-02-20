using UnityEngine;

namespace Minecraft {
    public class Chunk {
        /// <summary>
        /// Chunk size in blocks.
        /// </summary>
		public const int SIZE = 16;
		public const int VOLUME = SIZE * SIZE * SIZE;

		public Vector3Int Coordinate { get; set; } = Vector3Int.zero;

        public BlockMap BlockMap { get; set; } = new();

        public LiquidMap LiquidMap { get; set; } = new();

        public LightMap LightMap { get; set; } = new();

        public TreeData TreeData { get; set; } = new();

        public bool IsDirty { get; set; } = true;

        public bool IsComplete { get; set; } = false;

        public void MarkDirty() => IsDirty = true;

        public void MarkComplete() => IsComplete = true;
    }
}