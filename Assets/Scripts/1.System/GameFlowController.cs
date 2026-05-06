using System;
using UnityEngine;

public class GameFlowController : MonoBehaviour
{
    [SerializeField] private GameUIPresenter uiPresenter;
    [SerializeField] private GameManager gameManager;
    [SerializeField] private StageCatalog stageCatalog;
    [SerializeField] private StageProgressService progressService;
    [SerializeField] private DummyAdService dummyAdService;

    private bool _isBound;
    private bool _isShowingAd;

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
        if (_isBound) return;

        ResolveReferences();
        if (uiPresenter == null) return;

        uiPresenter.RetryRequested -= OnRetryRequested;
        uiPresenter.NextStageRequested -= OnNextStageRequested;
        uiPresenter.RetryRequested += OnRetryRequested;
        uiPresenter.NextStageRequested += OnNextStageRequested;

        if (gameManager != null)
        {
            gameManager.StageFailed -= OnStageFailed;
            gameManager.StageFailed += OnStageFailed;
        }

        _isBound = true;
    }

    private void Unbind()
    {
        if (!_isBound || uiPresenter == null) return;

        uiPresenter.RetryRequested -= OnRetryRequested;
        uiPresenter.NextStageRequested -= OnNextStageRequested;

        if (gameManager != null)
            gameManager.StageFailed -= OnStageFailed;

        _isBound = false;
    }

    private void OnRetryRequested()
    {
        if (gameManager == null)
            ResolveReferences();

        if (gameManager == null) return;

        ShowAdThenRun(AdPlacement.RestartStage, gameManager.RestartStage);
    }

    private void OnNextStageRequested()
    {
        if (progressService == null || stageCatalog == null || gameManager == null)
            ResolveReferences();

        if (progressService == null || stageCatalog == null || gameManager == null)
        {
            Debug.LogWarning("[GameFlowController] Cannot load next stage because stage flow references are missing.");
            return;
        }

        int nextStageIndex = progressService.CurrentStageIndex + 1;
        if (!LoadStage(nextStageIndex))
        {
            Debug.Log("[GameFlowController] All stages are cleared.");
        }
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

    private void OnStageFailed(int stageIndex, int failureCount)
    {
        if (failureCount < 3)
            return;

        ShowAdThenRun(AdPlacement.RetryAfterFailures, ResetFailureCount);
    }

    private void ResetFailureCount()
    {
        if (progressService != null)
        {
            progressService.ResetFailureCount();
            progressService.Save();
        }
    }

    private void ShowAdThenRun(AdPlacement placement, Action completed)
    {
        if (_isShowingAd)
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
            completed?.Invoke();
            return;
        }

        _isShowingAd = true;
        adService.Show(placement, success =>
        {
            _isShowingAd = false;
            completed?.Invoke();
        });
    }
}
