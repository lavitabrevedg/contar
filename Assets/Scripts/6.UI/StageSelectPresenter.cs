using UnityEngine;
using UnityEngine.SceneManagement;

public class StageSelectPresenter : MonoBehaviour
{
    private const string InGameSceneName = "InGameScene";
    private const int stagesPerPage = 12;

    [SerializeField] private StageSelectView view;
    [SerializeField] private StageCatalog stageCatalog;
    [SerializeField] private StageProgressService progressService;
    [SerializeField] private AudioService audioService;

    private int pageStartStageIndex;

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
            stageCatalog = Resources.Load<StageCatalog>("SettingDatas/StageCatalog");

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

        view.OpenStageSelectClicked -= OnOpenStageSelectClicked;
        view.CloseStageSelectClicked -= OnCloseStageSelectClicked;
        view.PreviousPageClicked -= OnPreviousPageClicked;
        view.NextPageClicked -= OnNextPageClicked;
        view.PlayCurrentStageClicked -= OnPlayCurrentStageClicked;
        view.StageClicked -= OnStageClicked;

        if (progressService != null)
            progressService.ProgressChanged -= Refresh;

        view.OpenStageSelectClicked += OnOpenStageSelectClicked;
        view.CloseStageSelectClicked += OnCloseStageSelectClicked;
        view.PreviousPageClicked += OnPreviousPageClicked;
        view.NextPageClicked += OnNextPageClicked;
        view.PlayCurrentStageClicked += OnPlayCurrentStageClicked;
        view.StageClicked += OnStageClicked;

        if (progressService != null)
            progressService.ProgressChanged += Refresh;
    }

    private void Unbind()
    {
        if (view == null)
            return;

        view.OpenStageSelectClicked -= OnOpenStageSelectClicked;
        view.CloseStageSelectClicked -= OnCloseStageSelectClicked;
        view.PreviousPageClicked -= OnPreviousPageClicked;
        view.NextPageClicked -= OnNextPageClicked;
        view.PlayCurrentStageClicked -= OnPlayCurrentStageClicked;
        view.StageClicked -= OnStageClicked;

        if (progressService != null)
            progressService.ProgressChanged -= Refresh;
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

    private void OnPreviousPageClicked()
    {
        if (pageStartStageIndex <= 0)
            return;

        pageStartStageIndex = Mathf.Max(0, pageStartStageIndex - stagesPerPage);
        PlayUiSound();
        Refresh();
    }

    private void OnNextPageClicked()
    {
        ResolveReferences();

        int stageCount = stageCatalog == null ? 0 : stageCatalog.StageCount;
        int nextPageStartStageIndex = pageStartStageIndex + stagesPerPage;
        if (nextPageStartStageIndex >= stageCount)
            return;

        pageStartStageIndex = nextPageStartStageIndex;
        PlayUiSound();
        Refresh();
    }

    private void OnPlayCurrentStageClicked()
    {
        int currentStageIndex = GetCurrentStageIndex();
        PlayStage(currentStageIndex);
    }

    private void OnStageClicked(int stageIndex)
    {
        PlayStage(stageIndex);
    }

    private void Refresh()
    {
        ResolveReferences();
        if (view == null)
            return;

        int stageCount = stageCatalog == null ? 0 : stageCatalog.StageCount;
        int currentStageIndex = GetCurrentStageIndex();
        bool canPlayCurrentStage = stageCount > 0 && IsStageAvailable(currentStageIndex);
        pageStartStageIndex = ClampPageStartStageIndex(pageStartStageIndex, stageCount);

        view.SetPageStartStageIndex(pageStartStageIndex);
        view.SetCurrentStageText(currentStageIndex, stageCount);
        view.SetCurrentStageButton(currentStageIndex, stageCount, canPlayCurrentStage);

        int buttonCount = view.StageButtonCount;
        for (int i = 0; i < buttonCount; i++)
        {
            int stageIndex = pageStartStageIndex + i;
            bool isInCatalog = stageIndex < stageCount;
            bool isAvailable = isInCatalog && IsStageAvailable(stageIndex);
            view.SetStageButton(i, stageIndex, isInCatalog, isAvailable);
        }

        bool canMovePrevious = pageStartStageIndex > 0;
        bool canMoveNext = pageStartStageIndex + stagesPerPage < stageCount;
        view.SetPageButtons(canMovePrevious, canMoveNext);
    }

    private int GetCurrentStageIndex()
    {
        ResolveReferences();

        int stageCount = stageCatalog == null ? 0 : stageCatalog.StageCount;
        int maxStageIndex = Mathf.Max(0, stageCount - 1);
        int currentStageIndex = progressService == null ? 0 : progressService.CurrentStageIndex;
        return Mathf.Clamp(currentStageIndex, 0, maxStageIndex);
    }

    private void PlayStage(int stageIndex)
    {
        ResolveReferences();

        if (!IsStageAvailable(stageIndex))
            return;

        if (progressService != null)
            progressService.SelectStageForPlay(stageIndex);

        PlayUiSound();
        SceneManager.LoadScene(InGameSceneName);
    }

    private int ClampPageStartStageIndex(int stageIndex, int stageCount)
    {
        if (stageCount <= 0)
            return 0;

        int lastPageStartStageIndex = ((stageCount - 1) / stagesPerPage) * stagesPerPage;
        return Mathf.Clamp(stageIndex, 0, lastPageStartStageIndex);
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
