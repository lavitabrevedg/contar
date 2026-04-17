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

## Game Concept

**장르**: 이동 횟수 기반 탈출 퍼즐 (2D)
**모티브**: [헬테이커(Helltaker)](https://store.steampowered.com/app/1289310/Helltaker/) — 격자 이동, 제한된 이동 횟수, 장애물 밀기 메커닉 참고

### 기본 룰
- 시작점과 출구가 존재하며, 플레이어는 이동할수록 보유 숫자가 줄어든다.
- 출구는 기본적으로 진입 가능하나, 특정 맵에서는 홀수/짝수일 때만 탈출 가능.
- **+타일**: 밟으면 숫자 증가 / **-타일**: 밟으면 숫자 감소
- **숫자 장애물**: 이동 횟수를 소모해 밀 수 있는 오브젝트

### 광고 전략
- 스킵권: 초기 지급 3장, 최대 5장 보유 가능
- 0~5스테이지: 광고 없음
- 3회 스테이지 클리어 시 스킵권 1장 증정
- 3회 실패 또는 스테이지 다시하기 시 광고 노출
- 광고 시청 또는 스킵권 사용으로 광고 스킵 가능

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

## Coding Rules

- `_` discard 패턴 사용 금지 (예: `_ => null`, `var _ = ...` 형태 모두 금지)
- **외부 읽기 전용 프로퍼티**는 백킹 필드 + 별도 프로퍼티 대신 `public int Value { get; private set; }` 자동 프로퍼티 방식 사용
  - 예외: getter/setter에 부가 로직이 있거나, `[SerializeField]`로 Inspector에 노출해야 할 때는 백킹 필드 방식 사용

## Asset Discipline

Every asset file committed to `Assets/` must have a paired `.meta` file. Unity generates these on import; always commit them together or the asset will lose its GUID references.

## Generated / Ignored Directories

`Library/`, `Temp/`, `Logs/`, `UserSettings/` are Unity-generated and excluded from version control. Never commit these.
