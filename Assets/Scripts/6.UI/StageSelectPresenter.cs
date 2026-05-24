using UnityEngine;
using UnityEngine.SceneManagement;

public class StageSelectPresenter : MonoBehaviour
{
    private const string InGameSceneName = "InGameScene";

    [SerializeField] private StageSelectView view;
    [SerializeField] private StageCatalog stageCatalog;
    [SerializeField] private StageProgressService progressService;
    [SerializeField] private AudioService audioService;

    private void Awake()
    {
        ResolveReferences();
    }

    private void OnEnable()
    {
        Bind();
        Refresh();
    }

    private void OnDisable()
    {
        Unbind();
    }

    private void ResolveReferences()
    {
        if (view == null)
            view = GetComponent<StageSelectView>();

        if (stageCatalog == null)
            stageCatalog = Resources.Load<StageCatalog>("StageCatalog");

        if (progressService == null)
            progressService = FindFirstObjectByType<StageProgressService>();

        if (audioService == null)
            audioService = FindFirstObjectByType<AudioService>();
    }

    private void Bind()
    {
        ResolveReferences();
        if (view == null)
            return;

        view.ContinueClicked -= OnContinueClicked;
        view.OpenStageSelectClicked -= OnOpenStageSelectClicked;
        view.CloseStageSelectClicked -= OnCloseStageSelectClicked;
        view.StageClicked -= OnStageClicked;

        view.ContinueClicked += OnContinueClicked;
        view.OpenStageSelectClicked += OnOpenStageSelectClicked;
        view.CloseStageSelectClicked += OnCloseStageSelectClicked;
        view.StageClicked += OnStageClicked;
    }

    private void Unbind()
    {
        if (view == null)
            return;

        view.ContinueClicked -= OnContinueClicked;
        view.OpenStageSelectClicked -= OnOpenStageSelectClicked;
        view.CloseStageSelectClicked -= OnCloseStageSelectClicked;
        view.StageClicked -= OnStageClicked;
    }

    private void OnContinueClicked()
    {
        PlayUiSound();
        SceneManager.LoadScene(InGameSceneName);
    }

    private void OnOpenStageSelectClicked()
    {
        PlayUiSound();

        if (view != null)
            view.SetPanelVisible(true);

        Refresh();
    }

    private void OnCloseStageSelectClicked()
    {
        PlayUiSound();

        if (view != null)
            view.SetPanelVisible(false);
    }

    private void OnStageClicked(int stageIndex)
    {
        ResolveReferences();

        if (!IsStageAvailable(stageIndex))
            return;

        if (progressService != null)
            progressService.SetCurrentStage(stageIndex);

        PlayUiSound();
        SceneManager.LoadScene(InGameSceneName);
    }

    private void Refresh()
    {
        ResolveReferences();
        if (view == null)
            return;

        int stageCount = stageCatalog == null ? 0 : stageCatalog.StageCount;
        int currentStageIndex = progressService == null ? 0 : Mathf.Clamp(progressService.CurrentStageIndex, 0, Mathf.Max(0, stageCount - 1));
        view.SetCurrentStageText(currentStageIndex, stageCount);

        int buttonCount = view.StageButtonCount;
        for (int i = 0; i < buttonCount; i++)
        {
            bool isInCatalog = i < stageCount;
            bool isAvailable = isInCatalog && IsStageAvailable(i);
            view.SetStageButton(i, i, isAvailable);
        }
    }

    private bool IsStageAvailable(int stageIndex)
    {
        ResolveReferences();

        if (stageCatalog == null || stageIndex < 0 || stageIndex >= stageCatalog.StageCount)
            return false;

        int highestClearedStageIndex = progressService == null ? -1 : progressService.HighestClearedStageIndex;
        return stageIndex <= highestClearedStageIndex + 1;
    }

    private void PlayUiSound()
    {
        if (audioService != null)
            audioService.PlayUi();
    }
}
