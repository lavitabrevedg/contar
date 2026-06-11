using System;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class GameUIView : MonoBehaviour
{
    [SerializeField] private TMP_Text moveCountText;
    [SerializeField] private TMP_Text stageText;
    [SerializeField] private TMP_Text skipTicketText;
    [SerializeField] private CanvasGroup clearPanel;
    [SerializeField] private CanvasGroup failPanel;
    [SerializeField] private Button retryButton;
    [SerializeField] private Button nextButton;
    [SerializeField] private Button skipButton;
    [SerializeField] private TMP_Text retryButtonText;
    [SerializeField] private TMP_Text nextButtonText;
    [SerializeField] private TMP_Text skipButtonText;
    [SerializeField] private TMP_Text clearStageText;
    [SerializeField] private TMP_Text clearMoveText;
    [SerializeField] private TMP_Text rewardText;
    [SerializeField] private Button lobbyButton;
    [SerializeField] private float panelTweenDuration = 0.18f;
    [SerializeField] private Ease panelEase = Ease.OutBack;

    private string retryButtonDefaultLabel;
    private string nextButtonDefaultLabel;
    private bool skipButtonShouldBeVisible = true;
    private bool skipButtonShouldInteract;
    private string skipButtonLabel = "스킵권 없음";

    public event Action RetryClicked;
    public event Action NextClicked;
    public event Action SkipClicked;
    public event Action LobbyClicked;

    private void Awake()
    {
        CacheButtonLabels();
    }

    private void OnEnable()
    {
        CacheButtonLabels();

        if (retryButton != null)
            retryButton.onClick.AddListener(NotifyRetryClicked);

        if (nextButton != null)
            nextButton.onClick.AddListener(NotifyNextClicked);

        if (skipButton != null)
            skipButton.onClick.AddListener(NotifySkipClicked);

        if (lobbyButton != null)
            lobbyButton.onClick.AddListener(NotifyLobbyClicked);
    }

    private void OnDisable()
    {
        if (retryButton != null)
            retryButton.onClick.RemoveListener(NotifyRetryClicked);

        if (nextButton != null)
            nextButton.onClick.RemoveListener(NotifyNextClicked);

        if (skipButton != null)
            skipButton.onClick.RemoveListener(NotifySkipClicked);

        if (lobbyButton != null)
            lobbyButton.onClick.RemoveListener(NotifyLobbyClicked);
    }

    public void SetMoveCount(int moveCount)
    {
        if (moveCountText == null) return;

        moveCountText.text = moveCount.ToString();
    }

    public void SetStageInfo(int stageNumber, int stageCount)
    {
        if (stageText == null) return;

        if (stageCount <= 0)
        {
            stageText.text = "스테이지 -";
            return;
        }

        stageText.text = $"스테이지 {stageNumber}/{stageCount}";
    }

    public void SetSkipTicketCount(int skipTicketCount, int maxSkipTicketCount)
    {
        if (skipTicketText == null) return;

        skipTicketText.text = $"스킵권 {skipTicketCount}/{maxSkipTicketCount}";
    }

    public void SetNextStageAvailable(bool isAvailable)
    {
        CacheButtonLabels();

        if (nextButton != null)
            nextButton.interactable = isAvailable;

        if (nextButtonText != null)
            nextButtonText.text = isAvailable ? nextButtonDefaultLabel : "마지막";
    }

    public void SetRetryButtonLabel(string label)
    {
        CacheButtonLabels();

        if (retryButtonText == null) return;

        retryButtonText.text = string.IsNullOrWhiteSpace(label) ? retryButtonDefaultLabel : label;
    }

    public void SetSkipButtonState(bool isVisible, bool isInteractable, string label)
    {
        if (skipButton == null) return;

        skipButtonShouldBeVisible = isVisible;
        skipButtonShouldInteract = isInteractable;
        skipButtonLabel = label;

        ApplySkipButtonState();
    }

    public void SetClearResult(int stageNumber, int remainingMoveCount, bool grantedSkipTicket, int skipTicketCount)
    {
        if (clearStageText != null)
            clearStageText.text = $"스테이지 {stageNumber} 클리어";

        if (clearMoveText != null)
            clearMoveText.text = $"남은 이동 {remainingMoveCount}";

        if (rewardText != null)
        {
            rewardText.gameObject.SetActive(grantedSkipTicket);
            rewardText.text = grantedSkipTicket ? $"스킵권 +1 ({skipTicketCount})" : string.Empty;
        }
    }

    private void ApplySkipButtonState()
    {
        if (skipButton == null) return;

        bool isFailPanelVisible = failPanel != null && failPanel.gameObject.activeSelf;
        skipButton.gameObject.SetActive(skipButtonShouldBeVisible && isFailPanelVisible);
        skipButton.interactable = skipButtonShouldInteract && isFailPanelVisible;

        if (skipButtonText != null)
            skipButtonText.text = skipButtonLabel;
    }

    public void ShowClear()
    {
        HidePanel(failPanel);
        ShowPanel(clearPanel);
        ApplySkipButtonState();
    }

    public void ShowFail()
    {
        HidePanel(clearPanel);
        ShowPanel(failPanel);
        ApplySkipButtonState();
    }

    public void HideResultPanels()
    {
        HidePanel(clearPanel);
        HidePanel(failPanel);
        ApplySkipButtonState();
    }

    private void NotifyRetryClicked()
    {
        RetryClicked?.Invoke();
    }

    private void NotifyNextClicked()
    {
        NextClicked?.Invoke();
    }

    private void NotifySkipClicked()
    {
        SkipClicked?.Invoke();
    }

    private void NotifyLobbyClicked()
    {
        LobbyClicked?.Invoke();
    }

    private void ShowPanel(CanvasGroup panel)
    {
        if (panel == null) return;

        panel.DOKill();
        panel.transform.DOKill();

        panel.gameObject.SetActive(true);
        panel.alpha = 0f;
        panel.transform.localScale = Vector3.one * 0.96f;
        panel.interactable = false;
        panel.blocksRaycasts = true;

        panel.DOFade(1f, panelTweenDuration);
        panel.transform.DOScale(Vector3.one, panelTweenDuration)
            .SetEase(panelEase)
            .OnComplete(() => panel.interactable = true);
    }

    private void HidePanel(CanvasGroup panel)
    {
        if (panel == null) return;

        panel.DOKill();
        panel.transform.DOKill();

        panel.interactable = false;
        panel.blocksRaycasts = false;

        panel.DOFade(0f, panelTweenDuration);
        panel.transform.DOScale(Vector3.one * 0.98f, panelTweenDuration)
            .SetEase(Ease.InQuad)
            .OnComplete(() => panel.gameObject.SetActive(false));
    }

    private void CacheButtonLabels()
    {
        ResolveButtonTextReferences();

        if (string.IsNullOrEmpty(retryButtonDefaultLabel))
        {
            if (retryButtonText != null && !string.IsNullOrWhiteSpace(retryButtonText.text))
                retryButtonDefaultLabel = retryButtonText.text;
            else
                retryButtonDefaultLabel = "다시 시도";
        }

        if (string.IsNullOrEmpty(nextButtonDefaultLabel))
        {
            if (nextButtonText != null && !string.IsNullOrWhiteSpace(nextButtonText.text))
                nextButtonDefaultLabel = nextButtonText.text;
            else
                nextButtonDefaultLabel = "다음";
        }
    }

    private void ResolveButtonTextReferences()
    {
        if (retryButtonText == null && retryButton != null)
            retryButtonText = retryButton.GetComponentInChildren<TMP_Text>(true);
    }
}
