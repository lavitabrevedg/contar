using System;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class GameUIView : MonoBehaviour
{
    private const float PrimaryButtonY = -159.3f;
    private const float SecondaryButtonY = -353.3f;
    private const float FailSecondaryButtonY = -333.3f;
    private const float FailTertiaryButtonY = -507.3f;

    [SerializeField] private TMP_Text moveCountText;
    [SerializeField] private TMP_Text stageText;
    [SerializeField] private TMP_Text exitConditionText;
    [SerializeField] private TMP_Text skipTicketText;
    [SerializeField] private CanvasGroup clearPanel;
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
    private string lobbyButtonDefaultLabel;
    private TMP_Text lobbyButtonText;
    private bool isShowingFailResult;
    private bool retryButtonShouldBeVisible = true;
    private bool retryButtonShouldInteract = true;
    private string retryButtonLabel = "Watch Ad +2";
    private bool skipButtonShouldBeVisible = true;
    private bool skipButtonShouldInteract;
    private string skipButtonLabel = "No Skip Tickets";
    private GameObject resultInputBlocker;

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

        KillResultPanelTweens();
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
            stageText.text = "Stage -";
            return;
        }

        stageText.text = $"Stage {stageNumber}/{stageCount}";
    }

    public void SetExitCondition(ExitCondition exitCondition)
    {
        if (exitConditionText == null) return;

        switch (exitCondition)
        {
            case ExitCondition.OddOnly:
                exitConditionText.text = "Exit: Odd Moves";
                break;
            case ExitCondition.EvenOnly:
                exitConditionText.text = "Exit: Even Moves";
                break;
            default:
                exitConditionText.text = "Exit: Any";
                break;
        }
    }

    public void SetSkipTicketCount(int skipTicketCount, int maxSkipTicketCount)
    {
        if (skipTicketText == null) return;

        skipTicketText.text = $"Skip Ticket {skipTicketCount}/{maxSkipTicketCount}";
    }

    public void SetNextStageAvailable(bool isAvailable)
    {
        CacheButtonLabels();

        if (nextButton != null)
            nextButton.interactable = isAvailable;

        if (nextButtonText != null)
            nextButtonText.text = isAvailable ? nextButtonDefaultLabel : "Last";
    }

    public void SetRetryButtonLabel(string label)
    {
        SetRetryButtonState(true, true, label);
    }

    public void SetRetryButtonState(bool isVisible, bool isInteractable, string label)
    {
        retryButtonShouldBeVisible = isVisible;
        retryButtonShouldInteract = isInteractable;
        retryButtonLabel = label;

        ApplyRetryButtonState();
    }

    public void SetSkipButtonState(bool isVisible, bool isInteractable, string label)
    {
        skipButtonShouldBeVisible = isVisible;
        skipButtonShouldInteract = isInteractable;
        skipButtonLabel = label;

        ApplySkipButtonState();
    }

    public void SetClearResult(int stageNumber, int remainingMoveCount, bool grantedSkipTicket, int skipTicketCount)
    {
        if (clearStageText != null)
            clearStageText.text = $"Stage {stageNumber} Clear";

        if (clearMoveText != null)
            clearMoveText.text = $"Moves Left {remainingMoveCount}";

        if (rewardText != null)
        {
            rewardText.gameObject.SetActive(grantedSkipTicket);
            rewardText.text = grantedSkipTicket ? $"Skip Ticket +1 ({skipTicketCount})" : string.Empty;
        }
    }

    private void ApplySkipButtonState()
    {
        if (skipButton == null) return;

        bool shouldShowSkipButton = skipButtonShouldBeVisible && isShowingFailResult;
        skipButton.gameObject.SetActive(shouldShowSkipButton);
        skipButton.interactable = skipButtonShouldInteract && shouldShowSkipButton;

        if (skipButtonText != null)
            skipButtonText.text = skipButtonLabel;
    }

    private void ApplyRetryButtonState()
    {
        if (retryButton == null) return;

        bool shouldShowRetryButton = retryButtonShouldBeVisible && isShowingFailResult;
        retryButton.gameObject.SetActive(shouldShowRetryButton);
        retryButton.interactable = retryButtonShouldInteract && shouldShowRetryButton;

        if (retryButtonText != null)
            retryButtonText.text = string.IsNullOrWhiteSpace(retryButtonLabel) ? retryButtonDefaultLabel : retryButtonLabel;
    }

    public void ShowClear()
    {
        isShowingFailResult = false;
        SetClearModeObjects();
        ShowPanel(clearPanel);
        SetResultInputBlockerVisible(true);
        ApplyRetryButtonState();
        ApplySkipButtonState();
    }

    public void SetLobbyButtonLabel(string label)
    {
        CacheButtonLabels();

        if (lobbyButtonText == null) return;

        lobbyButtonText.text = string.IsNullOrWhiteSpace(label) ? lobbyButtonDefaultLabel : label;
    }

    public void ShowFail()
    {
        isShowingFailResult = true;
        SetFailModeObjects();
        ShowPanel(clearPanel);
        SetResultInputBlockerVisible(true);
        ApplyRetryButtonState();
        ApplySkipButtonState();
    }

    public void HideResultPanels()
    {
        isShowingFailResult = false;
        HidePanel(clearPanel);
        SetResultInputBlockerVisible(false);
        ApplyRetryButtonState();
        ApplySkipButtonState();
    }

    private void SetClearModeObjects()
    {
        if (clearMoveText != null)
            clearMoveText.gameObject.SetActive(true);

        if (nextButton != null)
        {
            nextButton.gameObject.SetActive(true);
            SetButtonAnchoredY(nextButton, PrimaryButtonY);
        }

        if (retryButton != null)
            retryButton.gameObject.SetActive(false);

        if (lobbyButton != null)
        {
            lobbyButton.gameObject.SetActive(true);
            SetButtonAnchoredY(lobbyButton, SecondaryButtonY);
        }

        SetLobbyButtonLabel(lobbyButtonDefaultLabel);
    }

    private void SetFailModeObjects()
    {
        if (clearStageText != null)
            clearStageText.text = "Failed";

        if (clearMoveText != null)
            clearMoveText.gameObject.SetActive(false);

        if (rewardText != null)
        {
            rewardText.gameObject.SetActive(false);
            rewardText.text = string.Empty;
        }

        if (nextButton != null)
            nextButton.gameObject.SetActive(false);

        if (retryButton != null)
            SetButtonAnchoredY(retryButton, PrimaryButtonY);

        if (skipButton != null)
            SetButtonAnchoredY(skipButton, retryButtonShouldBeVisible ? FailSecondaryButtonY : PrimaryButtonY);

        if (lobbyButton != null)
        {
            lobbyButton.gameObject.SetActive(true);
            SetButtonAnchoredY(lobbyButton, retryButtonShouldBeVisible ? FailTertiaryButtonY : SecondaryButtonY);
        }

        SetLobbyButtonLabel("Restart");
    }

    private void SetButtonAnchoredY(Button button, float anchoredY)
    {
        if (button == null) return;

        RectTransform rectTransform = button.transform as RectTransform;
        if (rectTransform == null) return;

        Vector2 anchoredPosition = rectTransform.anchoredPosition;
        anchoredPosition.y = anchoredY;
        rectTransform.anchoredPosition = anchoredPosition;
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

        EnsureResultInputBlocker(panel);
        SetResultInputBlockerVisible(true);

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

        SetResultInputBlockerVisible(false);

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
                retryButtonDefaultLabel = "Retry";
        }

        if (string.IsNullOrEmpty(nextButtonDefaultLabel))
        {
            if (nextButtonText != null && !string.IsNullOrWhiteSpace(nextButtonText.text))
                nextButtonDefaultLabel = nextButtonText.text;
            else
                nextButtonDefaultLabel = "Next";
        }

        if (string.IsNullOrEmpty(lobbyButtonDefaultLabel))
        {
            if (lobbyButtonText != null && !string.IsNullOrWhiteSpace(lobbyButtonText.text))
                lobbyButtonDefaultLabel = lobbyButtonText.text;
            else
                lobbyButtonDefaultLabel = "Lobby";
        }
    }

    private void ResolveButtonTextReferences()
    {
        if (retryButtonText == null && retryButton != null)
            retryButtonText = retryButton.GetComponentInChildren<TMP_Text>(true);

        if (lobbyButtonText == null && lobbyButton != null)
            lobbyButtonText = lobbyButton.GetComponentInChildren<TMP_Text>(true);
    }

    private void EnsureResultInputBlocker(CanvasGroup panel)
    {
        if (resultInputBlocker != null) return;
        if (panel == null) return;
        if (panel.transform.parent == null) return;

        resultInputBlocker = new GameObject("ResultInputBlocker", typeof(RectTransform), typeof(Image));
        resultInputBlocker.transform.SetParent(panel.transform.parent, false);

        RectTransform blockerRect = resultInputBlocker.transform as RectTransform;
        if (blockerRect != null)
        {
            blockerRect.anchorMin = Vector2.zero;
            blockerRect.anchorMax = Vector2.one;
            blockerRect.offsetMin = Vector2.zero;
            blockerRect.offsetMax = Vector2.zero;
            blockerRect.localScale = Vector3.one;
        }

        Image blockerImage = resultInputBlocker.GetComponent<Image>();
        blockerImage.color = new Color(0f, 0f, 0f, 0.35f);
        blockerImage.raycastTarget = true;

        resultInputBlocker.SetActive(false);
    }

    private void SetResultInputBlockerVisible(bool isVisible)
    {
        EnsureResultInputBlocker(clearPanel);

        if (resultInputBlocker == null)
            return;

        if (isVisible && clearPanel != null)
        {
            resultInputBlocker.transform.SetAsLastSibling();
            clearPanel.transform.SetAsLastSibling();
        }

        resultInputBlocker.SetActive(isVisible);
    }

    private void KillResultPanelTweens()
    {
        if (clearPanel == null)
            return;

        clearPanel.DOKill();
        clearPanel.transform.DOKill();
    }
}
