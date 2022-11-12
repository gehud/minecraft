using System;
using System.Collections.Generic;
using UnityEngine;

namespace Minecraft {
    public class BlockDataManager : Singleton<BlockDataManager> {
        public IReadOnlyDictionary<BlockType, BlockData> Data => data;
        private readonly Dictionary<BlockType, BlockData> data = new();

        [Serializable]
        private struct BlockDataPair {
            public BlockType Type;
            public BlockData Data;
        }

        [SerializeField]
        private List<BlockDataPair> blockDataPairs = new();

        private void Awake() {
            foreach (var item in blockDataPairs)
                data.Add(item.Type, item.Data);
        }
    }
}