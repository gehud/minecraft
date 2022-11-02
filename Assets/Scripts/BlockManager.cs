using System;
using System.Collections.Generic;
using UnityEngine;

namespace Minecraft {
    public class BlockManager : Singleton<BlockManager> {
        public IReadOnlyDictionary<BlockType, Block> Blocks => blocks;
        private readonly Dictionary<BlockType, Block> blocks = new();

        [Serializable]
        private struct BlockDataPair {
            public BlockType Type;
            public Block Data;
        }

        [SerializeField]
        private List<BlockDataPair> blockPairs = new();

        private void Awake() {
            foreach (var item in blockPairs)
                blocks.Add(item.Type, item.Data);
        }
    }
}