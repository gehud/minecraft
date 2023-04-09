using UnityEngine;

namespace Minecraft {
    public class BlockProvider : MonoBehaviour {
		public Block[] Data => data;

        [SerializeField]
        private Block[] data;

        public Block Get(BlockType blockType) {
            return data[(int)blockType];
        }
    }
}