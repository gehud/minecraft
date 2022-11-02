using TMPro;
using UnityEngine;

namespace Minecraft.Player {
    [RequireComponent(typeof(Camera))]
    public class CameraController : MonoBehaviour {
        [SerializeField]
        private new Camera camera;
        [SerializeField]
        private Transform body;
        [SerializeField, Min(0)]
        private float sencitivity = 5;
        [SerializeField]
        private LayerMask raycastMask = ~0;
        [SerializeField]
        private TMP_Text voxelTypeText;

        private float roatationX = 0;

        private BlockType currentVoxel = BlockType.Stone;

        private void Start() {
            Cursor.lockState = CursorLockMode.Locked;
            voxelTypeText.text = currentVoxel.ToString();
        }

        private void Update() {
            float mouseX = Input.GetAxis("Mouse X");
            float mouseY = Input.GetAxis("Mouse Y");

            roatationX = Mathf.Clamp(roatationX - mouseY * sencitivity, -90, 90);
            transform.localEulerAngles = new Vector3(roatationX, 0, 0);
            body.Rotate(Vector3.up, mouseX * sencitivity);

            if (Input.GetMouseButtonDown(0)) {
                if (Physics.Raycast(camera.ScreenPointToRay(Input.mousePosition), out RaycastHit hitInfo, 5, raycastMask)) {
                    Vector3Int globalVoxelCooridnate = Vector3Int.FloorToInt(hitInfo.point);
                    if (hitInfo.normal.x > 0)
                        globalVoxelCooridnate.x--;
                    if (hitInfo.normal.y > 0)
                        globalVoxelCooridnate.y--;
                    if (hitInfo.normal.z > 0)
                        globalVoxelCooridnate.z--;
                    World.Instance.DestroyVoxel(globalVoxelCooridnate);
                }
            } else if (Input.GetMouseButtonDown(1)) {
                if (Physics.Raycast(camera.ScreenPointToRay(Input.mousePosition), out RaycastHit hitInfo, 5, raycastMask)) {
                    Vector3Int globalVoxelCooridnate = Vector3Int.FloorToInt(hitInfo.point);
                    if (hitInfo.normal.x < 0)
                        globalVoxelCooridnate.x--;
                    if (hitInfo.normal.y < 0)
                        globalVoxelCooridnate.y--;
                    if (hitInfo.normal.z < 0)
                        globalVoxelCooridnate.z--;
                    bool overlapPlayer = globalVoxelCooridnate == Vector3Int.FloorToInt(transform.parent.position)
                        || globalVoxelCooridnate == Vector3Int.FloorToInt(transform.parent.position + Vector3.up);
                    if (!overlapPlayer)
                        World.Instance.PlaceVoxel(globalVoxelCooridnate, currentVoxel);
                }
            }
            if (Input.GetKeyDown(KeyCode.Alpha1)) {
                currentVoxel = BlockType.Stone;
                voxelTypeText.text = currentVoxel.ToString();
            } else if (Input.GetKeyDown(KeyCode.Alpha2)) {
                currentVoxel = BlockType.Water;
                voxelTypeText.text = currentVoxel.ToString();
            }
        }
    }
}