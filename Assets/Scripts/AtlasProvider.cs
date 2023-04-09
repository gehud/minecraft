using UnityEngine;

namespace Minecraft {
    [ExecuteAlways]
    public class AtlasProvider : MonoBehaviour {
        public float TileStep => 16.0f / 256.0f;

        [SerializeField]
        private Texture2D atlas;
        [SerializeField]
        private Texture2D liquidAtlas;
        [SerializeField, Min(1)]
        private int mipCount = 4;

        public Texture2D GetAtlas(MaterialType materialType) {
            Texture2D atlas = null;

            if (materialType == MaterialType.Opaque || materialType == MaterialType.Transparent) {
                atlas = new Texture2D(this.atlas.width, this.atlas.height, this.atlas.format, mipCount, false) {
                    filterMode = FilterMode.Point
                };
                atlas.SetPixels32(this.atlas.GetPixels32());
            } else if (materialType == MaterialType.Liquid) {
				atlas = new Texture2D(liquidAtlas.width, liquidAtlas.height, liquidAtlas.format, mipCount, false) {
					filterMode = FilterMode.Point
				};
                atlas.SetPixels32(liquidAtlas.GetPixels32());
			}

            if (atlas != null) {
                atlas.Apply();
                return atlas;
            }

            return Texture2D.whiteTexture;
        }
    }
}
