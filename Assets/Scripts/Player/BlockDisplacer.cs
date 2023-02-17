using Minecraft.Physics;
using TMPro;
using UnityEngine;
using Zenject;

namespace Minecraft.Player {
	public class BlockDisplacer : MonoBehaviour {
        [SerializeField]
        private Transform player;
        [SerializeField]
        private float placementDistance = 5.0f;
        [SerializeField]
        private TMP_Text voxelTypeText;

        private BlockType currentBlock = BlockType.Stone;

        [Inject]
        private World World { get; }

        [Inject]
        private PhysicsWorld PhysicsSolver { get; }

        private void Awake() {
            voxelTypeText.text = currentBlock.ToString();
        }

        private void Update() {
            Camera camera = Camera.main;
            if (Input.GetMouseButtonDown(0)) {
                if (PhysicsSolver.Raycast(camera.ScreenPointToRay(Input.mousePosition), placementDistance, out RaycastHit hitInfo)) {
                    Vector3Int blockCoordinate = Vector3Int.FloorToInt(hitInfo.point);
                    World.DestroyVoxel(blockCoordinate);
                }
            } else if (Input.GetMouseButtonDown(1)) {
                if (PhysicsSolver.Raycast(camera.ScreenPointToRay(Input.mousePosition), placementDistance, out RaycastHit hitInfo)) {
                    Vector3Int blockCoordinate = Vector3Int.FloorToInt(hitInfo.point + hitInfo.normal);
					bool overlapPlayer = blockCoordinate == Vector3Int.FloorToInt(player.position)
                        || blockCoordinate == Vector3Int.FloorToInt(player.position + Vector3.up);
                    if (!overlapPlayer)
                        World.PlaceVoxel(blockCoordinate, currentBlock);
                }
            }

			if (Input.GetKeyDown(KeyCode.Alpha1)) {
                currentBlock = BlockType.Stone;
                voxelTypeText.text = currentBlock.ToString();
            } else if (Input.GetKeyDown(KeyCode.Alpha2)) {
                currentBlock = BlockType.Water;
                voxelTypeText.text = currentBlock.ToString();
            } else if (Input.GetKeyDown(KeyCode.Alpha3)) {
                currentBlock = BlockType.JackOLantern;
                voxelTypeText.text = currentBlock.ToString();
            }
        }
    }
}
