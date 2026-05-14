using System;
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

    private string retryButtonDefaultLabel;
    private string nextButtonDefaultLabel;
    private bool skipButtonShouldBeVisible = true;
    private bool skipButtonShouldInteract;
    private string skipButtonLabel = "스킵권 없음";

    public event Action RetryClicked;
    public event Action NextClicked;
    public event Action SkipClicked;

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
    }

    private void OnDisable()
    {
        if (retryButton != null)
            retryButton.onClick.RemoveListener(NotifyRetryClicked);

        if (nextButton != null)
            nextButton.onClick.RemoveListener(NotifyNextClicked);

        if (skipButton != null)
            skipButton.onClick.RemoveListener(NotifySkipClicked);
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

    private void ShowPanel(CanvasGroup panel)
    {
        if (panel == null) return;

        panel.gameObject.SetActive(true);
        panel.alpha = 1f;
        panel.interactable = true;
        panel.blocksRaycasts = true;
    }

    private void HidePanel(CanvasGroup panel)
    {
        if (panel == null) return;

        panel.alpha = 0f;
        panel.interactable = false;
        panel.blocksRaycasts = false;
        panel.gameObject.SetActive(false);
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
