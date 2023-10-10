using System;
using System.Collections.Generic;
using UnityEngine;

namespace Minecraft {
    [CreateAssetMenu]
    public class BlockDatabase : ScriptableObject, ISerializationCallbackReceiver {
        public IReadOnlyDictionary<BlockType, Block> Data => data;

        private Dictionary<BlockType, Block> data = new();

        [Serializable]
        private struct Pair {
            public BlockType Key;
            public Block Value;
        }

        [SerializeField]
        private List<Pair> pairs = new();

        void ISerializationCallbackReceiver.OnBeforeSerialize() {
            pairs.Clear();
            foreach (var item in data) {
                pairs.Add(new() {
                    Key = item.Key,
                    Value = item.Value
                });
            }
        }

        void ISerializationCallbackReceiver.OnAfterDeserialize() {
            data.Clear();
            foreach (var item in pairs) {
                data.Add(item.Key, item.Value);
            }
        }
    }
}