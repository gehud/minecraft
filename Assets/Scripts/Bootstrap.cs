using Unity.NetCode;
using UnityEngine.Scripting;

namespace Minecraft {
    [Preserve]
    public class Bootstrap : ClientServerBootstrap {
        public override bool Initialize(string defaultWorldName) {
            CreateLocalWorld(defaultWorldName);
            return true;
        }
    }
}