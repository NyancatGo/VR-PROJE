using System.Reflection;
using UnityEngine;

public static class XRDeviceSimulatorRuntimeGate
{
    private const string SettingsResourceName = "XRDeviceSimulatorSettings";
    private const string AutoInstantiateFieldName = "m_AutomaticallyInstantiateSimulatorPrefab";
    private const string EditorOnlyFieldName = "m_AutomaticallyInstantiateInEditorOnly";

    public static bool IsSimulatorEnabledForCurrentSession()
    {
        if (!Application.isEditor)
        {
            return false;
        }

        ScriptableObject settings = Resources.Load<ScriptableObject>(SettingsResourceName);
        if (settings == null)
        {
            return true;
        }

        bool autoInstantiate = ReadBoolField(settings, AutoInstantiateFieldName, true);
        bool editorOnly = ReadBoolField(settings, EditorOnlyFieldName, true);
        return autoInstantiate && (!editorOnly || Application.isEditor);
    }

    private static bool ReadBoolField(object target, string fieldName, bool defaultValue)
    {
        if (target == null || string.IsNullOrWhiteSpace(fieldName))
        {
            return defaultValue;
        }

        FieldInfo field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
        if (field == null)
        {
            return defaultValue;
        }

        object value = field.GetValue(target);
        return value is bool boolValue ? boolValue : defaultValue;
    }
}
