using System;
using Unity.Collections;
using Unity.Entities;

namespace Minecraft.Lighting {
    public struct LightingSystemData : IComponentData, IDisposable {
        public NativeArray<NativeQueue<LightingEntry>> AddQueues;
        public NativeArray<NativeQueue<LightingEntry>> RemoveQueues;

        public void Dispose() {
            foreach (var queue in AddQueues) {
                queue.Dispose();
            }

            AddQueues.Dispose();

            foreach (var queue in RemoveQueues) {
                queue.Dispose();
            }

            RemoveQueues.Dispose();
        }
    }
}