using System;
using System.Reflection;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.Localization.Tables;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class TutorialUIValidation
{
    private const string InGameScenePath = "Assets/Scenes/InGameScene.unity";
    private const string StageOnePath = "Assets/Data/Stages/Stage_001.asset";
    private const string StageFourPath = "Assets/Data/Stages/Stage_004.asset";
    private const string StageFivePath = "Assets/Data/Stages/Stage_005.asset";
    private const string StageSixPath = "Assets/Data/Stages/Stage_006.asset";
    private const string EnglishTablePath = "Assets/Localization/UI/UI_en.asset";
    private const string KoreanTablePath = "Assets/Localization/UI/UI_ko.asset";
    private const string MusicMixerPath = "Assets/Resources/Audio/MusicMixer.mixer";
    private const string HintGlowTexturePath =
        "Assets/Art/Effects/Hint/Generated/HintGlowMote.png";
    private const string HintPathTexturePath =
        "Assets/Art/Effects/Hint/Generated/HintPathLight.png";
    private const string HintButtonPrefabPath =
        "Assets/Art/Effects/Hint/Generated/HintButtonGuidanceGlow.prefab";
    private const string HintRoutePrefabPath =
        "Assets/Art/Effects/Hint/Generated/HintRouteGuidanceLight.prefab";
    private const float SafeMargin = 24f;

    [MenuItem("Tools/Contar/Validate Tutorial UI")]
    public static void ValidateAll()
    {
        Scene inGameScene = EditorSceneManager.OpenScene(InGameScenePath, OpenSceneMode.Single);
        if (!inGameScene.IsValid())
            throw new InvalidOperationException($"Failed to open {InGameScenePath}.");

        GameUIView gameUIView = UnityEngine.Object.FindFirstObjectByType<GameUIView>(FindObjectsInactive.Include);
        if (gameUIView == null)
            throw new InvalidOperationException("GameUIView is missing from InGameScene.");

        SerializedObject serializedView = new SerializedObject(gameUIView);
        ValidateTutorialHierarchy(serializedView);
        ValidateSwipeTutorialHierarchy(serializedView);
        ValidateHintButtonParticle(serializedView);
        ValidateDestroyedHintParticleHandling();
        ValidateResultDismissal(gameUIView, serializedView);
        ValidateStageOne();
        ValidateStageFour();
        ValidateStageFive();
        ValidateStageSix();
        ValidateLocalization();
        ValidatePanelLayout();
        ValidateMusicMixer();
        Debug.Log("[TutorialUIValidation] Tutorial UI, hint particles, localization, stages, mixer, layout, and result dismissal are valid.");
    }

    private static void ValidateTutorialHierarchy(SerializedObject serializedView)
    {
        GameObject tutorialDialog = GetRequiredReference<GameObject>(serializedView, "tutorialDialog");
        TMP_Text tutorialMessageText = GetRequiredReference<TMP_Text>(serializedView, "tutorialMessageText");
        Button tutorialAdvanceButton = GetRequiredReference<Button>(serializedView, "tutorialAdvanceButton");
        RectTransform tutorialPanelRect = GetRequiredReference<RectTransform>(serializedView, "tutorialPanelRect");
        Image tutorialPanelImage = GetRequiredReference<Image>(serializedView, "tutorialPanelImage");
        TutorialSpotlightGraphic tutorialSpotlight = GetRequiredReference<TutorialSpotlightGraphic>(
            serializedView,
            "tutorialSpotlight");

        if (tutorialDialog.activeSelf)
            throw new InvalidOperationException("TutorialDialog must be inactive by default.");

        Image dialogInputImage = tutorialDialog.GetComponent<Image>();
        if (dialogInputImage == null || !dialogInputImage.raycastTarget || dialogInputImage.color.a != 0f)
            throw new InvalidOperationException("TutorialDialog must use a transparent raycast Image.");

        if (tutorialAdvanceButton.gameObject != tutorialDialog)
            throw new InvalidOperationException("Tutorial advance button must cover TutorialDialog.");

        if (tutorialPanelRect.name != "TutorialPanel")
            throw new InvalidOperationException("TutorialPanel reference points to the wrong object.");

        if (!Approximately(tutorialPanelRect.sizeDelta, new Vector2(600f, 266f)))
            throw new InvalidOperationException("TutorialPanel size must be 600x266 at the reference resolution.");

        if (tutorialPanelImage.sprite == null
            || tutorialPanelImage.transform.parent != tutorialPanelRect
            || tutorialPanelImage.name != "TutorialPanelBackground")
        {
            throw new InvalidOperationException("TutorialPanel background is missing or is not a child of TutorialPanel.");
        }

        if (tutorialPanelImage.rectTransform.localRotation != Quaternion.identity)
            throw new InvalidOperationException("TutorialPanel background must flip vertically without rotating.");

        if (tutorialMessageText.transform.parent != tutorialPanelRect)
            throw new InvalidOperationException("Tutorial message text must remain below TutorialPanel.");

        if (tutorialPanelRect.Find("TutorialAdvanceText") != null)
            throw new InvalidOperationException("TutorialPanel must not display a Next or Start label.");

        ValidateTutorialFont(tutorialMessageText, "message");
        if (tutorialSpotlight.transform.parent != tutorialDialog.transform
            || tutorialSpotlight.raycastTarget
            || tutorialSpotlight.gameObject.layer != tutorialDialog.layer)
            throw new InvalidOperationException("Tutorial spotlight must be a non-raycast child of TutorialDialog.");

        if (tutorialDialog.transform.Find("TutorialDimmerLeft") != null
            || tutorialDialog.transform.Find("TutorialDimmerRight") != null
            || tutorialDialog.transform.Find("TutorialDimmerTop") != null
            || tutorialDialog.transform.Find("TutorialDimmerBottom") != null)
        {
            throw new InvalidOperationException("Legacy tutorial dimmers must be removed.");
        }

        ValidateSpotlightMesh(tutorialSpotlight);
    }

    private static void ValidateSwipeTutorialHierarchy(SerializedObject serializedView)
    {
        GameObject swipeTutorial = GetRequiredReference<GameObject>(serializedView, "swipeTutorial");
        Image swipeTutorialHandImage = GetRequiredReference<Image>(serializedView, "swipeTutorialHandImage");
        TMP_Text swipeTutorialText = GetRequiredReference<TMP_Text>(serializedView, "swipeTutorialText");

        if (swipeTutorial.activeSelf)
            throw new InvalidOperationException("SwipeTutorial must be inactive by default.");

        if (swipeTutorialHandImage.sprite == null || swipeTutorialHandImage.raycastTarget)
            throw new InvalidOperationException("Swipe tutorial hand must use a non-raycast sprite.");

        if (swipeTutorialText.raycastTarget)
            throw new InvalidOperationException("Swipe tutorial text must not block swipe input.");

        if (!Approximately(swipeTutorialHandImage.rectTransform.sizeDelta, new Vector2(260f, 260f)))
            throw new InvalidOperationException("Swipe tutorial hand must use the large 260x260 size.");

        ValidateConstant(typeof(GameUIView), "SwipeTutorialHandSize", 260f);
        ValidateConstant(typeof(GameUIView), "SwipeTutorialTravelDistance", 600f);
        ValidateConstant(typeof(GameUIView), "SwipeTutorialDragDuration", 0.75f);

        ValidateTutorialFont(swipeTutorialText, "swipe");
    }

    private static void ValidateConstant(Type declaringType, string fieldName, float expectedValue)
    {
        FieldInfo constantField = declaringType.GetField(
            fieldName,
            BindingFlags.Static | BindingFlags.NonPublic);
        if (constantField == null
            || !(constantField.GetRawConstantValue() is float)
            || !Mathf.Approximately((float)constantField.GetRawConstantValue(), expectedValue))
        {
            throw new InvalidOperationException($"Unexpected tutorial animation setting: {fieldName}");
        }
    }

    private static void ValidateHintButtonParticle(SerializedObject serializedView)
    {
        Button hintButton = GetRequiredReference<Button>(serializedView, "hintButton");
        HintButtonParticleEffect particleEffect = GetRequiredReference<HintButtonParticleEffect>(
            serializedView,
            "hintButtonParticleEffect");
        if (particleEffect.transform != hintButton.transform)
            throw new InvalidOperationException("Hint button particle effect must be attached to the Hint button.");

        SerializedObject serializedEffect = new SerializedObject(particleEffect);
        GameObject particlePrefab = GetRequiredReference<GameObject>(serializedEffect, "particlePrefab");
        if (particlePrefab.GetComponentInChildren<ParticleSystem>(true) == null)
            throw new InvalidOperationException("Hint button particle prefab must contain a ParticleSystem.");

        string particlePrefabPath = AssetDatabase.GetAssetPath(particlePrefab);
        if (particlePrefabPath != HintButtonPrefabPath)
            throw new InvalidOperationException("Hint button must use the generated guidance glow prefab.");

        SerializedProperty particleScale = serializedEffect.FindProperty("particleScale");
        SerializedProperty screenOffset = serializedEffect.FindProperty("screenOffset");
        if (particleScale == null || !Mathf.Approximately(particleScale.floatValue, 1f))
            throw new InvalidOperationException("Hint button particle scale must match the generated prefab scale.");

        if (screenOffset == null || screenOffset.vector2Value != Vector2.zero)
            throw new InvalidOperationException("Hint button particle must start at the button center.");

        ValidateHintButtonScreenCenter(particleEffect, hintButton);

        GameUIView gameUIView = serializedView.targetObject as GameUIView;
        FieldInfo isPlayingField = typeof(HintButtonParticleEffect).GetField(
            "isPlaying",
            BindingFlags.Instance | BindingFlags.NonPublic);
        if (gameUIView == null || isPlayingField == null)
            throw new InvalidOperationException("Hint button particle state validation is unavailable.");

        gameUIView.SetHintButtonState(true, true);
        if (!(bool)isPlayingField.GetValue(particleEffect))
            throw new InvalidOperationException("Hint button particles must play when the button is interactable.");

        gameUIView.SetHintButtonState(true, false);
        if ((bool)isPlayingField.GetValue(particleEffect))
            throw new InvalidOperationException("Hint button particles must stop when the button is disabled.");

        ValidateHintTexture(HintGlowTexturePath);
        ValidateHintTexture(HintPathTexturePath);
        ValidateHintParticlePrefab(
            HintButtonPrefabPath,
            HintGlowTexturePath,
            ParticleSystemRenderSpace.View);
        ValidateHintParticlePrefab(
            HintRoutePrefabPath,
            HintPathTexturePath,
            ParticleSystemRenderSpace.Local);
        ValidateHintRouteReference();
        ValidateHintRouteDirection();
    }

    private static void ValidateHintButtonScreenCenter(
        HintButtonParticleEffect particleEffect,
        Button hintButton)
    {
        MethodInfo getButtonScreenCenterMethod = typeof(HintButtonParticleEffect).GetMethod(
            "GetButtonScreenCenter",
            BindingFlags.Instance | BindingFlags.NonPublic);
        RectTransform hintButtonRect = hintButton.transform as RectTransform;
        if (getButtonScreenCenterMethod == null || hintButtonRect == null)
            throw new InvalidOperationException("Hint button screen-center validation is unavailable.");

        Vector3[] worldCorners = new Vector3[4];
        hintButtonRect.GetWorldCorners(worldCorners);
        Vector3 expectedWorldCenter = (worldCorners[0] + worldCorners[2]) * 0.5f;
        Vector2 expectedScreenCenter =
            RectTransformUtility.WorldToScreenPoint(null, expectedWorldCenter);
        object screenCenterResult =
            getButtonScreenCenterMethod.Invoke(particleEffect, new object[] { null });
        if (!(screenCenterResult is Vector2)
            || Vector2.Distance((Vector2)screenCenterResult, expectedScreenCenter) > 0.01f)
        {
            throw new InvalidOperationException("Hint particle must follow the RectTransform center, not its pivot.");
        }
    }

    private static void ValidateHintTexture(string texturePath)
    {
        Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>(texturePath);
        TextureImporter textureImporter = AssetImporter.GetAtPath(texturePath) as TextureImporter;
        if (texture == null || textureImporter == null)
            throw new InvalidOperationException($"Generated hint texture is missing: {texturePath}");

        if (texture.width != 256 || texture.height != 256)
            throw new InvalidOperationException($"Generated hint texture must be 256x256: {texturePath}");

        if (textureImporter.textureType != TextureImporterType.Sprite
            || !textureImporter.alphaIsTransparency
            || textureImporter.mipmapEnabled)
        {
            throw new InvalidOperationException($"Generated hint texture import settings are invalid: {texturePath}");
        }
    }

    private static void ValidateHintParticlePrefab(
        string prefabPath,
        string texturePath,
        ParticleSystemRenderSpace expectedAlignment)
    {
        GameObject particlePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
        if (particlePrefab == null)
            throw new InvalidOperationException($"Generated hint particle prefab is missing: {prefabPath}");

        ParticleSystem particleSystem = particlePrefab.GetComponentInChildren<ParticleSystem>(true);
        ParticleSystemRenderer particleRenderer =
            particlePrefab.GetComponentInChildren<ParticleSystemRenderer>(true);
        if (particleSystem == null || particleRenderer == null || particleRenderer.sharedMaterial == null)
            throw new InvalidOperationException($"Generated hint particle prefab is incomplete: {prefabPath}");

        if (particleRenderer.alignment != expectedAlignment)
            throw new InvalidOperationException($"Generated hint particle alignment is invalid: {prefabPath}");

        Texture2D expectedTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(texturePath);
        if (expectedTexture == null || particleRenderer.sharedMaterial.mainTexture != expectedTexture)
            throw new InvalidOperationException($"Generated hint particle texture is invalid: {prefabPath}");

        ParticleSystem.MainModule main = particleSystem.main;
        ParticleSystem.EmissionModule emission = particleSystem.emission;
        if (!main.loop || !main.playOnAwake || !emission.enabled)
            throw new InvalidOperationException($"Generated hint particle playback settings are invalid: {prefabPath}");

        ParticleSystem.VelocityOverLifetimeModule velocity = particleSystem.velocityOverLifetime;
        if (velocity.x.mode != velocity.y.mode || velocity.x.mode != velocity.z.mode)
            throw new InvalidOperationException($"Particle velocity curves must use the same mode: {prefabPath}");

        ValidateHintParticlePlayback(particlePrefab, prefabPath);
    }

    private static void ValidateHintParticlePlayback(GameObject particlePrefab, string prefabPath)
    {
        bool velocityModeErrorLogged = false;
        Application.LogCallback logHandler =
            (logCondition, logStackTrace, logType) =>
            {
                if (logCondition.Contains("Particle Velocity curves must all be in the same mode"))
                    velocityModeErrorLogged = true;
            };
        GameObject particleInstance = null;
        Application.logMessageReceived += logHandler;
        try
        {
            particleInstance = UnityEngine.Object.Instantiate(particlePrefab);
            ParticleSystem[] particleSystems =
                particleInstance.GetComponentsInChildren<ParticleSystem>(true);
            for (int particleIndex = 0; particleIndex < particleSystems.Length; particleIndex++)
            {
                ParticleSystem particleSystem = particleSystems[particleIndex];
                particleSystem.Play(true);
                particleSystem.Simulate(2.5f, true, false, true);
                particleSystem.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            }
        }
        finally
        {
            Application.logMessageReceived -= logHandler;
            if (particleInstance != null)
                UnityEngine.Object.DestroyImmediate(particleInstance);
        }

        if (velocityModeErrorLogged)
            throw new InvalidOperationException($"Particle velocity mode error occurred during playback: {prefabPath}");
    }

    private static void ValidateHintRouteReference()
    {
        MapGenerator mapGenerator = UnityEngine.Object.FindFirstObjectByType<MapGenerator>(FindObjectsInactive.Include);
        if (mapGenerator == null)
            throw new InvalidOperationException("MapGenerator is missing from InGameScene.");

        SerializedObject serializedMapGenerator = new SerializedObject(mapGenerator);
        GameObject routePrefab = GetRequiredReference<GameObject>(serializedMapGenerator, "hintEffectPrefab");
        SerializedProperty routeScale = serializedMapGenerator.FindProperty("hintEffectScale");
        if (AssetDatabase.GetAssetPath(routePrefab) != HintRoutePrefabPath)
            throw new InvalidOperationException("Hint route must use the generated directional light prefab.");

        if (routeScale == null || !Mathf.Approximately(routeScale.floatValue, 1f))
            throw new InvalidOperationException("Hint route particle scale must match the generated prefab scale.");
    }

    private static void ValidateHintRouteDirection()
    {
        MethodInfo getHintDirectionMethod = typeof(MapGenerator).GetMethod(
            "GetHintDirection",
            BindingFlags.Static | BindingFlags.NonPublic);
        if (getHintDirectionMethod == null)
            throw new InvalidOperationException("Hint route direction method is missing.");

        Vector2Int[] cardinalDirections =
        {
            Vector2Int.right,
            Vector2Int.left,
            Vector2Int.up,
            Vector2Int.down
        };
        for (int directionIndex = 0; directionIndex < cardinalDirections.Length; directionIndex++)
        {
            Vector2Int expectedDirection = cardinalDirections[directionIndex];
            PuzzleRouteStep routeStep = new PuzzleRouteStep(
                expectedDirection,
                new Vector2Int(2, 3),
                directionIndex == 0);
            object directionResult = getHintDirectionMethod.Invoke(null, new object[] { routeStep });
            if (!(directionResult is Vector2Int)
                || (Vector2Int)directionResult != expectedDirection)
            {
                throw new InvalidOperationException("Hint route must use only the recorded cardinal input direction.");
            }
        }

        PuzzleRouteStep invalidRouteStep = new PuzzleRouteStep(
            new Vector2Int(1, 1),
            new Vector2Int(2, 3),
            false);
        object fallbackDirectionResult =
            getHintDirectionMethod.Invoke(null, new object[] { invalidRouteStep });
        if (!(fallbackDirectionResult is Vector2Int)
            || (Vector2Int)fallbackDirectionResult != Vector2Int.right)
        {
            throw new InvalidOperationException("Invalid hint directions must fall back to a cardinal direction.");
        }
    }

    private static void ValidateDestroyedHintParticleHandling()
    {
        GameObject validationEffectObject = new GameObject(
            "HintParticleDestroyedReferenceValidation",
            typeof(HintButtonParticleEffect));
        GameObject validationParticleObject = new GameObject(
            "DestroyedHintParticle",
            typeof(ParticleSystem));
        try
        {
            HintButtonParticleEffect validationEffect =
                validationEffectObject.GetComponent<HintButtonParticleEffect>();
            ParticleSystem destroyedParticleSystem =
                validationParticleObject.GetComponent<ParticleSystem>();
            FieldInfo particleSystemsField = typeof(HintButtonParticleEffect).GetField(
                "particleSystems",
                BindingFlags.Instance | BindingFlags.NonPublic);
            MethodInfo stopParticlesMethod = typeof(HintButtonParticleEffect).GetMethod(
                "StopParticles",
                BindingFlags.Instance | BindingFlags.NonPublic);
            if (validationEffect == null || particleSystemsField == null || stopParticlesMethod == null)
                throw new InvalidOperationException("Destroyed hint particle validation is unavailable.");

            particleSystemsField.SetValue(
                validationEffect,
                new ParticleSystem[] { destroyedParticleSystem });
            UnityEngine.Object.DestroyImmediate(validationParticleObject);
            stopParticlesMethod.Invoke(validationEffect, null);
        }
        finally
        {
            if (validationParticleObject != null)
                UnityEngine.Object.DestroyImmediate(validationParticleObject);

            if (validationEffectObject != null)
                UnityEngine.Object.DestroyImmediate(validationEffectObject);
        }
    }

    private static void ValidateSpotlightMesh(TutorialSpotlightGraphic tutorialSpotlight)
    {
        MethodInfo populateMeshMethod = typeof(TutorialSpotlightGraphic).GetMethod(
            "OnPopulateMesh",
            BindingFlags.Instance | BindingFlags.NonPublic,
            null,
            new Type[] { typeof(VertexHelper) },
            null);
        if (populateMeshMethod == null)
            throw new InvalidOperationException("Tutorial spotlight mesh method is missing.");

        tutorialSpotlight.SetFocus(new Rect(-85f, -85f, 170f, 170f));
        VertexHelper vertexHelper = new VertexHelper();
        populateMeshMethod.Invoke(tutorialSpotlight, new object[] { vertexHelper });
        int vertexCount = vertexHelper.currentVertCount;
        vertexHelper.Dispose();
        tutorialSpotlight.ClearFocus();
        if (vertexCount != 16)
            throw new InvalidOperationException("Tutorial spotlight must render its four regions in one mesh.");
    }

    private static void ValidateTutorialFont(TMP_Text textComponent, string role)
    {
        if (textComponent.font == null || !textComponent.font.name.Contains("Pretendard"))
            throw new InvalidOperationException($"Tutorial {role} text does not use the Korean-capable font.");
    }

    private static void ValidateResultDismissal(GameUIView gameUIView, SerializedObject serializedView)
    {
        TMP_Text primaryActionButtonText = GetRequiredReference<TMP_Text>(serializedView, "primaryActionButtonText");
        MethodInfo setClearModeMethod = typeof(GameUIView).GetMethod(
            "SetClearModeObjects",
            BindingFlags.Instance | BindingFlags.NonPublic);
        MethodInfo notifyPrimaryActionMethod = typeof(GameUIView).GetMethod(
            "NotifyPrimaryActionClicked",
            BindingFlags.Instance | BindingFlags.NonPublic);

        if (setClearModeMethod == null || notifyPrimaryActionMethod == null)
            throw new InvalidOperationException("Result panel validation methods are missing.");

        gameUIView.SetNextStageAvailable(true);
        setClearModeMethod.Invoke(gameUIView, null);
        string labelBeforeDismissal = primaryActionButtonText.text;
        int nextRequestCount = 0;
        Action countNextRequest = () => nextRequestCount++;
        gameUIView.NextClicked += countNextRequest;

        notifyPrimaryActionMethod.Invoke(gameUIView, null);
        gameUIView.SetNextStageAvailable(false);
        notifyPrimaryActionMethod.Invoke(gameUIView, null);
        gameUIView.NextClicked -= countNextRequest;

        if (primaryActionButtonText.text != labelBeforeDismissal)
            throw new InvalidOperationException("Result button label changed while the panel was dismissing.");

        if (nextRequestCount != 1)
            throw new InvalidOperationException("Result button accepted multiple next-stage requests.");
    }

    private static T GetRequiredReference<T>(SerializedObject serializedObject, string propertyName)
        where T : UnityEngine.Object
    {
        SerializedProperty serializedProperty = serializedObject.FindProperty(propertyName);
        if (serializedProperty == null)
            throw new InvalidOperationException($"Serialized property is missing: {propertyName}");

        T referencedObject = serializedProperty.objectReferenceValue as T;
        if (referencedObject == null)
            throw new InvalidOperationException($"Serialized reference is missing: {propertyName}");

        return referencedObject;
    }

    private static void ValidateStageFour()
    {
        MapData stageData = LoadStage(StageFourPath);
        Vector2Int startPosition;
        SerializedTile startTile;
        if (!TryFindFirstTile(stageData, tileData => tileData.type == TileType.Start, out startPosition, out startTile))
            throw new InvalidOperationException("Stage 4 start tile is missing.");

        Vector2Int closestPositivePosition = default;
        SerializedTile closestPositiveTile = default;
        int closestDistance = int.MaxValue;
        VisitTiles(stageData, (tileData, tilePosition) =>
        {
            if (tileData.type != TileType.Move || tileData.value <= 0)
                return;

            int distance = Mathf.Abs(tilePosition.x - startPosition.x) + Mathf.Abs(tilePosition.y - startPosition.y);
            if (distance >= closestDistance)
                return;

            closestDistance = distance;
            closestPositivePosition = tilePosition;
            closestPositiveTile = tileData;
        });

        if (closestDistance == int.MaxValue
            || closestPositiveTile.value != 2
            || closestPositivePosition != new Vector2Int(2, 2))
        {
            throw new InvalidOperationException("Stage 4 nearest positive MoveTile must be the +2 tile at (2, 2).");
        }

        Vector2Int exitPosition;
        SerializedTile exitTile;
        if (!TryFindFirstTile(
                stageData,
                tileData => tileData.type == TileType.Exit && tileData.exitCondition != ExitCondition.Free,
                out exitPosition,
                out exitTile)
            || exitTile.exitCondition != ExitCondition.OddOnly)
        {
            throw new InvalidOperationException("Stage 4 conditional exit must be OddOnly.");
        }
    }

    private static void ValidateStageOne()
    {
        MapData stageData = LoadStage(StageOnePath);
        Vector2Int startPosition;
        SerializedTile startTile;
        Vector2Int exitPosition;
        SerializedTile exitTile;
        bool hasStart = TryFindFirstTile(
            stageData,
            tileData => tileData.type == TileType.Start,
            out startPosition,
            out startTile);
        bool hasExit = TryFindFirstTile(
            stageData,
            tileData => tileData.type == TileType.Exit,
            out exitPosition,
            out exitTile);
        if (!hasStart || !hasExit || exitPosition.y != startPosition.y || exitPosition.x <= startPosition.x)
            throw new InvalidOperationException("Stage 1 swipe tutorial requires an exit to the right of the start tile.");
    }

    private static void ValidateStageFive()
    {
        MapData stageData = LoadStage(StageFivePath);
        Vector2Int negativeMovePosition;
        SerializedTile negativeMoveTile;
        if (!TryFindFirstTile(
                stageData,
                tileData => tileData.type == TileType.Move && tileData.value < 0,
                out negativeMovePosition,
                out negativeMoveTile))
        {
            throw new InvalidOperationException("Stage 5 negative MoveTile is missing.");
        }
    }

    private static void ValidateStageSix()
    {
        MapData stageData = LoadStage(StageSixPath);
        Vector2Int obstaclePosition;
        SerializedTile obstacleTile;
        if (!TryFindFirstTile(
                stageData,
                tileData => tileData.type == TileType.NumberObstacle,
                out obstaclePosition,
                out obstacleTile))
        {
            throw new InvalidOperationException("Stage 6 NumberObstacle is missing.");
        }
    }

    private static MapData LoadStage(string stagePath)
    {
        MapData stageData = AssetDatabase.LoadAssetAtPath<MapData>(stagePath);
        if (stageData == null)
            throw new InvalidOperationException($"Failed to load {stagePath}.");

        return stageData;
    }

    private static bool TryFindFirstTile(
        MapData mapData,
        Predicate<SerializedTile> matchesTile,
        out Vector2Int tilePosition,
        out SerializedTile matchedTile)
    {
        tilePosition = default;
        matchedTile = default;
        if (mapData == null || mapData.rows == null || matchesTile == null)
            return false;

        for (int rowIndex = 0; rowIndex < mapData.rows.Length; rowIndex++)
        {
            Wrapper<SerializedTile> row = mapData.rows[rowIndex];
            if (row == null || row.values == null)
                continue;

            for (int columnIndex = 0; columnIndex < row.values.Length; columnIndex++)
            {
                SerializedTile tileData = row.values[columnIndex];
                if (!matchesTile(tileData))
                    continue;

                tilePosition = new Vector2Int(columnIndex, rowIndex);
                matchedTile = tileData;
                return true;
            }
        }

        return false;
    }

    private static void VisitTiles(MapData mapData, Action<SerializedTile, Vector2Int> visitTile)
    {
        if (mapData.rows == null)
            return;

        for (int rowIndex = 0; rowIndex < mapData.rows.Length; rowIndex++)
        {
            Wrapper<SerializedTile> row = mapData.rows[rowIndex];
            if (row == null || row.values == null)
                continue;

            for (int columnIndex = 0; columnIndex < row.values.Length; columnIndex++)
                visitTile(row.values[columnIndex], new Vector2Int(columnIndex, rowIndex));
        }
    }

    private static void ValidateLocalization()
    {
        StringTable englishTable = AssetDatabase.LoadAssetAtPath<StringTable>(EnglishTablePath);
        StringTable koreanTable = AssetDatabase.LoadAssetAtPath<StringTable>(KoreanTablePath);
        if (englishTable == null || koreanTable == null)
            throw new InvalidOperationException("Tutorial localization tables could not be loaded.");

        ValidateLocalizedEntry(englishTable, "Tutorial.PositiveMoveTile", "+ Move Tile\nStep on it to gain the shown number of moves.");
        ValidateLocalizedEntry(englishTable, "Tutorial.NegativeMoveTile", "- Move Tile\nStep on it to lose the shown number of moves.");
        ValidateLocalizedEntry(englishTable, "Tutorial.ExitConditionOdd", "Exit Condition\nYou can exit only when the remaining moves are odd.");
        ValidateLocalizedEntry(englishTable, "Tutorial.ExitConditionEven", "Exit Condition\nYou can exit only when the remaining moves are even.");
        ValidateLocalizedEntry(englishTable, "Tutorial.NumberObstacle", "Number Obstacle\nSpend the shown number of moves to push it.");
        ValidateLocalizedEntry(englishTable, "Tutorial.Swipe", "Swipe right to move.");

        ValidateLocalizedEntry(koreanTable, "Tutorial.PositiveMoveTile", "+ 이동 타일\n밟으면 표시된 수만큼 이동 횟수가 늘어납니다.");
        ValidateLocalizedEntry(koreanTable, "Tutorial.NegativeMoveTile", "- 이동 타일\n밟으면 표시된 수만큼 이동 횟수가 줄어듭니다.");
        ValidateLocalizedEntry(koreanTable, "Tutorial.ExitConditionOdd", "출구 조건\n남은 이동 횟수가 홀수일 때만 나갈 수 있습니다.");
        ValidateLocalizedEntry(koreanTable, "Tutorial.ExitConditionEven", "출구 조건\n남은 이동 횟수가 짝수일 때만 나갈 수 있습니다.");
        ValidateLocalizedEntry(koreanTable, "Tutorial.NumberObstacle", "숫자 장애물\n표시된 숫자만큼 이동 횟수를 사용해 밀 수 있습니다.");
        ValidateLocalizedEntry(koreanTable, "Tutorial.Swipe", "화면을 오른쪽으로 밀어 이동하세요.");
    }

    private static void ValidateLocalizedEntry(StringTable table, string key, string expectedValue)
    {
        StringTableEntry tableEntry = table.GetEntry(key);
        if (tableEntry == null || tableEntry.LocalizedValue != expectedValue)
            throw new InvalidOperationException($"Localization value mismatch: {table.LocaleIdentifier.Code}/{key}");
    }

    private static void ValidatePanelLayout()
    {
        Vector2 panelSize = new Vector2(600f, 266f);
        Rect referenceCanvas = new Rect(-540f, -960f, 1080f, 1920f);
        TutorialPanelPlacement centerPlacement = TutorialPanelLayout.Calculate(
            referenceCanvas,
            Vector2.zero,
            panelSize,
            SafeMargin,
            12f);
        if (!centerPlacement.IsAboveTarget || !Mathf.Approximately(centerPlacement.Position.x, 0f))
            throw new InvalidOperationException("Tutorial panel does not point above a centered target.");

        TutorialPanelPlacement topPlacement = TutorialPanelLayout.Calculate(
            referenceCanvas,
            new Vector2(0f, 900f),
            panelSize,
            SafeMargin,
            12f);
        if (topPlacement.IsAboveTarget)
            throw new InvalidOperationException("Tutorial panel must flip below a target near the top edge.");

        AssertPanelInside(referenceCanvas, centerPlacement, panelSize);
        AssertPanelInside(referenceCanvas, topPlacement, panelSize);
        AssertPanelInside(
            new Rect(-960f, -540f, 1920f, 1080f),
            TutorialPanelLayout.Calculate(
                new Rect(-960f, -540f, 1920f, 1080f),
                new Vector2(930f, 0f),
                panelSize,
                SafeMargin,
                12f),
            panelSize);
        AssertPanelInside(
            new Rect(-540f, -1200f, 1080f, 2400f),
            TutorialPanelLayout.Calculate(
                new Rect(-540f, -1200f, 1080f, 2400f),
                new Vector2(-520f, -1100f),
                panelSize,
                SafeMargin,
                12f),
            panelSize);
    }

    private static void AssertPanelInside(
        Rect canvasRect,
        TutorialPanelPlacement placement,
        Vector2 panelSize)
    {
        float halfWidth = panelSize.x * 0.5f;
        float halfHeight = panelSize.y * 0.5f;
        if (placement.Position.x - halfWidth < canvasRect.xMin + SafeMargin
            || placement.Position.x + halfWidth > canvasRect.xMax - SafeMargin
            || placement.Position.y - halfHeight < canvasRect.yMin + SafeMargin
            || placement.Position.y + halfHeight > canvasRect.yMax - SafeMargin)
        {
            throw new InvalidOperationException("Tutorial panel escaped the canvas safe area.");
        }
    }

    private static void ValidateMusicMixer()
    {
        AudioMixer musicMixer = AssetDatabase.LoadAssetAtPath<AudioMixer>(MusicMixerPath);
        if (musicMixer == null)
            throw new InvalidOperationException("MusicMixer asset is missing.");

        AudioMixerGroup[] matchingGroups = musicMixer.FindMatchingGroups("Music");
        if (matchingGroups == null || matchingGroups.Length == 0)
            throw new InvalidOperationException("MusicMixer Music group is missing.");

        object mixerController = musicMixer;
        object musicGroup = matchingGroups[0];
        Type mixerControllerType = mixerController.GetType();
        PropertyInfo targetSnapshotProperty = mixerControllerType.GetProperty(
            "TargetSnapshot",
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        if (targetSnapshotProperty == null)
            throw new InvalidOperationException("MusicMixer target snapshot is missing.");

        object targetSnapshot = targetSnapshotProperty.GetValue(mixerController);
        Type groupType = musicGroup.GetType();
        MethodInfo getVolumeMethod = groupType.GetMethod(
            "GetValueForVolume",
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        PropertyInfo effectsProperty = groupType.GetProperty(
            "effects",
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        if (targetSnapshot == null || getVolumeMethod == null || effectsProperty == null)
            throw new InvalidOperationException("MusicMixer validation data is missing.");

        object gainValue = getVolumeMethod.Invoke(
            musicGroup,
            new object[] { mixerController, targetSnapshot });
        if (!(gainValue is float) || !Mathf.Approximately((float)gainValue, 3f))
            throw new InvalidOperationException("MusicMixer must apply +3 dB gain.");

        Array effects = effectsProperty.GetValue(musicGroup) as Array;
        object compressorEffect = FindMixerEffect(effects, "Compressor");
        if (compressorEffect == null)
            throw new InvalidOperationException("MusicMixer peak limiter is missing.");

        ValidateMixerEffectParameter(compressorEffect, mixerController, targetSnapshot, "Threshold", -1f);
        ValidateMixerEffectParameter(compressorEffect, mixerController, targetSnapshot, "Attack", 0.1f);
        ValidateMixerEffectParameter(compressorEffect, mixerController, targetSnapshot, "Release", 50f);
        ValidateMixerEffectParameter(compressorEffect, mixerController, targetSnapshot, "Make up gain", 0f);
    }

    private static object FindMixerEffect(Array effects, string requiredEffectName)
    {
        if (effects == null)
            return null;

        for (int effectIndex = 0; effectIndex < effects.Length; effectIndex++)
        {
            object effect = effects.GetValue(effectIndex);
            if (effect == null)
                continue;

            PropertyInfo effectNameProperty = effect.GetType().GetProperty(
                "effectName",
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            string effectName = effectNameProperty == null
                ? string.Empty
                : effectNameProperty.GetValue(effect) as string;
            if (effectName == requiredEffectName)
                return effect;
        }

        return null;
    }

    private static void ValidateMixerEffectParameter(
        object effect,
        object mixerController,
        object targetSnapshot,
        string parameterName,
        float expectedValue)
    {
        MethodInfo getParameterMethod = effect.GetType().GetMethod(
            "GetValueForParameter",
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        if (getParameterMethod == null)
            throw new InvalidOperationException("MusicMixer parameter validation method is missing.");

        object parameterValue = getParameterMethod.Invoke(
            effect,
            new object[] { mixerController, targetSnapshot, parameterName });
        if (!(parameterValue is float) || !Mathf.Approximately((float)parameterValue, expectedValue))
            throw new InvalidOperationException($"MusicMixer parameter mismatch: {parameterName}");
    }

    private static bool Approximately(Vector2 left, Vector2 right)
    {
        return Mathf.Approximately(left.x, right.x) && Mathf.Approximately(left.y, right.y);
    }
}
