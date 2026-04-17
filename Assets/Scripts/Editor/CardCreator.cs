using UnityEngine;
using UnityEngine.UI;
using TMPro;

public static class CardCreator
{
    [UnityEditor.MenuItem("Tools/Create Cards For Panels")]
    public static void CreateCards()
    {
        CreateCardsForPanel("Assets/Samples/XR Interaction Toolkit/2.6.5/XR Device Simulator/Scenes/Modul1.unity", "sinif/UI_Canvas/FuturisticPanel/GlassContainer/ContentRoot/SunumlarPanel", "Sunum", "Sunum İçeriği");
        CreateCardsForPanel("Assets/Samples/XR Interaction Toolkit/2.6.5/XR Device Simulator/Scenes/Modul1.unity", "sinif/UI_Canvas/FuturisticPanel/GlassContainer/ContentRoot/VideolarPanel", "Video", "Video İçeriği");
        Debug.Log("Cards created successfully!");
    }

    private static void CreateCardsForPanel(string scenePath, string panelPath, string cardPrefix, string description)
    {
        UnityEditor.SceneManagement.EditorSceneManager.OpenScene(scenePath);
        System.Threading.Thread.Sleep(500);
        
        GameObject panel = GameObject.Find(panelPath);
        if (panel == null)
        {
            Debug.LogError("Panel not found: " + panelPath);
            return;
        }

        // Add GridLayoutGroup if not exists
        GridLayoutGroup grid = panel.GetComponent<GridLayoutGroup>();
        if (grid == null)
        {
            grid = panel.AddComponent<GridLayoutGroup>();
            grid.cellSize = new Vector2(520, 740);
            grid.spacing = new Vector2(30, 30);
            grid.startCorner = GridLayoutGroup.Corner.UpperLeft;
            grid.startAxis = GridLayoutGroup.Axis.Vertical;
            grid.constraint = GridLayoutGroup.Constraint.Flexible;
            grid.constraintCount = 0;
            grid.padding = new RectOffset(20, 20, 20, 20);
        }

        // Create 3 cards
        for (int i = 1; i <= 3; i++)
        {
            CreateCard(panel.transform, cardPrefix + "_" + i, "Başlık " + i, description + " " + i);
        }
        
        UnityEditor.SceneManagement.EditorSceneManager.SaveScene(UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());
    }

    private static void CreateCard(Transform parent, string cardName, string title, string description)
    {
        // Create Card GameObject
        GameObject card = new GameObject(cardName);
        card.transform.SetParent(parent);

        RectTransform rect = card.AddComponent<RectTransform>();
        card.AddComponent<CanvasRenderer>();
        Image bg = card.AddComponent<Image>();
        bg.color = new Color(0.0588f, 0.0863f, 0.1608f, 1f);
        Outline outline = card.AddComponent<Outline>();
        outline.effectColor = new Color(0f, 0.898f, 1f, 0.25f);
        outline.effectDistance = new Vector2(1.5f, -1.5f);

        // Thumbnail
        GameObject thumbnail = new GameObject("Thumbnail");
        thumbnail.transform.SetParent(card.transform);
        RectTransform thumbRect = thumbnail.AddComponent<RectTransform>();
        Image thumbImage = thumbnail.AddComponent<Image>();
        thumbImage.color = Color.white;
        thumbRect.anchorMin = new Vector2(0, 1);
        thumbRect.anchorMax = new Vector2(1, 1);
        thumbRect.pivot = new Vector2(0.5f, 1);
        thumbRect.anchoredPosition = Vector2.zero;
        thumbRect.sizeDelta = new Vector2(0, 465);

        // Title
        GameObject titleObj = new GameObject("Title");
        titleObj.transform.SetParent(card.transform);
        RectTransform titleRect = titleObj.AddComponent<RectTransform>();
        titleObj.AddComponent<CanvasRenderer>();
        TMPro.TextMeshProUGUI titleText = titleObj.AddComponent<TMPro.TextMeshProUGUI>();
        titleText.text = title;
        titleText.fontSize = 28;
        titleText.fontStyle = FontStyles.Bold;
        titleText.alignment = TextAlignmentOptions.TopLeft;
        titleText.color = Color.white;
        titleRect.anchorMin = new Vector2(0, 1);
        titleRect.anchorMax = new Vector2(1, 1);
        titleRect.pivot = new Vector2(0, 1);
        titleRect.anchoredPosition = new Vector2(18, -479);
        titleRect.sizeDelta = new Vector2(-36, 40);

        // Description
        GameObject descObj = new GameObject("Description");
        descObj.transform.SetParent(card.transform);
        RectTransform descRect = descObj.AddComponent<RectTransform>();
        descObj.AddComponent<CanvasRenderer>();
        TMPro.TextMeshProUGUI descText = descObj.AddComponent<TMPro.TextMeshProUGUI>();
        descText.text = description;
        descText.fontSize = 22;
        descText.alignment = TextAlignmentOptions.TopLeft;
        descText.color = new Color(0.7f, 0.7f, 0.7f, 1f);
        descRect.anchorMin = new Vector2(0, 1);
        descRect.anchorMax = new Vector2(1, 1);
        descRect.pivot = new Vector2(0, 1);
        descRect.anchoredPosition = new Vector2(18, -530);
        descRect.sizeDelta = new Vector2(-36, 150);
    }
}