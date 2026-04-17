using UnityEditor;
using UnityEngine;

public static class XRDeviceSimulatorGlobalToggle
{
    private const string SettingsAssetPath = "Assets/XRI/Settings/Resources/XRDeviceSimulatorSettings.asset";
    private const string EnabledMenuPath = "Tools/XR Device Simulator/Enabled";
    private const string SelectAssetMenuPath = "Tools/XR Device Simulator/Select Settings Asset";

    [MenuItem(EnabledMenuPath)]
    private static void ToggleEnabled()
    {
        bool nextState = !GetSimulatorEnabled();
        if (TrySetSimulatorEnabled(nextState))
        {
            ApplyPlayModeState(nextState);
            Debug.Log($"[XR Simulator] Global durum: {(nextState ? "ACIK" : "KAPALI")} (tum sahneler).");
        }
    }

    [MenuItem(EnabledMenuPath, true)]
    private static bool ValidateEnabledMenu()
    {
        Menu.SetChecked(EnabledMenuPath, GetSimulatorEnabled());
        return true;
    }

    [MenuItem(SelectAssetMenuPath)]
    private static void SelectSettingsAsset()
    {
        ScriptableObject settingsAsset = LoadSettingsAsset();
        if (settingsAsset == null)
        {
            Debug.LogError("[XR Simulator] XRDeviceSimulatorSettings.asset bulunamadi.");
            return;
        }

        Selection.activeObject = settingsAsset;
        EditorGUIUtility.PingObject(settingsAsset);
    }

    private static bool GetSimulatorEnabled()
    {
        ScriptableObject settingsAsset = LoadSettingsAsset();
        if (settingsAsset == null)
        {
            return true;
        }

        SerializedObject serializedSettings = new SerializedObject(settingsAsset);
        SerializedProperty autoInstantiateProperty =
            serializedSettings.FindProperty("m_AutomaticallyInstantiateSimulatorPrefab");
        SerializedProperty editorOnlyProperty =
            serializedSettings.FindProperty("m_AutomaticallyInstantiateInEditorOnly");

        if (autoInstantiateProperty == null)
        {
            return true;
        }

        bool autoInstantiate = autoInstantiateProperty.boolValue;
        bool editorOnly = editorOnlyProperty == null || editorOnlyProperty.boolValue;
        return autoInstantiate && editorOnly;
    }

    private static bool TrySetSimulatorEnabled(bool enabled)
    {
        ScriptableObject settingsAsset = LoadSettingsAsset();
        if (settingsAsset == null)
        {
            Debug.LogError("[XR Simulator] XRDeviceSimulatorSettings.asset bulunamadi, toggle uygulanamadi.");
            return false;
        }

        SerializedObject serializedSettings = new SerializedObject(settingsAsset);
        SerializedProperty autoInstantiateProperty =
            serializedSettings.FindProperty("m_AutomaticallyInstantiateSimulatorPrefab");
        SerializedProperty editorOnlyProperty =
            serializedSettings.FindProperty("m_AutomaticallyInstantiateInEditorOnly");

        if (autoInstantiateProperty == null)
        {
            Debug.LogError("[XR Simulator] m_AutomaticallyInstantiateSimulatorPrefab alani bulunamadi.");
            return false;
        }

        autoInstantiateProperty.boolValue = enabled;
        if (editorOnlyProperty != null)
        {
            editorOnlyProperty.boolValue = true;
        }

        serializedSettings.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(settingsAsset);
        AssetDatabase.SaveAssets();
        return true;
    }

    private static ScriptableObject LoadSettingsAsset()
    {
        ScriptableObject settingsAsset = AssetDatabase.LoadAssetAtPath<ScriptableObject>(SettingsAssetPath);
        if (settingsAsset != null)
        {
            return settingsAsset;
        }

        string[] settingGuids = AssetDatabase.FindAssets("XRDeviceSimulatorSettings t:ScriptableObject");
        if (settingGuids.Length == 0)
        {
            return null;
        }

        string discoveredPath = AssetDatabase.GUIDToAssetPath(settingGuids[0]);
        return AssetDatabase.LoadAssetAtPath<ScriptableObject>(discoveredPath);
    }

    private static void ApplyPlayModeState(bool enabled)
    {
        if (!Application.isPlaying)
        {
            return;
        }

        System.Type simulatorType =
            System.Type.GetType("UnityEngine.XR.Interaction.Toolkit.Inputs.Simulation.XRDeviceSimulator, Unity.XR.Interaction.Toolkit");
        if (simulatorType == null)
        {
            return;
        }

        Object[] simulators = Resources.FindObjectsOfTypeAll(simulatorType);
        for (int i = 0; i < simulators.Length; i++)
        {
            if (!(simulators[i] is Component simulatorComponent))
            {
                continue;
            }

            GameObject simulatorObject = simulatorComponent.gameObject;
            if (!simulatorObject.scene.IsValid())
            {
                continue;
            }

            if (simulatorObject.activeSelf != enabled)
            {
                simulatorObject.SetActive(enabled);
            }
        }
    }
}
