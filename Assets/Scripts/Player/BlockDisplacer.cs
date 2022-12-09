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
        private LayerMask layerMask = ~0;
        [SerializeField]
        private TMP_Text voxelTypeText;

        private BlockType currentBlock = BlockType.Stone;

        [Inject]
        private World World { get; }

        private void Awake() {
            voxelTypeText.text = currentBlock.ToString();
        }

        private void Update() {
            Camera camera = Camera.main;
            if (Input.GetMouseButtonDown(0)) {
                if (Physics.Raycast(camera.ScreenPointToRay(Input.mousePosition), out RaycastHit hitInfo, placementDistance, layerMask)) {
                    Vector3Int globalVoxelCooridnate = Vector3Int.FloorToInt(hitInfo.point);
                    if (hitInfo.normal.x > 0)
                        globalVoxelCooridnate.x--;
                    if (hitInfo.normal.y > 0)
                        globalVoxelCooridnate.y--;
                    if (hitInfo.normal.z > 0)
                        globalVoxelCooridnate.z--;
                    World.DestroyVoxel(globalVoxelCooridnate);
                }
            } else if (Input.GetMouseButtonDown(1)) {
                if (Physics.Raycast(camera.ScreenPointToRay(Input.mousePosition), out RaycastHit hitInfo, placementDistance, layerMask)) {
                    Vector3Int globalVoxelCooridnate = Vector3Int.FloorToInt(hitInfo.point);
                    if (hitInfo.normal.x < 0)
                        globalVoxelCooridnate.x--;
                    if (hitInfo.normal.y < 0)
                        globalVoxelCooridnate.y--;
                    if (hitInfo.normal.z < 0)
                        globalVoxelCooridnate.z--;
                    bool overlapPlayer = globalVoxelCooridnate == Vector3Int.FloorToInt(player.position)
                        || globalVoxelCooridnate == Vector3Int.FloorToInt(player.position + Vector3.up);
                    if (!overlapPlayer)
                        World.PlaceVoxel(globalVoxelCooridnate, currentBlock);
                }
            } else if (Input.GetMouseButtonDown(2)) {
				if (Physics.Raycast(camera.ScreenPointToRay(Input.mousePosition), out RaycastHit hitInfo, placementDistance, layerMask)) {
					Vector3Int globalVoxelCooridnate = Vector3Int.FloorToInt(hitInfo.point);
					if (hitInfo.normal.x < 0)
						globalVoxelCooridnate.x--;
					if (hitInfo.normal.y < 0)
						globalVoxelCooridnate.y--;
					if (hitInfo.normal.z < 0)
						globalVoxelCooridnate.z--;
					Debug.Log(World.GetLiquidAmount(globalVoxelCooridnate, BlockType.Water));
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
