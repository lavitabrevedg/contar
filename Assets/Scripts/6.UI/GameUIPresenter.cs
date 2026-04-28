using UnityEngine;

public class GameUIPresenter : MonoBehaviour
{
    [SerializeField] private GameStateModel stateModel;
    [SerializeField] private GameUIView view;

    private bool _isBound;

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
    }

    private void Bind()
    {
        if (_isBound) return;

        ResolveReferences();
        if (stateModel == null || view == null) return;

        stateModel.MoveCountChanged -= OnMoveCountChanged;
        stateModel.StateChanged -= OnStateChanged;
        stateModel.MoveCountChanged += OnMoveCountChanged;
        stateModel.StateChanged += OnStateChanged;

        _isBound = true;

        view.SetMoveCount(stateModel.MoveCount);
        OnStateChanged(stateModel.State);
    }

    private void Unbind()
    {
        if (!_isBound || stateModel == null) return;

        stateModel.MoveCountChanged -= OnMoveCountChanged;
        stateModel.StateChanged -= OnStateChanged;

        _isBound = false;
    }

    private void OnMoveCountChanged(int moveCount)
    {
        view.SetMoveCount(moveCount);
    }

    private void OnStateChanged(GameState state)
    {
        if (state == GameState.Playing)
        {
            view.HideResultPanels();
            return;
        }

        if (state == GameState.Cleared)
        {
            view.ShowClear();
            return;
        }

        if (state == GameState.Failed)
        {
            view.ShowFail();
        }
    }
}
