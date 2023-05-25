using UnityEngine;

namespace Minecraft {
    public class AtlasProvider : MonoBehaviour {
        [SerializeField]
        private Texture2D atlas;
        [SerializeField, Min(1)]
        private int mipCount = 4;
        [SerializeField]
        private Material sMaterial; 
        [SerializeField]
		private Material tMaterial;

		private Texture2D GetAtlas(Texture2D texture) {
            var atlas = new Texture2D(texture.width, texture.height, texture.format, mipCount, false) {
                filterMode = FilterMode.Point
            };
            atlas.SetPixels32(texture.GetPixels32());
            atlas.Apply();
            return atlas;
        }

		private void Awake() {
			var atlas = GetAtlas(this.atlas);
			sMaterial.SetTexture("_Atlas", atlas);
			tMaterial.SetTexture("_Atlas", atlas);
		}
	}
}
