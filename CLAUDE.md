# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

Unity VR Deprem Kurtarma Simülasyonu - A VR earthquake rescue training simulation built with Unity 2022.3.62f3, XR Interaction Toolkit 2.6.5, and Universal Render Pipeline.

## Build Commands

```bash
# Unity Editor must be installed. Build via:
# File > Build Settings > Build

# Command-line build:
"<unity-editor-path>" -quit -batchmode -buildTarget StandaloneWindows64 -buildPath "Build/Windows"

# Run play mode tests:
"<unity-editor-path>" -quit -batchmode -runPlayModeTests -testResults "Temp/test-results.xml"

# Run editor tests:
"<unity-editor-path>" -quit -batchmode -runEditorTests -testResults "Temp/test-results.xml"
```

## Architecture

### Core Systems

**Victim/Triage System**
- `YaraliController` - Victim NPC with triage state, pickup/drop logic, VR interaction via `XRSimpleInteractable`
- `TriyajManager` - Singleton managing triage assignments and scoring (green/yellow/red/black)
- `TriageCategory` enum - Defined in `Assets/Scripts/TriyajModul3/TriageCategory.cs`
- `RescueManager` - Tracks rescued/carried victims and win conditions

**VR Interaction Infrastructure**
- `XRRigAnchorUtility` - Internal utility for resolving VR anchor transforms (camera, back anchor, origin). **Critical: Do not use `Camera.main` in VR - use `XRRigAnchorUtility.ResolveCameraTransform()` instead**
- `VRGrabbable` - Makes objects interactable in VR with physics-based grab mechanics
- `EquipmentManager` - Attaches equipment to VR anchor points (head, hand, back)
- `PlayerPresence` - Tracks player body position relative to XROrigin

**Wallhack System**
- `YaraliWallhack` - Per-victim component enabling silhouette rendering through walls
- `WallhackToggleManager` - Master toggle (H key) for all victim wallhacks
- Requires `Custom/WallhackSilhouette` shader

**Safe Zone & Rescue**
- `SafeZone` - Trigger area where carried victims are dropped and counted as rescued
- Uses `XRRigAnchorUtility.ResolveBackAnchor()` to find where victims are attached

**Scene Management**
- `DepremSahnesiYoneticisi` - Spawns debris and damaged building prefabs at scene start
- `ModulePortal` - Loads next scene on VR interaction
- `SceneLoader` - Handles scene transitions

### Key Patterns

**VR Canvas Requirements (Critical)**
- Canvas `Render Mode` must be `World Space`
- Canvas scale must be ~`0.002` (not 1.0 - Unity units = meters)
- Use `TrackedDeviceGraphicRaycaster` (not `GraphicRaycaster`) on Canvas
- Event System must use `XR UI Input Module` (not `Standalone Input Module`)
- Canvas should position relative to player camera via `XROrigin().Camera.transform`

**VR Teleportation Safety**
- Teleport destinations need `MeshCollider` or `BoxCollider` with `isTrigger = false`
- Use invisible floor cubes (scale Y = 0.1) for complex mesh floors
- Raycast to find ground before teleporting:
  ```csharp
  if (Physics.Raycast(pos + Vector3.up * 10f, Vector3.down, out hit, 100f))
      pos.y = hit.point.y;
  ```

**XR Event Listeners**
- Always remove listeners in `OnDisable()` to prevent memory leaks and duplicate calls
- Use `selectEntered.AddListener` / `selectExited.RemoveListener` pattern

**Input System**
- Both legacy `Input.GetKeyDown()` and new Input System (`Keyboard.current`) are used
- Wrap new Input System in `#if ENABLE_INPUT_SYSTEM` guards

## Module Structure

Modular scene system where players progress through training scenarios. Each module is a separate Unity scene with its own assets and logic. Modules include training areas, hospital interiors, and triage practice zones.

## Critical Development Notes

1. **XRRigAnchorUtility location**: This internal utility class is currently defined inside `EquipmentManager.cs` at the bottom of the file (lines 100+). It should be in its own file but currently works as-is.

2. **Duplicate TriageCategory definition risk**: `TriageCategory` enum exists in `Assets/Scripts/TriyajModul3/TriageCategory.cs` but may also be referenced by `Assets/YaraliController.cs`. Ensure only one definition exists to avoid compilation errors.

3. **Wallhack shader dependency**: Wallhack system requires `Custom/WallhackSilhouette` shader. Without it, `YaraliWallhack.Setup()` fails silently and wallhack never appears.

4. **SafeZone collider requirement**: The collider must have `isTrigger = true` for `OnTriggerEnter` to fire.

5. **VRUIClickHelper**: Custom solution for VR button clicks since `XR UI Input Module` sometimes misses trigger input across different VR devices.
