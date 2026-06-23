using System;
using UnityEngine;

public class GameUIPresenter : MonoBehaviour
{
    [SerializeField] private GameStateModel stateModel;
    [SerializeField] private StageProgressService progressService;
    [SerializeField] private StageCatalog stageCatalog;
    [SerializeField] private GameUIView view;

    private bool isBound;
    private bool isProgressBound;

    public event Action RetryRequested;
    public event Action NextStageRequested;
    public event Action AdSkipTicketRequested;
    public event Action LobbyRequested;

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
    }

    private void OnDisable()
    {
        Unbind();
    }

    private void ResolveReferences()
    {
        if (view == null)
            view = GetComponent<GameUIView>();

        if (stateModel == null && GameManager.Instance != null)
            stateModel = GameManager.Instance.StateModel;

        if (stateModel == null)
            stateModel = FindFirstObjectByType<GameStateModel>();

        if (progressService == null && GameManager.Instance != null)
            progressService = GameManager.Instance.ProgressService;

        if (progressService == null)
            progressService = FindFirstObjectByType<StageProgressService>();

        if (stageCatalog == null)
            stageCatalog = Resources.Load<StageCatalog>("StageCatalog");
    }

    private void Bind()
    {
        ResolveReferences();
        if (stateModel == null || view == null) return;

        if (!isBound)
        {
            stateModel.MoveCountChanged -= OnMoveCountChanged;
            stateModel.StateChanged -= OnStateChanged;
            view.RetryClicked -= OnRetryClicked;
            view.NextClicked -= OnNextClicked;
            view.SkipClicked -= OnSkipClicked;
            view.LobbyClicked -= OnLobbyClicked;

            stateModel.MoveCountChanged += OnMoveCountChanged;
            stateModel.StateChanged += OnStateChanged;
            view.RetryClicked += OnRetryClicked;
            view.NextClicked += OnNextClicked;
            view.SkipClicked += OnSkipClicked;
            view.LobbyClicked += OnLobbyClicked;

            isBound = true;
        }

        BindProgress();

        view.SetMoveCount(stateModel.MoveCount);
        RefreshProgressView();
        OnStateChanged(stateModel.State);
    }

    private void BindProgress()
    {
        if (isProgressBound) return;
        if (progressService == null) return;

        progressService.ProgressChanged -= OnProgressChanged;
        progressService.ProgressChanged += OnProgressChanged;
        isProgressBound = true;
    }

    private void Unbind()
    {
        if (isBound && stateModel != null && view != null)
        {
            stateModel.MoveCountChanged -= OnMoveCountChanged;
            stateModel.StateChanged -= OnStateChanged;
            view.RetryClicked -= OnRetryClicked;
            view.NextClicked -= OnNextClicked;
            view.SkipClicked -= OnSkipClicked;
            view.LobbyClicked -= OnLobbyClicked;
        }

        if (isProgressBound && progressService != null)
            progressService.ProgressChanged -= OnProgressChanged;

        isBound = false;
        isProgressBound = false;
    }

    public void RefreshProgressView()
    {
        ResolveReferences();
        if (view == null) return;

        if (progressService == null)
        {
            view.SetStageInfo(0, 0);
            view.SetSkipTicketCount(0, 0);
            view.SetNextStageAvailable(false);
            view.SetRetryButtonLabel("Retry");
            view.SetSkipButtonState(false, false, "No Skip Tickets");
            return;
        }

        int stageCount = stageCatalog == null ? 0 : stageCatalog.StageCount;
        int stageNumber = stageCount <= 0 ? 0 : Mathf.Clamp(progressService.CurrentStageIndex + 1, 1, stageCount);
        bool hasNextStage = stageCount > 0 && progressService.CurrentStageIndex + 1 < stageCount;
        bool isAdRequired = progressService.ShouldShowAdForFailureRetry(progressService.CurrentStageIndex);
        bool hasAdSkipTicket = progressService.HasAdSkipTicket;
        string retryLabel = isAdRequired ? "Watch Ad" : "Retry";
        string skipLabel = hasAdSkipTicket ? $"Use Skip Ticket ({progressService.SkipTicketCount})" : "No Skip Tickets";

        view.SetStageInfo(stageNumber, stageCount);
        view.SetSkipTicketCount(progressService.SkipTicketCount, progressService.MaxSkipTicketCountValue);
        view.SetNextStageAvailable(hasNextStage);
        view.SetRetryButtonLabel(retryLabel);
        view.SetSkipButtonState(isAdRequired, hasAdSkipTicket, skipLabel);
    }

    private void OnMoveCountChanged(int moveCount)
    {
        view.SetMoveCount(moveCount);
    }

    private void OnStateChanged(GameState state)
    {
        if (state == GameState.Playing)
        {
            RefreshProgressView();
            view.HideResultPanels();
            return;
        }

        if (state == GameState.Cleared)
        {
            RefreshProgressView();
            SetClearResultView();
            view.ShowClear();
            return;
        }

        if (state == GameState.Failed)
        {
            RefreshProgressView();
            view.ShowFail();
        }
    }

    private void OnProgressChanged()
    {
        RefreshProgressView();
    }

    private void OnRetryClicked()
    {
        RetryRequested?.Invoke();
    }

    private void OnNextClicked()
    {
        NextStageRequested?.Invoke();
    }

    private void OnSkipClicked()
    {
        AdSkipTicketRequested?.Invoke();
    }

    private void OnLobbyClicked()
    {
        LobbyRequested?.Invoke();
    }

    private void SetClearResultView()
    {
        if (view == null)
            return;

        GameManager gameManager = GameManager.Instance;
        int stageNumber = progressService == null ? 0 : progressService.CurrentStageIndex + 1;
        int remainingMoveCount = stateModel == null ? 0 : stateModel.MoveCount;
        StageClearProgressResult progressResult = gameManager == null ? new StageClearProgressResult(false, false, 0) : gameManager.LastStageClearProgressResult;
        view.SetClearResult(stageNumber, remainingMoveCount, progressResult.GrantedSkipTicket, progressResult.SkipTicketCount);
    }
}
