using TMPro;
using UnityEngine;

namespace Minecraft.UI
{
    [RequireComponent(typeof(TextMeshProUGUI))]
    public class FPSText : MonoBehaviour
    {
        [SerializeField, Min(0.0f)]
        private float updateTime = 0.2f;

        private TextMeshProUGUI text;

        private float lastUpdateTime = 0.0f;

        private void Awake()
        {
            text = GetComponent<TextMeshProUGUI>();
        }

        private void Update()
        {
            var time = Time.time;
            if (time - lastUpdateTime < updateTime)
            {
                return;
            }

            lastUpdateTime = time;

            var fps = (int)(1.0f / Time.unscaledDeltaTime);
            text.text = $"FPS: {fps}";
        }
    }
}