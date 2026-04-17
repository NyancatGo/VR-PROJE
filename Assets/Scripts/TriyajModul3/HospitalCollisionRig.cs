using UnityEngine;
using System.Collections.Generic;
#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// Triyaj hastanesi icin gorunmez ve guvenli bir collision rig kurar.
/// Duvar clip'lerini ve zemin dusmelerini azaltmak icin basit box collider'lar uretir.
/// </summary>
[ExecuteAlways]
[DisallowMultipleComponent]
public class HospitalCollisionRig : MonoBehaviour
{
    private const string CollisionRootName = "Hospital_Collision_Rig";
    private const string FloorGroupName = "Hospital_Floor_Safety";
    private const string WallGroupName = "Hospital_Wall_Blockers";
    private const string DoorGroupName = "Hospital_DoorFrame_Blockers";
    private const string PropGroupName = "Hospital_Major_Prop_Blockers";
    private const string VolumeRootName = "Hospital_Dark_Volume";
    private const string TeleportLayerName = "Teleport";
    private const float MinimumBlockerSize = 0.05f;

    [Header("Hospital Collision Rig")]
    [Tooltip("Gorunmez guvenli tabanin kalinligi.")]
    [SerializeField] private float floorThickness = 0.45f;

    [Tooltip("Duvar blocker'larinin minimum kalinligi.")]
    [SerializeField] private float wallThickness = 0.32f;

    [Tooltip("Kapi gecislerinde acik birakilacak genislik.")]
    [SerializeField] private float doorClearWidth = 1.25f;

    [Tooltip("Blocker'lara eklenecek kucuk genisletme payi.")]
    [SerializeField] private Vector3 boundsPadding = new Vector3(0.08f, 0.08f, 0.08f);

    [Tooltip("Yatak ve buyuk kasa gibi buyuk prop'lara da blocker ekler.")]
    [SerializeField] private bool includeMajorProps = true;

    private Transform collisionRoot;
#if UNITY_EDITOR
    private bool editorRebuildQueued;
#endif

    private sealed class CollisionSources
    {
        public readonly List<Transform> Floors = new List<Transform>();
        public readonly List<Transform> Walls = new List<Transform>();
        public readonly List<Transform> DoorFrames = new List<Transform>();
        public readonly List<Transform> DoorLeaves = new List<Transform>();
        public readonly List<Transform> MajorProps = new List<Transform>();
    }

    private void Reset()
    {
        if (Application.isPlaying)
        {
            return;
        }

#if UNITY_EDITOR
        QueueEditorRebuild();
#endif
    }

    private void OnEnable()
    {
        if (Application.isPlaying)
        {
            SetRigEnabled(true);
            return;
        }

#if UNITY_EDITOR
        QueueEditorRebuild();
#endif
    }

    private void OnDisable()
    {
        SetRigEnabled(false);
    }

    private void OnValidate()
    {
        if (Application.isPlaying)
        {
            return;
        }

#if UNITY_EDITOR
        QueueEditorRebuild();
#endif
    }

    [ContextMenu("Rebuild Hospital Collision Rig")]
    public void RebuildHospitalCollisionRig()
    {
#if UNITY_EDITOR
        CancelQueuedEditorRebuild();
#endif

        if (!gameObject.scene.IsValid())
        {
            return;
        }

        Transform root = GetOrCreateCollisionRoot();
        int floorLayer = gameObject.layer;
        int blockerLayer = ResolveBlockerLayer();

        Transform floorGroup = GetOrCreateGroup(root, FloorGroupName, floorLayer);
        Transform wallGroup = GetOrCreateGroup(root, WallGroupName, blockerLayer);
        Transform doorGroup = GetOrCreateGroup(root, DoorGroupName, blockerLayer);
        Transform propGroup = GetOrCreateGroup(root, PropGroupName, blockerLayer);

        ClearGroupChildren(floorGroup);
        ClearGroupChildren(wallGroup);
        ClearGroupChildren(doorGroup);
        ClearGroupChildren(propGroup);

        CollisionSources sources = CollectCollisionSources();
        BuildFloorSafety(sources.Floors, floorGroup);
        BuildWallAndDoorBlockers(sources.Walls, sources.DoorFrames, sources.DoorLeaves, wallGroup, doorGroup);

        if (includeMajorProps)
        {
            BuildMajorPropBlockers(sources.MajorProps, propGroup);
        }
    }

    [ContextMenu("Clear Hospital Collision Rig")]
    public void ClearHospitalCollisionRig()
    {
#if UNITY_EDITOR
        CancelQueuedEditorRebuild();
#endif

        Transform root = GetExistingCollisionRoot();
        if (root == null)
        {
            return;
        }

        DestroyObject(root.gameObject);
        collisionRoot = null;
    }

#if UNITY_EDITOR
    private void QueueEditorRebuild()
    {
        if (editorRebuildQueued)
        {
            return;
        }

        editorRebuildQueued = true;
        EditorApplication.delayCall -= ProcessQueuedEditorRebuild;
        EditorApplication.delayCall += ProcessQueuedEditorRebuild;
    }

    private void CancelQueuedEditorRebuild()
    {
        if (!editorRebuildQueued)
        {
            return;
        }

        editorRebuildQueued = false;
        EditorApplication.delayCall -= ProcessQueuedEditorRebuild;
    }

    private void ProcessQueuedEditorRebuild()
    {
        EditorApplication.delayCall -= ProcessQueuedEditorRebuild;

        if (this == null)
        {
            return;
        }

        editorRebuildQueued = false;

        if (Application.isPlaying || !gameObject.scene.IsValid())
        {
            return;
        }

        RebuildHospitalCollisionRig();
        SetRigEnabled(enabled);
    }
#endif

    private void BuildFloorSafety(List<Transform> floorSources, Transform floorGroup)
    {
        bool hasBounds = false;
        Bounds floorBounds = default;

        for (int i = 0; i < floorSources.Count; i++)
        {
            if (!TryCalculateBoundsRelativeToTransform(floorSources[i], transform, out Bounds localBounds))
            {
                continue;
            }

            if (!hasBounds)
            {
                floorBounds = localBounds;
                hasBounds = true;
            }
            else
            {
                floorBounds.Encapsulate(localBounds.min);
                floorBounds.Encapsulate(localBounds.max);
            }
        }

        if (!hasBounds)
        {
            return;
        }

        GameObject blocker = CreateRigChild("Main_Floor_Safety", floorGroup);
        BoxCollider collider = blocker.AddComponent<BoxCollider>();

        Vector3 size = floorBounds.size;
        size.x += boundsPadding.x * 2f;
        size.z += boundsPadding.z * 2f;
        size.y = Mathf.Max(floorThickness, MinimumBlockerSize);

        Vector3 center = floorBounds.center;
        center.y = floorBounds.max.y - (size.y * 0.5f) + 0.01f;

        collider.isTrigger = false;
        collider.center = center;
        collider.size = size;
    }

    private void BuildWallAndDoorBlockers(
        List<Transform> wallSources,
        List<Transform> doorFrameSources,
        List<Transform> doorLeafSources,
        Transform wallGroup,
        Transform doorGroup)
    {
        for (int i = 0; i < doorFrameSources.Count; i++)
        {
            CreateDoorFrameBlockers(doorFrameSources[i], doorGroup);
        }

        for (int i = 0; i < wallSources.Count; i++)
        {
            CreateOrientedBlocker(wallSources[i], wallGroup, wallSources[i].name + "_Blocker");
        }

        for (int i = 0; i < doorLeafSources.Count; i++)
        {
            CreateOrientedBlocker(doorLeafSources[i], wallGroup, doorLeafSources[i].name + "_Blocker");
        }
    }

    private void BuildMajorPropBlockers(List<Transform> propSources, Transform propGroup)
    {
        for (int i = 0; i < propSources.Count; i++)
        {
            CreateOrientedBlocker(propSources[i], propGroup, propSources[i].name + "_PropBlocker");
        }
    }

    private void CreateOrientedBlocker(Transform target, Transform parent, string blockerName)
    {
        if (!TryCalculateBoundsRelativeToTransform(target, target, out Bounds localBounds))
        {
            return;
        }

        GameObject blocker = CreateRigChild(blockerName, parent);
        blocker.transform.position = target.position;
        blocker.transform.rotation = target.rotation;

        BoxCollider collider = blocker.AddComponent<BoxCollider>();
        collider.isTrigger = false;

        Vector3 size = localBounds.size;
        size.x += boundsPadding.x;
        size.y += boundsPadding.y;
        size.z += boundsPadding.z;

        if (size.x <= size.z)
        {
            size.x = Mathf.Max(size.x, wallThickness);
        }
        else
        {
            size.z = Mathf.Max(size.z, wallThickness);
        }

        size.x = Mathf.Max(size.x, MinimumBlockerSize);
        size.y = Mathf.Max(size.y, MinimumBlockerSize);
        size.z = Mathf.Max(size.z, MinimumBlockerSize);

        collider.center = localBounds.center;
        collider.size = size;
    }

    private void CreateDoorFrameBlockers(Transform target, Transform parent)
    {
        if (!TryCalculateBoundsRelativeToTransform(target, target, out Bounds localBounds))
        {
            return;
        }

        Vector3 size = localBounds.size;
        int widthAxis = size.x >= size.z ? 0 : 2;
        int depthAxis = widthAxis == 0 ? 2 : 0;

        float totalWidth = size[widthAxis];
        float totalDepth = Mathf.Max(size[depthAxis] + boundsPadding[depthAxis], wallThickness);
        float totalHeight = size.y + boundsPadding.y;
        float clearWidth = Mathf.Min(doorClearWidth, totalWidth - 0.2f);

        if (clearWidth <= 0f || totalWidth <= clearWidth + 0.05f)
        {
            CreateOrientedBlocker(target, parent, target.name + "_DoorFallback");
            return;
        }

        float sideWidth = Mathf.Max((totalWidth - clearWidth) * 0.5f, 0.12f);
        float lintelHeight = Mathf.Clamp(totalHeight * 0.2f, 0.2f, 0.45f);
        float postHeight = Mathf.Max(totalHeight - lintelHeight, 0.45f);

        GameObject frameRoot = CreateRigChild(target.name + "_Frame", parent);
        frameRoot.transform.position = target.position;
        frameRoot.transform.rotation = target.rotation;

        Vector3 min = localBounds.min;
        Vector3 max = localBounds.max;
        Vector3 center = localBounds.center;

        if (widthAxis == 0)
        {
            AddLocalBox(frameRoot.transform, "Left_Post",
                new Vector3(min.x + sideWidth * 0.5f, min.y + postHeight * 0.5f, center.z),
                new Vector3(sideWidth + boundsPadding.x, postHeight, totalDepth));

            AddLocalBox(frameRoot.transform, "Right_Post",
                new Vector3(max.x - sideWidth * 0.5f, min.y + postHeight * 0.5f, center.z),
                new Vector3(sideWidth + boundsPadding.x, postHeight, totalDepth));

            AddLocalBox(frameRoot.transform, "Top_Lintel",
                new Vector3(center.x, max.y - lintelHeight * 0.5f, center.z),
                new Vector3(totalWidth + boundsPadding.x, lintelHeight, totalDepth));
        }
        else
        {
            AddLocalBox(frameRoot.transform, "Left_Post",
                new Vector3(center.x, min.y + postHeight * 0.5f, min.z + sideWidth * 0.5f),
                new Vector3(totalDepth, postHeight, sideWidth + boundsPadding.z));

            AddLocalBox(frameRoot.transform, "Right_Post",
                new Vector3(center.x, min.y + postHeight * 0.5f, max.z - sideWidth * 0.5f),
                new Vector3(totalDepth, postHeight, sideWidth + boundsPadding.z));

            AddLocalBox(frameRoot.transform, "Top_Lintel",
                new Vector3(center.x, max.y - lintelHeight * 0.5f, center.z),
                new Vector3(totalDepth, lintelHeight, totalWidth + boundsPadding.z));
        }
    }

    private void AddLocalBox(Transform parent, string objectName, Vector3 localCenter, Vector3 localSize)
    {
        GameObject blocker = CreateRigChild(objectName, parent);
        BoxCollider collider = blocker.AddComponent<BoxCollider>();
        collider.isTrigger = false;
        collider.center = localCenter;
        collider.size = new Vector3(
            Mathf.Max(localSize.x, MinimumBlockerSize),
            Mathf.Max(localSize.y, MinimumBlockerSize),
            Mathf.Max(localSize.z, MinimumBlockerSize));
    }

    private CollisionSources CollectCollisionSources()
    {
        CollisionSources sources = new CollisionSources();
        for (int i = 0; i < transform.childCount; i++)
        {
            CollectCollisionSourcesRecursive(transform.GetChild(i), sources);
        }

        return sources;
    }

    private bool CollectCollisionSourcesRecursive(Transform current, CollisionSources sources)
    {
        string lowerName = current.name.ToLowerInvariant();
        if (IsIgnoredCandidate(lowerName))
        {
            return false;
        }

        bool hasRelevantDescendant = false;
        for (int i = 0; i < current.childCount; i++)
        {
            hasRelevantDescendant |= CollectCollisionSourcesRecursive(current.GetChild(i), sources);
        }

        if (!HasRenderableGeometry(current))
        {
            return hasRelevantDescendant;
        }

        bool isFloor = IsFloorCandidate(lowerName);
        bool isDoorFrame = IsDoorFrameCandidate(lowerName);
        bool isDoorLeaf = IsDoorLeafCandidate(lowerName);
        bool isWall = IsWallCandidate(lowerName);
        bool isMajorProp = IsMajorPropCandidate(lowerName);
        bool isRelevant = isFloor || isDoorFrame || isDoorLeaf || isWall || isMajorProp;

        if (!isRelevant)
        {
            return hasRelevantDescendant;
        }

        if (!hasRelevantDescendant)
        {
            if (isFloor)
            {
                sources.Floors.Add(current);
            }

            if (isDoorFrame)
            {
                sources.DoorFrames.Add(current);
            }
            else if (isDoorLeaf)
            {
                sources.DoorLeaves.Add(current);
            }
            else if (isWall)
            {
                sources.Walls.Add(current);
            }

            if (isMajorProp)
            {
                sources.MajorProps.Add(current);
            }
        }

        return true;
    }

    private static bool HasRenderableGeometry(Transform source)
    {
        return TryCalculateBoundsRelativeToTransform(source, source, out _);
    }

    private static bool IsIgnoredCandidate(string lowerName)
    {
        return lowerName.Contains("light")
            || lowerName.Contains("camera")
            || lowerName.Contains("spawn")
            || lowerName.Contains("collision")
            || lowerName.Contains("volume")
            || lowerName.Contains("canvas")
            || lowerName.Contains("menu")
            || lowerName.Contains("ui")
            || lowerName.Contains("npc")
            || lowerName.Contains("doctor")
            || lowerName.Contains("bodyguard")
            || lowerName.Contains("player")
            || lowerName.Contains("xr")
            || lowerName.Contains("roof")
            || lowerName.Contains("ceiling");
    }

    private static bool IsFloorCandidate(string objectName)
    {
        return objectName.ToLowerInvariant().Contains("floor");
    }

    private static bool IsWallCandidate(string lowerName)
    {
        return lowerName.Contains("wall")
            && !lowerName.Contains("roof")
            && !lowerName.Contains("ceiling");
    }

    private static bool IsDoorFrameCandidate(string lowerName)
    {
        return lowerName.Contains("door_01_base")
            || lowerName.Contains("door base");
    }

    private static bool IsDoorLeafCandidate(string lowerName)
    {
        return lowerName.Contains("door")
            && !IsDoorFrameCandidate(lowerName);
    }

    private static bool IsMajorPropCandidate(string lowerName)
    {
        if (lowerName.Contains("roof"))
        {
            return false;
        }

        return lowerName.Contains("bed")
            || lowerName.Contains("med_box")
            || lowerName.Contains(" crate")
            || lowerName.EndsWith("box")
            || lowerName.Contains(" box")
            || lowerName.Contains("cabinet")
            || lowerName.Contains("locker")
            || lowerName.Contains("desk")
            || lowerName.Contains("table")
            || lowerName.Contains("column")
            || lowerName.Contains("pillar");
    }

    private Transform GetOrCreateCollisionRoot()
    {
        if (collisionRoot != null)
        {
            return collisionRoot;
        }

        collisionRoot = GetExistingCollisionRoot();
        if (collisionRoot != null)
        {
            return collisionRoot;
        }

        GameObject rootObject = new GameObject(CollisionRootName);
        rootObject.transform.SetParent(transform, false);
        rootObject.layer = gameObject.layer;
        collisionRoot = rootObject.transform;
        return collisionRoot;
    }

    private Transform GetExistingCollisionRoot()
    {
        return transform.Find(CollisionRootName);
    }

    private Transform GetOrCreateGroup(Transform parent, string groupName, int layer)
    {
        Transform existing = parent.Find(groupName);
        if (existing != null)
        {
            existing.gameObject.layer = layer;
            return existing;
        }

        GameObject groupObject = new GameObject(groupName);
        groupObject.transform.SetParent(parent, false);
        groupObject.layer = layer;
        return groupObject.transform;
    }

    private GameObject CreateRigChild(string objectName, Transform parent)
    {
        GameObject child = new GameObject(objectName);
        child.transform.SetParent(parent, false);
        child.layer = parent != null ? parent.gameObject.layer : gameObject.layer;
        return child;
    }

    private int ResolveBlockerLayer()
    {
        int teleportLayer = LayerMask.NameToLayer(TeleportLayerName);
        if (teleportLayer >= 0)
        {
            return teleportLayer;
        }

        Debug.LogWarning($"[{nameof(HospitalCollisionRig)}] '{TeleportLayerName}' layer bulunamadi, blocker'lar varsayilan layer'da kalacak.", this);
        return gameObject.layer;
    }

    private void ClearGroupChildren(Transform group)
    {
        for (int i = group.childCount - 1; i >= 0; i--)
        {
            DestroyObject(group.GetChild(i).gameObject);
        }
    }

    private void DestroyObject(GameObject target)
    {
        if (Application.isPlaying)
        {
            Destroy(target);
        }
        else
        {
            DestroyImmediate(target);
        }
    }

    private void SetRigEnabled(bool isEnabled)
    {
        Transform root = GetExistingCollisionRoot();
        if (root == null)
        {
            return;
        }

        Collider[] colliders = root.GetComponentsInChildren<Collider>(true);
        for (int i = 0; i < colliders.Length; i++)
        {
            colliders[i].enabled = isEnabled;
        }
    }

    private static bool TryCalculateBoundsRelativeToTransform(Transform source, Transform relativeTo, out Bounds localBounds)
    {
        Renderer[] renderers = source.GetComponentsInChildren<Renderer>(true);
        bool hasBounds = false;
        localBounds = default;

        for (int i = 0; i < renderers.Length; i++)
        {
            Bounds rendererBounds = renderers[i].bounds;
            if (rendererBounds.size == Vector3.zero)
            {
                continue;
            }

            Vector3 min = rendererBounds.min;
            Vector3 max = rendererBounds.max;

            Vector3[] corners =
            {
                new Vector3(min.x, min.y, min.z),
                new Vector3(min.x, min.y, max.z),
                new Vector3(min.x, max.y, min.z),
                new Vector3(min.x, max.y, max.z),
                new Vector3(max.x, min.y, min.z),
                new Vector3(max.x, min.y, max.z),
                new Vector3(max.x, max.y, min.z),
                new Vector3(max.x, max.y, max.z)
            };

            for (int cornerIndex = 0; cornerIndex < corners.Length; cornerIndex++)
            {
                Vector3 localCorner = relativeTo.InverseTransformPoint(corners[cornerIndex]);
                if (!hasBounds)
                {
                    localBounds = new Bounds(localCorner, Vector3.zero);
                    hasBounds = true;
                    continue;
                }

                localBounds.Encapsulate(localCorner);
            }
        }

        return hasBounds;
    }
}
