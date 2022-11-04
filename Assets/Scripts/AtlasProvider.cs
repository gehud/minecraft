using UnityEngine;

namespace Minecraft {
    public class AtlasProvider : Singleton<AtlasProvider>, IAtlasProvider {
        public Texture2D Atlas {
            get {
                var atlas = new Texture2D(this.atlas.width, this.atlas.height, this.atlas.format, mipCount, false) {
                    filterMode = FilterMode.Point
                };

                atlas.SetPixels32(this.atlas.GetPixels32());
                atlas.Apply();

                return atlas;
            }
        }

        [SerializeField]
        private Texture2D atlas;
        [SerializeField, Min(1)]
        private int mipCount = 4;
    }
}
