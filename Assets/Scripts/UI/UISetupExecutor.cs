using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using TrainingUI;

/// <summary>
/// Modül 1 sahnesindeki tüm UI bileşenlerini tek seferde
/// konfigüre eden birleşik kurulum scripti.
/// ConfigureTabManager ve SetupTabListeners scriptlerinin
/// tüm işlevlerini kapsar.
/// </summary>
public class UISetupExecutor : MonoBehaviour
{
    [Header("Otomatik Çalışsın")]
    public bool runOnStart = true;

    private static GameObject contentRoot;
    private TrainingUI.TabManager tabManager;

    void Start()
    {
        if (runOnStart)
        {
            Execute();
        }
    }

    public void Execute()
    {
        SetupActiveLines();
        SetupContentPanels();
        SetupTabManager();
        SetupTabListeners();
        SetupSearchBarKeyboard();

        // İlk tab'ı (Infografikler) aktif et
        if (tabManager != null && tabManager.tabs.Count > 0)
        {
            tabManager.SelectTab(0);
        }

        Debug.Log("[UISetupExecutor] UI Setup Complete!");
    }

    // ═══════════════════════════════════════
    //  ACTIVE LINE OLUŞTURMA
    // ═══════════════════════════════════════

    void SetupActiveLines()
    {
        SetupActiveLineForTab("sinif/UI_Canvas/FuturisticPanel/GlassContainer/Tab1");
        SetupActiveLineForTab("sinif/UI_Canvas/FuturisticPanel/GlassContainer/Tab2");
        SetupActiveLineForTab("sinif/UI_Canvas/FuturisticPanel/GlassContainer/Tab3");
    }

    void SetupActiveLineForTab(string tabPath)
    {
        GameObject tab = GameObject.Find(tabPath);
        if (tab == null)
        {
            Debug.LogError($"[UISetupExecutor] Tab not found: {tabPath}");
            return;
        }

        Transform existingLine = tab.transform.Find("ActiveLine");
        if (existingLine != null)
        {
            existingLine.gameObject.SetActive(false);
            return;
        }

        GameObject activeLine = new GameObject("ActiveLine");
        activeLine.transform.SetParent(tab.transform);
        activeLine.transform.localPosition = new Vector3(0f, -56f, 0f);
        activeLine.transform.localScale = Vector3.one;

        RectTransform rect = activeLine.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.1f, 0f);
        rect.anchorMax = new Vector2(0.9f, 0f);
        rect.anchoredPosition = new Vector2(0f, 2f);
        rect.sizeDelta = new Vector2(0f, 3f);
        rect.pivot = new Vector2(0.5f, 0f);

        Image img = activeLine.AddComponent<Image>();
        img.color = new Color(0f, 0.898f, 1f, 1f);

        activeLine.SetActive(false);
    }

    // ═══════════════════════════════════════
    //  İÇERİK PANELLERİ OLUŞTURMA
    // ═══════════════════════════════════════

    void SetupContentPanels()
    {
        contentRoot = GameObject.Find("sinif/UI_Canvas/FuturisticPanel/GlassContainer/ContentRoot");
        if (contentRoot == null)
        {
            Debug.LogError("[UISetupExecutor] ContentRoot not found");
            return;
        }

        GameObject infographicsPanel = contentRoot.transform.Find("Infografikler")?.gameObject;
        if (infographicsPanel != null)
        {
            LayoutElement le = infographicsPanel.GetComponent<LayoutElement>();
            if (le == null)
            {
                le = infographicsPanel.AddComponent<LayoutElement>();
            }
            le.preferredHeight = 350f;
            le.flexibleWidth = 1f;
        }

        CreateContentPanel("SunumlarPanel", contentRoot.transform);
        CreateContentPanel("VideolarPanel", contentRoot.transform);
    }

    void CreateContentPanel(string panelName, Transform parent)
    {
        Transform existing = parent.Find(panelName);
        if (existing != null)
        {
            return;
        }

        GameObject panel = new GameObject(panelName);
        panel.transform.SetParent(parent);
        panel.transform.localPosition = Vector3.zero;
        panel.transform.localScale = Vector3.one;

        RectTransform rect = panel.AddComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.anchoredPosition = Vector2.zero;
        rect.sizeDelta = new Vector2(0f, 0f);
        rect.pivot = new Vector2(0.5f, 0.5f);

        VerticalLayoutGroup layout = panel.AddComponent<VerticalLayoutGroup>();
        layout.spacing = 15;
        layout.padding = new RectOffset(20, 20, 20, 20);
        layout.childAlignment = TextAnchor.UpperCenter;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;

        LayoutElement layoutElement = panel.AddComponent<LayoutElement>();
        layoutElement.preferredHeight = 350f;
        layoutElement.flexibleWidth = 1f;

        GameObject titleObj = new GameObject("Title");
        titleObj.transform.SetParent(panel.transform);
        titleObj.transform.localPosition = Vector3.zero;
        titleObj.transform.localScale = Vector3.one;

        RectTransform titleRect = titleObj.AddComponent<RectTransform>();
        titleRect.anchorMin = Vector2.zero;
        titleRect.anchorMax = Vector2.one;
        titleRect.sizeDelta = new Vector2(0f, 0f);

        TextMeshProUGUI titleText = titleObj.AddComponent<TextMeshProUGUI>();
        titleText.text = panelName.Replace("Panel", "");
        titleText.fontSize = 24;
        titleText.alignment = TextAlignmentOptions.Center;
        titleText.color = new Color(0f, 0.898f, 1f, 1f);

        LayoutElement titleLayout = titleObj.AddComponent<LayoutElement>();
        titleLayout.preferredHeight = 40f;
    }

    // ═══════════════════════════════════════
    //  TAB MANAGER KONFİGÜRASYONU
    //  (ConfigureTabManager.cs işlevini kapsar)
    // ═══════════════════════════════════════

    void SetupTabManager()
    {
        GameObject glassContainer = GameObject.Find("sinif/UI_Canvas/FuturisticPanel/GlassContainer");
        if (glassContainer == null)
        {
            Debug.LogError("[UISetupExecutor] GlassContainer not found");
            return;
        }

        tabManager = glassContainer.GetComponent<TrainingUI.TabManager>();
        if (tabManager == null)
        {
            tabManager = glassContainer.AddComponent<TrainingUI.TabManager>();
        }

        GameObject tab1 = GameObject.Find("sinif/UI_Canvas/FuturisticPanel/GlassContainer/Tab1");
        GameObject tab2 = GameObject.Find("sinif/UI_Canvas/FuturisticPanel/GlassContainer/Tab2");
        GameObject tab3 = GameObject.Find("sinif/UI_Canvas/FuturisticPanel/GlassContainer/Tab3");

        tabManager.tabs = new List<TrainingUI.TabManager.TabData>();

        if (tab1 != null)
        {
            TrainingUI.TabManager.TabData tab1Data = new TrainingUI.TabManager.TabData();
            tab1Data.tabName = "İnfografikler";
            tab1Data.tabButton = tab1;
            tab1Data.activeLine = tab1.transform.Find("ActiveLine")?.gameObject;
            tab1Data.contentPanel = contentRoot != null
                ? contentRoot.transform.Find("Infografikler")?.gameObject
                : null;
            tabManager.tabs.Add(tab1Data);
        }

        if (tab2 != null)
        {
            TrainingUI.TabManager.TabData tab2Data = new TrainingUI.TabManager.TabData();
            tab2Data.tabName = "Sunumlar";
            tab2Data.tabButton = tab2;
            tab2Data.activeLine = tab2.transform.Find("ActiveLine")?.gameObject;
            tab2Data.contentPanel = contentRoot != null
                ? contentRoot.transform.Find("SunumlarPanel")?.gameObject
                : null;
            tabManager.tabs.Add(tab2Data);
        }

        if (tab3 != null)
        {
            TrainingUI.TabManager.TabData tab3Data = new TrainingUI.TabManager.TabData();
            tab3Data.tabName = "Videolar";
            tab3Data.tabButton = tab3;
            tab3Data.activeLine = tab3.transform.Find("ActiveLine")?.gameObject;
            tab3Data.contentPanel = contentRoot != null
                ? contentRoot.transform.Find("VideolarPanel")?.gameObject
                : null;
            tabManager.tabs.Add(tab3Data);
        }

        // TabButton bileşenlerini konfigüre et
        SetupTabButton(tab1, 0, tabManager);
        SetupTabButton(tab2, 1, tabManager);
        SetupTabButton(tab3, 2, tabManager);
    }

    void SetupTabButton(GameObject tab, int index, TrainingUI.TabManager manager)
    {
        if (tab == null) return;

        TrainingUI.TabButton tabButton = tab.GetComponent<TrainingUI.TabButton>();
        if (tabButton == null)
        {
            tabButton = tab.AddComponent<TrainingUI.TabButton>();
        }

        tabButton.tabIndex = index;
        tabButton.tabManager = manager;
        tabButton.backgroundImage = tab.GetComponent<Image>();
        tabButton.textComponent = tab.GetComponentInChildren<TextMeshProUGUI>();
        tabButton.activeLine = tab.transform.Find("ActiveLine")?.gameObject;
        tabButton.normalColor = new Color(0.039f, 0.086f, 0.157f, 0.6f);
        tabButton.activeColor = new Color(0f, 0.898f, 1f, 0.12f);
    }

    // ═══════════════════════════════════════
    //  TAB BUTON LISTENER EKLEME
    //  (SetupTabListeners.cs işlevini kapsar)
    // ═══════════════════════════════════════

    void SetupTabListeners()
    {
        if (tabManager == null) return;

        Button tab1Button = GameObject.Find("sinif/UI_Canvas/FuturisticPanel/GlassContainer/Tab1")?.GetComponent<Button>();
        Button tab2Button = GameObject.Find("sinif/UI_Canvas/FuturisticPanel/GlassContainer/Tab2")?.GetComponent<Button>();
        Button tab3Button = GameObject.Find("sinif/UI_Canvas/FuturisticPanel/GlassContainer/Tab3")?.GetComponent<Button>();

        if (tab1Button != null)
        {
            tab1Button.onClick.RemoveAllListeners();
            tab1Button.onClick.AddListener(() => tabManager.SelectTab(0));
        }

        if (tab2Button != null)
        {
            tab2Button.onClick.RemoveAllListeners();
            tab2Button.onClick.AddListener(() => tabManager.SelectTab(1));
        }

        if (tab3Button != null)
        {
            tab3Button.onClick.RemoveAllListeners();
            tab3Button.onClick.AddListener(() => tabManager.SelectTab(2));
        }
    }

    // ═══════════════════════════════════════
    //  SEARCH BAR KLAVYE ENTEGRASYONU
    // ═══════════════════════════════════════

    void SetupSearchBarKeyboard()
    {
        GameObject searchBar = GameObject.Find("sinif/UI_Canvas/FuturisticPanel/GlassContainer/SearchBar");
        if (searchBar == null) return;

        // SearchBar'a tıklama özelliği ekle
        Button searchButton = searchBar.GetComponent<Button>();
        if (searchButton == null)
        {
            searchButton = searchBar.AddComponent<Button>();
            // Orijinal görünümünü korumak için Navigation'u None yap
            var nav = searchButton.navigation;
            nav.mode = Navigation.Mode.None;
            searchButton.navigation = nav;
        }

        // Mevcut Image'ı target graphic olarak kullan
        Image searchImage = searchBar.GetComponent<Image>();
        if (searchImage != null)
        {
            searchButton.targetGraphic = searchImage;
            // Renk değişimini minimal tut
            var colors = searchButton.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(0.9f, 0.95f, 1f, 1f);
            colors.pressedColor = new Color(0.8f, 0.9f, 1f, 1f);
            searchButton.colors = colors;
        }

        // SearchText referansını bul
        Transform searchTextTransform = searchBar.transform.Find("SearchText");
        TextMeshProUGUI searchText = searchTextTransform != null
            ? searchTextTransform.GetComponent<TextMeshProUGUI>()
            : null;

        // VR Keyboard referansını bul veya oluştur
        searchButton.onClick.RemoveAllListeners();
        searchButton.onClick.AddListener(() =>
        {
            var keyboard = FindObjectOfType<VRKeyboard>(true);
            if (keyboard != null)
            {
                keyboard.Show(searchText);
            }
            else
            {
                Debug.LogWarning("[UISetupExecutor] VRKeyboard bulunamadı!");
            }
        });
    }
}
