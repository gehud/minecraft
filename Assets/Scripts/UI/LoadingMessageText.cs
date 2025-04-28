using TMPro;
using UnityEngine;

namespace Minecraft.UI
{
    public class LoadingMessageText : MonoBehaviour
    {
        [SerializeField]
        private TextMeshProUGUI text;
        [SerializeField, Min(0f)]
        private float animationTime = 1f;

        private float lastTime = 0f;
        private int dotCount = 3;

        private void Update()
        {
            if (Time.time - lastTime >= animationTime)
            {
                ++dotCount;

                if (dotCount > 3)
                {
                    dotCount = 0;
                }

                text.text = $"Loading{new string('.', dotCount)}";

                lastTime = Time.time;
            }
        }
    }
}