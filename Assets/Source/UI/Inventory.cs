using System;
using UnityEngine;

namespace Minecraft.UI {
    public class Inventory : MonoBehaviour {
        public static readonly Vector2Int GridSize = new(9, 4);

        [SerializeField]
        private BlockViewFactory blockViewFactory;
        [SerializeField]
        private RectTransform primarySlotContainer;
        [SerializeField]
        private RectTransform hotbarSlotContainer;

        private Slot[] primarySlots;
        private Slot[] hotbarSlots;

        public Slot GetSlot(Vector2Int coordinate) {
            if (IsOutOfBounds(coordinate)) {
                throw new ArgumentOutOfRangeException(nameof(coordinate));
            }

            if (coordinate.y == 0) {
                return hotbarSlots[coordinate.x];
            }

            return primarySlots[coordinate.x + (coordinate.y - 1) * GridSize.x];
        }

        public Vector2Int GetCoordinate(Slot slot) {
            for (int x = 0; x < GridSize.x; x++) {
                for (int y = 0; y < GridSize.y; y++) {
                    var coordinate = new Vector2Int(x, y);
                    if (GetSlot(coordinate) == slot) {
                        return coordinate;
                    }
                }
            }

            return new Vector2Int(-1, -1);
        }

        private bool IsOutOfBounds(Vector2Int coordinate) {
            return coordinate.x < 0
                || coordinate.y < 0
                || coordinate.x >= GridSize.x
                || coordinate.y >= GridSize.y;
        }

        private void PlaceItem(SlotItem item, Vector2Int coordinate) {
            item.Slot = GetSlot(coordinate);
        }

        private void Awake() {
            primarySlots = new Slot[primarySlotContainer.childCount];
            for (int i = 0; i < primarySlotContainer.childCount; i++) {
                primarySlots[i] = primarySlotContainer.GetChild(i).GetComponent<Slot>();
            }

            hotbarSlots = new Slot[hotbarSlotContainer.childCount];
            for (int i = 0; i < hotbarSlotContainer.childCount; i++) {
                hotbarSlots[i] = hotbarSlotContainer.GetChild(i).GetComponent<Slot>();
            }

            PlaceItem(blockViewFactory.Create(BlockType.Stone), new Vector2Int(0, 0));
            PlaceItem(blockViewFactory.Create(BlockType.Dirt), new Vector2Int(1, 0));
            PlaceItem(blockViewFactory.Create(BlockType.Grass), new Vector2Int(2, 0));
            PlaceItem(blockViewFactory.Create(BlockType.Glass), new Vector2Int(3, 0));
            PlaceItem(blockViewFactory.Create(BlockType.JackOLantern), new Vector2Int(4, 0));

            gameObject.SetActive(false);
        }
    }
}