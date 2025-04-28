using Minecraft.Player;
using TMPro;
using Unity.Collections;
using Unity.Entities;
using Unity.Transforms;
using UnityEngine;

namespace Minecraft
{
    [RequireComponent(typeof(TextMeshProUGUI))]
    public class PositionText : MonoBehaviour
    {
        private TextMeshProUGUI text;
        private World world;

        private void Awake()
        {
            text = GetComponent<TextMeshProUGUI>();
            world = World.DefaultGameObjectInjectionWorld;
        }

        private void Update()
        {
            var entityManager = world.EntityManager;
            var querry = new EntityQueryBuilder(Allocator.Temp)
                .WithAll<PlayerMovement, LocalToWorld>()
                .Build(entityManager);

            if (querry.TryGetSingletonEntity<PlayerMovement>(out var entity))
            {
                var localToWorld = entityManager.GetComponentData<LocalToWorld>(entity);

                var position = localToWorld.Position;

                text.text = $"Position: ({position.x:n2}, {position.y:n2}, {position.z:n2})";
            }

            querry.Dispose();
        }
    }
}
