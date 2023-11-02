using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Minecraft.UI {
    public class SlotItem : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler {
        public static event Action<SlotItem> OnDrag;
        public static event Action<SlotItem> OnDrop;
        
        public Slot Slot {
            get => slot;
            set {
                slot = value;
                transform.SetParent(slot.transform, false);
                transform.localPosition = Vector3.zero;
                OnDrop?.Invoke(this);
            }
        }
        
        private Slot slot;
        private Slot startSlot;

        void IBeginDragHandler.OnBeginDrag(PointerEventData eventData) {
            var raycastResults = new List<RaycastResult>();
            EventSystem.current.RaycastAll(eventData, raycastResults);
            foreach (var raycastResult in raycastResults) {
                if (raycastResult.gameObject && raycastResult.gameObject.TryGetComponent(out Slot slot)) {
                    startSlot = slot;
                    break;
                }
            }

            OnDrag?.Invoke(this);
        }

        void IDragHandler.OnDrag(PointerEventData eventData) {
            transform.position += (Vector3)eventData.delta;
        }

        void IEndDragHandler.OnEndDrag(PointerEventData eventData) {
            var raycastResults = new List<RaycastResult>();
            EventSystem.current.RaycastAll(eventData, raycastResults);
            foreach (var raycastResult in raycastResults) {
                if (raycastResult.gameObject && raycastResult.gameObject.TryGetComponent(out Slot slot)) {
                    Slot = slot;
                    return;
                }
            }

            if (startSlot) {
                Slot = startSlot;
            }
        }
    }
}