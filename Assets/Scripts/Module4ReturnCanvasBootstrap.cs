using System.Globalization;
using System.Text;
using TMPro;
using TriyajModul3;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit.UI;

/// <summary>
/// Ensures Module 4 has a VR-friendly world-space return canvas near spawn.
/// </summary>
[DisallowMultipleComponent]
public class Module4ReturnCanvasBootstrap : MonoBehaviour
{
    private const string Module4SceneToken = "modul4_yanginmudahale";
    private const string CanvasRootName = "Module4_ReturnCanvas";
    private const string PanelName = "RootPanel";
    private const string ButtonName = "ReturnToModule1Button";
    private const string ButtonLabel = "Modul 1'e Don";

    [SerializeField] private float distanceFromSpawn = 1.6f;
    [SerializeField] private float verticalOffset = -0.12f;
    [SerializeField] private float ambulanceTopOffset = 0.08f;
    private static bool sceneHookRegistered;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void RegisterSceneHook()
    {
        if (sceneHookRegistered)
        {
            return;
        }

        SceneManager.sceneLoaded += OnSceneLoaded;
        sceneHookRegistered = true;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void EnsureOnInitialScene()
    {
        Scene activeScene = SceneManager.GetActiveScene();
        if (!activeScene.IsValid() || !activeScene.isLoaded || !IsModule4Scene(activeScene.name))
        {
            return;
        }

        EnsureBootstrapInstance();
    }

    private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (!scene.IsValid() || !scene.isLoaded || !IsModule4Scene(scene.name))
        {
            return;
        }

        EnsureBootstrapInstance();
    }

    private static void EnsureBootstrapInstance()
    {
        Module4ReturnCanvasBootstrap existing = FindObjectOfType<Module4ReturnCanvasBootstrap>(true);
        if (existing != null)
        {
            existing.EnsureCanvas();
            return;
        }

        GameObject host = new GameObject(nameof(Module4ReturnCanvasBootstrap));
        host.AddComponent<Module4ReturnCanvasBootstrap>();
    }

    private void Awake()
    {
        if (!IsModule4Scene(SceneManager.GetActiveScene().name))
        {
            Destroy(gameObject);
            return;
        }

        EnsureCanvas();
    }

    private void EnsureCanvas()
    {
        Canvas canvas = FindOrCreateCanvas();
        if (canvas == null)
        {
            return;
        }

        EnsureEventSystemForXrUi();
        EnsureCanvasRaycasters(canvas.gameObject);
        EnsureVruiClickHelper(canvas.gameObject);
        ConfigureCanvasPlacement(canvas.transform);
        EnsurePanelAndButton(canvas.transform);
    }

    private Canvas FindOrCreateCanvas()
    {
        GameObject canvasObject = GameObject.Find(CanvasRootName);
        if (canvasObject == null)
        {
            canvasObject = new GameObject(
                CanvasRootName,
                typeof(RectTransform),
                typeof(Canvas),
                typeof(CanvasScaler),
                typeof(GraphicRaycaster));
        }

        int uiLayer = LayerMask.NameToLayer("UI");
        if (uiLayer >= 0)
        {
            SetLayerRecursively(canvasObject.transform, uiLayer);
        }

        Canvas canvas = canvasObject.GetComponent<Canvas>();
        if (canvas == null)
        {
            canvas = canvasObject.AddComponent<Canvas>();
        }

        canvas.renderMode = RenderMode.WorldSpace;

        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
        if (scaler == null)
        {
            scaler = canvasObject.AddComponent<CanvasScaler>();
        }

        scaler.uiScaleMode = CanvasScaler.ScaleMode.ConstantPixelSize;
        scaler.dynamicPixelsPerUnit = 10f;

        RectTransform rect = canvasObject.GetComponent<RectTransform>();
        rect.sizeDelta = new Vector2(1000f, 450f);
        rect.localScale = Vector3.one * 0.002f;

        return canvas;
    }

    private void EnsureCanvasRaycasters(GameObject canvasObject)
    {
        if (canvasObject.GetComponent<TrackedDeviceGraphicRaycaster>() == null)
        {
            canvasObject.AddComponent<TrackedDeviceGraphicRaycaster>();
        }

        GraphicRaycaster fallbackRaycaster = canvasObject.GetComponent<GraphicRaycaster>();
        if (fallbackRaycaster == null)
        {
            canvasObject.AddComponent<GraphicRaycaster>();
        }
    }

    private void EnsureVruiClickHelper(GameObject canvasObject)
    {
        if (canvasObject.GetComponent<VRUIClickHelper>() == null)
        {
            canvasObject.AddComponent<VRUIClickHelper>();
        }
    }

    private void EnsureEventSystemForXrUi()
    {
        EventSystem eventSystem = EventSystem.current;
        if (eventSystem == null)
        {
            GameObject eventSystemObject = new GameObject("EventSystem", typeof(EventSystem));
            eventSystem = eventSystemObject.GetComponent<EventSystem>();
        }

        if (eventSystem.GetComponent<XRUIInputModule>() == null)
        {
            eventSystem.gameObject.AddComponent<XRUIInputModule>();
        }

        StandaloneInputModule standaloneInput = eventSystem.GetComponent<StandaloneInputModule>();
        if (standaloneInput != null)
        {
            standaloneInput.enabled = false;
        }
    }

    private void ConfigureCanvasPlacement(Transform canvasTransform)
    {
        Transform spawnAnchor = ResolveSpawnAnchor();
        float canvasHalfHeight = GetCanvasHalfHeightWorld(canvasTransform);

        if (TryResolveAmbulanceTopPosition(spawnAnchor, canvasHalfHeight, out Vector3 ambulanceTopPosition))
        {
            canvasTransform.position = ambulanceTopPosition;
            FaceCanvasTowardsSource(canvasTransform, spawnAnchor);
            return;
        }

        if (spawnAnchor == null)
        {
            return;
        }

        Vector3 forward = spawnAnchor.forward;
        forward.y = 0f;
        if (forward.sqrMagnitude < 0.0001f)
        {
            forward = Vector3.forward;
        }

        forward.Normalize();

        Vector3 canvasPosition = spawnAnchor.position + forward * distanceFromSpawn;
        canvasPosition.y = spawnAnchor.position.y + verticalOffset;
        canvasTransform.position = canvasPosition;

        FaceCanvasTowardsSource(canvasTransform, spawnAnchor);
    }

    private bool TryResolveAmbulanceTopPosition(Transform spawnAnchor, float canvasHalfHeight, out Vector3 position)
    {
        position = Vector3.zero;

        Transform ambulanceAnchor = ResolveAmbulanceAnchor(spawnAnchor);
        if (ambulanceAnchor == null)
        {
            return false;
        }

        Vector3 anchorPos = ambulanceAnchor.position;
        float estimatedRoofY = anchorPos.y + 2.6f;

        if (TryGetCombinedRendererBounds(ambulanceAnchor, out Bounds rendererBounds))
        {
            estimatedRoofY = Mathf.Clamp(rendererBounds.max.y, anchorPos.y + 2.2f, anchorPos.y + 3.2f);
        }
        else if (TryGetCombinedColliderBounds(ambulanceAnchor, out Bounds colliderBounds))
        {
            estimatedRoofY = Mathf.Clamp(colliderBounds.max.y, anchorPos.y + 2.2f, anchorPos.y + 3.2f);
        }

        position = new Vector3(
            anchorPos.x,
            estimatedRoofY + canvasHalfHeight + 0.1f,
            anchorPos.z
        );

        return true;
    }

    private Transform ResolveAmbulanceAnchor(Transform spawnAnchor)
    {
        GameObject[] allObjects = FindObjectsOfType<GameObject>(true);
        if (allObjects == null || allObjects.Length == 0)
        {
            return null;
        }

        string[] keywords = { "ambulance", "ambulans" };
        Vector3 referencePosition = spawnAnchor != null ? spawnAnchor.position : Vector3.zero;
        bool hasReference = spawnAnchor != null;

        Transform bestVehicle = null;
        float bestVehicleScore = float.MaxValue;
        Transform bestTrigger = null;
        float bestTriggerScore = float.MaxValue;

        for (int i = 0; i < allObjects.Length; i++)
        {
            GameObject candidate = allObjects[i];
            if (candidate == null)
            {
                continue;
            }

            string lowerName = candidate.name.ToLowerInvariant();
            bool isAmbulanceName = false;
            for (int k = 0; k < keywords.Length; k++)
            {
                if (lowerName.Contains(keywords[k]))
                {
                    isAmbulanceName = true;
                    break;
                }
            }

            if (!isAmbulanceName)
            {
                continue;
            }

            bool hasRenderableGeometry = candidate.GetComponentInChildren<Renderer>(true) != null;
            bool hasColliderGeometry = candidate.GetComponentInChildren<Collider>(true) != null;
            if (!hasRenderableGeometry && !hasColliderGeometry)
            {
                continue;
            }

            Vector3 samplePosition = candidate.transform.position;
            float score = hasReference
                ? (samplePosition - referencePosition).sqrMagnitude
                : samplePosition.sqrMagnitude;

            bool isTriggerObject = lowerName.Contains("trigger");
            if (isTriggerObject)
            {
                if (score < bestTriggerScore)
                {
                    bestTriggerScore = score;
                    bestTrigger = candidate.transform;
                }

                continue;
            }

            if (score < bestVehicleScore)
            {
                bestVehicleScore = score;
                bestVehicle = candidate.transform;
            }
        }

        if (bestVehicle != null)
        {
            return bestVehicle;
        }

        if (bestTrigger != null)
        {
            return bestTrigger;
        }

        GameObject namedTrigger = GameObject.Find("AmbulanceTrigger");
        if (namedTrigger != null)
        {
            return namedTrigger.transform;
        }

        return null;
    }

    private static float GetCanvasHalfHeightWorld(Transform canvasTransform)
    {
        RectTransform rect = canvasTransform as RectTransform;
        if (rect == null)
        {
            return 0.45f;
        }

        float worldHeight = rect.rect.height * Mathf.Abs(rect.lossyScale.y);
        if (worldHeight <= 0.001f)
        {
            return 0.45f;
        }

        return worldHeight * 0.5f;
    }

    private static bool TryGetCombinedRendererBounds(Transform root, out Bounds combinedBounds)
    {
        combinedBounds = default;
        if (root == null)
        {
            return false;
        }

        Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true);
        bool found = false;
        for (int i = 0; i < renderers.Length; i++)
        {
            Renderer renderer = renderers[i];
            if (renderer == null || !renderer.enabled)
            {
                continue;
            }

            if (!found)
            {
                combinedBounds = renderer.bounds;
                found = true;
            }
            else
            {
                combinedBounds.Encapsulate(renderer.bounds);
            }
        }

        return found;
    }

    private static bool TryGetCombinedColliderBounds(Transform root, out Bounds combinedBounds)
    {
        combinedBounds = default;
        if (root == null)
        {
            return false;
        }

        Collider[] colliders = root.GetComponentsInChildren<Collider>(true);
        bool found = false;
        for (int i = 0; i < colliders.Length; i++)
        {
            Collider collider = colliders[i];
            if (collider == null || !collider.enabled)
            {
                continue;
            }

            if (!found)
            {
                combinedBounds = collider.bounds;
                found = true;
            }
            else
            {
                combinedBounds.Encapsulate(collider.bounds);
            }
        }

        return found;
    }

    private static void FaceCanvasTowardsSource(Transform canvasTransform, Transform preferredSource)
    {
        if (canvasTransform == null)
        {
            return;
        }

        Vector3 sourcePosition;
        if (preferredSource != null)
        {
            sourcePosition = preferredSource.position;
        }
        else
        {
            Transform cameraTransform = XRCameraHelper.GetPlayerCameraTransform();
            sourcePosition = cameraTransform != null ? cameraTransform.position : canvasTransform.position - Vector3.forward;
        }

        // TERS YAZI YANSIMA ÇÖZÜMÜ:
        // LookDirection, objeden kameraya DEĞIL, kameradan objeye/ileriye doğru olmalıdır ki Canvas'ın Z "arkası" kameraya dönük olsun (textler Z- de okunur)
        Vector3 lookDirection = canvasTransform.position - sourcePosition;
        lookDirection.y = 0f;
        if (lookDirection.sqrMagnitude < 0.0001f)
        {
            lookDirection = Vector3.forward;
        }

        canvasTransform.rotation = Quaternion.LookRotation(lookDirection.normalized, Vector3.up);
    }

    private Transform ResolveSpawnAnchor()
    {
        string[] names = { "VR_SpawnPoint", "VRSpawnPoint", "PlayerSpawn" };
        for (int i = 0; i < names.Length; i++)
        {
            GameObject marker = GameObject.Find(names[i]);
            if (marker != null)
            {
                return marker.transform;
            }
        }

        VRSpawnPoint spawnPoint = FindObjectOfType<VRSpawnPoint>(true);
        if (spawnPoint != null)
        {
            return spawnPoint.transform;
        }

        Transform cameraTransform = XRCameraHelper.GetPlayerCameraTransform();
        if (cameraTransform != null)
        {
            return cameraTransform;
        }

        return null;
    }

    private void EnsurePanelAndButton(Transform canvasRoot)
    {
        Transform panelTransform = canvasRoot.Find(PanelName);
        GameObject panelObject;
        if (panelTransform == null)
        {
            panelObject = new GameObject(PanelName, typeof(RectTransform), typeof(Image));
            panelObject.transform.SetParent(canvasRoot, false);
        }
        else
        {
            panelObject = panelTransform.gameObject;
        }

        RectTransform panelRect = panelObject.GetComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0f, 0f);
        panelRect.anchorMax = new Vector2(1f, 1f);
        panelRect.offsetMin = Vector2.zero;
        panelRect.offsetMax = Vector2.zero;

        Image panelImage = panelObject.GetComponent<Image>();
        panelImage.color = new Color(0.04f, 0.07f, 0.12f, 0.82f);

        Transform buttonTransform = panelObject.transform.Find(ButtonName);
        GameObject buttonObject;
        if (buttonTransform == null)
        {
            buttonObject = new GameObject(ButtonName, typeof(RectTransform), typeof(Image), typeof(Button));
            buttonObject.transform.SetParent(panelObject.transform, false);
        }
        else
        {
            buttonObject = buttonTransform.gameObject;
        }

        RectTransform buttonRect = buttonObject.GetComponent<RectTransform>();
        buttonRect.anchorMin = new Vector2(0.5f, 0.5f);
        buttonRect.anchorMax = new Vector2(0.5f, 0.5f);
        buttonRect.pivot = new Vector2(0.5f, 0.5f);
        buttonRect.sizeDelta = new Vector2(520f, 130f);
        buttonRect.anchoredPosition = Vector2.zero;

        Image buttonImage = buttonObject.GetComponent<Image>();
        buttonImage.color = new Color(0.08f, 0.48f, 0.86f, 0.96f);

        Button button = buttonObject.GetComponent<Button>();
        button.transition = Selectable.Transition.ColorTint;

        ModuleReturnToSceneButton returnHandler = buttonObject.GetComponent<ModuleReturnToSceneButton>();
        if (returnHandler == null)
        {
            returnHandler = buttonObject.AddComponent<ModuleReturnToSceneButton>();
        }

        button.onClick.RemoveListener(returnHandler.LoadTargetScene);
        button.onClick.AddListener(returnHandler.LoadTargetScene);

        EnsureButtonLabel(buttonObject.transform);
    }

    private void EnsureButtonLabel(Transform buttonTransform)
    {
        Transform textTransform = buttonTransform.Find("Text");
        GameObject textObject;
        if (textTransform == null)
        {
            textObject = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
            textObject.transform.SetParent(buttonTransform, false);
        }
        else
        {
            textObject = textTransform.gameObject;
        }

        RectTransform textRect = textObject.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;

        Text legacyLabel = textObject.GetComponent<Text>();
        if (legacyLabel != null)
        {
            Destroy(legacyLabel);
        }

        TextMeshProUGUI label = textObject.GetComponent<TextMeshProUGUI>();
        if (label == null)
        {
            label = textObject.AddComponent<TextMeshProUGUI>();
        }

        if (label.font == null)
        {
            TextMeshProUGUI sceneTmp = FindObjectOfType<TextMeshProUGUI>(true);
            if (sceneTmp != null && sceneTmp.font != null)
            {
                label.font = sceneTmp.font;
            }
        }

        if (label.font == null)
        {
            TMP_FontAsset fallback = Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF");
            if (fallback != null)
            {
                label.font = fallback;
            }
        }

        if (label.font == null)
        {
            label.font = TMP_Settings.defaultFontAsset;
        }

        label.text = ButtonLabel;
        label.alignment = TextAlignmentOptions.Center;
        label.color = Color.white;
        label.enableAutoSizing = true;
        label.fontSizeMin = 24f;
        label.fontSizeMax = 56f;
        label.raycastTarget = false;
    }

    private static bool IsModule4Scene(string sceneName)
    {
        return NormalizeSceneToken(sceneName) == Module4SceneToken;
    }

    private static string NormalizeSceneToken(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        string decomposed = value.Normalize(NormalizationForm.FormD);
        var sb = new StringBuilder(decomposed.Length);

        for (int i = 0; i < decomposed.Length; i++)
        {
            char c = decomposed[i];
            if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)
            {
                sb.Append(char.ToLowerInvariant(c));
            }
        }

        return sb.ToString().Normalize(NormalizationForm.FormC);
    }

    private static void SetLayerRecursively(Transform root, int layer)
    {
        if (root == null)
        {
            return;
        }

        root.gameObject.layer = layer;
        for (int i = 0; i < root.childCount; i++)
        {
            SetLayerRecursively(root.GetChild(i), layer);
        }
    }
}