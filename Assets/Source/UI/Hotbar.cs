using UnityEngine;
using UnityEngine.InputSystem;

namespace Minecraft.UI {
    [RequireComponent(typeof(RectTransform))]
    public class Hotbar : MonoBehaviour {
        public static SlotItem Selected => selected;
        private static SlotItem selected;

        [SerializeField]
        private Inventory inventory;
        [SerializeField]
        private RectTransform cursor;
        [SerializeField]
        private RectTransform slotContainer;

        private const int slotSize = 96;
        private const int slotCount = 9;

        private int position;

        private void UpdateSelected() {
            var slot = slotContainer.GetChild(position);

            if (slot.childCount == 0) {
                selected = null;
                return;
            }

            var item = slot.GetChild(0).GetComponent<HotbarItem>();
            selected = item.SlotItem;
        }

        private void OnItemDrop(SlotItem item) {
            var coordinate = inventory.GetCoordinate(item.Slot);
            if (coordinate.y != 0) {
                return;
            }

            var slot = slotContainer.GetChild(coordinate.x);
            var clone = Instantiate(item);
            Destroy(clone.GetComponent<SlotItem>());
            var hotbarItem = clone.gameObject.AddComponent<HotbarItem>();
            hotbarItem.SlotItem = item;
            if (slot.childCount != 0) {
                Destroy(slot.GetChild(0).gameObject);
            }

            clone.transform.SetParent(slot, false);
            clone.transform.localPosition = Vector3.zero;

            UpdateSelected();
        }

        private void OnItemDrag(SlotItem item) {
            var coordinate = inventory.GetCoordinate(item.Slot);
            if (coordinate.y != 0) {
                return;
            }

            var slot = slotContainer.GetChild(coordinate.x);
            if (slot.childCount != 0) {
                Destroy(slot.GetChild(0).gameObject);
            }

            UpdateSelected();
        }

        private void MoveCursor(int position) {
            this.position = position;
            cursor.anchoredPosition = position * slotSize * Vector2.right;
            UpdateSelected();
        }

        private bool HandleKeyboardWalk() {
            var keyboard = Keyboard.current;
            if (keyboard == null) {
                return false;
            }

            if (keyboard[Key.Digit1].wasPressedThisFrame) {
                MoveCursor(0);
                return true;
            } 

            if (keyboard[Key.Digit2].wasPressedThisFrame) {
                MoveCursor(1);
                return true;
            }

            if (keyboard[Key.Digit3].wasPressedThisFrame) {
                MoveCursor(2);
                return true;
            }
            
            if (keyboard[Key.Digit4].wasPressedThisFrame) {
                MoveCursor(3);
                return true;
            } 
            
            if (keyboard[Key.Digit5].wasPressedThisFrame) {
                MoveCursor(4);
                return true;
            } 
            
            if (keyboard[Key.Digit6].wasPressedThisFrame) {
                MoveCursor(5);
                return true;
            } 
            
            if (keyboard[Key.Digit7].wasPressedThisFrame) {
                MoveCursor(6);
                return true;
            } 
            
            if (keyboard[Key.Digit8].wasPressedThisFrame) {
                MoveCursor(7);
                return true;
            } 
            
            if (keyboard[Key.Digit9].wasPressedThisFrame) {
                MoveCursor(8);
                return true;
            }

            return false;
        }

        private bool HandleMouseWalk() {
            var mouse = Mouse.current;
            if (mouse == null) {
                return false;
            }

            var newPosition = position;
            var scroll = mouse.scroll.value.y;
            if (scroll > 0.0f) {
                --newPosition;
                if (newPosition < 0) {
                    newPosition = slotCount - 1;
                }

                MoveCursor(newPosition);
                return true;
            } else if (scroll < 0.0f) {
                ++newPosition;
                if (newPosition >= slotCount) {
                    newPosition = 0;
                }

                MoveCursor(newPosition);
                return true;
            }

            return false;
        }

        private void Awake() {
            MoveCursor(0);
        }

        private void OnEnable() {
            SlotItem.OnDrop += OnItemDrop;
            SlotItem.OnDrag += OnItemDrag;
        }

        private void OnDisable() {
            SlotItem.OnDrop -= OnItemDrop;
            SlotItem.OnDrag -= OnItemDrag;
        }

        private void Update() {
            if (HandleKeyboardWalk()) {
                return;
            }

            if (HandleMouseWalk()) {
                return;
            }
        }

        private void OnDestroy() {
            selected = null;
        }
    }
}