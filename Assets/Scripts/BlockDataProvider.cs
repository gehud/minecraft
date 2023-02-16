using UnityEngine;

namespace Minecraft {
    public class BlockDataProvider : MonoBehaviour {
		public BlockData[] Data => data;

        [SerializeField]
        private BlockData[] data;

        public BlockData Get(BlockType blockType) {
            return data[(int)blockType];
        }
    }
}