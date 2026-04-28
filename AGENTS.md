# AGENTS.md

## 프로젝트 개요

- `contar`는 Unity 6000.3.12f1 LTS 기반의 2D 퍼즐 게임 프로젝트다.
- Universal Render Pipeline, 이하 URP를 사용한다.
- 게임 컨셉은 Helltaker에서 영감을 받은 격자 이동 기반 퍼즐이다.
- 플레이어는 제한된 이동 횟수 안에서 타일을 이동하고, 장애물을 밀거나 조작하며 출구 조건을 만족해야 한다.

## Unity 버전

- Unity `6000.3.12f1 LTS`를 기준으로 작업한다.
- 프로젝트를 열기 전 해당 Unity 버전이 설치되어 있는지 확인한다.

## 실행 방법

1. Unity Hub에서 프로젝트 폴더를 연다.
2. Unity가 `Packages/manifest.json` 기준으로 패키지를 자동 임포트한다.
3. 기본 플레이 씬은 `Assets/Scenes/InGameScene.unity`를 우선 기준으로 한다.
4. 에디터에서 Play를 눌러 실행한다.
5. 빌드가 필요하면 Unity의 Build Settings를 사용한다.

## 게임 규칙

- 시작 타일과 출구 타일이 존재한다.
- 플레이어는 이동할 때마다 이동 횟수를 소비한다.
- 이동 횟수가 0 이하가 되고 클리어 조건을 만족하지 못하면 실패한다.
- 출구는 기본적으로 진입 가능하지만, 특정 맵에서는 별도 조건을 만족해야 클리어될 수 있다.
- `MoveTile`은 플레이어가 밟으면 이동 횟수를 증가 또는 감소시킨다.
- `NumberObstacle`은 이동 횟수를 소비해서 밀 수 있는 장애물이다.

## 광고 및 스킵권 기획

- 스킵권은 초기 3개 지급, 최대 5개 보유를 기준으로 한다.
- 0~5 스테이지 구간에서는 광고를 노출하지 않는다.
- 3개 스테이지 클리어마다 스킵권 1개를 지급한다.
- 3회 실패 또는 스테이지 다시하기 시 광고 노출을 고려한다.
- 광고 시청 또는 스킵권 사용으로 광고 스킵/스테이지 스킵이 가능하도록 기획한다.

## 아키텍처

- 렌더러는 URP를 사용한다.
- 2D 전용 Renderer 설정은 `Assets/Settings/Renderer2D.asset`을 기준으로 한다.
- 전역 URP 설정은 `Assets/Settings/UniversalRP.asset`을 기준으로 한다.
- 입력은 Legacy Input이 아니라 New Input System을 사용한다.
- 입력 바인딩은 `Assets/InputSystem_Actions.inputactions`를 기준으로 한다.
- 씬은 Orthographic 2D/보드 뷰 카메라와 2D Global Light를 사용한다.
- 프로젝트에는 `SpriteRenderer` 기반 2D 오브젝트와 `MeshRenderer` 기반 3D 오브젝트가 함께 존재할 수 있다.
- 카메라 효과는 `Assets/DefaultVolumeProfile.asset`의 URP Volume 프로필을 기준으로 한다.
- 커스텀 C# 스크립트는 `Assets/Scripts/` 아래에 둔다.

## 주요 패키지

- 2D Sprite 및 애니메이션: `com.unity.2d.sprite`, `com.unity.2d.animation`
- Tilemap: `com.unity.2d.tilemap`, `com.unity.tilemap.extras`
- SpriteShape: `com.unity.2d.spriteshape`
- Aseprite / PSD Import: `com.unity.2d.aseprite`, `com.unity.2d.psdimporter`
- Input System: `com.unity.inputsystem` v1.19.0
- UI: `com.unity.ugui` v2.0.0
- Timeline: `com.unity.timeline` v1.8.11
- Test Framework: `com.unity.test-framework` v1.6.0
- Visual Scripting: `com.unity.visualscripting` v1.9.11

## 코딩 규칙

- `_` discard 패턴을 사용하지 않는다.
  - 예: `_ => null`, `var _ = ...` 형태를 피한다.
- 단순 읽기 전용 값은 가능하면 자동 프로퍼티를 사용한다.
  - 예: `public int Value { get; private set; }`
- 단, getter/setter에 추가 로직이 있거나 `[SerializeField]`로 Inspector에 노출해야 하는 경우에는 backing field를 사용할 수 있다.
- 기존 코드 스타일과 폴더 구조를 우선 따른다.
- 불필요한 추상화나 대규모 리팩터링은 피한다.

## 에셋 규칙

- `Assets/` 아래에 커밋되는 모든 에셋은 반드시 대응되는 `.meta` 파일을 함께 관리한다.
- Unity가 생성한 `.meta` 파일을 임의로 삭제하거나 새로 만들지 않는다.
- 비주얼 구성은 가능하면 스크립트보다 프리팹, 씬, 머티리얼, 스프라이트 설정으로 처리한다.
- 스크립트는 이동, 판정, 상태 관리 같은 게임 로직에 집중한다.

## 생성/무시 폴더

- `Library/`, `Temp/`, `Logs/`, `UserSettings/`는 Unity 생성 폴더다.
- 위 폴더들은 버전 관리 대상이 아니며 커밋하지 않는다.
- 필요한 경우 읽을 수는 있지만, 의도적으로 수정하거나 정리하지 않는다.

## 사용자 승인 규칙

- 코드, 프리팹, 씬, 에셋, 프로젝트 파일, git 상태를 수정하기 전에는 반드시 변경 예정 내용을 먼저 보여준다.
- 사용자가 명시적으로 승인하기 전까지 파일을 수정하지 않는다.
- 파일 읽기, 검색, `git status`, 빌드 확인처럼 저장소 파일을 바꾸지 않는 작업은 사전 승인 없이 할 수 있다.
- 사용자가 명시적으로 요청하지 않은 변경사항은 되돌리거나 덮어쓰거나 삭제하지 않는다.
- 이미 작업 트리에 있는 미추적/수정 파일은 사용자 변경일 수 있으므로 조심해서 다룬다.

## Git 규칙

- 브랜치 작업이 필요하면 먼저 브랜치명과 목적을 사용자에게 보여준다.
- 커밋, push, PR 생성은 사용자가 요청하거나 승인한 경우에만 진행한다.
- 작업 중 관련 없는 변경사항은 건드리지 않는다.
