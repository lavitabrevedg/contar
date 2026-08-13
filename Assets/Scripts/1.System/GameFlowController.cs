using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameFlowController : MonoBehaviour
{
    private const float HintAvailabilityRefreshInterval = 0.5f;
    private const int PositiveMoveAndExitTutorialStageIndex = 3;
    private const int NegativeMoveTutorialStageIndex = 4;
    private const int NumberObstacleTutorialStageIndex = 5;
    private const string PositiveMoveTutorialCompletedKey = "Tutorial.PositiveMove.Completed";
    private const string NegativeMoveTutorialCompletedKey = "Tutorial.NegativeMove.Completed";
    private const string ExitConditionTutorialCompletedKey = "Tutorial.ExitCondition.Completed";
    private const string NumberObstacleTutorialCompletedKey = "Tutorial.NumberObstacle.Completed";

    public static void ResetTutorialProgress()
    {
        PlayerPrefs.DeleteKey(PositiveMoveTutorialCompletedKey);
        PlayerPrefs.DeleteKey(NegativeMoveTutorialCompletedKey);
        PlayerPrefs.DeleteKey(ExitConditionTutorialCompletedKey);
        PlayerPrefs.DeleteKey(NumberObstacleTutorialCompletedKey);
        PlayerPrefs.Save();
    }

    [SerializeField] private GameUIPresenter uiPresenter;
    [SerializeField] private GameManager gameManager;
    [SerializeField] private StageCatalog stageCatalog;
    [SerializeField] private StageProgressService progressService;
    [SerializeField] private GoogleAdMobService googleAdMobService;
    [SerializeField] private bool useInspectorStageOnStart;

    private IAdService adService;
    private AudioService audioService;
    private PuzzleSolveResult pendingHintResult;
    private bool isBound;
    private bool isShowingAd;
    private bool isHintUnlocked;
    private bool isMapRevealPlaying;
    private TutorialStep[] activeTutorialSteps;
    private int activeTutorialStepIndex;
    private int lastTutorialAdvanceFrame = -1;
    private float nextHintAvailabilityRefreshTime;

    private void Awake()
    {
        ResolveReferences();
    }

    private void OnEnable()
    {
        Bind();
    }

    private void Start()
    {
        Bind();
        bool stageLoaded = LoadInitialStage();
        PlayMapReveal(stageLoaded);
        RefreshHintAvailability();
    }

    private void Update()
    {
        if (Time.unscaledTime < nextHintAvailabilityRefreshTime)
            return;

        nextHintAvailabilityRefreshTime = Time.unscaledTime + HintAvailabilityRefreshInterval;
        RefreshHintAvailability();
    }

    private void OnDisable()
    {
        Unbind();
        EndHintInteraction(false);
        EndTutorial(false);
    }

    private void ResolveReferences()
    {
        if (uiPresenter == null)
            uiPresenter = FindFirstObjectByType<GameUIPresenter>();

        if (gameManager == null)
            gameManager = GameManager.Instance;

        if (gameManager == null)
            gameManager = FindFirstObjectByType<GameManager>();

        if (progressService == null && gameManager != null)
            progressService = gameManager.ProgressService;

        if (progressService == null)
            progressService = FindFirstObjectByType<StageProgressService>();

        if (progressService == null && gameManager != null)
            progressService = gameManager.gameObject.AddComponent<StageProgressService>();

        if (stageCatalog == null)
            stageCatalog = Resources.Load<StageCatalog>("SettingDatas/StageCatalog");

        if (audioService == null)
            audioService = FindFirstObjectByType<AudioService>();

        ResolveAdService();
    }

    private void ResolveAdService()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        if (googleAdMobService == null)
            googleAdMobService = GoogleAdMobService.Instance;

        if (googleAdMobService == null)
            googleAdMobService = FindFirstObjectByType<GoogleAdMobService>();

        if (googleAdMobService == null)
        {
            GameObject adServiceObject = new GameObject("GoogleAdMobService");
            googleAdMobService = adServiceObject.AddComponent<GoogleAdMobService>();
        }

        adService = googleAdMobService;
#elif UNITY_EDITOR
        adService = new EditorHintAdService();
#else
        adService = null;
#endif
    }

    private void Bind()
    {
        if (isBound)
            return;

        ResolveReferences();
        if (uiPresenter == null)
            return;

        uiPresenter.NextStageRequested += OnNextStageRequested;
        uiPresenter.RestartRequested += OnRestartRequested;
        uiPresenter.LobbyRequested += OnLobbyRequested;
        uiPresenter.HintRequested += OnHintRequested;
        uiPresenter.HintConfirmed += OnHintConfirmed;
        uiPresenter.HintCanceled += OnHintCanceled;
        uiPresenter.TutorialAdvanceRequested += OnTutorialAdvanceRequested;
        isBound = true;
    }

    private void Unbind()
    {
        if (!isBound || uiPresenter == null)
            return;

        uiPresenter.NextStageRequested -= OnNextStageRequested;
        uiPresenter.RestartRequested -= OnRestartRequested;
        uiPresenter.LobbyRequested -= OnLobbyRequested;
        uiPresenter.HintRequested -= OnHintRequested;
        uiPresenter.HintConfirmed -= OnHintConfirmed;
        uiPresenter.HintCanceled -= OnHintCanceled;
        uiPresenter.TutorialAdvanceRequested -= OnTutorialAdvanceRequested;
        isBound = false;
    }

    private void OnNextStageRequested()
    {
        ResetHintAttempt();
        bool stageLoaded = LoadNextStage();
        PlayMapReveal(stageLoaded);
    }

    private void OnRestartRequested()
    {
        if (gameManager == null)
            ResolveReferences();

        if (gameManager == null)
            return;

        ResetHintAttempt();
        gameManager.RestartStage();
        PlayMapReveal(true);
    }

    private void OnLobbyRequested()
    {
        EndTutorial(false);
        ResetHintAttempt();
        SceneManager.LoadScene("LobbyScene");
    }

    private void OnHintRequested()
    {
        if (gameManager == null)
            ResolveReferences();

        if (gameManager == null || gameManager.State != GameState.Playing)
            return;

        if (gameManager.IsHintRouteVisible)
            return;

        gameManager.StopHintRoute();

        PuzzleSolveResult solveResult;
        if (!gameManager.TrySolveCurrentState(out solveResult))
            return;

        if (!solveResult.IsSolvable)
        {
            pendingHintResult = null;
            gameManager.SetInputBlocked(true);
            uiPresenter.ShowHintMessage("Restart Recommended");
            return;
        }

        if (isHintUnlocked)
        {
            gameManager.PlayHintRoute(solveResult.Route);
            return;
        }

        if (adService == null || !adService.IsReady(AdPlacement.HintRoute))
        {
            RefreshHintAvailability();
            return;
        }

        pendingHintResult = solveResult;
        gameManager.SetInputBlocked(true);
        uiPresenter.ShowHintConfirmation();
    }

    private void OnHintConfirmed()
    {
        if (isShowingAd || pendingHintResult == null)
            return;

        if (adService == null || !adService.IsReady(AdPlacement.HintRoute))
        {
            pendingHintResult = null;
            gameManager.SetInputBlocked(false);
            uiPresenter.HideHintDialog();
            RefreshHintAvailability();
            return;
        }

        isShowingAd = true;
        uiPresenter.HideHintDialog();
        RefreshHintAvailability();
        adService.Show(AdPlacement.HintRoute, OnHintAdCompleted);
    }

    private void OnHintAdCompleted(bool rewardEarned)
    {
        isShowingAd = false;
        pendingHintResult = null;

        if (!rewardEarned)
        {
            if (gameManager != null)
                gameManager.SetInputBlocked(false);

            uiPresenter.ShowHintMessage("Ad Not Completed");
            RefreshHintAvailability();
            return;
        }

        isHintUnlocked = true;
        if (gameManager != null)
            gameManager.SetInputBlocked(false);

        RevealCurrentRoute();
        RefreshHintAvailability();
    }

    private void OnHintCanceled()
    {
        EndHintInteraction(true);
    }

    private void OnTutorialAdvanceRequested()
    {
        if (activeTutorialSteps == null || lastTutorialAdvanceFrame == Time.frameCount)
            return;

        lastTutorialAdvanceFrame = Time.frameCount;
        TutorialStep completedStep = activeTutorialSteps[activeTutorialStepIndex];
        PlayerPrefs.SetInt(completedStep.CompletedKey, 1);
        PlayerPrefs.Save();

        activeTutorialStepIndex++;
        if (activeTutorialStepIndex < activeTutorialSteps.Length)
        {
            if (!ShowActiveTutorialStep())
                EndTutorial(true);

            return;
        }

        EndTutorial(true);
    }

    private void RevealCurrentRoute()
    {
        if (gameManager == null)
            return;

        PuzzleSolveResult solveResult;
        if (!gameManager.TrySolveCurrentState(out solveResult) || !solveResult.IsSolvable)
        {
            gameManager.SetInputBlocked(true);
            uiPresenter.ShowHintMessage("Restart Recommended");
            return;
        }

        gameManager.PlayHintRoute(solveResult.Route);
    }

    private void RefreshHintAvailability()
    {
        if (uiPresenter == null)
            return;

        bool isPlaying = gameManager != null && gameManager.State == GameState.Playing;
        bool adIsReady = adService != null && adService.IsReady(AdPlacement.HintRoute);
        bool isInteractable = isPlaying
            && !isShowingAd
            && !isMapRevealPlaying
            && (isHintUnlocked || adIsReady);
        uiPresenter.SetHintButtonState(isPlaying, isInteractable);
    }

    private void EndHintInteraction(bool refreshAvailability)
    {
        pendingHintResult = null;
        if (gameManager != null)
            gameManager.SetInputBlocked(false);

        if (uiPresenter != null)
            uiPresenter.HideHintDialog();

        if (refreshAvailability)
            RefreshHintAvailability();
    }

    private void ResetHintAttempt()
    {
        isHintUnlocked = false;
        isShowingAd = false;
        pendingHintResult = null;

        if (gameManager != null)
        {
            gameManager.StopHintRoute();
            gameManager.SetInputBlocked(false);
        }

        if (uiPresenter != null)
            uiPresenter.HideHintDialog();
    }

    private bool LoadInitialStage()
    {
        ResolveReferences();
        if (gameManager == null)
            return false;

        if (useInspectorStageOnStart && TryLoadInspectorStage())
            return true;

        if (LoadSavedStage())
            return true;

        return TryLoadInspectorStage();
    }

    private void PlayMapReveal(bool stageLoaded)
    {
        if (gameManager == null)
            ResolveReferences();

        MapGenerator mapGenerator = gameManager == null ? null : gameManager.MapGenerator;
        if (!stageLoaded || gameManager == null || mapGenerator == null)
        {
            CompleteMapReveal();
            return;
        }

        isMapRevealPlaying = true;
        gameManager.SetInputBlocked(true);
        mapGenerator.PlayInitialReveal(CompleteMapReveal);
    }

    private void CompleteMapReveal()
    {
        isMapRevealPlaying = false;

        if (audioService == null)
            audioService = FindFirstObjectByType<AudioService>();

        if (audioService != null)
            audioService.PlayBackgroundMusic();

        if (TryStartStageTutorial())
            return;

        if (gameManager != null)
            gameManager.SetInputBlocked(false);

        RefreshHintAvailability();
    }

    private bool TryStartStageTutorial()
    {
        if (progressService == null || gameManager == null || uiPresenter == null)
            return false;

        int stageIndex = progressService.CurrentStageIndex;
        List<TutorialStep> tutorialSteps = BuildTutorialSteps(stageIndex);
        return StartTutorial(tutorialSteps);
    }

    private List<TutorialStep> BuildTutorialSteps(int stageIndex)
    {
        List<TutorialStep> tutorialSteps = new List<TutorialStep>();
        MapGenerator mapGenerator = gameManager == null ? null : gameManager.MapGenerator;
        MapData mapData = mapGenerator == null ? null : mapGenerator.mapData;
        if (mapData == null)
            return tutorialSteps;

        if (stageIndex == PositiveMoveAndExitTutorialStageIndex)
        {
            AddPositiveMoveTutorialStep(mapData, tutorialSteps);
            AddExitConditionTutorialStep(mapData, tutorialSteps);
        }
        else if (stageIndex == NegativeMoveTutorialStageIndex)
        {
            AddFirstMatchingTutorialStep(
                mapData,
                TutorialMessage.NegativeMoveTile,
                NegativeMoveTutorialCompletedKey,
                tileData => tileData.type == TileType.Move && tileData.value < 0,
                tutorialSteps);
        }
        else if (stageIndex == NumberObstacleTutorialStageIndex)
        {
            AddFirstMatchingTutorialStep(
                mapData,
                TutorialMessage.NumberObstacle,
                NumberObstacleTutorialCompletedKey,
                tileData => tileData.type == TileType.NumberObstacle,
                tutorialSteps);
        }

        return tutorialSteps;
    }

    private void AddPositiveMoveTutorialStep(MapData mapData, List<TutorialStep> tutorialSteps)
    {
        if (PlayerPrefs.GetInt(PositiveMoveTutorialCompletedKey, 0) != 0)
            return;

        Vector2Int startPosition;
        if (!TryFindFirstTile(mapData, tileData => tileData.type == TileType.Start, out startPosition))
        {
            Debug.LogWarning("[GameFlowController] Positive MoveTile tutorial skipped because the start tile is missing.");
            return;
        }

        Vector2Int closestMovePosition;
        if (!TryFindClosestPositiveMoveTile(mapData, startPosition, out closestMovePosition))
        {
            Debug.LogWarning("[GameFlowController] Positive MoveTile tutorial target is missing.");
            return;
        }

        tutorialSteps.Add(new TutorialStep(
            TutorialMessage.PositiveMoveTile,
            closestMovePosition,
            PositiveMoveTutorialCompletedKey));
    }

    private void AddExitConditionTutorialStep(MapData mapData, List<TutorialStep> tutorialSteps)
    {
        if (PlayerPrefs.GetInt(ExitConditionTutorialCompletedKey, 0) != 0)
            return;

        Vector2Int exitPosition;
        SerializedTile exitTileData;
        if (!TryFindFirstTile(
                mapData,
                tileData => tileData.type == TileType.Exit && tileData.exitCondition != ExitCondition.Free,
                out exitPosition,
                out exitTileData))
        {
            Debug.LogWarning("[GameFlowController] Exit condition tutorial target is missing.");
            return;
        }

        TutorialMessage tutorialMessage = exitTileData.exitCondition == ExitCondition.EvenOnly
            ? TutorialMessage.ExitConditionEven
            : TutorialMessage.ExitConditionOdd;
        tutorialSteps.Add(new TutorialStep(
            tutorialMessage,
            exitPosition,
            ExitConditionTutorialCompletedKey));
    }

    private void AddFirstMatchingTutorialStep(
        MapData mapData,
        TutorialMessage tutorialMessage,
        string completedKey,
        System.Predicate<SerializedTile> matchesTile,
        List<TutorialStep> tutorialSteps)
    {
        if (PlayerPrefs.GetInt(completedKey, 0) != 0)
            return;

        Vector2Int targetPosition;
        if (!TryFindFirstTile(mapData, matchesTile, out targetPosition))
        {
            Debug.LogWarning($"[GameFlowController] Tutorial target is missing. tutorial={tutorialMessage}");
            return;
        }

        tutorialSteps.Add(new TutorialStep(tutorialMessage, targetPosition, completedKey));
    }

    private bool StartTutorial(List<TutorialStep> tutorialSteps)
    {
        if (tutorialSteps == null || tutorialSteps.Count == 0)
            return false;

        activeTutorialSteps = tutorialSteps.ToArray();
        activeTutorialStepIndex = 0;
        lastTutorialAdvanceFrame = -1;
        gameManager.SetInputBlocked(true);

        if (ShowActiveTutorialStep())
            return true;

        EndTutorial(false);
        return false;
    }

    private bool ShowActiveTutorialStep()
    {
        if (activeTutorialSteps == null
            || activeTutorialStepIndex < 0
            || activeTutorialStepIndex >= activeTutorialSteps.Length)
        {
            return false;
        }

        TutorialStep activeStep = activeTutorialSteps[activeTutorialStepIndex];
        MapGenerator mapGenerator = gameManager == null ? null : gameManager.MapGenerator;
        BaseTile targetTile = mapGenerator == null
            ? null
            : mapGenerator.GetTile(activeStep.GridPosition.x, activeStep.GridPosition.y);
        if (targetTile == null)
        {
            Debug.LogWarning($"[GameFlowController] Tutorial target disappeared. tutorial={activeStep.Message}");
            return false;
        }

        return uiPresenter.ShowTutorialStep(activeStep.Message, targetTile.transform);
    }

    private void EndTutorial(bool refreshHintAvailability)
    {
        activeTutorialSteps = null;
        activeTutorialStepIndex = 0;
        lastTutorialAdvanceFrame = -1;

        if (uiPresenter != null)
            uiPresenter.HideTutorialDialog();

        if (gameManager != null)
            gameManager.SetInputBlocked(false);

        if (refreshHintAvailability)
            RefreshHintAvailability();
    }

    private static bool TryFindClosestPositiveMoveTile(
        MapData mapData,
        Vector2Int startPosition,
        out Vector2Int closestMovePosition)
    {
        closestMovePosition = default;
        bool foundMoveTile = false;
        int closestDistance = int.MaxValue;

        for (int y = 0; y < mapData.height; y++)
        {
            Wrapper<SerializedTile> row = mapData.rows == null || y >= mapData.rows.Length
                ? null
                : mapData.rows[y];
            if (row == null || row.values == null)
                continue;

            for (int x = 0; x < mapData.width && x < row.values.Length; x++)
            {
                SerializedTile tileData = row.values[x];
                if (tileData.type != TileType.Move || tileData.value <= 0)
                    continue;

                int distance = Mathf.Abs(x - startPosition.x) + Mathf.Abs(y - startPosition.y);
                if (distance >= closestDistance)
                    continue;

                closestDistance = distance;
                closestMovePosition = new Vector2Int(x, y);
                foundMoveTile = true;
            }
        }

        return foundMoveTile;
    }

    private static bool TryFindFirstTile(
        MapData mapData,
        System.Predicate<SerializedTile> matchesTile,
        out Vector2Int tilePosition)
    {
        SerializedTile tileData;
        return TryFindFirstTile(mapData, matchesTile, out tilePosition, out tileData);
    }

    private static bool TryFindFirstTile(
        MapData mapData,
        System.Predicate<SerializedTile> matchesTile,
        out Vector2Int tilePosition,
        out SerializedTile matchedTileData)
    {
        tilePosition = default;
        matchedTileData = default;
        if (mapData == null || matchesTile == null)
            return false;

        for (int y = 0; y < mapData.height; y++)
        {
            Wrapper<SerializedTile> row = mapData.rows == null || y >= mapData.rows.Length
                ? null
                : mapData.rows[y];
            if (row == null || row.values == null)
                continue;

            for (int x = 0; x < mapData.width && x < row.values.Length; x++)
            {
                SerializedTile tileData = row.values[x];
                if (!matchesTile(tileData))
                    continue;

                tilePosition = new Vector2Int(x, y);
                matchedTileData = tileData;
                return true;
            }
        }

        return false;
    }

    private sealed class TutorialStep
    {
        public TutorialMessage Message { get; }
        public Vector2Int GridPosition { get; }
        public string CompletedKey { get; }

        public TutorialStep(TutorialMessage message, Vector2Int gridPosition, string completedKey)
        {
            Message = message;
            GridPosition = gridPosition;
            CompletedKey = completedKey;
        }
    }

    private bool TryLoadInspectorStage()
    {
        if (gameManager == null)
            return false;

        MapGenerator mapGenerator = gameManager.MapGenerator;
        MapData inspectorMapData = mapGenerator == null ? null : mapGenerator.mapData;
        if (inspectorMapData == null)
            return false;

        LoadInspectorStage(inspectorMapData);
        return true;
    }

    private void LoadInspectorStage(MapData mapData)
    {
        if (mapData == null || gameManager == null)
            return;

        int stageIndex = stageCatalog == null ? -1 : stageCatalog.IndexOf(mapData);
        if (stageIndex >= 0)
        {
            gameManager.SetStage(mapData, stageIndex);
            Debug.Log($"[GameFlowController] Loaded inspector stage. stageIndex={stageIndex}, stageName={mapData.name}");
            return;
        }

        MapGenerator mapGenerator = gameManager.MapGenerator;
        if (mapGenerator != null)
            mapGenerator.SetMapData(mapData, true);

        Debug.Log($"[GameFlowController] Loaded inspector map data outside catalog. stageName={mapData.name}");
    }

    private bool LoadSavedStage()
    {
        ResolveReferences();
        if (progressService == null || stageCatalog == null || gameManager == null)
            return false;

        int stageIndex = Mathf.Clamp(
            progressService.CurrentStageIndex,
            0,
            Mathf.Max(0, stageCatalog.StageCount - 1));
        return LoadStage(stageIndex);
    }

    private bool LoadStage(int stageIndex)
    {
        if (stageCatalog == null || gameManager == null)
            return false;

        MapData mapData;
        if (!stageCatalog.TryGetStage(stageIndex, out mapData))
            return false;

        gameManager.SetStage(mapData, stageIndex);
        Debug.Log($"[GameFlowController] Loaded stage. stageIndex={stageIndex}, stageName={mapData.name}");
        return true;
    }

    private bool LoadNextStage()
    {
        if (progressService == null || stageCatalog == null || gameManager == null)
            ResolveReferences();

        if (progressService == null || stageCatalog == null || gameManager == null)
        {
            Debug.LogWarning("[GameFlowController] Cannot load next stage because stage flow references are missing.");
            return false;
        }

        int nextStageIndex = progressService.CurrentStageIndex + 1;
        if (LoadStage(nextStageIndex))
            return true;

        Debug.Log("[GameFlowController] All stages are cleared.");
        uiPresenter.RefreshProgressView();
        return false;
    }
}
