using TMPro;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.UI;

namespace Minecraft.UI {
    public class BlockView : SlotItem {
        public BlockType BlockType {
            get => blockType;
            set {
                blockType = value;
                var description = blockDatabase.Data[value];
                var size = 16.0f / 256.0f;
                var texturing = description.Texturing;
                top.uvRect = new Rect((float2)texturing.Top * size, Vector2.one * size);
                front.uvRect = new Rect((float2)texturing.Front * size, Vector2.one * size);
                right.uvRect = new Rect((float2)texturing.Right * size, Vector2.one * size);
            }
        }

        private BlockType blockType;

        public int Count {
            get => count;
            set {
                count = value;
                countText.text = value.ToString();
                countText.gameObject.SetActive(count > 0);
            }
        }

        private int count;

        [SerializeField]
        private BlockDatabase blockDatabase;
        [SerializeField]
        private RawImage top;
        [SerializeField]
        private RawImage front;
        [SerializeField]
        private RawImage right;
        [SerializeField]
        private TMP_Text countText;

        private void Awake() {
            Count = 0;
        }
    }
}