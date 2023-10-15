using System;
using Unity.Mathematics;
using UnityEngine;

namespace Minecraft {
    [Serializable]
    public struct Texturing {
        public int2 Right;
        public int2 Left;
        public int2 Top;
        public int2 Bottom;
        public int2 Front;
        public int2 Back;
    }

    [CreateAssetMenu]
    public class Block : ScriptableObject {
        public Texturing Texturing => texturing;

        public bool IsSolid => isSolid;

        [SerializeField]
        private Texturing texturing;
        [SerializeField]
        private bool isSolid = true;
    }
}