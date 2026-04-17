using UnityEngine;
using System;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "NewModuleData", menuName = "Education/Module Data")]
public class ModuleData : ScriptableObject
{
    [Header("General Info")]
    public string moduleTitle;
    public string sceneName;
    [TextArea(3, 5)]
    public string moduleDescription;
    public Sprite thumbnail;

    [Header("Educational Content")]
    public List<Sprite> slides = new List<Sprite>();
    public string videoURL; // URL or local path
    public Sprite infographic;
    
    [Header("Visual Theme")]
    public Color themeColor = Color.cyan;
}
