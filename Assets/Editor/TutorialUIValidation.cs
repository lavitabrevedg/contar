using System;
using System.Reflection;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Localization.Tables;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class TutorialUIValidation
{
    private const string InGameScenePath = "Assets/Scenes/InGameScene.unity";
    private const string StageFourPath = "Assets/Data/Stages/Stage_004.asset";
    private const string StageFivePath = "Assets/Data/Stages/Stage_005.asset";
    private const string StageSixPath = "Assets/Data/Stages/Stage_006.asset";
    private const string EnglishTablePath = "Assets/Localization/UI/UI_en.asset";
    private const string KoreanTablePath = "Assets/Localization/UI/UI_ko.asset";
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
        ValidateResultDismissal(gameUIView, serializedView);
        ValidateStageFour();
        ValidateStageFive();
        ValidateStageSix();
        ValidateLocalization();
        ValidatePanelLayout();
        Debug.Log("[TutorialUIValidation] Tracking tutorial UI, localization, stages, layout, and result dismissal are valid.");
    }

    private static void ValidateTutorialHierarchy(SerializedObject serializedView)
    {
        GameObject tutorialDialog = GetRequiredReference<GameObject>(serializedView, "tutorialDialog");
        TMP_Text tutorialMessageText = GetRequiredReference<TMP_Text>(serializedView, "tutorialMessageText");
        Button tutorialAdvanceButton = GetRequiredReference<Button>(serializedView, "tutorialAdvanceButton");
        RectTransform tutorialPanelRect = GetRequiredReference<RectTransform>(serializedView, "tutorialPanelRect");
        Image tutorialPanelImage = GetRequiredReference<Image>(serializedView, "tutorialPanelImage");
        RectTransform dimmerLeft = GetRequiredReference<RectTransform>(serializedView, "tutorialDimmerLeft");
        RectTransform dimmerRight = GetRequiredReference<RectTransform>(serializedView, "tutorialDimmerRight");
        RectTransform dimmerTop = GetRequiredReference<RectTransform>(serializedView, "tutorialDimmerTop");
        RectTransform dimmerBottom = GetRequiredReference<RectTransform>(serializedView, "tutorialDimmerBottom");

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
        ValidateDimmer(dimmerLeft, tutorialDialog.transform);
        ValidateDimmer(dimmerRight, tutorialDialog.transform);
        ValidateDimmer(dimmerTop, tutorialDialog.transform);
        ValidateDimmer(dimmerBottom, tutorialDialog.transform);
    }

    private static void ValidateTutorialFont(TMP_Text textComponent, string role)
    {
        if (textComponent.font == null || !textComponent.font.name.Contains("Pretendard"))
            throw new InvalidOperationException($"Tutorial {role} text does not use the Korean-capable font.");
    }

    private static void ValidateDimmer(RectTransform dimmerRect, Transform tutorialDialogTransform)
    {
        Image dimmerImage = dimmerRect.GetComponent<Image>();
        if (dimmerRect.parent != tutorialDialogTransform || dimmerImage == null || dimmerImage.raycastTarget)
            throw new InvalidOperationException($"Invalid tutorial dimmer: {dimmerRect.name}");
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

        ValidateLocalizedEntry(koreanTable, "Tutorial.PositiveMoveTile", "+ 이동 타일\n밟으면 표시된 수만큼 이동 횟수가 늘어납니다.");
        ValidateLocalizedEntry(koreanTable, "Tutorial.NegativeMoveTile", "- 이동 타일\n밟으면 표시된 수만큼 이동 횟수가 줄어듭니다.");
        ValidateLocalizedEntry(koreanTable, "Tutorial.ExitConditionOdd", "출구 조건\n남은 이동 횟수가 홀수일 때만 나갈 수 있습니다.");
        ValidateLocalizedEntry(koreanTable, "Tutorial.ExitConditionEven", "출구 조건\n남은 이동 횟수가 짝수일 때만 나갈 수 있습니다.");
        ValidateLocalizedEntry(koreanTable, "Tutorial.NumberObstacle", "숫자 장애물\n표시된 숫자만큼 이동 횟수를 사용해 밀 수 있습니다.");
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

    private static bool Approximately(Vector2 left, Vector2 right)
    {
        return Mathf.Approximately(left.x, right.x) && Mathf.Approximately(left.y, right.y);
    }
}
