using UnityEngine;
using UnityEngine.SceneManagement;

public class GameFlowController : MonoBehaviour
{
    private const float HintAvailabilityRefreshInterval = 0.5f;

    [SerializeField] private GameUIPresenter uiPresenter;
    [SerializeField] private GameManager gameManager;
    [SerializeField] private StageCatalog stageCatalog;
    [SerializeField] private StageProgressService progressService;
    [SerializeField] private GoogleAdMobService googleAdMobService;
    [SerializeField] private bool useInspectorStageOnStart;

    private IAdService adService;
    private PuzzleSolveResult pendingHintResult;
    private bool isBound;
    private bool isShowingAd;
    private bool isHintUnlocked;
    private bool isMapRevealPlaying;
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
                gameManager.SetInputBlocked(true);

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

        if (gameManager != null)
            gameManager.SetInputBlocked(false);

        RefreshHintAvailability();
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
