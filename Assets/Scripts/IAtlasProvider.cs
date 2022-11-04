using UnityEngine;

namespace Minecraft {
    public interface IAtlasProvider {
        Texture2D GetAtlas(MaterialType materialType);
    }
}