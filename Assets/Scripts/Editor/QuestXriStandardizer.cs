#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using Unity.XR.CoreUtils;
using UnityEditor;
using UnityEditor.Presets;
using UnityEditor.SceneManagement;
using UnityEditor.XR.Management;
using UnityEditor.XR.Management.Metadata;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Inputs;
using UnityEngine.XR.Interaction.Toolkit.Samples.StarterAssets;
using UnityEngine.XR.Interaction.Toolkit.UI;
using UnityEngine.XR.Management;
using UnityEngine.XR.OpenXR;
using UnityEngine.XR.OpenXR.Features.Interactions;
using UnityEngine.XR.OpenXR.Features.MetaQuestSupport;

public static class QuestXriStandardizer
{
    const string InputActionsPath = "Assets/Samples/XR Interaction Toolkit/2.6.5/Starter Assets/XRI Default Input Actions.inputactions";
    const string DirectInteractorPrefabPath = "Assets/Samples/XR Interaction Toolkit/2.6.5/Starter Assets/Prefabs/Interactors/Direct Interactor.prefab";
    const string RayInteractorPrefabPath = "Assets/Samples/XR Interaction Toolkit/2.6.5/Starter Assets/Prefabs/Interactors/Ray Interactor.prefab";
    const string TeleportInteractorPrefabPath = "Assets/Samples/XR Interaction Toolkit/2.6.5/Starter Assets/Prefabs/Interactors/Teleport Interactor.prefab";
    const string LeftControllerModelPath = "Assets/Samples/XR Interaction Toolkit/2.6.5/Starter Assets/Prefabs/Controllers/XR Controller Left.prefab";
    const string RightControllerModelPath = "Assets/Samples/XR Interaction Toolkit/2.6.5/Starter Assets/Prefabs/Controllers/XR Controller Right.prefab";
    const string LeftControllerPresetPath = "Assets/Samples/XR Interaction Toolkit/2.6.5/Starter Assets/Presets/XRI Default Left Controller.preset";
    const string RightControllerPresetPath = "Assets/Samples/XR Interaction Toolkit/2.6.5/Starter Assets/Presets/XRI Default Right Controller.preset";
    const string InteractionLayerSettingsPath = "Assets/XRI/Settings/Resources/InteractionLayerSettings.asset";
    const string XrGeneralSettingsAssetPath = "Assets/XR/XRGeneralSettingsPerBuildTarget.asset";
    const string OpenXrLoaderTypeName = "UnityEngine.XR.OpenXR.OpenXRLoader";
    const float ComfortableCameraYOffset = 1.36144f;
    const float ComfortableMoveSpeed = 2.5f;
    const float ComfortableTurnSpeed = 60f;
    const float ComfortableSnapTurnAmount = 45f;
    const float ComfortableSnapTurnDebounce = 0.5f;
    const float ComfortableSpawnEyeHeight = 1.65f;

    static readonly string[] ScenePaths =
    {
        "Assets/Scenes/Modul1.unity",
        "Assets/Scenes/Modul2_Guvenlik.unity",
        "Assets/Scenes/Mod\\u00fcl3_Triyaj.unity".Replace("\\u00fc", "\u00fc"),
    };

    static int s_GrabPhysicsLayer;
    static int s_TeleportPhysicsLayer;
    static int s_DefaultInteractionMask;
    static int s_GrabInteractionMask;
    static int s_TeleportInteractionMask;
    static int s_RayInteractionMask;
    static InputActionAsset s_InputActions;

    [MenuItem("Tools/XR/Apply Quest XRI Standard")]
    public static void ApplyFromMenu()
    {
        Apply();
    }

    public static void Apply()
    {
        s_InputActions = AssetDatabase.LoadAssetAtPath<InputActionAsset>(InputActionsPath);
        if (s_InputActions == null)
            throw new InvalidOperationException($"Missing input actions asset at '{InputActionsPath}'.");

        ConfigureProjectSettings();

        foreach (var scenePath in ScenePaths)
            StandardizeScene(scenePath);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("Quest XRI standardization complete.");
    }

    static void ConfigureProjectSettings()
    {
        EnsureTagsAndPhysicsLayers();
        EnsureInteractionLayers();
        EnsureBuildSettings();
        EnsureXrManagementSettings();
    }

    static void EnsureTagsAndPhysicsLayers()
    {
        var tagManagerAsset = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset").FirstOrDefault();
        if (tagManagerAsset == null)
            throw new InvalidOperationException("Unable to load TagManager.asset.");

        var serializedObject = new SerializedObject(tagManagerAsset);
        EnsureTag(serializedObject.FindProperty("tags"), "Player");
        s_GrabPhysicsLayer = EnsureLayer(serializedObject.FindProperty("layers"), "Grab", 8);
        s_TeleportPhysicsLayer = EnsureLayer(serializedObject.FindProperty("layers"), "Teleport", 9);
        serializedObject.ApplyModifiedPropertiesWithoutUndo();
        AssetDatabase.SaveAssets();
    }

    static void EnsureInteractionLayers()
    {
        var layerSettingsAsset = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(InteractionLayerSettingsPath);
        if (layerSettingsAsset == null)
            throw new InvalidOperationException($"Missing interaction layer settings asset at '{InteractionLayerSettingsPath}'.");

        var serializedObject = new SerializedObject(layerSettingsAsset);
        var namesProperty = serializedObject.FindProperty("m_LayerNames");
        namesProperty.arraySize = 32;
        namesProperty.GetArrayElementAtIndex(0).stringValue = "Default";
        namesProperty.GetArrayElementAtIndex(1).stringValue = "Grab";
        namesProperty.GetArrayElementAtIndex(2).stringValue = "Teleport";
        namesProperty.GetArrayElementAtIndex(3).stringValue = "UI";
        serializedObject.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(layerSettingsAsset);

        s_DefaultInteractionMask = InteractionLayerMask.GetMask("Default");
        s_GrabInteractionMask = InteractionLayerMask.GetMask("Grab");
        s_TeleportInteractionMask = InteractionLayerMask.GetMask("Teleport");
        s_RayInteractionMask = InteractionLayerMask.GetMask("Default", "Grab", "UI");
    }

    static void EnsureBuildSettings()
    {
        EditorBuildSettings.scenes = ScenePaths
            .Select(path => new EditorBuildSettingsScene(path, true))
            .ToArray();
    }

    static void EnsureXrManagementSettings()
    {
        var perBuildTarget = GetOrCreatePerBuildTargetSettings();
        DisableBuildTargetXrSettings(perBuildTarget, BuildTargetGroup.Standalone);
        EnsureBuildTargetXrSettings(perBuildTarget, BuildTargetGroup.Android);
        EditorUtility.SetDirty(perBuildTarget);
        AssetDatabase.SaveAssets();
    }

    static XRGeneralSettingsPerBuildTarget GetOrCreatePerBuildTargetSettings()
    {
        if (EditorBuildSettings.TryGetConfigObject(XRGeneralSettings.k_SettingsKey, out XRGeneralSettingsPerBuildTarget settings) &&
            settings != null)
        {
            return settings;
        }

        var guids = AssetDatabase.FindAssets("t:XRGeneralSettingsPerBuildTarget");
        foreach (var guid in guids)
        {
            var assetPath = AssetDatabase.GUIDToAssetPath(guid);
            settings = AssetDatabase.LoadAssetAtPath<XRGeneralSettingsPerBuildTarget>(assetPath);
            if (settings == null)
                continue;

            EditorBuildSettings.AddConfigObject(XRGeneralSettings.k_SettingsKey, settings, true);
            return settings;
        }

        EnsureFolder("Assets/XR");
        settings = ScriptableObject.CreateInstance<XRGeneralSettingsPerBuildTarget>();
        AssetDatabase.CreateAsset(settings, XrGeneralSettingsAssetPath);
        AssetDatabase.SaveAssets();
        EditorBuildSettings.AddConfigObject(XRGeneralSettings.k_SettingsKey, settings, true);
        return settings;
    }

    static void EnsureFolder(string assetPath)
    {
        if (AssetDatabase.IsValidFolder(assetPath))
            return;

        var parts = assetPath.Split('/');
        var currentPath = parts[0];
        for (var i = 1; i < parts.Length; i++)
        {
            var nextPath = $"{currentPath}/{parts[i]}";
            if (!AssetDatabase.IsValidFolder(nextPath))
                AssetDatabase.CreateFolder(currentPath, parts[i]);

            currentPath = nextPath;
        }
    }

    static void EnsureBuildTargetXrSettings(XRGeneralSettingsPerBuildTarget perBuildTarget, BuildTargetGroup buildTargetGroup)
    {
        if (!perBuildTarget.HasSettingsForBuildTarget(buildTargetGroup))
            perBuildTarget.CreateDefaultSettingsForBuildTarget(buildTargetGroup);

        if (!perBuildTarget.HasManagerSettingsForBuildTarget(buildTargetGroup))
            perBuildTarget.CreateDefaultManagerSettingsForBuildTarget(buildTargetGroup);

        var generalSettings = perBuildTarget.SettingsForBuildTarget(buildTargetGroup);
        var managerSettings = perBuildTarget.ManagerSettingsForBuildTarget(buildTargetGroup);

        generalSettings.AssignedSettings = managerSettings;
        generalSettings.InitManagerOnStart = true;
        managerSettings.automaticLoading = true;
        managerSettings.automaticRunning = true;
        XRPackageMetadataStore.AssignLoader(managerSettings, OpenXrLoaderTypeName, buildTargetGroup);

        ConfigureOpenXrFeatures(buildTargetGroup);

        EditorUtility.SetDirty(generalSettings);
        EditorUtility.SetDirty(managerSettings);
    }

    static void DisableBuildTargetXrSettings(XRGeneralSettingsPerBuildTarget perBuildTarget, BuildTargetGroup buildTargetGroup)
    {
        if (!perBuildTarget.HasSettingsForBuildTarget(buildTargetGroup))
            return;

        var generalSettings = perBuildTarget.SettingsForBuildTarget(buildTargetGroup);
        if (generalSettings != null)
        {
            generalSettings.InitManagerOnStart = false;
            EditorUtility.SetDirty(generalSettings);
        }

        if (!perBuildTarget.HasManagerSettingsForBuildTarget(buildTargetGroup))
            return;

        var managerSettings = perBuildTarget.ManagerSettingsForBuildTarget(buildTargetGroup);
        if (managerSettings == null)
            return;

        managerSettings.automaticLoading = false;
        managerSettings.automaticRunning = false;

        var serializedObject = new SerializedObject(managerSettings);
        var loadersProperty = serializedObject.FindProperty("m_Loaders");
        if (loadersProperty != null)
        {
            loadersProperty.ClearArray();
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
        }

        EditorUtility.SetDirty(managerSettings);
    }

    static void ConfigureOpenXrFeatures(BuildTargetGroup buildTargetGroup)
    {
        var settings = OpenXRSettings.GetSettingsForBuildTargetGroup(buildTargetGroup);
        if (settings == null)
        {
            Debug.LogWarning($"OpenXR settings missing for {buildTargetGroup}.");
            return;
        }

        EnableFeature<OculusTouchControllerProfile>(settings, true);
        if (buildTargetGroup == BuildTargetGroup.Android)
            EnableFeature<MetaQuestFeature>(settings, true);
    }

    static void EnableFeature<T>(OpenXRSettings settings, bool enabled) where T : UnityEngine.XR.OpenXR.Features.OpenXRFeature
    {
        var feature = settings.GetFeature<T>();
        if (feature == null)
        {
            Debug.LogWarning($"OpenXR feature '{typeof(T).Name}' is missing in {settings.name}.");
            return;
        }

        feature.enabled = enabled;
        EditorUtility.SetDirty(feature);
    }

    static void StandardizeScene(string scenePath)
    {
        var scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
        var xrOrigin = FindFirstInScene<XROrigin>(scene);
        if (xrOrigin == null)
            throw new InvalidOperationException($"No XR Origin found in scene '{scenePath}'.");

        var inputActionManager = EnsureSingleInputActionManager(scene);
        EnsureSingleComponentInScene<XRInteractionManager>(scene, "XR Interaction Manager");

        EnsureSpawnPoint(scene, xrOrigin);
        CleanupCamerasAndAudio(scene, xrOrigin);
        StandardizeRig(xrOrigin);
        OptimizeSceneErgonomics(scene, xrOrigin);

        var playerPresence = EnsurePlayerPresence(xrOrigin);
        var playerBackPoint = EnsurePlayerBackPoint(playerPresence);

        StandardizeControllers(scene);
        StandardizeTeleportSurfaces(scene, xrOrigin);
        StandardizeSceneScripts(scene, xrOrigin, playerPresence, playerBackPoint);

        EditorUtility.SetDirty(inputActionManager);
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
    }

    static InputActionManager EnsureSingleInputActionManager(Scene scene)
    {
        var managers = FindInScene<InputActionManager>(scene);
        InputActionManager inputActionManager;
        if (managers.Count == 0)
        {
            var go = new GameObject("Input Action Manager");
            inputActionManager = go.AddComponent<InputActionManager>();
        }
        else
        {
            inputActionManager = managers[0];
            for (var i = 1; i < managers.Count; i++)
                UnityEngine.Object.DestroyImmediate(managers[i]);
        }

        inputActionManager.actionAssets = new List<InputActionAsset> { s_InputActions };
        return inputActionManager;
    }

    static T EnsureSingleComponentInScene<T>(Scene scene, string objectName) where T : Component
    {
        var components = FindInScene<T>(scene);
        if (components.Count == 0)
        {
            var go = new GameObject(objectName);
            return go.AddComponent<T>();
        }

        var primary = components[0];
        for (var i = 1; i < components.Count; i++)
            UnityEngine.Object.DestroyImmediate(components[i]);

        return primary;
    }

    static void EnsureSpawnPoint(Scene scene, XROrigin xrOrigin)
    {
        var existingMarker = FindNamedTransform(scene, "VRSpawnPoint")
            ?? FindNamedTransform(scene, "VR_SpawnPoint")
            ?? FindNamedTransform(scene, "PlayerSpawn");

        GameObject spawnObject;
        if (existingMarker != null)
        {
            spawnObject = existingMarker.gameObject;
        }
        else
        {
            spawnObject = new GameObject("VRSpawnPoint");
            spawnObject.transform.position = xrOrigin.transform.position;
            spawnObject.transform.rotation = xrOrigin.transform.rotation;
        }

        var xrRigPositioner = xrOrigin.GetComponent<XRRigPositioner>();
        if (xrRigPositioner != null)
        {
            spawnObject.transform.position = xrRigPositioner.modul1SeatPosition;
            spawnObject.transform.rotation = Quaternion.Euler(0f, xrRigPositioner.modul1RotationY, 0f);
            UnityEngine.Object.DestroyImmediate(xrRigPositioner);
        }

        var spawnPoint = spawnObject.GetComponent<VRSpawnPoint>();
        if (spawnPoint == null)
            spawnPoint = spawnObject.AddComponent<VRSpawnPoint>();

        var serializedObject = new SerializedObject(spawnPoint);
        serializedObject.FindProperty("xrOrigin").objectReferenceValue = xrOrigin;
        serializedObject.FindProperty("useThisObjectPosition").boolValue = true;
        serializedObject.FindProperty("spawnYRotation").floatValue = spawnObject.transform.eulerAngles.y;
        serializedObject.FindProperty("reapplyFrameCount").intValue = 2;
        serializedObject.ApplyModifiedPropertiesWithoutUndo();
    }

    static void CleanupCamerasAndAudio(Scene scene, XROrigin xrOrigin)
    {
        var xrCamera = xrOrigin.Camera != null ? xrOrigin.Camera.GetComponent<Camera>() : null;
        var cameras = FindInScene<Camera>(scene);

        foreach (var camera in cameras)
        {
            if (camera == xrCamera)
            {
                camera.tag = "MainCamera";
                camera.gameObject.SetActive(true);
                if (camera.GetComponent<AudioListener>() == null)
                    camera.gameObject.AddComponent<AudioListener>();
                continue;
            }

            camera.tag = "Untagged";
            var listener = camera.GetComponent<AudioListener>();
            if (listener != null)
                listener.enabled = false;
            camera.gameObject.SetActive(false);
        }

        foreach (var listener in FindInScene<AudioListener>(scene))
            listener.enabled = xrCamera != null && listener.gameObject == xrCamera.gameObject;
    }

    static void StandardizeRig(XROrigin xrOrigin)
    {
        xrOrigin.RequestedTrackingOriginMode = XROrigin.TrackingOriginMode.NotSpecified;
        xrOrigin.CameraYOffset = ComfortableCameraYOffset;

        var cameraOffsetTransform = xrOrigin.CameraFloorOffsetObject != null
            ? xrOrigin.CameraFloorOffsetObject.transform
            : null;
        if (cameraOffsetTransform != null)
            cameraOffsetTransform.localPosition = new Vector3(0f, ComfortableCameraYOffset, 0f);

        var playerBodyTracking = xrOrigin.GetComponent<PlayerBodyTracking>();
        if (playerBodyTracking != null)
            UnityEngine.Object.DestroyImmediate(playerBodyTracking);

        var rigidbody = xrOrigin.GetComponent<Rigidbody>();
        if (rigidbody != null)
            UnityEngine.Object.DestroyImmediate(rigidbody);

        var capsuleCollider = xrOrigin.GetComponent<CapsuleCollider>();
        if (capsuleCollider != null)
            UnityEngine.Object.DestroyImmediate(capsuleCollider);

        var characterController = xrOrigin.GetComponent<CharacterController>();
        if (characterController == null)
            characterController = xrOrigin.gameObject.AddComponent<CharacterController>();

        characterController.radius = 0.25f;
        characterController.height = 1.75f;
        characterController.center = new Vector3(0f, 0.9f, 0f);
        characterController.skinWidth = 0.02f;
        characterController.stepOffset = 0.2f;
        characterController.slopeLimit = 60f;
        characterController.minMoveDistance = 0f;

        var locomotionSystem = xrOrigin.GetComponent<LocomotionSystem>();
        if (locomotionSystem == null)
            locomotionSystem = xrOrigin.gameObject.AddComponent<LocomotionSystem>();
        locomotionSystem.xrOrigin = xrOrigin;

        var moveProvider = xrOrigin.GetComponent<ActionBasedContinuousMoveProvider>();
        if (moveProvider == null)
            moveProvider = xrOrigin.gameObject.AddComponent<ActionBasedContinuousMoveProvider>();
        moveProvider.system = locomotionSystem;
        moveProvider.forwardSource = xrOrigin.Camera != null ? xrOrigin.Camera.transform : xrOrigin.transform;
        moveProvider.moveSpeed = ComfortableMoveSpeed;
        moveProvider.enableStrafe = true;
        moveProvider.useGravity = true;
        moveProvider.gravityApplicationMode = ContinuousMoveProviderBase.GravityApplicationMode.Immediately;
        moveProvider.leftHandMoveAction = new InputActionProperty(FindActionReference("XRI LeftHand Locomotion", "Move"));
        moveProvider.rightHandMoveAction = new InputActionProperty(FindActionReference("XRI RightHand Locomotion", "Move"));

        var turnProvider = xrOrigin.GetComponent<ActionBasedContinuousTurnProvider>();
        if (turnProvider == null)
            turnProvider = xrOrigin.gameObject.AddComponent<ActionBasedContinuousTurnProvider>();
        turnProvider.system = locomotionSystem;
        turnProvider.turnSpeed = ComfortableTurnSpeed;
        turnProvider.leftHandTurnAction = new InputActionProperty(FindActionReference("XRI LeftHand Locomotion", "Turn"));
        turnProvider.rightHandTurnAction = new InputActionProperty(FindActionReference("XRI RightHand Locomotion", "Turn"));

        var snapTurnProvider = xrOrigin.GetComponent<ActionBasedSnapTurnProvider>();
        if (snapTurnProvider == null)
            snapTurnProvider = xrOrigin.gameObject.AddComponent<ActionBasedSnapTurnProvider>();
        snapTurnProvider.system = locomotionSystem;
        snapTurnProvider.turnAmount = ComfortableSnapTurnAmount;
        snapTurnProvider.debounceTime = ComfortableSnapTurnDebounce;
        snapTurnProvider.delayTime = 0f;
        snapTurnProvider.enableTurnLeftRight = true;
        snapTurnProvider.enableTurnAround = true;
        snapTurnProvider.leftHandSnapTurnAction = new InputActionProperty(FindActionReference("XRI LeftHand Locomotion", "Snap Turn"));
        snapTurnProvider.rightHandSnapTurnAction = new InputActionProperty(FindActionReference("XRI RightHand Locomotion", "Snap Turn"));

        var teleportationProvider = xrOrigin.GetComponent<TeleportationProvider>();
        if (teleportationProvider == null)
            teleportationProvider = xrOrigin.gameObject.AddComponent<TeleportationProvider>();
        teleportationProvider.system = locomotionSystem;
        teleportationProvider.delayTime = 0f;

        var driver = xrOrigin.GetComponent<CharacterControllerDriver>();
        if (driver == null)
            driver = xrOrigin.gameObject.AddComponent<CharacterControllerDriver>();
        driver.locomotionProvider = moveProvider;
        driver.minHeight = 1f;
        driver.maxHeight = 2.2f;
    }

    static void OptimizeSceneErgonomics(Scene scene, XROrigin xrOrigin)
    {
        if (scene.name != "Mod\u00fcl3_Triyaj")
            return;

        var spawnTransform = FindNamedTransform(scene, "VR_SpawnPoint")
            ?? FindNamedTransform(scene, "VRSpawnPoint")
            ?? FindNamedTransform(scene, "PlayerSpawn");
        var planeTransform = FindNamedTransform(scene, "Plane");

        if (spawnTransform == null || planeTransform == null)
            return;

        var targetPosition = spawnTransform.position;
        targetPosition.x = planeTransform.position.x;
        targetPosition.y = planeTransform.position.y + ComfortableSpawnEyeHeight;
        spawnTransform.position = targetPosition;
        spawnTransform.rotation = Quaternion.Euler(0f, spawnTransform.eulerAngles.y, 0f);

        var spawnPoint = spawnTransform.GetComponent<VRSpawnPoint>();
        if (spawnPoint == null)
            return;

        var serializedObject = new SerializedObject(spawnPoint);
        serializedObject.FindProperty("spawnYRotation").floatValue = spawnTransform.eulerAngles.y;
        serializedObject.ApplyModifiedPropertiesWithoutUndo();
    }

    static Transform EnsurePlayerPresence(XROrigin xrOrigin)
    {
        var playerPresence = FindChildTransform(xrOrigin.transform, "PlayerPresence");
        if (playerPresence == null)
        {
            var playerPresenceObject = new GameObject("PlayerPresence");
            playerPresence = playerPresenceObject.transform;
            playerPresence.SetParent(xrOrigin.transform, false);
        }

        playerPresence.tag = "Player";

        var playerPresenceComponent = playerPresence.GetComponent<PlayerPresence>();
        if (playerPresenceComponent == null)
            playerPresenceComponent = playerPresence.gameObject.AddComponent<PlayerPresence>();

        var serializedObject = new SerializedObject(playerPresenceComponent);
        serializedObject.FindProperty("xrOrigin").objectReferenceValue = xrOrigin;
        serializedObject.ApplyModifiedPropertiesWithoutUndo();

        var capsule = playerPresence.GetComponent<CapsuleCollider>();
        if (capsule == null)
            capsule = playerPresence.gameObject.AddComponent<CapsuleCollider>();
        capsule.isTrigger = true;
        capsule.direction = 1;

        var rigidbody = playerPresence.GetComponent<Rigidbody>();
        if (rigidbody == null)
            rigidbody = playerPresence.gameObject.AddComponent<Rigidbody>();
        rigidbody.isKinematic = true;
        rigidbody.useGravity = false;

        return playerPresence;
    }

    static Transform EnsurePlayerBackPoint(Transform playerPresence)
    {
        var backPoint = FindChildTransform(playerPresence, "PlayerBackPoint");
        if (backPoint == null)
        {
            var backPointObject = new GameObject("PlayerBackPoint");
            backPoint = backPointObject.transform;
            backPoint.SetParent(playerPresence, false);
        }

        backPoint.localPosition = new Vector3(0f, 1.2f, -0.35f);
        backPoint.localRotation = Quaternion.identity;
        return backPoint;
    }

    static void StandardizeControllers(Scene scene)
    {
        var leftController = FindNamedTransform(scene, "Left Controller");
        var rightController = FindNamedTransform(scene, "Right Controller");

        if (leftController != null)
            StandardizeController(leftController, true);
        if (rightController != null)
            StandardizeController(rightController, false);
    }

    static void StandardizeController(Transform controllerRoot, bool isLeftHand)
    {
        var actionBasedController = controllerRoot.GetComponent<ActionBasedController>();
        if (actionBasedController == null)
            throw new InvalidOperationException($"ActionBasedController missing on '{controllerRoot.name}'.");

        ApplyControllerPreset(actionBasedController, isLeftHand);
        ConfigureActionBasedController(actionBasedController, controllerRoot, isLeftHand);

        RemoveRootRayComponents(controllerRoot);

        var directObject = EnsureInteractorChild(controllerRoot, "Direct Interactor", DirectInteractorPrefabPath, Vector3.zero, Quaternion.identity, true);
        var rayObject = EnsureInteractorChild(controllerRoot, "Ray Interactor", RayInteractorPrefabPath, Vector3.zero, Quaternion.identity, true);
        var teleportObject = EnsureInteractorChild(controllerRoot, "Teleport Interactor", TeleportInteractorPrefabPath, Vector3.zero, Quaternion.identity, false);

        var directInteractor = directObject.GetComponent<XRDirectInteractor>();
        var rayInteractor = rayObject.GetComponent<XRRayInteractor>();
        var teleportInteractor = teleportObject.GetComponent<XRRayInteractor>();

        directInteractor.interactionLayers = s_GrabInteractionMask;
        directInteractor.physicsLayerMask = 1 << s_GrabPhysicsLayer;

        rayInteractor.interactionLayers = s_RayInteractionMask;
        rayInteractor.raycastMask = (1 << s_GrabPhysicsLayer) | (1 << s_TeleportPhysicsLayer) | (1 << 5);
        rayInteractor.enableUIInteraction = true;

        teleportInteractor.interactionLayers = s_TeleportInteractionMask;
        teleportInteractor.raycastMask = 1 << s_TeleportPhysicsLayer;
        teleportInteractor.enableUIInteraction = false;

        var interactionGroup = controllerRoot.GetComponent<XRInteractionGroup>();
        if (interactionGroup == null)
            interactionGroup = controllerRoot.gameObject.AddComponent<XRInteractionGroup>();
        interactionGroup.startingGroupMembers = new List<UnityEngine.Object> { directInteractor, rayInteractor, teleportInteractor };

        var controllerManager = controllerRoot.GetComponent<ActionBasedControllerManager>();
        if (controllerManager == null)
            controllerManager = controllerRoot.gameObject.AddComponent<ActionBasedControllerManager>();

        ConfigureControllerManager(controllerManager, interactionGroup, directInteractor, rayInteractor, teleportInteractor, isLeftHand);
    }

    static void ApplyControllerPreset(ActionBasedController controller, bool isLeftHand)
    {
        var presetPath = isLeftHand ? LeftControllerPresetPath : RightControllerPresetPath;
        var preset = AssetDatabase.LoadAssetAtPath<Preset>(presetPath);
        if (preset == null)
        {
            Debug.LogWarning($"Missing controller preset at '{presetPath}'.");
            return;
        }

        preset.ApplyTo(controller);
    }

    static void ConfigureActionBasedController(ActionBasedController controller, Transform controllerRoot, bool isLeftHand)
    {
        var modelPrefabPath = isLeftHand ? LeftControllerModelPath : RightControllerModelPath;
        var modelPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(modelPrefabPath);
        if (modelPrefab == null)
            throw new InvalidOperationException($"Missing controller model prefab at '{modelPrefabPath}'.");

        var visualParent = EnsureControllerVisualParent(controllerRoot, isLeftHand);

        var serializedObject = new SerializedObject(controller);
        serializedObject.FindProperty("m_UpdateTrackingType").intValue = 0;
        serializedObject.FindProperty("m_EnableInputTracking").boolValue = true;
        serializedObject.FindProperty("m_EnableInputActions").boolValue = true;
        serializedObject.FindProperty("m_ModelPrefab").objectReferenceValue = modelPrefab;
        serializedObject.FindProperty("m_ModelParent").objectReferenceValue = visualParent;
        serializedObject.ApplyModifiedPropertiesWithoutUndo();
    }

    static Transform EnsureControllerVisualParent(Transform controllerRoot, bool isLeftHand)
    {
        var childName = isLeftHand ? "Controller Visual Left" : "Controller Visual Right";
        var visualParent = FindChildTransform(controllerRoot, childName);
        if (visualParent == null)
        {
            var go = new GameObject(childName);
            visualParent = go.transform;
            visualParent.SetParent(controllerRoot, false);
        }

        visualParent.localPosition = new Vector3(isLeftHand ? -0.012f : 0.012f, -0.012f, 0.018f);
        visualParent.localRotation = Quaternion.identity;
        visualParent.localScale = Vector3.one;
        return visualParent;
    }

    static void RemoveRootRayComponents(Transform controllerRoot)
    {
        var rootRay = controllerRoot.GetComponent<XRRayInteractor>();
        if (rootRay != null)
            UnityEngine.Object.DestroyImmediate(rootRay);

        var rootLineVisual = controllerRoot.GetComponent<XRInteractorLineVisual>();
        if (rootLineVisual != null)
            UnityEngine.Object.DestroyImmediate(rootLineVisual);

        var rootLineRenderer = controllerRoot.GetComponent<LineRenderer>();
        if (rootLineRenderer != null)
            UnityEngine.Object.DestroyImmediate(rootLineRenderer);
    }

    static GameObject EnsureInteractorChild(Transform controllerRoot, string childName, string prefabPath, Vector3 localPosition, Quaternion localRotation, bool active)
    {
        var existing = FindChildTransform(controllerRoot, childName);
        GameObject interactorObject;
        if (existing != null)
        {
            interactorObject = existing.gameObject;
        }
        else
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (prefab == null)
                throw new InvalidOperationException($"Missing interactor prefab at '{prefabPath}'.");

            interactorObject = PrefabUtility.InstantiatePrefab(prefab) as GameObject;
            if (interactorObject == null)
                throw new InvalidOperationException($"Could not instantiate prefab '{prefabPath}'.");

            interactorObject.name = childName;
            interactorObject.transform.SetParent(controllerRoot, false);
        }

        interactorObject.transform.localPosition = localPosition;
        interactorObject.transform.localRotation = localRotation;
        interactorObject.SetActive(active);
        return interactorObject;
    }

    static void ConfigureControllerManager(
        ActionBasedControllerManager controllerManager,
        XRInteractionGroup interactionGroup,
        XRDirectInteractor directInteractor,
        XRRayInteractor rayInteractor,
        XRRayInteractor teleportInteractor,
        bool isLeftHand)
    {
        var interactionMap = isLeftHand ? "XRI LeftHand Interaction" : "XRI RightHand Interaction";
        var locomotionMap = isLeftHand ? "XRI LeftHand Locomotion" : "XRI RightHand Locomotion";

        var serializedObject = new SerializedObject(controllerManager);
        serializedObject.FindProperty("m_ManipulationInteractionGroup").objectReferenceValue = interactionGroup;
        serializedObject.FindProperty("m_DirectInteractor").objectReferenceValue = directInteractor;
        serializedObject.FindProperty("m_RayInteractor").objectReferenceValue = rayInteractor;
        serializedObject.FindProperty("m_TeleportInteractor").objectReferenceValue = teleportInteractor;
        serializedObject.FindProperty("m_TeleportModeActivate").objectReferenceValue = FindActionReference(locomotionMap, "Teleport Mode Activate");
        serializedObject.FindProperty("m_TeleportModeCancel").objectReferenceValue = FindActionReference(locomotionMap, "Teleport Mode Cancel");
        serializedObject.FindProperty("m_Turn").objectReferenceValue = FindActionReference(locomotionMap, "Turn");
        serializedObject.FindProperty("m_SnapTurn").objectReferenceValue = FindActionReference(locomotionMap, "Snap Turn");
        serializedObject.FindProperty("m_Move").objectReferenceValue = FindActionReference(locomotionMap, "Move");
        serializedObject.FindProperty("m_UIScroll").objectReferenceValue = FindActionReference(interactionMap, "UI Scroll");
        serializedObject.FindProperty("m_SmoothMotionEnabled").boolValue = true;
        serializedObject.FindProperty("m_SmoothTurnEnabled").boolValue = false;
        serializedObject.FindProperty("m_UIScrollingEnabled").boolValue = true;
        serializedObject.ApplyModifiedPropertiesWithoutUndo();
    }

    static void StandardizeTeleportSurfaces(Scene scene, XROrigin xrOrigin)
    {
        var teleportationProvider = xrOrigin.GetComponent<TeleportationProvider>();
        if (teleportationProvider == null)
            return;

        var teleportSurfaceNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        if (scene.name == "Modul1")
        {
            teleportSurfaceNames.UnionWith(new[] { "zeminTavanDuvar" });
        }
        else if (scene.name == "Modul2_Guvenlik")
        {
            teleportSurfaceNames.UnionWith(new[] { "Road_Segment", "Road_Heaved", "Road_Crack", "Roads", "Ground", "Ground_Base" });
        }
        else if (scene.name == "Mod\u00fcl3_Triyaj")
        {
            teleportSurfaceNames.UnionWith(new[] { "Zemin", "Plane" });
        }

        foreach (var candidate in EnumerateNamedTransforms(scene, teleportSurfaceNames))
        {
            var collider = EnsureCollider(candidate.gameObject);
            if (collider == null)
                continue;

            candidate.gameObject.layer = s_TeleportPhysicsLayer;

            var teleportArea = candidate.GetComponent<TeleportationArea>();
            if (teleportArea == null)
                teleportArea = candidate.gameObject.AddComponent<TeleportationArea>();

            teleportArea.teleportationProvider = teleportationProvider;
            teleportArea.interactionLayers = s_TeleportInteractionMask;
            teleportArea.filterSelectionByHitNormal = true;
            teleportArea.upNormalToleranceDegrees = 60f;
        }
    }

    static Collider EnsureCollider(GameObject gameObject)
    {
        var collider = gameObject.GetComponent<Collider>();
        if (collider != null)
            return collider;

        var meshFilter = gameObject.GetComponent<MeshFilter>();
        if (meshFilter != null && meshFilter.sharedMesh != null)
            return gameObject.AddComponent<MeshCollider>();

        return null;
    }

    static void StandardizeSceneScripts(Scene scene, XROrigin xrOrigin, Transform playerPresence, Transform playerBackPoint)
    {
        foreach (var equipmentManager in FindInScene<EquipmentManager>(scene))
        {
            var serializedObject = new SerializedObject(equipmentManager);
            serializedObject.FindProperty("playerRoot").objectReferenceValue = xrOrigin.transform;
            serializedObject.FindProperty("headAnchor").objectReferenceValue = xrOrigin.Camera != null ? xrOrigin.Camera.transform : null;
            serializedObject.FindProperty("backAnchor").objectReferenceValue = playerBackPoint;
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
            StandardizeGrabbable(equipmentManager.gameObject);
        }

        foreach (var grabbable in FindInScene<VRGrabbable>(scene))
            StandardizeGrabbable(grabbable.gameObject);

        foreach (var portal in FindInScene<ModulePortal>(scene))
        {
            portal.gameObject.layer = 5;
            var collider = EnsureCollider(portal.gameObject);
            if (collider != null)
                collider.isTrigger = true;

            var interactable = portal.GetComponent<XRSimpleInteractable>();
            if (interactable == null)
                interactable = portal.gameObject.AddComponent<XRSimpleInteractable>();
            interactable.interactionLayers = s_DefaultInteractionMask;
        }

        foreach (var victim in FindInScene<YaraliController>(scene))
        {
            victim.gameObject.layer = s_GrabPhysicsLayer;
            var interactable = victim.GetComponent<XRSimpleInteractable>();
            if (interactable == null)
                interactable = victim.gameObject.AddComponent<XRSimpleInteractable>();
            interactable.interactionLayers = s_GrabInteractionMask;

            var serializedObject = new SerializedObject(victim);
            serializedObject.FindProperty("backAnchor").objectReferenceValue = playerBackPoint;
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
        }

        foreach (var safeZone in FindInScene<SafeZone>(scene))
        {
            var serializedObject = new SerializedObject(safeZone);
            serializedObject.FindProperty("playerPresence").objectReferenceValue = playerPresence;
            serializedObject.FindProperty("backAnchor").objectReferenceValue = playerBackPoint;
            serializedObject.ApplyModifiedPropertiesWithoutUndo();

            var collider = safeZone.GetComponent<Collider>();
            if (collider != null)
                collider.isTrigger = true;
        }

        if (scene.name == "Modul1")
            EnsureModul1Ui(scene);

        if (scene.name == "Modul2_Guvenlik")
            EnsureModul2TestGrabbable(scene, xrOrigin);
    }

    static void StandardizeGrabbable(GameObject gameObject)
    {
        gameObject.layer = s_GrabPhysicsLayer;

        var rigidbody = gameObject.GetComponent<Rigidbody>();
        if (rigidbody == null)
            rigidbody = gameObject.AddComponent<Rigidbody>();
        rigidbody.useGravity = true;

        var collider = gameObject.GetComponent<Collider>();
        if (collider == null)
            collider = gameObject.AddComponent<BoxCollider>();

        var interactable = gameObject.GetComponent<XRGrabInteractable>();
        if (interactable == null)
            interactable = gameObject.AddComponent<XRGrabInteractable>();

        interactable.interactionLayers = s_GrabInteractionMask;
        interactable.movementType = XRBaseInteractable.MovementType.VelocityTracking;
        interactable.throwOnDetach = true;
        interactable.throwVelocityScale = 1.2f;
        interactable.throwAngularVelocityScale = 1f;

        if (interactable.attachTransform == null)
        {
            var attachPoint = FindChildTransform(gameObject.transform, "Attach Point");
            if (attachPoint == null)
            {
                var attachObject = new GameObject("Attach Point");
                attachPoint = attachObject.transform;
                attachPoint.SetParent(gameObject.transform, false);
            }

            interactable.attachTransform = attachPoint;
        }
    }

    static void EnsureModul1Ui(Scene scene)
    {
        var eventSystem = FindNamedTransform(scene, "EventSystem");
        if (eventSystem != null)
        {
            if (eventSystem.GetComponent<XRUIInputModule>() == null)
                eventSystem.gameObject.AddComponent<XRUIInputModule>();

            var standaloneInputModule = eventSystem.GetComponent<UnityEngine.EventSystems.StandaloneInputModule>();
            if (standaloneInputModule != null)
                UnityEngine.Object.DestroyImmediate(standaloneInputModule);
        }

        foreach (var canvas in FindInScene<Canvas>(scene))
        {
            if (canvas.renderMode == RenderMode.WorldSpace && canvas.GetComponent<TrackedDeviceGraphicRaycaster>() == null)
                canvas.gameObject.AddComponent<TrackedDeviceGraphicRaycaster>();
        }
    }

    static void EnsureModul2TestGrabbable(Scene scene, XROrigin xrOrigin)
    {
        if (FindInScene<VRGrabbable>(scene).Count > 0)
            return;

        var referenceTransform = xrOrigin.Camera != null ? xrOrigin.Camera.transform : xrOrigin.transform;
        var forward = referenceTransform.forward;
        forward.y = 0f;
        if (forward.sqrMagnitude < 0.001f)
            forward = xrOrigin.transform.forward;
        forward.Normalize();

        var testObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
        testObject.name = "VR_Test_Equipment";
        testObject.transform.position = referenceTransform.position + forward * 1.2f + Vector3.up * 0.4f;
        testObject.transform.localScale = new Vector3(0.25f, 0.15f, 0.35f);
        testObject.AddComponent<VRGrabbable>();
        StandardizeGrabbable(testObject);
    }

    static InputActionReference FindActionReference(string mapName, string actionName)
    {
        var assets = AssetDatabase.LoadAllAssetsAtPath(InputActionsPath);
        for (var i = 0; i < assets.Length; i++)
        {
            if (assets[i] is InputActionReference reference &&
                reference.action != null &&
                reference.action.actionMap != null &&
                reference.action.actionMap.name == mapName &&
                reference.action.name == actionName)
            {
                return reference;
            }
        }

        throw new InvalidOperationException($"Input action reference not found for {mapName}/{actionName}.");
    }

    static void EnsureTag(SerializedProperty tagsProperty, string tagName)
    {
        for (var i = 0; i < tagsProperty.arraySize; i++)
        {
            if (tagsProperty.GetArrayElementAtIndex(i).stringValue == tagName)
                return;
        }

        for (var i = 0; i < tagsProperty.arraySize; i++)
        {
            var element = tagsProperty.GetArrayElementAtIndex(i);
            if (string.IsNullOrEmpty(element.stringValue))
            {
                element.stringValue = tagName;
                return;
            }
        }

        throw new InvalidOperationException($"Unable to add tag '{tagName}'.");
    }

    static int EnsureLayer(SerializedProperty layersProperty, string layerName, int preferredIndex)
    {
        for (var i = 0; i < layersProperty.arraySize; i++)
        {
            if (layersProperty.GetArrayElementAtIndex(i).stringValue == layerName)
                return i;
        }

        if (string.IsNullOrEmpty(layersProperty.GetArrayElementAtIndex(preferredIndex).stringValue))
        {
            layersProperty.GetArrayElementAtIndex(preferredIndex).stringValue = layerName;
            return preferredIndex;
        }

        for (var i = 8; i < layersProperty.arraySize; i++)
        {
            if (string.IsNullOrEmpty(layersProperty.GetArrayElementAtIndex(i).stringValue))
            {
                layersProperty.GetArrayElementAtIndex(i).stringValue = layerName;
                return i;
            }
        }

        throw new InvalidOperationException($"Unable to add layer '{layerName}'.");
    }

    static T FindFirstInScene<T>(Scene scene) where T : Component
    {
        return FindInScene<T>(scene).FirstOrDefault();
    }

    static List<T> FindInScene<T>(Scene scene) where T : Component
    {
        var results = new List<T>();
        foreach (var root in scene.GetRootGameObjects())
            results.AddRange(root.GetComponentsInChildren<T>(true));
        return results;
    }

    static Transform FindNamedTransform(Scene scene, string objectName)
    {
        foreach (var root in scene.GetRootGameObjects())
        {
            var match = FindChildTransform(root.transform, objectName);
            if (match != null)
                return match;
        }

        return null;
    }

    static IEnumerable<Transform> EnumerateNamedTransforms(Scene scene, HashSet<string> names)
    {
        var results = new List<Transform>();
        foreach (var root in scene.GetRootGameObjects())
        {
            foreach (var child in root.GetComponentsInChildren<Transform>(true))
            {
                if (names.Contains(child.name))
                    results.Add(child);
            }
        }

        return results;
    }

    static Transform FindChildTransform(Transform root, string objectName)
    {
        if (root == null)
            return null;

        if (root.name == objectName)
            return root;

        foreach (var child in root.GetComponentsInChildren<Transform>(true))
        {
            if (child.name == objectName)
                return child;
        }

        return null;
    }
}
#endif
