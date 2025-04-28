using UnityEngine;
using UnityEngine.UI;

namespace Minecraft.UI
{
    public class LoadingProgressBar : MonoBehaviour
    {
        [SerializeField]
        private Slider slider;

        private void OnGameLoading(float progress)
        {
            slider.value = progress;
        }

        private void OnEnable()
        {
            GameLoadingSystem.GameLoading += OnGameLoading;    
        }

        private void OnDisable()
        {
            GameLoadingSystem.GameLoading -= OnGameLoading;
        }
    }
}