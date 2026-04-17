using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using System.Text;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using UnityEngine.XR;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Inputs;
using UnityEngine.XR.Interaction.Toolkit.Inputs.Simulation;
using TriyajModul3;
#if UNITY_EDITOR
using UnityEditor;
#endif
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

/// <summary>
/// Sahne geçişlerinde XR Device Simulator camera referansını stabilize eder.
/// Domain/Scene reload kapalı olsa bile yeni sahnenin XR kamerasına tekrar bağlar.
/// </summary>
public class XRSceneRuntimeStabilizer : MonoBehaviour
{
    private static XRSceneRuntimeStabilizer _instance;
    private static bool _pendingTransitionSpawnApply;
    private static Unity.XR.CoreUtils.XROrigin _transitionPreservedOrigin;
    private const string SimulatorPrefabPath = "Assets/Samples/XR Interaction Toolkit/2.6.5/XR Device Simulator/XR Device Simulator.prefab";
    private const string Module2SceneToken = "modul2_guvenlik";
    private const string Module3SceneToken = "modul3_triyaj";
    private const string HospitalDarkVolumeName = "Hospital_Dark_Volume";
    private const string SimulatorUiRootName = "XR Device Simulator UI";
    private const float RuntimeCameraYOffset = 0f;
    private static readonly string[] Module2SafeGroundNames = { "VR_SpawnPoint", "VRSpawnPoint", "PlayerSpawn", "Ground_Base" };
    private static readonly Vector2[] Module2GroundSampleOffsets =
    {
        Vector2.zero,
        new Vector2(0f, 2f),
        new Vector2(0f, -2f),
        new Vector2(2f, 0f),
        new Vector2(-2f, 0f),
        new Vector2(2f, 2f),
        new Vector2(2f, -2f),
        new Vector2(-2f, 2f),
        new Vector2(-2f, -2f),
        new Vector2(0f, 4f),
        new Vector2(0f, -4f),
        new Vector2(4f, 0f),
        new Vector2(-4f, 0f),
    };

    public static bool IsSimulatorEnabledForCurrentSession()
    {
        return XRDeviceSimulatorRuntimeGate.IsSimulatorEnabledForCurrentSession();
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Bootstrap()
    {
        ResetSimulatorSingletonIfNeeded();

        if (_instance != null)
        {
            return;
        }

        GameObject host = new GameObject("__XRSceneRuntimeStabilizer");
        DontDestroyOnLoad(host);
        _instance = host.AddComponent<XRSceneRuntimeStabilizer>();
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void Start()
    {
        StartCoroutine(RebindSimulatorRoutine());
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        bool transitionLoad = _pendingTransitionSpawnApply;
        bool criticalXrModule = IsModule2Scene(scene.name) || IsModule3Scene(scene.name);

        if (IsModule3Scene(scene.name))
        {
            DisableHospitalDarkVolumeIfPresent();
        }

        ResolvePreservedOriginForLoadedScene(scene);
        EnsureInputActionManagerEnabled();
        EnsureActionBasedControllerActionsEnabled();

        StartCoroutine(RebindSimulatorRoutine());
        if (transitionLoad)
        {
            StartCoroutine(ApplyTransitionSpawnRoutine(scene));
        }
        else if (criticalXrModule)
        {
            StartCoroutine(EnsureCriticalModuleRuntimeRoutine(scene));
        }

        StartCoroutine(ApplySceneAtmosphereGuardsRoutine(scene, transitionLoad));
    }

    private IEnumerator ApplyTransitionSpawnRoutine(Scene scene)
    {
        bool hasSceneStartPose = TryGetSceneXrOriginStartPose(scene, out Vector3 sceneStartPosition, out float sceneStartYaw);
        bool criticalXrModule = IsModule2Scene(scene.name) || IsModule3Scene(scene.name);

        // Yeni sahnenin XR rig kurulumu ve simulator rebinding akisi tamamlansin.
        yield return null;
        yield return null;
        yield return null;

        Unity.XR.CoreUtils.XROrigin targetOrigin = criticalXrModule
            ? EnsureCriticalModuleRuntime(scene)
            : null;

        bool applied = false;
        bool usedSceneStartFallback = false;
        bool usedModuleSafeFallback = false;
        if (VRSpawnPoint.TryResolveSceneAuthoredSpawnPose(
            out Vector3 authoredSpawnPosition,
            out float authoredSpawnYaw,
            out int authoredReapplyFrames))
        {
            applied = TryApplyRigRootSpawn(targetOrigin, authoredSpawnPosition, authoredSpawnYaw, authoredReapplyFrames);
        }

        if (!applied && TryResolveCriticalModuleFallbackSpawn(
            scene,
            targetOrigin,
            out Vector3 moduleFallbackPosition,
            out float moduleFallbackYaw))
        {
            applied = TryApplyRigRootSpawn(targetOrigin, moduleFallbackPosition, moduleFallbackYaw, 4);
            usedModuleSafeFallback = applied;
        }

        if (!applied && hasSceneStartPose)
        {
            applied = TryApplyRigRootSpawn(targetOrigin, sceneStartPosition, sceneStartYaw, 4);
            usedSceneStartFallback = applied;
        }

        if (applied)
        {
            VRSpawnPoint.MarkExternalSpawnApplied();
            if (criticalXrModule)
            {
                StartCoroutine(VerifyCriticalModulePoseRoutine(scene, targetOrigin));
            }

            if (IsModule2Scene(scene.name) && usedModuleSafeFallback)
            {
                Debug.Log("[XRRuntimeStabilizer] Modul2 sahne rig/marker eksiginde Ground_Base uzerinden guvenli VR spawn uygulandi.");
            }
            else if (IsModule2Scene(scene.name) && usedSceneStartFallback)
            {
                Debug.Log("[XRRuntimeStabilizer] Modul2 spawn marker bulunamadigi icin scene baslangic pozu fallback uygulandi.");
            }
            else
            {
                Debug.Log("[XRRuntimeStabilizer] Gecis sonrasi spawn, VRSpawnPoint tarafindan tek otorite ile uygulandi.");
            }
        }
        else
        {
            Debug.LogWarning("[XRRuntimeStabilizer] Gecis sonrasi spawn uygulanamadi. VRSpawnPoint yerel fallback kullanacak.");
        }

        _pendingTransitionSpawnApply = false;

        if (IsModule3Scene(scene.name))
        {
            yield return null;
            if (IsPlayerCameraInsideHospitalDarkVolume())
            {
                Debug.LogWarning("[XRRuntimeStabilizer] Modul 3 girisinde kamera dark volume icinde tespit edildi, spawn tekrar uygulanacak.");
                if (VRSpawnPoint.TryResolveSceneAuthoredSpawnPose(
                    out Vector3 retryPosition,
                    out float retryYaw,
                    out int retryFrames))
                {
                    TryApplyRigRootSpawn(targetOrigin, retryPosition, retryYaw, retryFrames);
                }
            }
        }
    }

    private IEnumerator EnsureCriticalModuleRuntimeRoutine(Scene scene)
    {
        yield return null;
        yield return null;
        yield return null;

        Unity.XR.CoreUtils.XROrigin targetOrigin = EnsureCriticalModuleRuntime(scene);
        if (targetOrigin == null)
        {
            yield break;
        }

        if (VRSpawnPoint.TryResolveSceneAuthoredSpawnPose(
            out Vector3 authoredSpawnPosition,
            out float authoredSpawnYaw,
            out int authoredReapplyFrames))
        {
            VRSpawnPoint.TryRespawnPlayerRigRoot(this, targetOrigin, authoredSpawnPosition, authoredSpawnYaw, authoredReapplyFrames);
            StartCoroutine(VerifyCriticalModulePoseRoutine(scene, targetOrigin));
        }
        else if (TryResolveCriticalModuleFallbackSpawn(
            scene,
            targetOrigin,
            out Vector3 moduleFallbackPosition,
            out float moduleFallbackYaw))
        {
            VRSpawnPoint.TryRespawnPlayerRigRoot(this, targetOrigin, moduleFallbackPosition, moduleFallbackYaw, 4);
            StartCoroutine(VerifyCriticalModulePoseRoutine(scene, targetOrigin));
            Debug.Log("[XRRuntimeStabilizer] Kritik XR modul dogrudan acildi; guvenli runtime spawn fallback uygulandi.");
        }
    }

    private bool TryApplyRigRootSpawn(
        Unity.XR.CoreUtils.XROrigin targetOrigin,
        Vector3 position,
        float yaw,
        int reapplyFrames)
    {
        if (targetOrigin != null)
        {
            return VRSpawnPoint.TryRespawnPlayerRigRoot(this, targetOrigin, position, yaw, reapplyFrames);
        }

        return VRSpawnPoint.TryRespawnPlayerRigRoot(this, position, yaw, reapplyFrames);
    }

    private IEnumerator VerifyCriticalModulePoseRoutine(Scene scene, Unity.XR.CoreUtils.XROrigin targetOrigin)
    {
        if (!IsModule2Scene(scene.name) && !IsModule3Scene(scene.name))
        {
            yield break;
        }

        const int GuardFrameCount = 45;
        const int GroundCheckStartFrame = 30;
        for (int i = 0; i < GuardFrameCount; i++)
        {
            yield return null;

            if (targetOrigin == null || targetOrigin.Equals(null))
            {
                targetOrigin = EnsureCriticalModuleRuntime(scene);
            }

            if (targetOrigin == null || targetOrigin.Equals(null))
            {
                continue;
            }

            bool includeGroundCheck = i >= GroundCheckStartFrame;
            if (!IsOriginPoseUnsafe(targetOrigin, includeGroundCheck, out string reason))
            {
                continue;
            }

            bool correctionApplied = false;
            if (VRSpawnPoint.TryResolveSceneAuthoredSpawnPose(
                out Vector3 authoredSpawnPosition,
                out float authoredSpawnYaw,
                out int authoredReapplyFrames))
            {
                TryApplyRigRootSpawn(targetOrigin, authoredSpawnPosition, authoredSpawnYaw, authoredReapplyFrames);
                correctionApplied = true;
            }
            else if (TryResolveCriticalModuleFallbackSpawn(
                scene,
                targetOrigin,
                out Vector3 moduleFallbackPosition,
                out float moduleFallbackYaw))
            {
                TryApplyRigRootSpawn(targetOrigin, moduleFallbackPosition, moduleFallbackYaw, 4);
                correctionApplied = true;
            }

            if (correctionApplied)
            {
                Debug.LogWarning("[XRRuntimeStabilizer] Kritik XR modul spawn guard duzeltmesi uygulandi: " + reason);
                yield return null;
            }
        }
    }

    private static bool IsOriginPoseUnsafe(Unity.XR.CoreUtils.XROrigin origin, bool includeGroundCheck, out string reason)
    {
        reason = string.Empty;
        if (origin == null)
        {
            reason = "origin missing";
            return true;
        }

        Vector3 originPosition = origin.transform.position;
        if (!IsFiniteVector(originPosition))
        {
            reason = "origin pose invalid";
            return true;
        }

        Camera camera = origin.Camera != null ? origin.Camera : origin.GetComponentInChildren<Camera>(true);
        if (camera != null)
        {
            Vector3 cameraPosition = camera.transform.position;
            if (!IsFiniteVector(cameraPosition))
            {
                reason = "camera pose invalid";
                return true;
            }
        }

        if (!includeGroundCheck)
        {
            return false;
        }

        if (!TryResolveGuardGroundY(originPosition, out float groundY))
        {
            return false;
        }

        CharacterController characterController = origin.GetComponent<CharacterController>();
        if (characterController != null)
        {
            Vector3 controllerCenter = origin.transform.TransformPoint(characterController.center);
            float feetY = controllerCenter.y - (characterController.height * 0.5f);
            if (feetY < groundY - 0.08f)
            {
                reason = "character feet below ground";
                return true;
            }
        }
        else if (originPosition.y < groundY - 0.25f)
        {
            reason = "origin below ground";
            return true;
        }

        if (camera != null)
        {
            Vector3 cameraPosition = camera.transform.position;
            if (cameraPosition.y < groundY - 0.05f)
            {
                reason = "camera below ground";
                return true;
            }
        }

        return false;
    }

    private static bool TryResolveGuardGroundY(Vector3 targetPos, out float groundY)
    {
        groundY = 0f;
        const float MinGroundNormalY = 0.45f;
        const float WideProbeHeight = 4f;
        const float WideProbeDistance = 8f;
        const float MaxRaiseAboveTarget = 3.0f;
        const float MaxDropBelowTarget = 1.0f;

        RaycastHit[] hits = Physics.RaycastAll(
            targetPos + (Vector3.up * WideProbeHeight),
            Vector3.down,
            WideProbeDistance,
            ~0,
            QueryTriggerInteraction.Ignore);

        bool foundCandidate = false;
        float bestCandidateY = 0f;
        float bestAbsDelta = float.MaxValue;
        for (int i = 0; i < hits.Length; i++)
        {
            RaycastHit hit = hits[i];
            if (hit.normal.y < MinGroundNormalY)
            {
                continue;
            }

            float deltaFromTarget = hit.point.y - targetPos.y;
            if (deltaFromTarget > MaxRaiseAboveTarget || deltaFromTarget < -MaxDropBelowTarget)
            {
                continue;
            }

            float absDelta = Mathf.Abs(deltaFromTarget);
            if (!foundCandidate || absDelta < bestAbsDelta)
            {
                bestCandidateY = hit.point.y;
                bestAbsDelta = absDelta;
                foundCandidate = true;
            }
        }

        if (!foundCandidate)
        {
            return false;
        }

        groundY = bestCandidateY;
        return true;
    }

    private static bool IsFiniteVector(Vector3 value)
    {
        return IsFinite(value.x) && IsFinite(value.y) && IsFinite(value.z);
    }

    private static bool IsFinite(float value)
    {
        return !float.IsNaN(value) && !float.IsInfinity(value);
    }

    private IEnumerator ApplySceneAtmosphereGuardsRoutine(Scene scene, bool transitionLoad)
    {
        yield return null;
        yield return null;

        // Unity'nin sahne asenkron yüklemelerinde ambient aydınlatmasını unutma hatasını tüm sahnelerde çöz
        DynamicGI.UpdateEnvironment();

        if (!IsModule3Scene(scene.name))
        {
            yield break;
        }

        RenderSettings.fog = false;
        RenderSettings.ambientMode = AmbientMode.Skybox;
        RenderSettings.ambientIntensity = Mathf.Max(RenderSettings.ambientIntensity, 1f);

        DisableHospitalDarkVolumeIfPresent();
        ForceClearFadeOverlayIfPresent();
        NormalizeModule3Lighting(transitionLoad);

        Debug.Log("[XRRuntimeStabilizer] Modul 3 girisinde atmosfer guard tamamlandi. Poz VRSpawnPoint'e birakilir.");
    }

    private static void NormalizeModule3Lighting(bool transitionLoad)
    {
        Light directionalLight = FindMainDirectionalLight();
        float appliedLightIntensity = -1f;
        float appliedShadowStrength = -1f;
        if (directionalLight != null)
        {
            // Modul 1 atmosferinden tasinan olasi dusuk aydinlatma/golge state'ini sifirla.
            directionalLight.color = Color.white;
            directionalLight.intensity = transitionLoad
                ? 1.15f
                : Mathf.Max(directionalLight.intensity, 1.0f);

            directionalLight.shadowStrength = transitionLoad
                ? 0.72f
                : Mathf.Min(directionalLight.shadowStrength, 0.85f);

            appliedLightIntensity = directionalLight.intensity;
            appliedShadowStrength = directionalLight.shadowStrength;
        }

        // Global post-FX guard: Module 3 her yuklemede normalize edilir.
        Volume[] volumes = Object.FindObjectsOfType<Volume>(true);
        if (volumes == null)
        {
            return;
        }

        for (int i = 0; i < volumes.Length; i++)
        {
            Volume volume = volumes[i];
            // Guard uses sharedProfile to avoid auto-cloning just for the null check.
            if (volume == null || !volume.isGlobal || volume.sharedProfile == null)
            {
                continue;
            }

            // Use volume.profile (not sharedProfile) so Unity auto-instantiates a runtime
            // copy — shared asset on disk is never mutated.
            VolumeProfile runtimeProfile = volume.profile;

            if (runtimeProfile.TryGet(out Vignette vignette))
            {
                vignette.active = true;
                vignette.intensity.overrideState = true;
                vignette.intensity.value = Mathf.Min(vignette.intensity.value, 0.08f);
                vignette.smoothness.overrideState = true;
                vignette.smoothness.value = Mathf.Clamp(vignette.smoothness.value, 0.2f, 0.30f);
            }

            if (runtimeProfile.TryGet(out ColorAdjustments colorAdjustments))
            {
                colorAdjustments.postExposure.overrideState = true;
                colorAdjustments.postExposure.value = Mathf.Max(colorAdjustments.postExposure.value, 0.10f);
                colorAdjustments.contrast.overrideState = true;
                colorAdjustments.contrast.value = Mathf.Min(colorAdjustments.contrast.value, 4f);
            }
        }

        Debug.Log(
            "[XRRuntimeStabilizer] Modul3 light normalize => intensity="
            + appliedLightIntensity.ToString("0.00")
            + ", shadowStrength="
            + appliedShadowStrength.ToString("0.00"));
    }

    private static void ForceClearFadeOverlayIfPresent()
    {
        FadeEffectManager fadeManager = Object.FindObjectOfType<FadeEffectManager>(true);
        if (fadeManager != null)
        {
            fadeManager.InstantFadeClear();
        }
    }

    private static Light FindMainDirectionalLight()
    {
        Light[] lights = Object.FindObjectsOfType<Light>(true);
        if (lights == null)
        {
            return null;
        }

        for (int i = 0; i < lights.Length; i++)
        {
            Light light = lights[i];
            if (light == null || light.type != LightType.Directional || !light.gameObject.activeInHierarchy)
            {
                continue;
            }

            return light;
        }

        return null;
    }

    private static bool TryGetSceneXrOriginStartPose(Scene scene, out Vector3 position, out float yaw)
    {
        position = Vector3.zero;
        yaw = 0f;

        if (!scene.IsValid() || !scene.isLoaded)
        {
            return false;
        }

        GameObject[] rootObjects = scene.GetRootGameObjects();
        if (rootObjects == null)
        {
            return false;
        }

        for (int i = 0; i < rootObjects.Length; i++)
        {
            if (rootObjects[i] == null)
            {
                continue;
            }

            Unity.XR.CoreUtils.XROrigin xrOrigin = rootObjects[i].GetComponentInChildren<Unity.XR.CoreUtils.XROrigin>(true);
            if (xrOrigin == null)
            {
                continue;
            }

            position = xrOrigin.transform.position;
            yaw = xrOrigin.transform.eulerAngles.y;
            return true;
        }

        return false;
    }

    private static Unity.XR.CoreUtils.XROrigin EnsureCriticalModuleRuntime(Scene scene)
    {
        Unity.XR.CoreUtils.XROrigin targetOrigin = FindFirstSceneXROrigin(scene);
        if (targetOrigin == null)
        {
            targetOrigin = GetAlivePreservedOrigin();
        }

        if (targetOrigin == null)
        {
            targetOrigin = XRCameraHelper.GetXROrigin();
        }

        if (targetOrigin == null)
        {
            targetOrigin = CreateRuntimeFallbackOrigin(scene);
        }

        if (targetOrigin == null)
        {
            Debug.LogError("[XRRuntimeStabilizer] Kritik XR modul icin kullanilabilir XROrigin bulunamadi/olusturulamadi.");
            return null;
        }

        if (!targetOrigin.gameObject.activeSelf)
        {
            targetOrigin.gameObject.SetActive(true);
        }

        EnsureOriginCameraReferences(targetOrigin);
        EnsureCharacterControllerReady(targetOrigin);
        EnsureLocomotionSystemForOrigin(targetOrigin);
        EnsureRealDevicePoseFallbacks(targetOrigin);
        XRCameraHelper.ClearCache();
        EnsureLocomotionProvidersEnabled();
        EnsureInputActionManagerEnabled();
        EnsureActionBasedControllerActionsEnabled();
        return targetOrigin;
    }

    private static void ResolvePreservedOriginForLoadedScene(Scene scene)
    {
        Unity.XR.CoreUtils.XROrigin preservedOrigin = GetAlivePreservedOrigin();
        if (preservedOrigin == null)
        {
            return;
        }

        Unity.XR.CoreUtils.XROrigin sceneOrigin = FindFirstSceneXROrigin(scene);
        if (sceneOrigin != null && sceneOrigin != preservedOrigin)
        {
            preservedOrigin.gameObject.SetActive(false);
            Object.Destroy(preservedOrigin.gameObject);
            _transitionPreservedOrigin = null;
            XRCameraHelper.ClearCache();
            Debug.Log("[XRRuntimeStabilizer] Yeni sahnede kendi XR Origin'i bulundu; tasinan gecis rig'i pasiflestirilip temizlendi.");
            return;
        }

        if (!preservedOrigin.gameObject.activeSelf)
        {
            preservedOrigin.gameObject.SetActive(true);
        }
    }

    private static void PreserveCurrentXROriginForTransition()
    {
        XRCameraHelper.ClearCache();
        Unity.XR.CoreUtils.XROrigin currentOrigin = XRCameraHelper.GetXROrigin();
        if (currentOrigin == null)
        {
            currentOrigin = FindFirstAvailableXROrigin();
        }

        if (currentOrigin == null)
        {
            _transitionPreservedOrigin = null;
            return;
        }

        currentOrigin.gameObject.SetActive(true);
        Object.DontDestroyOnLoad(currentOrigin.gameObject);
        _transitionPreservedOrigin = currentOrigin;
    }

    private static Unity.XR.CoreUtils.XROrigin GetAlivePreservedOrigin()
    {
        if (_transitionPreservedOrigin == null || _transitionPreservedOrigin.Equals(null))
        {
            _transitionPreservedOrigin = null;
            return null;
        }

        return _transitionPreservedOrigin;
    }

    private static Unity.XR.CoreUtils.XROrigin FindFirstAvailableXROrigin()
    {
        Unity.XR.CoreUtils.XROrigin[] origins = Object.FindObjectsOfType<Unity.XR.CoreUtils.XROrigin>(true);
        if (origins == null || origins.Length == 0)
        {
            return null;
        }

        for (int i = 0; i < origins.Length; i++)
        {
            if (origins[i] != null && origins[i].gameObject.activeInHierarchy)
            {
                return origins[i];
            }
        }

        return origins[0];
    }

    private static Unity.XR.CoreUtils.XROrigin FindFirstSceneXROrigin(Scene scene)
    {
        if (!scene.IsValid() || !scene.isLoaded)
        {
            return null;
        }

        GameObject[] rootObjects = scene.GetRootGameObjects();
        if (rootObjects == null)
        {
            return null;
        }

        for (int i = 0; i < rootObjects.Length; i++)
        {
            if (rootObjects[i] == null)
            {
                continue;
            }

            Unity.XR.CoreUtils.XROrigin sceneOrigin =
                rootObjects[i].GetComponentInChildren<Unity.XR.CoreUtils.XROrigin>(true);
            if (sceneOrigin != null)
            {
                return sceneOrigin;
            }
        }

        return null;
    }

    private static void EnsureOriginCameraReferences(Unity.XR.CoreUtils.XROrigin origin)
    {
        if (origin.Origin == null)
        {
            origin.Origin = origin.gameObject;
        }

        if (origin.CameraFloorOffsetObject == null)
        {
            Transform offsetTransform = FindChildByName(origin.transform, "Camera Offset");
            if (offsetTransform == null)
            {
                GameObject offsetObject = new GameObject("Camera Offset");
                offsetObject.transform.SetParent(origin.transform, false);
                offsetTransform = offsetObject.transform;
            }

            origin.CameraFloorOffsetObject = offsetTransform.gameObject;
        }

        Camera originCamera = origin.Camera;
        if (originCamera == null)
        {
            originCamera = origin.GetComponentInChildren<Camera>(true);
        }

        if (originCamera == null)
        {
            GameObject cameraObject = new GameObject("Main Camera");
            Transform cameraParent = origin.CameraFloorOffsetObject != null
                ? origin.CameraFloorOffsetObject.transform
                : origin.transform;
            cameraObject.transform.SetParent(cameraParent, false);
            originCamera = cameraObject.AddComponent<Camera>();
        }

        origin.Camera = originCamera;
        originCamera.gameObject.SetActive(true);
        originCamera.tag = "MainCamera";

        if (origin.RequestedTrackingOriginMode != Unity.XR.CoreUtils.XROrigin.TrackingOriginMode.Floor)
        {
            origin.RequestedTrackingOriginMode = Unity.XR.CoreUtils.XROrigin.TrackingOriginMode.Floor;
            origin.CameraYOffset = RuntimeCameraYOffset;
        }

        XRRealDevicePoseFallback cameraPoseFallback = originCamera.GetComponent<XRRealDevicePoseFallback>();
        if (HasAuthoritativePoseDriver(originCamera.gameObject))
        {
            if (cameraPoseFallback != null)
            {
                cameraPoseFallback.enabled = false;
            }
        }
        else
        {
            if (cameraPoseFallback == null)
            {
                cameraPoseFallback = originCamera.gameObject.AddComponent<XRRealDevicePoseFallback>();
            }

            cameraPoseFallback.enabled = true;
            cameraPoseFallback.Configure(XRNode.CenterEye, true, true);
        }
    }

    private static void EnsureCharacterControllerReady(Unity.XR.CoreUtils.XROrigin origin)
    {
        CharacterController controller = origin.GetComponent<CharacterController>();
        if (controller == null)
        {
            controller = origin.gameObject.AddComponent<CharacterController>();
            controller.height = 1.7f;
            controller.radius = 0.25f;
            controller.center = new Vector3(0f, 0.85f, 0f);
            return;
        }

        controller.radius = Mathf.Max(controller.radius, 0.2f);
        if (controller.height < 1.2f)
        {
            controller.height = 1.7f;
        }

        float minCenterY = (controller.height * 0.5f) - 0.15f;
        if (controller.center.y < minCenterY)
        {
            controller.center = new Vector3(controller.center.x, minCenterY, controller.center.z);
        }
    }

    private static void EnsureLocomotionSystemForOrigin(Unity.XR.CoreUtils.XROrigin origin)
    {
        LocomotionSystem locomotionSystem = origin.GetComponent<LocomotionSystem>();
        if (locomotionSystem == null)
        {
            locomotionSystem = origin.gameObject.AddComponent<LocomotionSystem>();
        }

        locomotionSystem.xrOrigin = origin;
        locomotionSystem.enabled = true;

        LocomotionProvider[] providers = origin.GetComponentsInChildren<LocomotionProvider>(true);
        for (int i = 0; i < providers.Length; i++)
        {
            if (providers[i] == null)
            {
                continue;
            }

            providers[i].system = locomotionSystem;
            providers[i].enabled = true;
        }

        if (origin.GetComponentInChildren<TeleportationProvider>(true) == null)
        {
            TeleportationProvider teleportationProvider = origin.gameObject.AddComponent<TeleportationProvider>();
            teleportationProvider.system = locomotionSystem;
        }

        if (origin.GetComponentInChildren<ContinuousMoveProviderBase>(true) == null)
        {
            DeviceBasedContinuousMoveProvider moveProvider = origin.gameObject.AddComponent<DeviceBasedContinuousMoveProvider>();
            moveProvider.system = locomotionSystem;
        }

        if (origin.GetComponentInChildren<ContinuousTurnProviderBase>(true) == null
            && origin.GetComponentInChildren<SnapTurnProviderBase>(true) == null)
        {
            DeviceBasedContinuousTurnProvider turnProvider = origin.gameObject.AddComponent<DeviceBasedContinuousTurnProvider>();
            turnProvider.system = locomotionSystem;
        }
    }

    private static void EnsureRealDevicePoseFallbacks(Unity.XR.CoreUtils.XROrigin origin)
    {
        EnsureControllerPoseFallback(origin, "Left Controller", XRNode.LeftHand);
        EnsureControllerPoseFallback(origin, "Right Controller", XRNode.RightHand);
    }

    private static void EnsureControllerPoseFallback(
        Unity.XR.CoreUtils.XROrigin origin,
        string preferredName,
        XRNode node)
    {
        Transform controllerTransform = FindControllerTransform(origin.transform, node);
        if (controllerTransform == null)
        {
            GameObject controllerObject = new GameObject(preferredName);
            Transform parent = origin.CameraFloorOffsetObject != null
                ? origin.CameraFloorOffsetObject.transform
                : origin.transform;
            controllerObject.transform.SetParent(parent, false);
            controllerTransform = controllerObject.transform;
        }

        if (controllerTransform.GetComponent<XRController>() == null
            && controllerTransform.GetComponent<ActionBasedController>() == null)
        {
            XRController controller = controllerTransform.gameObject.AddComponent<XRController>();
            controller.controllerNode = node;
        }

        XRRealDevicePoseFallback poseFallback = controllerTransform.GetComponent<XRRealDevicePoseFallback>();
        if (HasAuthoritativePoseDriver(controllerTransform.gameObject))
        {
            if (poseFallback != null)
            {
                poseFallback.enabled = false;
            }

            return;
        }

        if (poseFallback == null)
        {
            poseFallback = controllerTransform.gameObject.AddComponent<XRRealDevicePoseFallback>();
        }

        poseFallback.enabled = true;
        poseFallback.Configure(node, true, true);
    }

    private static bool HasAuthoritativePoseDriver(GameObject target)
    {
        if (target == null)
        {
            return false;
        }

        if (target.GetComponent<ActionBasedController>() != null
            || target.GetComponent<XRController>() != null)
        {
            return true;
        }

        Component[] components = target.GetComponents<Component>();
        for (int i = 0; i < components.Length; i++)
        {
            Component component = components[i];
            if (component == null)
            {
                continue;
            }

            string typeName = component.GetType().Name;
            if (string.Equals(typeName, "TrackedPoseDriver", System.StringComparison.Ordinal))
            {
                return true;
            }
        }

        return false;
    }

    private static Transform FindControllerTransform(Transform root, XRNode node)
    {
        if (root == null)
        {
            return null;
        }

        // 1) Legacy XRController: controllerNode alanından kesin eşleşme.
        XRController[] xrControllers = root.GetComponentsInChildren<XRController>(true);
        for (int i = 0; i < xrControllers.Length; i++)
        {
            XRController controller = xrControllers[i];
            if (controller != null && controller.controllerNode == node)
            {
                return controller.transform;
            }
        }

        // 2) ActionBasedController dahil XRBaseController soyu: isim-tabanli esleme.
        //    XRController ve ActionBasedController paralel siniflardir; GetComponentsInChildren<XRController>
        //    ActionBasedController'i yakalamaz. XRI 2.x rig'lerinde ABC varsayilandir, bu yuzden bu dal kritik.
        XRBaseController[] baseControllers = root.GetComponentsInChildren<XRBaseController>(true);
        for (int i = 0; i < baseControllers.Length; i++)
        {
            XRBaseController baseController = baseControllers[i];
            if (baseController == null)
            {
                continue;
            }

            string controllerName = NormalizeControllerSearchName(baseController.name);
            if (MatchesControllerSide(controllerName, node))
            {
                return baseController.transform;
            }

            Transform controllerParent = baseController.transform.parent;
            if (controllerParent != null
                && MatchesControllerSide(NormalizeControllerSearchName(controllerParent.name), node))
            {
                return baseController.transform;
            }
        }

        // 3) Son fallback: dogrudan transform adiyla arama.
        Transform[] children = root.GetComponentsInChildren<Transform>(true);
        for (int i = 0; i < children.Length; i++)
        {
            Transform child = children[i];
            if (child == null)
            {
                continue;
            }

            string childName = NormalizeControllerSearchName(child.name);
            if (MatchesControllerSide(childName, node) && LooksLikeControllerOrHand(childName))
            {
                return child;
            }
        }

        return null;
    }

    private static string NormalizeControllerSearchName(string value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return string.Empty;
        }

        return value
            .ToLowerInvariant()
            .Replace("\u011f", "g")
            .Replace("\u0131", "i")
            .Replace("\u015f", "s")
            .Replace("\u00e7", "c")
            .Replace("\u00f6", "o")
            .Replace("\u00fc", "u");
    }

    private static readonly char[] ControllerNameDelimiters =
        { ' ', '_', '-', '.', '(', ')', '/', '\\', '\t' };

    private static bool MatchesControllerSide(string normalizedName, XRNode node)
    {
        if (string.IsNullOrEmpty(normalizedName))
        {
            return false;
        }

        // Token-tabanli esleme: "Console" icindeki "sol" veya "_Root" icindeki "_r"
        // gibi substring false-positive'lerini engeller. Isimleri delim'lerle parcaliyoruz,
        // her token'i whitelist'te tam esleme (veya sol/sag/left/right on-ek) ile kontrol ediyoruz.
        string[] tokens = normalizedName.Split(ControllerNameDelimiters,
            System.StringSplitOptions.RemoveEmptyEntries);
        if (tokens.Length == 0)
        {
            return false;
        }

        bool isLeft = node == XRNode.LeftHand;
        for (int i = 0; i < tokens.Length; i++)
        {
            string token = tokens[i];
            if (token.Length == 0)
            {
                continue;
            }

            if (isLeft)
            {
                if (token == "l" || token == "left" || token == "sol" || token == "solel"
                    || token.StartsWith("left") || token.EndsWith("left"))
                {
                    return true;
                }
            }
            else
            {
                if (token == "r" || token == "right" || token == "sag" || token == "sagel"
                    || token.StartsWith("right") || token.EndsWith("right"))
                {
                    return true;
                }
            }
        }

        return false;
    }

    private static bool LooksLikeControllerOrHand(string normalizedName)
    {
        if (string.IsNullOrEmpty(normalizedName))
        {
            return false;
        }

        if (normalizedName.Contains("controller") || normalizedName.Contains("hand"))
        {
            return true;
        }

        // "el" (Turkce "hand") 2 karakter oldugu icin "panel"/"model"/"helmet" gibi
        // masum isimlere substring olarak gelir. Sadece tam token eslesmesi kabul edilir.
        string[] tokens = normalizedName.Split(ControllerNameDelimiters,
            System.StringSplitOptions.RemoveEmptyEntries);
        for (int i = 0; i < tokens.Length; i++)
        {
            if (tokens[i] == "el")
            {
                return true;
            }
        }

        return false;
    }

    private static Unity.XR.CoreUtils.XROrigin CreateRuntimeFallbackOrigin(Scene scene)
    {
        GameObject originObject = new GameObject("XR Origin (Runtime Guard)");
        if (scene.IsValid() && scene.isLoaded)
        {
            SceneManager.MoveGameObjectToScene(originObject, scene);
        }

        Unity.XR.CoreUtils.XROrigin origin = originObject.AddComponent<Unity.XR.CoreUtils.XROrigin>();
        origin.Origin = originObject;

        GameObject offsetObject = new GameObject("Camera Offset");
        offsetObject.transform.SetParent(originObject.transform, false);
        origin.CameraFloorOffsetObject = offsetObject;
        origin.RequestedTrackingOriginMode = Unity.XR.CoreUtils.XROrigin.TrackingOriginMode.Floor;
        origin.CameraYOffset = RuntimeCameraYOffset;

        GameObject cameraObject = new GameObject("Main Camera");
        cameraObject.transform.SetParent(offsetObject.transform, false);
        Camera camera = cameraObject.AddComponent<Camera>();
        cameraObject.AddComponent<AudioListener>();
        origin.Camera = camera;

        EnsureOriginCameraReferences(origin);
        EnsureCharacterControllerReady(origin);
        EnsureLocomotionSystemForOrigin(origin);
        EnsureRealDevicePoseFallbacks(origin);

        Debug.Log("[XRRuntimeStabilizer] Sahnede XR Origin yoktu; runtime guard minimal XR rig olusturdu.");
        return origin;
    }

    private static bool TryResolveCriticalModuleFallbackSpawn(
        Scene scene,
        Unity.XR.CoreUtils.XROrigin targetOrigin,
        out Vector3 position,
        out float yaw)
    {
        position = Vector3.zero;
        yaw = 0f;

        if (IsModule2Scene(scene.name))
        {
            for (int i = 0; i < Module2SafeGroundNames.Length; i++)
            {
                Transform marker = FindSceneTransform(scene, Module2SafeGroundNames[i]);
                if (marker == null)
                {
                    continue;
                }

                Collider markerCollider = marker.GetComponent<Collider>();
                if (markerCollider != null && markerCollider.enabled && !markerCollider.isTrigger)
                {
                    if (!TryResolveClearSpawnOnCollider(markerCollider, targetOrigin, out position))
                    {
                        continue;
                    }
                }
                else
                {
                    position = marker.position;
                }

                yaw = marker.eulerAngles.y;
                return true;
            }

            if (TryFindLargestGroundCollider(scene, out Collider groundCollider))
            {
                if (TryResolveClearSpawnOnCollider(groundCollider, targetOrigin, out position))
                {
                    yaw = 0f;
                    return true;
                }
            }
        }

        if (IsModule3Scene(scene.name) && TryGetSceneXrOriginStartPose(scene, out position, out yaw))
        {
            return true;
        }

        return false;
    }

    private static bool TryResolveClearSpawnOnCollider(
        Collider groundCollider,
        Unity.XR.CoreUtils.XROrigin targetOrigin,
        out Vector3 position)
    {
        position = Vector3.zero;
        if (groundCollider == null || !groundCollider.enabled || groundCollider.isTrigger)
        {
            return false;
        }

        Bounds bounds = groundCollider.bounds;
        float minX = bounds.min.x + 0.5f;
        float maxX = bounds.max.x - 0.5f;
        float minZ = bounds.min.z + 0.5f;
        float maxZ = bounds.max.z - 0.5f;
        if (minX > maxX)
        {
            minX = bounds.min.x;
            maxX = bounds.max.x;
        }

        if (minZ > maxZ)
        {
            minZ = bounds.min.z;
            maxZ = bounds.max.z;
        }

        for (int i = 0; i < Module2GroundSampleOffsets.Length; i++)
        {
            Vector2 sampleOffset = Module2GroundSampleOffsets[i];
            float sampleX = Mathf.Clamp(
                bounds.center.x + sampleOffset.x,
                minX,
                maxX);

            float sampleZ = Mathf.Clamp(
                bounds.center.z + sampleOffset.y,
                minZ,
                maxZ);

            Vector3 rayStart = new Vector3(sampleX, bounds.max.y + 5f, sampleZ);
            if (!Physics.Raycast(
                    rayStart,
                    Vector3.down,
                    out RaycastHit hit,
                    12f,
                    ~0,
                    QueryTriggerInteraction.Ignore))
            {
                continue;
            }

            if (hit.collider != groundCollider || hit.normal.y < 0.45f)
            {
                continue;
            }

            Vector3 candidatePosition = hit.point + Vector3.up * 0.05f;
            if (!IsSpawnCapsuleClear(targetOrigin, candidatePosition, groundCollider))
            {
                continue;
            }

            position = candidatePosition;
            return true;
        }

        return false;
    }

    private static bool IsSpawnCapsuleClear(
        Unity.XR.CoreUtils.XROrigin targetOrigin,
        Vector3 candidatePosition,
        Collider groundCollider)
    {
        GetSpawnCapsuleMetrics(targetOrigin, out float radius, out float height, out Vector3 centerOffset);
        float halfSegment = Mathf.Max(0f, (height * 0.5f) - radius);
        Vector3 worldCenter = candidatePosition + centerOffset;
        Vector3 p1 = worldCenter + (Vector3.up * halfSegment);
        Vector3 p2 = worldCenter - (Vector3.up * halfSegment);

        Collider[] overlaps = Physics.OverlapCapsule(p1, p2, radius, ~0, QueryTriggerInteraction.Ignore);
        for (int i = 0; i < overlaps.Length; i++)
        {
            Collider overlap = overlaps[i];
            if (overlap == null)
            {
                continue;
            }

            // Zeminin kendisi float precision nedeniyle overlap listesinde gelebilir; aday reddedilmesin.
            if (overlap == groundCollider)
            {
                continue;
            }

            if (targetOrigin != null && !targetOrigin.Equals(null))
            {
                if (overlap == targetOrigin.GetComponent<CharacterController>())
                {
                    continue;
                }

                if (overlap.transform.IsChildOf(targetOrigin.transform))
                {
                    continue;
                }
            }

            return false;
        }

        return true;
    }

    private static void GetSpawnCapsuleMetrics(
        Unity.XR.CoreUtils.XROrigin targetOrigin,
        out float radius,
        out float height,
        out Vector3 centerOffset)
    {
        CharacterController controller = targetOrigin != null && !targetOrigin.Equals(null)
            ? targetOrigin.GetComponent<CharacterController>()
            : null;

        if (controller != null)
        {
            radius = Mathf.Max(0.2f, controller.radius);
            height = Mathf.Max(1.2f, controller.height);
            centerOffset = controller.center;
            return;
        }

        radius = 0.25f;
        height = 1.7f;
        centerOffset = new Vector3(0f, 0.85f, 0f);
    }

    private static bool TryFindLargestGroundCollider(Scene scene, out Collider groundCollider)
    {
        groundCollider = null;
        float bestArea = 0f;
        Collider[] colliders = Object.FindObjectsOfType<Collider>(true);
        if (colliders == null)
        {
            return false;
        }

        for (int i = 0; i < colliders.Length; i++)
        {
            Collider candidate = colliders[i];
            if (candidate == null
                || !candidate.enabled
                || candidate.isTrigger
                || candidate.gameObject.scene != scene)
            {
                continue;
            }

            string candidateName = candidate.gameObject.name.ToLowerInvariant();
            if (!candidateName.Contains("ground")
                && !candidateName.Contains("floor")
                && !candidateName.Contains("road")
                && !candidateName.Contains("plane"))
            {
                continue;
            }

            Bounds bounds = candidate.bounds;
            float area = Mathf.Abs(bounds.size.x * bounds.size.z);
            if (area > bestArea)
            {
                bestArea = area;
                groundCollider = candidate;
            }
        }

        return groundCollider != null;
    }

    private static Transform FindSceneTransform(Scene scene, string objectName)
    {
        if (!scene.IsValid() || !scene.isLoaded || string.IsNullOrWhiteSpace(objectName))
        {
            return null;
        }

        GameObject[] rootObjects = scene.GetRootGameObjects();
        if (rootObjects == null)
        {
            return null;
        }

        for (int i = 0; i < rootObjects.Length; i++)
        {
            if (rootObjects[i] == null)
            {
                continue;
            }

            Transform found = FindChildByName(rootObjects[i].transform, objectName);
            if (found != null)
            {
                return found;
            }
        }

        return null;
    }

    private static void ReapplySceneStartRigPose(Vector3 position, float yaw)
    {
        Unity.XR.CoreUtils.XROrigin xrOrigin = XRCameraHelper.GetXROrigin();
        if (xrOrigin == null)
        {
            return;
        }

        CharacterController characterController = xrOrigin.GetComponent<CharacterController>();
        bool restoreCharacterController = characterController != null && characterController.enabled;
        if (characterController != null)
        {
            characterController.enabled = false;
        }

        xrOrigin.transform.SetPositionAndRotation(position, Quaternion.Euler(0f, yaw, 0f));

        Transform cameraFloorOffset = xrOrigin.CameraFloorOffsetObject != null ? xrOrigin.CameraFloorOffsetObject.transform : null;
        if (cameraFloorOffset != null)
        {
            cameraFloorOffset.localPosition = new Vector3(0f, cameraFloorOffset.localPosition.y, 0f);
        }

        if (characterController != null)
        {
            characterController.enabled = restoreCharacterController;
        }
    }

    private static void DisableHospitalDarkVolumeIfPresent()
    {
        Volume[] volumes = Object.FindObjectsOfType<Volume>(true);
        if (volumes == null)
        {
            return;
        }

        bool changed = false;
        for (int i = 0; i < volumes.Length; i++)
        {
            Volume volume = volumes[i];
            if (volume == null || volume.isGlobal)
            {
                continue;
            }

            bool isHospitalDarkVolume =
                string.Equals(volume.gameObject.name, HospitalDarkVolumeName, System.StringComparison.Ordinal)
                || (volume.sharedProfile != null
                    && string.Equals(volume.sharedProfile.name, "TriyajHospitalDarkProfile", System.StringComparison.Ordinal));

            if (!isHospitalDarkVolume)
            {
                continue;
            }

            volume.weight = 0f;
            volume.enabled = false;

            Collider triggerCollider = volume.GetComponent<Collider>();
            if (triggerCollider != null)
            {
                triggerCollider.enabled = false;
            }

            if (volume.gameObject.activeSelf)
            {
                volume.gameObject.SetActive(false);
            }

            changed = true;
        }

        if (changed)
        {
            Debug.Log("[XRRuntimeStabilizer] Modul 3 karanlik lokal volume devre disi birakildi.");
        }
    }

    private static bool IsModule3Scene(string sceneName)
    {
        return NormalizeSceneToken(sceneName) == Module3SceneToken;
    }

    private static bool IsModule2Scene(string sceneName)
    {
        return NormalizeSceneToken(sceneName) == Module2SceneToken;
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

    private IEnumerator RebindSimulatorRoutine()
    {
        // XR Origin ve kamera setup'ının tamamlanması için kısa bekleme.
        yield return null;
        yield return null;

        ResetSimulatorSingletonIfNeeded();
        XRCameraHelper.ClearCache();

        // Always run global XR maintenance regardless of whether the simulator is active.
        // On real hardware (Quest Pro, etc.) the simulator is off, but locomotion providers
        // and InputActionManager still need to be live after a scene load.
        EnsureLocomotionProvidersEnabled();
        EnsureInputActionManagerEnabled();
        EnsureActionBasedControllerActionsEnabled();

        bool simulatorEnabled = IsSimulatorEnabledForCurrentSession();
        XRDeviceSimulator simulator = FindObjectOfType<XRDeviceSimulator>(true);
        if (!simulatorEnabled)
        {
            if (simulator != null && simulator.gameObject.activeSelf)
            {
                simulator.gameObject.SetActive(false);
                Debug.Log("[XRRuntimeStabilizer] XR Device Simulator global ayar nedeniyle devre disi birakildi.");
            }

            yield break;
        }

        if (simulator == null)
        {
            simulator = TrySpawnSimulatorInEditor();
        }

        if (simulator == null)
        {
            Debug.LogWarning("[XRRuntimeStabilizer] XR Device Simulator bulunamadi.");
            yield break;
        }

        if (!simulator.gameObject.activeSelf)
        {
            simulator.gameObject.SetActive(true);
        }

        if (simulator.GetComponent<SimulatorCameraFixer>() == null)
        {
            simulator.gameObject.AddComponent<SimulatorCameraFixer>();
        }

        Transform playerCameraTransform = XRCameraHelper.GetPlayerCameraTransform();
        if (playerCameraTransform == null)
        {
            Debug.LogWarning("[XRRuntimeStabilizer] XR kamera bulunamadi, simulator camera baglanamadi.");
            yield break;
        }

        if (simulator.cameraTransform != playerCameraTransform)
        {
            simulator.cameraTransform = playerCameraTransform;
            Debug.Log("[XRRuntimeStabilizer] XR Device Simulator yeni sahnenin kamerasina baglandi.");
        }

        EnsureSimulatorInputActions(simulator);

        if (IsModule3Scene(SceneManager.GetActiveScene().name))
        {
            ForceSimulatorControllerDefaults(simulator);
            ApplyModule3SimulatorInputGuards(simulator);
        }
    }

    private static void ForceSimulatorControllerDefaults(XRDeviceSimulator simulator)
    {
        if (simulator == null)
        {
            return;
        }

        try
        {
            const BindingFlags instanceNonPublic = BindingFlags.Instance | BindingFlags.NonPublic;
            System.Type simulatorType = typeof(XRDeviceSimulator);

            FieldInfo targetedField = simulatorType.GetField("m_TargetedDeviceInput", instanceNonPublic);
            System.Type targetedDevicesType = simulatorType.GetNestedType("TargetedDevices", BindingFlags.NonPublic | BindingFlags.Public);
            if (targetedField != null && targetedDevicesType != null)
            {
                object fpsValue = System.Enum.Parse(targetedDevicesType, "FPS");
                targetedField.SetValue(simulator, fpsValue);
            }

            FieldInfo deviceModeField = simulatorType.GetField("m_DeviceMode", instanceNonPublic);
            if (deviceModeField != null)
            {
                object controllerMode = System.Enum.Parse(typeof(XRDeviceSimulator.DeviceMode), "Controller");
                deviceModeField.SetValue(simulator, controllerMode);
            }

            FieldInfo dirtyField = simulatorType.GetField("m_DeviceModeDirty", instanceNonPublic);
            if (dirtyField != null)
            {
                dirtyField.SetValue(simulator, true);
            }

            FieldInfo startedField = simulatorType.GetField("m_StartedDeviceModeChange", instanceNonPublic);
            if (startedField != null)
            {
                startedField.SetValue(simulator, false);
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogWarning("[XRRuntimeStabilizer] Simulator varsayilan moda resetlenemedi: " + ex.Message);
        }
    }

    private static XRDeviceSimulator TrySpawnSimulatorInEditor()
    {
        if (!IsSimulatorEnabledForCurrentSession())
        {
            return null;
        }

#if UNITY_EDITOR
        GameObject simulatorPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(SimulatorPrefabPath);
        if (simulatorPrefab == null)
        {
            Debug.LogWarning("[XRRuntimeStabilizer] XR Device Simulator prefab bulunamadi: " + SimulatorPrefabPath);
            return null;
        }

        GameObject instance = Object.Instantiate(simulatorPrefab);
        if (instance == null)
        {
            return null;
        }

        instance.name = "XR Device Simulator";
        XRDeviceSimulator simulator = instance.GetComponent<XRDeviceSimulator>();
        if (simulator == null)
        {
            Debug.LogWarning("[XRRuntimeStabilizer] Olusturulan prefab XRDeviceSimulator bileseni icermiyor.");
            Object.Destroy(instance);
            return null;
        }

        Debug.Log("[XRRuntimeStabilizer] XR Device Simulator sahnede yoktu, prefabdan olusturuldu.");
        return simulator;
#else
        return null;
#endif
    }

    public static void PrepareForSceneTransition()
    {
        PreserveCurrentXROriginForTransition();
        _pendingTransitionSpawnApply = true;
        VRSpawnPoint.EnableExternalSpawnControlForNextScene();
        ForceResetSimulatorBeforeSceneTransition();
    }

    private static void ForceResetSimulatorBeforeSceneTransition()
    {
        XRDeviceSimulator simulator = FindObjectOfType<XRDeviceSimulator>(true);
        if (simulator != null)
        {
            ForceSimulatorControllerDefaults(simulator);
            simulator.cameraTransform = null;

            if (simulator.gameObject.activeSelf)
            {
                simulator.gameObject.SetActive(false);
            }
        }

        SetSimulatorSingleton(null);
        ResetSimulatorInternalState();
    }

    private static void ResetSimulatorInternalState()
    {
        try
        {
            PropertyInfo instanceProp = typeof(XRDeviceSimulator).GetProperty(
                "instance",
                BindingFlags.Public | BindingFlags.Static);
            XRDeviceSimulator current = instanceProp != null
                ? instanceProp.GetValue(null, null) as XRDeviceSimulator
                : null;

            if (current == null || current.Equals(null))
            {
                return;
            }

            const BindingFlags instanceNonPublic = BindingFlags.Instance | BindingFlags.NonPublic;
            System.Type simulatorType = typeof(XRDeviceSimulator);

            FieldInfo deviceModeField = simulatorType.GetField("m_DeviceMode", instanceNonPublic);
            if (deviceModeField != null)
            {
                object controllerMode = System.Enum.Parse(typeof(XRDeviceSimulator.DeviceMode), "Controller");
                deviceModeField.SetValue(current, controllerMode);
            }

            FieldInfo targetedField = simulatorType.GetField("m_TargetedDeviceInput", instanceNonPublic);
            System.Type targetedDevicesType = simulatorType.GetNestedType("TargetedDevices", BindingFlags.NonPublic | BindingFlags.Public);
            if (targetedField != null && targetedDevicesType != null)
            {
                object fpsValue = System.Enum.Parse(targetedDevicesType, "FPS");
                targetedField.SetValue(current, fpsValue);
            }

            FieldInfo dirtyField = simulatorType.GetField("m_DeviceModeDirty", instanceNonPublic);
            if (dirtyField != null)
            {
                dirtyField.SetValue(current, true);
            }

            FieldInfo startedField = simulatorType.GetField("m_StartedDeviceModeChange", instanceNonPublic);
            if (startedField != null)
            {
                startedField.SetValue(current, false);
            }
        }
        catch (System.Exception)
        {
        }
    }

    private static void EnsureLocomotionProvidersEnabled()
    {
        Unity.XR.CoreUtils.XROrigin xrOrigin = XRCameraHelper.GetXROrigin();
        if (xrOrigin == null)
        {
            return;
        }

        bool changed = false;
        changed |= EnableBehaviourIfNeeded(xrOrigin.GetComponent<LocomotionSystem>());
        changed |= EnableBehavioursIfNeeded(xrOrigin.GetComponentsInChildren<LocomotionSystem>(true));
        changed |= EnableBehavioursIfNeeded(xrOrigin.GetComponentsInChildren<ContinuousMoveProviderBase>(true));
        changed |= EnableBehavioursIfNeeded(xrOrigin.GetComponentsInChildren<ContinuousTurnProviderBase>(true));
        changed |= EnableBehavioursIfNeeded(xrOrigin.GetComponentsInChildren<SnapTurnProviderBase>(true));
        changed |= EnableBehavioursIfNeeded(xrOrigin.GetComponentsInChildren<TeleportationProvider>(true));

        if (changed)
        {
            Debug.Log("[XRRuntimeStabilizer] Locomotion bilesenleri yeniden aktif edildi.");
        }
    }

    /// <summary>
    /// Finds InputActionManagers present in loaded scenes and forces their assets enabled.
    /// This covers real-hardware sessions where the simulator never runs but the rig's
    /// ActionBasedControllers still depend on an active InputActionManager to receive input.
    /// </summary>
    private static void EnsureInputActionManagerEnabled()
    {
#if ENABLE_INPUT_SYSTEM
        InputActionManager[] actionManagers = Object.FindObjectsOfType<InputActionManager>(true);
        if (actionManagers == null || actionManagers.Length == 0)
        {
            return;
        }

        bool changed = false;
        for (int i = 0; i < actionManagers.Length; i++)
        {
            InputActionManager actionManager = actionManagers[i];
            if (actionManager == null)
            {
                continue;
            }

            if (!actionManager.enabled)
            {
                actionManager.enabled = true;
                changed = true;
            }

            actionManager.EnableInput();
            changed = true;
        }

        if (changed)
        {
            Debug.Log("[XRRuntimeStabilizer] InputActionManager action assetleri yeniden enable edildi.");
        }
#endif
    }

    private static void EnsureActionBasedControllerActionsEnabled()
    {
#if ENABLE_INPUT_SYSTEM
        ActionBasedController[] controllers = Object.FindObjectsOfType<ActionBasedController>(true);
        if (controllers == null || controllers.Length == 0)
        {
            return;
        }

        bool changed = false;
        for (int i = 0; i < controllers.Length; i++)
        {
            ActionBasedController controller = controllers[i];
            if (controller == null)
            {
                continue;
            }

            changed |= EnableInputAction(controller.positionAction);
            changed |= EnableInputAction(controller.rotationAction);
            changed |= EnableInputAction(controller.isTrackedAction);
            changed |= EnableInputAction(controller.trackingStateAction);
            changed |= EnableInputAction(controller.selectAction);
            changed |= EnableInputAction(controller.selectActionValue);
            changed |= EnableInputAction(controller.activateAction);
            changed |= EnableInputAction(controller.activateActionValue);
            changed |= EnableInputAction(controller.uiPressAction);
            changed |= EnableInputAction(controller.uiPressActionValue);
            changed |= EnableInputAction(controller.uiScrollAction);
            changed |= EnableInputAction(controller.hapticDeviceAction);
            changed |= EnableInputAction(controller.rotateAnchorAction);
            changed |= EnableInputAction(controller.directionalAnchorRotationAction);
            changed |= EnableInputAction(controller.translateAnchorAction);
            changed |= EnableInputAction(controller.scaleToggleAction);
            changed |= EnableInputAction(controller.scaleDeltaAction);
        }

        if (changed)
        {
            Debug.Log("[XRRuntimeStabilizer] ActionBasedController input actionlari manuel enable edildi.");
        }
#endif
    }

#if ENABLE_INPUT_SYSTEM
    private static bool EnableInputAction(InputActionProperty actionProperty)
    {
        InputAction action = actionProperty.action;
        if (action == null || action.enabled)
        {
            return false;
        }

        action.Enable();
        return true;
    }
#endif

    private static void ApplyModule3SimulatorInputGuards(XRDeviceSimulator simulator)
    {
        var simulators = new HashSet<XRDeviceSimulator>();
        if (simulator != null)
        {
            simulators.Add(simulator);
        }

        XRDeviceSimulator[] discoveredSimulators = Object.FindObjectsOfType<XRDeviceSimulator>(true);
        for (int i = 0; i < discoveredSimulators.Length; i++)
        {
            XRDeviceSimulator discovered = discoveredSimulators[i];
            if (discovered != null)
            {
                simulators.Add(discovered);
            }
        }

#if ENABLE_INPUT_SYSTEM
        foreach (XRDeviceSimulator current in simulators)
        {
            DisableSimulatorAction(current.cycleDevicesAction, "Cycle Devices");
        }
#endif

        bool uiHidden = false;
        foreach (XRDeviceSimulator current in simulators)
        {
            Transform simulatorUiRoot = FindChildByName(current.transform, SimulatorUiRootName);
            if (simulatorUiRoot == null || !simulatorUiRoot.gameObject.activeSelf)
            {
                continue;
            }

            simulatorUiRoot.gameObject.SetActive(false);
            uiHidden = true;
        }

        if (uiHidden)
        {
            Debug.Log("[XRRuntimeStabilizer] Modul 3 icin XR Device Simulator UI tum instance'larda gizlendi.");
        }
    }

#if ENABLE_INPUT_SYSTEM
    private static void DisableSimulatorAction(InputActionReference actionReference, string actionName)
    {
        if (actionReference == null || actionReference.action == null)
        {
            return;
        }

        if (!actionReference.action.enabled)
        {
            return;
        }

        actionReference.action.Disable();
        Debug.Log("[XRRuntimeStabilizer] Modul 3 icin simulator aksiyonu devre disi: " + actionName);
    }
#endif

    private static Transform FindChildByName(Transform root, string objectName)
    {
        if (root == null || string.IsNullOrWhiteSpace(objectName))
        {
            return null;
        }

        foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
        {
            if (string.Equals(child.name, objectName, System.StringComparison.OrdinalIgnoreCase))
            {
                return child;
            }
        }

        return null;
    }

    private static bool EnableBehaviourIfNeeded(Behaviour behaviour)
    {
        if (behaviour == null || behaviour.enabled)
        {
            return false;
        }

        behaviour.enabled = true;
        return true;
    }

    private static bool EnableBehavioursIfNeeded<TBehaviour>(TBehaviour[] behaviours)
        where TBehaviour : Behaviour
    {
        bool changed = false;
        if (behaviours == null)
        {
            return false;
        }

        for (int i = 0; i < behaviours.Length; i++)
        {
            changed |= EnableBehaviourIfNeeded(behaviours[i]);
        }

        return changed;
    }

    private static void EnsureSimulatorInputActions(XRDeviceSimulator simulator)
    {
#if ENABLE_INPUT_SYSTEM
        if (simulator == null)
        {
            return;
        }

        InputActionManager actionManager = Object.FindObjectOfType<InputActionManager>(true);
        bool created = false;

        if (actionManager == null)
        {
            GameObject managerObject = GameObject.Find("Input Action Manager");
            if (managerObject == null)
            {
                managerObject = new GameObject("Input Action Manager");
            }

            actionManager = managerObject.GetComponent<InputActionManager>();
            if (actionManager == null)
            {
                actionManager = managerObject.AddComponent<InputActionManager>();
            }

            created = true;
        }

        if (actionManager.actionAssets == null)
        {
            actionManager.actionAssets = new List<InputActionAsset>();
        }

        bool assetsChanged = false;
        assetsChanged |= AddActionAssetIfMissing(actionManager.actionAssets, simulator.deviceSimulatorActionAsset);
        assetsChanged |= AddActionAssetIfMissing(actionManager.actionAssets, simulator.controllerActionAsset);

        if (!actionManager.enabled || created || assetsChanged)
        {
            actionManager.enabled = false;
            actionManager.enabled = true;
        }

        if (created || assetsChanged)
        {
            Debug.Log("[XRRuntimeStabilizer] Input Action Manager ve simulator action assetleri dogrulandi.");
        }
#endif
    }

#if ENABLE_INPUT_SYSTEM
    private static bool AddActionAssetIfMissing(List<InputActionAsset> assets, InputActionAsset asset)
    {
        if (assets == null || asset == null)
        {
            return false;
        }

        if (assets.Contains(asset))
        {
            return false;
        }

        assets.Add(asset);
        return true;
    }
#endif

    private static void ResetSimulatorSingletonIfNeeded()
    {
        // Domain reload kapaliyken XRDeviceSimulator.instance stale kalabiliyor.
        // Bu durumda yeni sahnedeki simulator kendini haksiz yere destroy ediyor.
        XRDeviceSimulator current = GetSimulatorSingleton();

        if (current != null && !current.Equals(null))
        {
            return;
        }

        SetSimulatorSingleton(null);
    }

    private static XRDeviceSimulator GetSimulatorSingleton()
    {
        PropertyInfo instanceProp = typeof(XRDeviceSimulator).GetProperty(
            "instance",
            BindingFlags.Public | BindingFlags.Static);

        return instanceProp != null ? instanceProp.GetValue(null, null) as XRDeviceSimulator : null;
    }

    private static void SetSimulatorSingleton(XRDeviceSimulator simulator)
    {
        PropertyInfo instanceProp = typeof(XRDeviceSimulator).GetProperty(
            "instance",
            BindingFlags.Public | BindingFlags.Static);

        if (instanceProp != null)
        {
            MethodInfo setter = instanceProp.GetSetMethod(true);
            if (setter != null)
            {
                setter.Invoke(null, new object[] { simulator });
                return;
            }
        }

        FieldInfo backingField = typeof(XRDeviceSimulator).GetField(
            "<instance>k__BackingField",
            BindingFlags.NonPublic | BindingFlags.Static);
        if (backingField != null)
        {
            backingField.SetValue(null, simulator);
        }
    }

    private static bool IsPlayerCameraInsideHospitalDarkVolume()
    {
        Transform cameraTransform = XRCameraHelper.GetPlayerCameraTransform();
        if (cameraTransform == null)
        {
            return false;
        }

        Collider[] colliders = Object.FindObjectsOfType<Collider>(true);
        if (colliders == null)
        {
            return false;
        }

        for (int i = 0; i < colliders.Length; i++)
        {
            Collider collider = colliders[i];
            if (collider == null || !collider.enabled || !collider.gameObject.activeInHierarchy)
            {
                continue;
            }

            if (!string.Equals(collider.gameObject.name, HospitalDarkVolumeName, System.StringComparison.Ordinal))
            {
                continue;
            }

            Vector3 point = cameraTransform.position;
            Vector3 closest = collider.ClosestPoint(point);
            if ((closest - point).sqrMagnitude <= 0.0001f)
            {
                return true;
            }
        }

        return false;
    }
}
