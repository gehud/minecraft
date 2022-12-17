using UnityEngine;
using UnityEngine.UI;
using Zenject;

namespace Minecraft {
    public class Splash : MonoBehaviour {
        [SerializeField]
        private Image image;
        [SerializeField]
        private BlockData block;
        [SerializeField, Min(1)]
        private int tiling = 10;

        [Inject]
        private AtlasManager AtlasManager { get; }

        private void Start() {
            var atlas = AtlasManager.GetAtlas(MaterialType.Opaque);
            var step = Mathf.RoundToInt(atlas.width * AtlasManager.TileStep);
            var position = block.TexturingData.FrontFace * step;
            var texture = new Texture2D(step, step) {
                filterMode = FilterMode.Point,
            };
            texture.SetPixels(atlas.GetPixels(position.x, position.y, step, step));
            texture.Apply();
            var aspect = Screen.width / (float)Screen.height;
            image.material.mainTexture = texture;
            image.material.mainTextureScale = new Vector2(tiling * aspect, tiling);
            image.color = Color.white;
        }
    }
}