using System;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Components;
using UnityEngine.Localization.Settings;
using UnityEngine.UI;

public class GameUIView : MonoBehaviour
{
    private const float PrimaryButtonY = -159.3f;
    private const float SecondaryButtonY = -353.3f;
    private const string UiStringTableName = "UI";
    private const string ExitConditionOddKey = "Text.ExitCondition_Odd";
    private const string ExitConditionEvenKey = "Text.ExitCondition_Even";
    private const string MovesLeftKey = "Text.MovesLeft";
    private const string ClearStageKey = "Text.ClearStage";
    private const string FailureStageKey = "Text.FailStage";
    private const string KoreanLocaleCode = "ko";

    [SerializeField] private TMP_Text moveCountText;
    [SerializeField] private TMP_Text stageText;
    [SerializeField] private TMP_Text exitConditionText;
    [SerializeField] private CanvasGroup clearPanel;
    [SerializeField] private Button primaryActionButton;
    [SerializeField] private TMP_Text primaryActionButtonText;
    [SerializeField] private TMP_Text clearStageText;
    [SerializeField] private TMP_Text clearMoveText;
    [SerializeField] private Button lobbyButton;
    [SerializeField] private Button hintButton;
    [SerializeField] private GameObject hintDialog;
    [SerializeField] private TMP_Text hintDialogTitle;
    [SerializeField] private Button hintConfirmButton;
    [SerializeField] private Button hintCancelButton;
    [SerializeField] private TMP_Text hintCancelButtonText;
    [SerializeField] private GameObject resultInputBlocker;
    [SerializeField] private float panelTweenDuration = 0.18f;
    [SerializeField] private Ease panelEase = Ease.OutBack;
    [SerializeField] private float koreanClearStageFontSize = 110f;
    [SerializeField] private TMP_FontAsset koreanClearStageFont;

    private string lobbyButtonDefaultLabel;
    private TMP_Text lobbyButtonText;
    private float clearStageDefaultFontSize;
    private TMP_FontAsset clearStageDefaultFont;
    private bool isNextStageAvailable;
    private bool isShowingClearResult;
    private readonly LocalizedString exitConditionLocalizedString = new LocalizedString(UiStringTableName, ExitConditionOddKey);
    private readonly LocalizedString clearStageLocalizedString = new LocalizedString(UiStringTableName, ClearStageKey);
    private readonly LocalizedString failureStageLocalizedString = new LocalizedString(UiStringTableName, FailureStageKey);
    private readonly LocalizedString movesLeftLocalizedString = new LocalizedString(UiStringTableName, MovesLeftKey);

    public event Action RestartClicked;
    public event Action NextClicked;
    public event Action LobbyClicked;
    public event Action HintClicked;
    public event Action HintConfirmed;
    public event Action HintCanceled;

    private void Awake()
    {
        DisablePrimaryActionLocalizer();
        CacheButtonLabels();
        CacheClearStageDefaults();
        exitConditionLocalizedString.StringChanged += UpdateExitConditionText;
        clearStageLocalizedString.StringChanged += UpdateClearStageText;
        failureStageLocalizedString.StringChanged += UpdateFailureStageText;
        movesLeftLocalizedString.StringChanged += UpdateMovesLeftText;
    }

    private void OnDestroy()
    {
        exitConditionLocalizedString.StringChanged -= UpdateExitConditionText;
        clearStageLocalizedString.StringChanged -= UpdateClearStageText;
        failureStageLocalizedString.StringChanged -= UpdateFailureStageText;
        movesLeftLocalizedString.StringChanged -= UpdateMovesLeftText;
    }

    private void OnEnable()
    {
        CacheButtonLabels();

        if (primaryActionButton != null)
            primaryActionButton.onClick.AddListener(NotifyPrimaryActionClicked);

        if (lobbyButton != null)
            lobbyButton.onClick.AddListener(NotifyLobbyClicked);

        if (hintButton != null)
            hintButton.onClick.AddListener(NotifyHintClicked);

        if (hintConfirmButton != null)
            hintConfirmButton.onClick.AddListener(NotifyHintConfirmed);

        if (hintCancelButton != null)
            hintCancelButton.onClick.AddListener(NotifyHintCanceled);
    }

    private void OnDisable()
    {
        if (primaryActionButton != null)
            primaryActionButton.onClick.RemoveListener(NotifyPrimaryActionClicked);

        if (lobbyButton != null)
            lobbyButton.onClick.RemoveListener(NotifyLobbyClicked);

        if (hintButton != null)
            hintButton.onClick.RemoveListener(NotifyHintClicked);

        if (hintConfirmButton != null)
            hintConfirmButton.onClick.RemoveListener(NotifyHintConfirmed);

        if (hintCancelButton != null)
            hintCancelButton.onClick.RemoveListener(NotifyHintCanceled);

        HideHintDialog();
        KillResultPanelTweens();
    }

    public void SetMoveCount(int moveCount)
    {
        if (moveCountText == null)
            return;

        moveCountText.text = Mathf.Max(0, moveCount).ToString();
    }

    public void SetStageInfo(int stageNumber, int stageCount)
    {
        if (stageText == null)
            return;

        stageText.text = stageCount <= 0 ? "Stage -" : $"Stage {stageNumber}/{stageCount}";
    }

    public void SetExitCondition(ExitCondition exitCondition)
    {
        if (exitConditionText == null)
            return;

        bool hasExitCondition = exitCondition != ExitCondition.Free;
        exitConditionText.gameObject.SetActive(hasExitCondition);
        if (!hasExitCondition)
            return;

        exitConditionLocalizedString.SetReference(UiStringTableName, GetExitConditionKey(exitCondition));
        exitConditionLocalizedString.RefreshString();
    }

    public void SetNextStageAvailable(bool isAvailable)
    {
        isNextStageAvailable = isAvailable;

        if (isShowingClearResult)
            ConfigureClearPrimaryAction();
    }

    public void SetClearResult(int stageNumber, int remainingMoveCount)
    {
        clearStageLocalizedString.Arguments = new object[] { stageNumber };
        clearStageLocalizedString.RefreshString();

        movesLeftLocalizedString.Arguments = new object[] { Mathf.Max(0, remainingMoveCount) };
        movesLeftLocalizedString.RefreshString();
    }

    public void SetHintButtonState(bool isVisible, bool isInteractable)
    {
        if (hintButton == null)
            return;

        hintButton.gameObject.SetActive(isVisible);
        hintButton.interactable = isVisible && isInteractable;
    }

    public void ShowHintConfirmation()
    {
        if (hintDialog == null)
            return;

        if (hintDialogTitle != null)
            hintDialogTitle.text = "Watch Ad to Reveal Route";

        if (hintConfirmButton != null)
            hintConfirmButton.gameObject.SetActive(true);

        if (hintCancelButtonText != null)
            hintCancelButtonText.text = "Cancel";

        hintDialog.SetActive(true);
        hintDialog.transform.SetAsLastSibling();
    }

    public void ShowHintMessage(string message)
    {
        if (hintDialog == null)
            return;

        if (hintDialogTitle != null)
            hintDialogTitle.text = message;

        if (hintConfirmButton != null)
            hintConfirmButton.gameObject.SetActive(false);

        if (hintCancelButtonText != null)
            hintCancelButtonText.text = "OK";

        hintDialog.SetActive(true);
        hintDialog.transform.SetAsLastSibling();
    }

    public void HideHintDialog()
    {
        if (hintDialog != null)
            hintDialog.SetActive(false);
    }

    public void ShowClear()
    {
        SetClearModeObjects();
        ShowPanel(clearPanel);
        SetResultInputBlockerVisible(true);
    }

    public void ShowFail()
    {
        SetFailModeObjects();
        ShowPanel(clearPanel);
        SetResultInputBlockerVisible(true);
    }

    public void HideResultPanels()
    {
        HidePanel(clearPanel);
        SetResultInputBlockerVisible(false);
    }

    private void SetClearModeObjects()
    {
        isShowingClearResult = true;

        if (clearMoveText != null)
            clearMoveText.gameObject.SetActive(true);

        ConfigureClearPrimaryAction();

        if (lobbyButton != null)
        {
            lobbyButton.gameObject.SetActive(true);
            SetButtonAnchoredY(lobbyButton, SecondaryButtonY);
        }

        SetLobbyButtonLabel(lobbyButtonDefaultLabel);
    }

    private void SetFailModeObjects()
    {
        isShowingClearResult = false;

        if (clearStageText != null)
        {
            failureStageLocalizedString.RefreshString();
        }

        if (clearMoveText != null)
            clearMoveText.gameObject.SetActive(false);

        if (primaryActionButton != null)
        {
            primaryActionButton.gameObject.SetActive(true);
            primaryActionButton.interactable = true;
            SetButtonAnchoredY(primaryActionButton, PrimaryButtonY);
        }

        SetPrimaryActionLabel("Retry", "다시도전");

        if (lobbyButton != null)
        {
            lobbyButton.gameObject.SetActive(true);
            SetButtonAnchoredY(lobbyButton, SecondaryButtonY);
        }

        SetLobbyButtonLabel("Lobby");
    }

    private void ConfigureClearPrimaryAction()
    {
        if (primaryActionButton != null)
        {
            primaryActionButton.gameObject.SetActive(true);
            primaryActionButton.interactable = isNextStageAvailable;
            SetButtonAnchoredY(primaryActionButton, PrimaryButtonY);
        }

        SetPrimaryActionLabel(
            isNextStageAvailable ? "Next" : "Last",
            isNextStageAvailable ? "다음" : "마지막");
    }

    private void SetPrimaryActionLabel(string englishLabel, string koreanLabel)
    {
        if (primaryActionButtonText != null)
            primaryActionButtonText.text = IsKoreanLocaleSelected() ? koreanLabel : englishLabel;
    }

    private void DisablePrimaryActionLocalizer()
    {
        if (primaryActionButtonText == null)
            return;

        LocalizeStringEvent primaryActionLocalizer = primaryActionButtonText.GetComponent<LocalizeStringEvent>();
        if (primaryActionLocalizer != null)
            primaryActionLocalizer.enabled = false;
    }

    private void SetLobbyButtonLabel(string label)
    {
        CacheButtonLabels();
        if (lobbyButtonText != null)
            lobbyButtonText.text = string.IsNullOrWhiteSpace(label) ? lobbyButtonDefaultLabel : label;
    }

    private static string GetExitConditionKey(ExitCondition exitCondition)
    {
        switch (exitCondition)
        {
            case ExitCondition.OddOnly:
                return ExitConditionOddKey;
            case ExitCondition.EvenOnly:
                return ExitConditionEvenKey;
            default:
                return ExitConditionOddKey;
        }
    }

    private void UpdateExitConditionText(string localizedText)
    {
        if (exitConditionText != null)
            exitConditionText.text = localizedText;
    }

    private void UpdateClearStageText(string localizedText)
    {
        if (clearStageText != null)
        {
            clearStageText.text = localizedText;
            SetClearStageFont(IsKoreanLocaleSelected());
            clearStageText.fontSize = IsKoreanLocaleSelected()
                ? koreanClearStageFontSize
                : clearStageDefaultFontSize;
        }
    }

    private void UpdateFailureStageText(string localizedText)
    {
        if (clearStageText != null)
        {
            clearStageText.text = localizedText;
            SetClearStageFont(IsKoreanLocaleSelected());
            clearStageText.fontSize = IsKoreanLocaleSelected()
                ? koreanClearStageFontSize
                : clearStageDefaultFontSize;
        }
    }

    private void CacheClearStageDefaults()
    {
        if (clearStageText != null)
        {
            clearStageDefaultFontSize = clearStageText.fontSize;
            clearStageDefaultFont = clearStageText.font;
        }
    }

    private void SetClearStageFont(bool useKoreanFont)
    {
        if (clearStageText == null)
            return;

        if (useKoreanFont && koreanClearStageFont != null)
        {
            clearStageText.font = koreanClearStageFont;
            return;
        }

        RestoreClearStageDefaultFont();
    }

    private void RestoreClearStageDefaultFont()
    {
        if (clearStageText != null && clearStageDefaultFont != null)
            clearStageText.font = clearStageDefaultFont;
    }

    private static bool IsKoreanLocaleSelected()
    {
        Locale selectedLocale = LocalizationSettings.SelectedLocale;
        return selectedLocale != null && selectedLocale.Identifier.Code == KoreanLocaleCode;
    }

    private void UpdateMovesLeftText(string localizedText)
    {
        if (clearMoveText != null)
            clearMoveText.text = localizedText;
    }

    private void SetButtonAnchoredY(Button button, float anchoredY)
    {
        RectTransform rectTransform = button == null ? null : button.transform as RectTransform;
        if (rectTransform == null)
            return;

        Vector2 anchoredPosition = rectTransform.anchoredPosition;
        anchoredPosition.y = anchoredY;
        rectTransform.anchoredPosition = anchoredPosition;
    }

    private void NotifyPrimaryActionClicked()
    {
        if (isShowingClearResult)
            NextClicked?.Invoke();
        else
            RestartClicked?.Invoke();
    }

    private void NotifyLobbyClicked()
    {
        LobbyClicked?.Invoke();
    }

    private void NotifyHintClicked()
    {
        HintClicked?.Invoke();
    }

    private void NotifyHintConfirmed()
    {
        HintConfirmed?.Invoke();
    }

    private void NotifyHintCanceled()
    {
        HintCanceled?.Invoke();
    }

    private void ShowPanel(CanvasGroup panel)
    {
        if (panel == null)
            return;

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
        if (panel == null)
            return;

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

        if (string.IsNullOrEmpty(lobbyButtonDefaultLabel))
        {
            lobbyButtonDefaultLabel = lobbyButtonText != null && !string.IsNullOrWhiteSpace(lobbyButtonText.text)
                ? lobbyButtonText.text
                : "Lobby";
        }
    }

    private void ResolveButtonTextReferences()
    {
        if (primaryActionButtonText == null && primaryActionButton != null)
            primaryActionButtonText = primaryActionButton.GetComponentInChildren<TMP_Text>(true);

        if (lobbyButtonText == null && lobbyButton != null)
            lobbyButtonText = lobbyButton.GetComponentInChildren<TMP_Text>(true);
    }

    private void SetResultInputBlockerVisible(bool isVisible)
    {
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
