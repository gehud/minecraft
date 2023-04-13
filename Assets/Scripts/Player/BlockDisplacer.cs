using Minecraft.Physics;
using Minecraft.UI;
using Unity.Netcode;
using UnityEngine;
using Zenject;

namespace Minecraft.Player {
	public class BlockDisplacer : NetworkBehaviour {
        public BlockType Current => currentBlock;

        [SerializeField]
        private Transform player;
        [SerializeField]
        private float placementDistance = 5.0f;

        private BlockType currentBlock = BlockType.Stone;

        [Inject]
        private readonly World world;

        [Inject]
        private readonly PhysicsWorld physicsSolver;

        [Inject]
        private readonly BlockProvider blockProvider;

        [Inject]
        private readonly SelectedBlockText selectedBlockText;

        [Inject]
        private readonly UIController uIController;

        [Inject]
        private readonly ISavePayload savePayload;

        private void Update() {
			if (savePayload.Role != ConnectionRoles.None && !IsOwner)
				return;

			if (uIController.IsUsing)
                return;

            Camera camera = Camera.main;
            if (Input.GetMouseButtonDown(0)) {
                if (physicsSolver.Raycast(camera.ScreenPointToRay(Input.mousePosition), placementDistance, out RaycastHit hitInfo)) {
                    Vector3Int blockCoordinate = Vector3Int.FloorToInt(hitInfo.point);
                    Vector3Int normal = Vector3Int.RoundToInt(hitInfo.normal);
					if (blockProvider.Get(world.GetBlock(blockCoordinate + normal)).IsVegetation)
                        world.DestroyVoxel(blockCoordinate + normal);
                    else
                        world.DestroyVoxel(blockCoordinate);
                }
            } else if (Input.GetMouseButtonDown(1)) {
                if (physicsSolver.Raycast(camera.ScreenPointToRay(Input.mousePosition), placementDistance, out RaycastHit hitInfo)) {
                    Vector3Int blockCoordinate = Vector3Int.FloorToInt(hitInfo.point + hitInfo.normal);
					bool overlapPlayer = blockCoordinate == Vector3Int.FloorToInt(player.position)
                        || blockCoordinate == Vector3Int.FloorToInt(player.position + Vector3.up);
                    if (!overlapPlayer)
                        world.PlaceVoxel(blockCoordinate, currentBlock);
                }
            }

			if (Input.GetKeyDown(KeyCode.Alpha1)) {
                currentBlock = BlockType.Stone;
                selectedBlockText.Text.text = currentBlock.ToString();
			} else if (Input.GetKeyDown(KeyCode.Alpha2)) {
                currentBlock = BlockType.Water;
                selectedBlockText.Text.text = currentBlock.ToString();
            } else if (Input.GetKeyDown(KeyCode.Alpha3)) {
                currentBlock = BlockType.JackOLantern;
                selectedBlockText.Text.text = currentBlock.ToString();
            }
        }
    }
}
