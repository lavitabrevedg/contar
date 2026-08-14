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

    public event Action NextStageRequested;
    public event Action RestartRequested;
    public event Action LobbyRequested;
    public event Action HintRequested;
    public event Action HintConfirmed;
    public event Action HintCanceled;
    public event Action TutorialAdvanceRequested;

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
            stageCatalog = Resources.Load<StageCatalog>("SettingDatas/StageCatalog");
    }

    private void Bind()
    {
        ResolveReferences();
        if (stateModel == null || view == null)
            return;

        if (!isBound)
        {
            stateModel.MoveCountChanged += OnMoveCountChanged;
            stateModel.StateChanged += OnStateChanged;
            view.RestartClicked += OnRestartClicked;
            view.NextClicked += OnNextClicked;
            view.LobbyClicked += OnLobbyClicked;
            view.HintClicked += OnHintClicked;
            view.HintConfirmed += OnHintConfirmed;
            view.HintCanceled += OnHintCanceled;
            view.TutorialAdvanced += OnTutorialAdvanced;
            isBound = true;
        }

        BindProgress();
        view.SetMoveCount(stateModel.MoveCount);
        RefreshProgressView();
        OnStateChanged(stateModel.State);
    }

    private void BindProgress()
    {
        if (isProgressBound || progressService == null)
            return;

        progressService.ProgressChanged += OnProgressChanged;
        isProgressBound = true;
    }

    private void Unbind()
    {
        if (isBound && stateModel != null && view != null)
        {
            stateModel.MoveCountChanged -= OnMoveCountChanged;
            stateModel.StateChanged -= OnStateChanged;
            view.RestartClicked -= OnRestartClicked;
            view.NextClicked -= OnNextClicked;
            view.LobbyClicked -= OnLobbyClicked;
            view.HintClicked -= OnHintClicked;
            view.HintConfirmed -= OnHintConfirmed;
            view.HintCanceled -= OnHintCanceled;
            view.TutorialAdvanced -= OnTutorialAdvanced;
        }

        if (isProgressBound && progressService != null)
            progressService.ProgressChanged -= OnProgressChanged;

        isBound = false;
        isProgressBound = false;
    }

    public void RefreshProgressView()
    {
        ResolveReferences();
        if (view == null)
            return;

        int stageCount = stageCatalog == null ? 0 : stageCatalog.StageCount;
        int stageNumber = progressService == null || stageCount <= 0
            ? 0
            : Mathf.Clamp(progressService.CurrentStageIndex + 1, 1, stageCount);
        bool hasNextStage = progressService != null
            && stageCount > 0
            && progressService.CurrentStageIndex + 1 < stageCount;

        view.SetStageInfo(stageNumber, stageCount);
        view.SetExitCondition(GetCurrentExitCondition());
        view.SetNextStageAvailable(hasNextStage);
    }

    public void SetHintButtonState(bool isVisible, bool isInteractable)
    {
        if (view != null)
            view.SetHintButtonState(isVisible, isInteractable);
    }

    public void ShowHintConfirmation()
    {
        if (view != null)
            view.ShowHintConfirmation();
    }

    public void ShowHintMessage(string message)
    {
        if (view != null)
            view.ShowHintMessage(message);
    }

    public void HideHintDialog()
    {
        if (view != null)
            view.HideHintDialog();
    }

    public bool ShowTutorialStep(TutorialMessage tutorialMessage, Transform targetTransform)
    {
        return view != null && view.ShowTutorialStep(tutorialMessage, targetTransform);
    }

    public void HideTutorialDialog()
    {
        if (view != null)
            view.HideTutorialDialog();
    }

    public bool ShowSwipeTutorial(Vector2Int swipeDirection)
    {
        return view != null && view.ShowSwipeTutorial(swipeDirection);
    }

    public void HideSwipeTutorial()
    {
        if (view != null)
            view.HideSwipeTutorial();
    }

    private ExitCondition GetCurrentExitCondition()
    {
        GameManager gameManager = GameManager.Instance;
        MapGenerator mapGenerator = gameManager == null ? null : gameManager.MapGenerator;
        MapData mapData = mapGenerator == null ? null : mapGenerator.mapData;
        if (mapData == null || mapData.rows == null)
            return ExitCondition.Free;

        for (int y = 0; y < mapData.rows.Length; y++)
        {
            Wrapper<SerializedTile> row = mapData.rows[y];
            if (row == null || row.values == null)
                continue;

            for (int x = 0; x < row.values.Length; x++)
            {
                SerializedTile tile = row.values[x];
                if (tile.type == TileType.Exit)
                    return tile.exitCondition;
            }
        }

        return ExitCondition.Free;
    }

    private void OnMoveCountChanged(int moveCount)
    {
        view.SetMoveCount(moveCount);
    }

    private void OnStateChanged(GameState state)
    {
        SetHintButtonState(state == GameState.Playing, false);

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

    private void OnRestartClicked()
    {
        RestartRequested?.Invoke();
    }

    private void OnNextClicked()
    {
        NextStageRequested?.Invoke();
    }

    private void OnLobbyClicked()
    {
        LobbyRequested?.Invoke();
    }

    private void OnHintClicked()
    {
        HintRequested?.Invoke();
    }

    private void OnHintConfirmed()
    {
        HintConfirmed?.Invoke();
    }

    private void OnHintCanceled()
    {
        HintCanceled?.Invoke();
    }

    private void OnTutorialAdvanced()
    {
        TutorialAdvanceRequested?.Invoke();
    }

    private void SetClearResultView()
    {
        int stageNumber = progressService == null ? 0 : progressService.CurrentStageIndex + 1;
        int remainingMoveCount = stateModel == null ? 0 : stateModel.MoveCount;
        view.SetClearResult(stageNumber, remainingMoveCount);
    }
}
