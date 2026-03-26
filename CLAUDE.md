# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

**contar** is a 2D Unity game project using Unity 6000.3.12f1 LTS with the Universal Render Pipeline (URP).

## Unity Version

Unity **6000.3.12f1 (LTS)** — ensure this exact version is installed before opening the project.

## How to Open and Run

1. Open the project folder in Unity Hub with Unity 6000.3.12f1 LTS
2. Unity will auto-import packages from `Packages/manifest.json`
3. Open `Assets/Scenes/SampleScene.unity`
4. Press **Play** to run in the editor, or use **File → Build Settings → Build And Run** for a standalone build (default: 1920×1080 fullscreen)

## Architecture

- **Renderer**: Universal Render Pipeline (URP) v17.3 with a 2D-specific renderer (`Assets/Settings/Renderer2D.asset`). The global URP config is at `Assets/Settings/UniversalRP.asset`.
- **Input**: New Input System (not legacy). Input bindings are defined in `Assets/InputSystem_Actions.inputactions`.
- **Scene structure**: Orthographic 2D camera + 2D Global Light. The scene uses both `SpriteRenderer` (2D) and `MeshRenderer` (3D) objects.
- **Post-processing**: `Assets/DefaultVolumeProfile.asset` is the global URP Volume profile for camera effects.
- **Input actions**: `Assets/InputSystem_Actions.inputactions` defines a `Player` action map with Move (Vector2), Look (Vector2), Attack, Interact (with Hold), and Crouch already bound.
- **Scripts**: All custom C# scripts go under `Assets/`. There are none yet; add them here as the project grows.

## Key Packages (from `Packages/manifest.json`)

| Purpose | Package |
|---|---|
| 2D sprites & animation | `com.unity.2d.sprite`, `com.unity.2d.animation` |
| Tilemaps | `com.unity.2d.tilemap`, `com.unity.tilemap.extras` |
| Sprite shapes | `com.unity.2d.spriteshape` |
| Aseprite / PSD import | `com.unity.2d.aseprite`, `com.unity.2d.psdimporter` |
| Input | `com.unity.inputsystem` v1.19.0 |
| UI | `com.unity.ugui` v2.0.0 (supports both UI Toolkit and legacy uGUI) |
| Timeline / cutscenes | `com.unity.timeline` v1.8.11 |
| Testing | `com.unity.test-framework` v1.6.0 (not yet configured) |
| Visual Scripting | `com.unity.visualscripting` v1.9.11 |

## Asset Discipline

Every asset file committed to `Assets/` must have a paired `.meta` file. Unity generates these on import; always commit them together or the asset will lose its GUID references.

## Generated / Ignored Directories

`Library/`, `Temp/`, `Logs/`, `UserSettings/` are Unity-generated and excluded from version control. Never commit these.
