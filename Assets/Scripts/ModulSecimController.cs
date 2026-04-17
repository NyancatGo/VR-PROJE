using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using TMPro;
using TrainingAnalytics;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit.Inputs.Simulation;

/// <summary>
/// Modul secimi dropdown'undan bir modul secildiginde
/// onay panelini gosterir ve onaydan sonra sahne gecisi yapar.
/// </summary>
public class ModulSecimController : MonoBehaviour
{
    [Header("UI Referanslari")]
    [Tooltip("Modul secimi dropdown'u")]
    public Dropdown modulDropdown;

    [Tooltip("Onay modal paneli")]
    public GameObject modalSingleButton;

    [Tooltip("Modal icindeki onay butonu")]
    public Button onaylaButton;

    [Tooltip("Modal icindeki mesaj metni")]
    public Text modalMessageText;

    [Tooltip("Interactive Controls paneli")]
    public GameObject interactiveControls;

    [Tooltip("Scroll UI Sample paneli")]
    public GameObject scrollUISample;

    [Header("Sahne Ayarlari")]
    [Tooltip("Modul 2 sahne adi")]
    public string modul2SceneName = "Modul2_Guvenlik";

    [Tooltip("Modul 3 sahne adi")]
    public string modul3SceneName = "Mod\u00FCl3_Triyaj";

    [Tooltip("Modul 4 sahne adi")]
    public string modul4SceneName = "Mod\u00FCl4_yanginmudahale";

    private string selectedSceneName;
    private string selectedModuleId;
    private string selectedModuleName;
    private bool isSceneLoading;

    private void Start()
    {
        if (modalSingleButton != null)
        {
            modalSingleButton.SetActive(false);
        }

        if (modulDropdown != null)
        {
            modulDropdown.ClearOptions();
            modulDropdown.AddOptions(new List<string>
            {
                "Modul Seciniz",
                "Modul 2",
                "Modul 3",
                "Modul 4"
            });
            modulDropdown.value = 0;
            modulDropdown.onValueChanged.AddListener(OnDropdownValueChanged);
        }

        if (onaylaButton != null)
        {
            onaylaButton.onClick.AddListener(OnOnaylaClicked);
        }

        TrainingAnalyticsFacade.OnModuleEntered(
            TrainingAnalyticsFacade.Module1Id,
            TrainingAnalyticsFacade.Module1Name,
            new Dictionary<string, object>
            {
                { AnalyticsParams.SelectionSource, "scene_start" }
            });
    }

    private void OnDropdownValueChanged(int index)
    {
        if (index == 0)
        {
            selectedSceneName = string.Empty;
            selectedModuleId = string.Empty;
            selectedModuleName = string.Empty;

            if (modalSingleButton != null)
            {
                modalSingleButton.SetActive(false);
            }

            if (interactiveControls != null)
            {
                interactiveControls.SetActive(true);
            }

            if (scrollUISample != null)
            {
                scrollUISample.SetActive(true);
            }

            return;
        }

        switch (index)
        {
            case 1:
                selectedSceneName = modul2SceneName;
                selectedModuleId = TrainingAnalyticsFacade.Module2Id;
                selectedModuleName = TrainingAnalyticsFacade.Module2Name;
                break;

            case 2:
                selectedSceneName = modul3SceneName;
                selectedModuleId = TrainingAnalyticsFacade.Module3Id;
                selectedModuleName = TrainingAnalyticsFacade.Module3Name;
                break;

            case 3:
                selectedSceneName = modul4SceneName;
                selectedModuleId = TrainingAnalyticsFacade.Module4Id;
                selectedModuleName = TrainingAnalyticsFacade.Module4Name;
                break;

            default:
                return;
        }

        if (modalMessageText != null)
        {
            string modulAdi = modulDropdown.options[index].text;
            modalMessageText.text = modulAdi + "'e gecmeyi onayliyor musunuz?";
        }

        if (modalSingleButton != null)
        {
            modalSingleButton.SetActive(true);
        }

        if (interactiveControls != null)
        {
            interactiveControls.SetActive(false);
        }

        if (scrollUISample != null)
        {
            scrollUISample.SetActive(false);
        }

        TrainingAnalyticsFacade.TrackModuleTransitionIntent(
            TrainingAnalyticsFacade.Module1Id,
            TrainingAnalyticsFacade.Module1Name,
            selectedModuleId,
            selectedModuleName,
            "dropdown");

        Debug.Log("Modul secildi: " + modulDropdown.options[index].text + " -> Sahne: " + selectedSceneName);
    }

    private void OnOnaylaClicked()
    {
        if (isSceneLoading)
        {
            return;
        }

        if (!ParticipantManager.HasParticipant)
        {
            if (modalMessageText != null)
            {
                modalMessageText.text = "Lütfen önce Modül 1'de kayıt olun!";
            }
            return;
        }

        string resolvedSceneName = ResolveBuildSceneName(selectedSceneName);
        if (string.IsNullOrEmpty(resolvedSceneName))
        {
            Debug.LogWarning("Sahne adi belirlenemedi veya Build Settings icinde bulunamadi.");
            return;
        }

        isSceneLoading = true;
        SetLoadingUiState(false);
        Debug.Log("Module gecis onaylandi. Sahne yukleniyor: " + resolvedSceneName);
        StartCoroutine(LoadSceneRoutine(resolvedSceneName));
    }

    private IEnumerator LoadSceneRoutine(string sceneName)
    {
        XRSceneRuntimeStabilizer.PrepareForSceneTransition();
        XRCameraHelper.ClearCache();
        yield return null;

        AsyncOperation loadOperation = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Single);
        if (loadOperation == null)
        {
            Debug.LogError("Sahne gecisi baslatilamadi: " + sceneName);
            isSceneLoading = false;
            SetLoadingUiState(true);
            yield break;
        }

        while (!loadOperation.isDone)
        {
            yield return null;
        }

        // Scene switch sonrasında stale XR/camera referanslarını temizle.
        XRCameraHelper.ClearCache();
        yield return null;

        EnsureSimulatorReadyInEditor();
    }

    private static string ResolveBuildSceneName(string requestedSceneName)
    {
        if (string.IsNullOrWhiteSpace(requestedSceneName))
        {
            return string.Empty;
        }

        if (SceneUtility.GetBuildIndexByScenePath(requestedSceneName) >= 0)
        {
            return requestedSceneName;
        }

        if (SceneUtility.GetBuildIndexByScenePath("Assets/" + requestedSceneName + ".unity") >= 0)
        {
            return requestedSceneName;
        }

        string normalizedRequested = NormalizeSceneToken(requestedSceneName);
        for (int i = 0; i < SceneManager.sceneCountInBuildSettings; i++)
        {
            string scenePath = SceneUtility.GetScenePathByBuildIndex(i);
            if (string.IsNullOrEmpty(scenePath))
            {
                continue;
            }

            string fileName = System.IO.Path.GetFileNameWithoutExtension(scenePath);
            if (NormalizeSceneToken(fileName) == normalizedRequested)
            {
                return fileName;
            }
        }

        Debug.LogWarning("[ModulSecim] Build Settings icinde eslesen sahne bulunamadi: " + requestedSceneName);
        return string.Empty;
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

    private void OnDestroy()
    {
        if (modulDropdown != null)
        {
            modulDropdown.onValueChanged.RemoveListener(OnDropdownValueChanged);
        }

        if (onaylaButton != null)
        {
            onaylaButton.onClick.RemoveListener(OnOnaylaClicked);
        }
    }

    private void SetLoadingUiState(bool interactable)
    {
        if (modulDropdown != null)
        {
            modulDropdown.interactable = interactable;
        }

        if (onaylaButton != null)
        {
            onaylaButton.interactable = interactable;
        }
    }

    private static void EnsureSimulatorReadyInEditor()
    {
        if (!Application.isEditor)
        {
            return;
        }

        if (!XRSceneRuntimeStabilizer.IsSimulatorEnabledForCurrentSession())
        {
            GameObject simulatorToDisable = GameObject.Find("XR Device Simulator");
            if (simulatorToDisable != null && simulatorToDisable.activeSelf)
            {
                simulatorToDisable.SetActive(false);
            }

            return;
        }

        GameObject simulatorObject = GameObject.Find("XR Device Simulator");
        if (simulatorObject == null)
        {
            Debug.LogWarning("[ModulSecim] XR Device Simulator sahnede bulunamadi.");
            return;
        }

        if (!simulatorObject.activeSelf)
        {
            simulatorObject.SetActive(true);
            Debug.Log("[ModulSecim] XR Device Simulator aktif hale getirildi.");
        }

        if (simulatorObject.GetComponent<SimulatorCameraFixer>() == null)
        {
            simulatorObject.AddComponent<SimulatorCameraFixer>();
        }

        XRDeviceSimulator simulator = simulatorObject.GetComponent<XRDeviceSimulator>();
        if (simulator == null)
        {
            return;
        }

        if (simulator.cameraTransform == null)
        {
            Transform cameraTransform = XRCameraHelper.GetPlayerCameraTransform();
            if (cameraTransform != null)
            {
                simulator.cameraTransform = cameraTransform;
                Debug.Log("[ModulSecim] XR Device Simulator cameraTransform yeniden baglandi.");
            }
        }
    }
}
