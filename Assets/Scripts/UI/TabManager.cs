using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.Events;

namespace TrainingUI
{
    public class TabManager : MonoBehaviour
    {
        [System.Serializable]
        public class TabData
        {
            public string tabName;
            public GameObject tabButton;
            public GameObject contentPanel;
            public GameObject activeLine;
            public UnityEvent onTabSelected;
        }

        [Header("Tab Ayarları")]
        public List<TabData> tabs = new List<TabData>();

        [Header("Genel Ayarlar")]
        public Color activeColor = new Color(0f, 0.898f, 1f, 1f);
        public Color inactiveColor = new Color(0.039f, 0.086f, 0.157f, 0.6f);
        public Color activeTextColor = Color.white;
        public Color inactiveTextColor = new Color(0.7f, 0.7f, 0.7f, 1f);

        private int currentActiveTabIndex = -1;

        void Start()
        {
            // İlk tab'ı aktif et
            if (tabs.Count > 0)
            {
                SelectTab(0);
            }
        }

        public void SelectTab(int index)
        {
            if (index < 0 || index >= tabs.Count)
                return;

            // TÜM tab'ları deaktif et (sadece öncekini değil, hepsini)
            for (int i = 0; i < tabs.Count; i++)
            {
                DeactivateTab(i);
            }

            // Yeni tab'ı aktif et
            currentActiveTabIndex = index;
            ActivateTab(index);

            // Event'i tetikle
            if (tabs[index].onTabSelected != null)
            {
                tabs[index].onTabSelected.Invoke();
            }
        }

        void ActivateTab(int index)
        {
            TabData tab = tabs[index];

            // ActiveLine göster
            if (tab.activeLine != null)
            {
                tab.activeLine.SetActive(true);
            }

            // Tab button rengini değiştir
            Image buttonImage = tab.tabButton.GetComponent<Image>();
            if (buttonImage != null)
            {
                buttonImage.color = activeColor;
            }

            // Text rengini değiştir
            TextMeshProUGUI textComponent = tab.tabButton.GetComponentInChildren<TextMeshProUGUI>();
            if (textComponent != null)
            {
                textComponent.color = activeTextColor;
            }

            // İçerik panelini göster
            if (tab.contentPanel != null)
            {
                tab.contentPanel.SetActive(true);
            }
        }

        void DeactivateTab(int index)
        {
            TabData tab = tabs[index];

            // ActiveLine gizle
            if (tab.activeLine != null)
            {
                tab.activeLine.SetActive(false);
            }

            // Tab button rengini değiştir
            Image buttonImage = tab.tabButton.GetComponent<Image>();
            if (buttonImage != null)
            {
                buttonImage.color = inactiveColor;
            }

            // Text rengini değiştir
            TextMeshProUGUI textComponent = tab.tabButton.GetComponentInChildren<TextMeshProUGUI>();
            if (textComponent != null)
            {
                textComponent.color = inactiveTextColor;
            }

            // İçerik panelini gizle
            if (tab.contentPanel != null)
            {
                tab.contentPanel.SetActive(false);
            }
        }

        public int GetCurrentActiveTabIndex()
        {
            return currentActiveTabIndex;
        }

        public TabData GetCurrentTab()
        {
            if (currentActiveTabIndex >= 0 && currentActiveTabIndex < tabs.Count)
                return tabs[currentActiveTabIndex];
            return null;
        }
    }
}
