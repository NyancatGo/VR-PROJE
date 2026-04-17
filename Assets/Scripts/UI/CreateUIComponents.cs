using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems;
using TrainingUI;

public class CreateUIComponents : MonoBehaviour
{
    [Header("Referanslar")]
    public GameObject glassContainer;

    void Start()
    {
        CreateActiveLines();
        CreateContentPanels();
        SetupTabManager();
    }

    void CreateActiveLines()
    {
        // Tab2 için ActiveLine oluştur
        CreateActiveLineForTab("Tab2", "sinif/UI_Canvas/FuturisticPanel/GlassContainer/Tab2");
        
        // Tab3 için ActiveLine oluştur
        CreateActiveLineForTab("Tab3", "sinif/UI_Canvas/FuturisticPanel/GlassContainer/Tab3");
    }

    void CreateActiveLineForTab(string objectName, string parentPath)
    {
        GameObject tab = GameObject.Find(parentPath);
        if (tab == null)
        {
            Debug.LogError($"Tab not found: {parentPath}");
            return;
        }

        // ActiveLine yoksa oluştur
        Transform existingLine = tab.transform.Find("ActiveLine");
        if (existingLine != null)
        {
            Debug.Log($"{objectName} already has ActiveLine");
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

    void CreateContentPanels()
    {
        GameObject contentRoot = GameObject.Find("sinif/UI_Canvas/FuturisticPanel/GlassContainer/ContentRoot");
        if (contentRoot == null)
        {
            Debug.LogError("ContentRoot not found");
            return;
        }

        // Sunumlar için panel
        CreateContentPanel("SunumlarPanel", contentRoot.transform, new Vector2(0f, 0f));
        
        // Videolar için panel
        CreateContentPanel("VideolarPanel", contentRoot.transform, new Vector2(0f, -400f));

        Debug.Log("Content panels created");
    }

    void CreateContentPanel(string panelName, Transform parent, Vector2 anchoredPosition)
    {
        // Panel zaten varsa silip yeniden oluşturma
        Transform existing = parent.Find(panelName);
        if (existing != null)
        {
            Debug.Log($"{panelName} already exists");
            return;
        }

        GameObject panel = new GameObject(panelName);
        panel.transform.SetParent(parent);
        panel.transform.localPosition = new Vector3(anchoredPosition.x, anchoredPosition.y, 0f);
        panel.transform.localScale = Vector3.one;

        RectTransform rect = panel.AddComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = new Vector2(0f, 0f);
        rect.pivot = new Vector2(0.5f, 0.5f);

        VerticalLayoutGroup layout = panel.AddComponent<VerticalLayoutGroup>();
        layout.spacing = 20;
        layout.padding = new RectOffset(20, 20, 20, 20);
        layout.childAlignment = TextAnchor.UpperCenter;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;

        LayoutElement layoutElement = panel.AddComponent<LayoutElement>();
        layoutElement.preferredHeight = 350f;
        layoutElement.flexibleWidth = 1f;
    }

    void SetupTabManager()
    {
        // GlassContainer'da TabManager bul veya oluştur
        GameObject glassContainer = GameObject.Find("sinif/UI_Canvas/FuturisticPanel/GlassContainer");
        if (glassContainer == null)
        {
            Debug.LogError("GlassContainer not found");
            return;
        }

        TabManager tabManager = glassContainer.GetComponent<TabManager>();
        if (tabManager == null)
        {
            tabManager = glassContainer.AddComponent<TabManager>();
        }

        Debug.Log("TabManager setup complete");
    }
}
