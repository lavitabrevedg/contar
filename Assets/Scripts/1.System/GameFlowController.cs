using System;
using UnityEngine;

public class GameFlowController : MonoBehaviour
{
    [SerializeField] private GameUIPresenter uiPresenter;
    [SerializeField] private GameManager gameManager;
    [SerializeField] private StageCatalog stageCatalog;
    [SerializeField] private StageProgressService progressService;
    [SerializeField] private DummyAdService dummyAdService;

    private bool isBound;
    private bool isShowingAd;

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
        LoadInitialStage();
    }

    private void OnDisable()
    {
        Unbind();
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
            stageCatalog = Resources.Load<StageCatalog>("StageCatalog");

        if (dummyAdService == null)
            dummyAdService = FindFirstObjectByType<DummyAdService>();

        if (dummyAdService == null && gameManager != null)
            dummyAdService = gameManager.gameObject.AddComponent<DummyAdService>();
    }

    private void Bind()
    {
        if (isBound) return;

        ResolveReferences();
        if (uiPresenter == null) return;

        uiPresenter.RetryRequested -= OnRetryRequested;
        uiPresenter.NextStageRequested -= OnNextStageRequested;
        uiPresenter.AdSkipTicketRequested -= OnAdSkipTicketRequested;
        uiPresenter.RetryRequested += OnRetryRequested;
        uiPresenter.NextStageRequested += OnNextStageRequested;
        uiPresenter.AdSkipTicketRequested += OnAdSkipTicketRequested;

        isBound = true;
    }

    private void Unbind()
    {
        if (!isBound || uiPresenter == null) return;

        uiPresenter.RetryRequested -= OnRetryRequested;
        uiPresenter.NextStageRequested -= OnNextStageRequested;
        uiPresenter.AdSkipTicketRequested -= OnAdSkipTicketRequested;

        isBound = false;
    }

    private void OnRetryRequested()
    {
        if (gameManager == null)
            ResolveReferences();

        if (gameManager == null) return;

        RestartStageAfterAdChoice();
    }

    private void OnNextStageRequested()
    {
        LoadNextStage();
    }

    private void OnAdSkipTicketRequested()
    {
        if (progressService == null || gameManager == null)
            ResolveReferences();

        if (gameManager == null)
        {
            Debug.LogWarning("[GameFlowController] Cannot restart stage because GameManager is missing.");
            return;
        }

        if (!IsAdRequiredForCurrentStage())
        {
            gameManager.RestartStage();
            return;
        }

        if (progressService != null && progressService.TryUseAdSkipTicket())
        {
            gameManager.RestartStage();
            return;
        }

        Debug.Log("[GameFlowController] Cannot skip ad because there are no skip tickets.");
        if (uiPresenter != null)
            uiPresenter.RefreshProgressView();
    }

    private void LoadInitialStage()
    {
        ResolveReferences();

        if (gameManager == null)
            return;

        MapGenerator mapGenerator = gameManager.MapGenerator;
        MapData inspectorMapData = mapGenerator == null ? null : mapGenerator.mapData;
        if (inspectorMapData != null)
        {
            LoadInspectorStage(inspectorMapData);
            return;
        }

        LoadSavedStage();
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

    private void LoadSavedStage()
    {
        ResolveReferences();

        if (progressService == null || stageCatalog == null || gameManager == null)
            return;

        int stageIndex = Mathf.Clamp(progressService.CurrentStageIndex, 0, Mathf.Max(0, stageCatalog.StageCount - 1));
        LoadStage(stageIndex);
    }

    private bool LoadStage(int stageIndex)
    {
        if (stageCatalog == null || gameManager == null)
            return false;

        if (!stageCatalog.TryGetStage(stageIndex, out MapData mapData))
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
        if (uiPresenter != null)
            uiPresenter.RefreshProgressView();

        return false;
    }

    private void RestartStageAfterAdChoice()
    {
        if (gameManager == null)
            return;

        if (!IsAdRequiredForCurrentStage())
        {
            gameManager.RestartStage();
            return;
        }

        ShowAdThenRun(AdPlacement.RestartStage, gameManager.RestartStage, true);
    }

    private bool IsAdRequiredForCurrentStage()
    {
        if (progressService == null)
            ResolveReferences();

        if (progressService == null)
            return false;

        return !progressService.ShouldSuppressAds(progressService.CurrentStageIndex);
    }

    private void ShowAdThenRun(AdPlacement placement, Action completed, bool requireReadyAd)
    {
        if (isShowingAd)
            return;

        if (progressService == null)
            ResolveReferences();

        int stageIndex = progressService == null ? 0 : progressService.CurrentStageIndex;
        if (progressService == null || progressService.ShouldSuppressAds(stageIndex))
        {
            completed?.Invoke();
            return;
        }

        IAdService adService = dummyAdService;
        if (adService == null || !adService.IsReady(placement))
        {
            Debug.LogWarning($"[GameFlowController] Ad is not ready. placement={placement}");

            if (!requireReadyAd)
                completed?.Invoke();

            return;
        }

        isShowingAd = true;
        adService.Show(placement, adSucceeded =>
        {
            isShowingAd = false;

            if (adSucceeded || !requireReadyAd)
                completed?.Invoke();
        });
    }
}
