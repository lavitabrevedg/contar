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
        LoadSavedStage();
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
        uiPresenter.SkipStageRequested -= OnSkipStageRequested;
        uiPresenter.RetryRequested += OnRetryRequested;
        uiPresenter.NextStageRequested += OnNextStageRequested;
        uiPresenter.SkipStageRequested += OnSkipStageRequested;

        isBound = true;
    }

    private void Unbind()
    {
        if (!isBound || uiPresenter == null) return;

        uiPresenter.RetryRequested -= OnRetryRequested;
        uiPresenter.NextStageRequested -= OnNextStageRequested;
        uiPresenter.SkipStageRequested -= OnSkipStageRequested;

        isBound = false;
    }

    private void OnRetryRequested()
    {
        if (gameManager == null)
            ResolveReferences();

        if (gameManager == null) return;

        ShowAdThenRun(AdPlacement.RestartStage, gameManager.RestartStage, false);
    }

    private void OnNextStageRequested()
    {
        LoadNextStage();
    }

    private void OnSkipStageRequested()
    {
        if (progressService == null || stageCatalog == null || gameManager == null)
            ResolveReferences();

        if (progressService == null || stageCatalog == null || gameManager == null)
        {
            Debug.LogWarning("[GameFlowController] Cannot skip stage because stage flow references are missing.");
            return;
        }

        if (!HasNextStage())
        {
            Debug.Log("[GameFlowController] Cannot skip because there is no next stage.");
            return;
        }

        if (progressService.TryUseSkipTicket())
        {
            LoadNextStage();
            return;
        }

        if (progressService.ShouldSuppressAds(progressService.CurrentStageIndex))
        {
            Debug.Log("[GameFlowController] Cannot skip because there are no skip tickets and ads are suppressed for this stage.");
            return;
        }

        ShowAdThenRun(AdPlacement.SkipStage, () => LoadNextStage(), true);
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

    private bool HasNextStage()
    {
        if (progressService == null || stageCatalog == null)
            return false;

        return progressService.CurrentStageIndex + 1 < stageCatalog.StageCount;
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
            if (!requireReadyAd)
                completed?.Invoke();

            return;
        }

        IAdService adService = dummyAdService;
        if (adService == null || !adService.IsReady(placement))
        {
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
