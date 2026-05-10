using System.IO;
using System.Xml;
using UnityEditor;
using UnityEditor.Android;
using UnityEditor.Build;
using UnityEngine;

public sealed class QuestAndroidManifestPostprocessor : IPostGenerateGradleAndroidProject, IOrderedCallback
{
    private const string AndroidNamespace = "http://schemas.android.com/apk/res/android";
    private const string ToolsNamespace = "http://schemas.android.com/tools";
    private const string UnityPlayerActivity = "com.unity3d.player.UnityPlayerActivity";

    public int callbackOrder => 10000;

    public void OnPostGenerateGradleAndroidProject(string path)
    {
        string manifestPath = Path.Combine(path, "src", "main", "AndroidManifest.xml");
        if (!File.Exists(manifestPath))
        {
            Debug.LogWarning("[QuestManifest] AndroidManifest.xml bulunamadi: " + manifestPath);
            return;
        }

        PatchUnityPlayerActivity(manifestPath);

        string xrManifestPath = Path.Combine(path, "xrmanifest.androidlib", "AndroidManifest.xml");
        if (File.Exists(xrManifestPath))
        {
            PatchEyeTrackingOnly(xrManifestPath);
        }
    }

    private static void PatchUnityPlayerActivity(string manifestPath)
    {
        XmlDocument document = new XmlDocument
        {
            PreserveWhitespace = true
        };
        document.Load(manifestPath);

        XmlNamespaceManager namespaces = new XmlNamespaceManager(document.NameTable);
        namespaces.AddNamespace("android", AndroidNamespace);
        namespaces.AddNamespace("tools", ToolsNamespace);

        XmlElement activity = document.SelectSingleNode(
            "//activity[@android:name='" + UnityPlayerActivity + "']",
            namespaces) as XmlElement;

        if (activity == null)
        {
            Debug.LogWarning("[QuestManifest] UnityPlayerActivity bulunamadi: " + manifestPath);
            return;
        }

        activity.SetAttribute("screenOrientation", AndroidNamespace, "landscape");
        activity.SetAttribute("hardwareAccelerated", AndroidNamespace, "true");
        activity.SetAttribute("replace", ToolsNamespace, "android:screenOrientation,android:hardwareAccelerated");

        RemoveEyeTrackingPermission(document, namespaces);
        EnsureEyeTrackingOptional(document, namespaces);

        document.Save(manifestPath);
        Debug.Log("[QuestManifest] Quest manifest guard uygulandi: " + manifestPath);
    }

    private static void PatchEyeTrackingOnly(string manifestPath)
    {
        XmlDocument document = new XmlDocument
        {
            PreserveWhitespace = true
        };
        document.Load(manifestPath);

        XmlNamespaceManager namespaces = new XmlNamespaceManager(document.NameTable);
        namespaces.AddNamespace("android", AndroidNamespace);
        namespaces.AddNamespace("tools", ToolsNamespace);

        RemoveEyeTrackingPermission(document, namespaces);
        EnsureEyeTrackingOptional(document, namespaces);

        document.Save(manifestPath);
        Debug.Log("[QuestManifest] XR manifest eye tracking guard uygulandi: " + manifestPath);
    }

    private static void RemoveEyeTrackingPermission(XmlDocument document, XmlNamespaceManager namespaces)
    {
        XmlNodeList permissions = document.SelectNodes(
            "//uses-permission[@android:name='com.oculus.permission.EYE_TRACKING']",
            namespaces);

        if (permissions == null)
        {
            return;
        }

        foreach (XmlNode permission in permissions)
        {
            permission.ParentNode?.RemoveChild(permission);
        }
    }

    private static void EnsureEyeTrackingOptional(XmlDocument document, XmlNamespaceManager namespaces)
    {
        XmlElement eyeTracking = document.SelectSingleNode(
            "//uses-feature[@android:name='oculus.software.eye_tracking']",
            namespaces) as XmlElement;

        if (eyeTracking == null)
        {
            XmlElement manifest = document.DocumentElement;
            if (manifest == null)
            {
                return;
            }

            eyeTracking = document.CreateElement("uses-feature");
            manifest.InsertBefore(eyeTracking, manifest.FirstChild);
            eyeTracking.SetAttribute("name", AndroidNamespace, "oculus.software.eye_tracking");
        }

        eyeTracking.SetAttribute("required", AndroidNamespace, "false");
        eyeTracking.SetAttribute("replace", ToolsNamespace, "android:required");
    }
}
