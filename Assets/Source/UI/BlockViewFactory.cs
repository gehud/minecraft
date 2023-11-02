using UnityEngine;

namespace Minecraft.UI {
    public class BlockViewFactory : MonoBehaviour {
        [SerializeField]
        private BlockView prefab;

        public BlockView Create(BlockType blockType) {
            var view = Instantiate(prefab);
            view.BlockType = blockType;
            return view;
        }
    }
}