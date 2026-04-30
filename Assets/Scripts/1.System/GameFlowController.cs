using UnityEngine;

public class GameFlowController : MonoBehaviour
{
    [SerializeField] private GameUIPresenter uiPresenter;
    [SerializeField] private GameManager gameManager;

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
        if (uiPresenter == null)
            uiPresenter = FindFirstObjectByType<GameUIPresenter>();

        if (gameManager == null)
            gameManager = GameManager.Instance;

        if (gameManager == null)
            gameManager = FindFirstObjectByType<GameManager>();
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

        _isBound = true;
    }

    private void Unbind()
    {
        if (!_isBound || uiPresenter == null) return;

        uiPresenter.RetryRequested -= OnRetryRequested;
        uiPresenter.NextStageRequested -= OnNextStageRequested;

        _isBound = false;
    }

    private void OnRetryRequested()
    {
        if (gameManager == null)
            ResolveReferences();

        if (gameManager == null) return;

        gameManager.RestartStage();
    }

    private void OnNextStageRequested()
    {
        Debug.Log("[GameFlowController] Next stage requested, but stage flow is not implemented yet.");
    }
}
