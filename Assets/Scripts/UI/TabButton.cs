using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

namespace TrainingUI
{
    public class TabButton : MonoBehaviour, IPointerClickHandler
    {
        [Header("Tab Ayarları")]
        public int tabIndex;
        public TabManager tabManager;

        [Header("Button Referansları")]
        public Image backgroundImage;
        public TextMeshProUGUI textComponent;
        public GameObject activeLine;

        [Header("Renk Ayarları")]
        public Color normalColor = new Color(0.039f, 0.086f, 0.157f, 0.6f);
        public Color highlightedColor = new Color(0f, 0.898f, 1f, 0.22f);
        public Color activeColor = new Color(0f, 0.898f, 1f, 0.12f);
        public Color pressedColor = new Color(0f, 0.898f, 1f, 0.32f);

        private bool isActive = false;

        void Start()
        {
            if (tabManager == null)
            {
                // Sahnedeki TabManager'ı bul
                tabManager = FindObjectOfType<TabManager>();
            }

            // Başlangıçta durumu ayarla
            UpdateVisuals(false);
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (tabManager != null)
            {
                tabManager.SelectTab(tabIndex);
            }
        }

        public void SetActive(bool active)
        {
            isActive = active;
            UpdateVisuals(active);
        }

        void UpdateVisuals(bool active)
        {
            if (backgroundImage != null)
            {
                backgroundImage.color = active ? activeColor : normalColor;
            }

            if (textComponent != null)
            {
                textComponent.color = active ? Color.white : new Color(0.7f, 0.7f, 0.7f, 1f);
            }

            if (activeLine != null)
            {
                activeLine.SetActive(active);
            }
        }

        public bool IsActive()
        {
            return isActive;
        }
    }
}
