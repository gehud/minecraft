using Minecraft.Utilities;
using TMPro;
using UnityEditor;
using UnityEngine;
using Zenject;

namespace Minecraft.UI {
	public class DebugDisplay : MonoBehaviour {
		[SerializeField]
		private Transform player;
		[SerializeField]
		private new Camera camera;

		[SerializeField]
		private TMP_Text positionText;
		[SerializeField]
		private TMP_Text cursorText;
		[SerializeField]
		private TMP_Text blockText;
		[SerializeField]
		private TMP_Text normalBlockText;

		[Inject]
		private World World { get; }

		private void Start() {
			gameObject.SetActive(false);
		}

		private void Update() {
			positionText.text = player.position.ToString();

			var ray = camera.ScreenPointToRay(Input.mousePosition);
			if (Physics.Raycast(ray, out RaycastHit hitInfo)) {
				var blockCoordinate = CoordinateUtility.ToCoordinate(hitInfo.point);
				if (hitInfo.normal.x > 0)
					blockCoordinate.x--;
				if (hitInfo.normal.y > 0)
					blockCoordinate.y--;
				if (hitInfo.normal.z > 0)
					blockCoordinate.z--;
				blockText.text = ObjectNames.NicifyVariableName(World.GetBlock(blockCoordinate).ToString());
				var normalBlockCoordinate = blockCoordinate + Vector3Int.FloorToInt(hitInfo.normal);
				cursorText.text = normalBlockCoordinate.ToString();
				normalBlockText.text = ObjectNames.NicifyVariableName(World.GetBlock(normalBlockCoordinate).ToString());
			} else {
				cursorText.text = Vector3.zero.ToString();
				blockText.text = ObjectNames.NicifyVariableName(BlockType.Air.ToString());
				normalBlockText.text = ObjectNames.NicifyVariableName(BlockType.Air.ToString());
			}
		}
	}
}