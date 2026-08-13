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
    private const string TutorialPositiveMoveKey = "Tutorial.PositiveMoveTile";
    private const string TutorialNegativeMoveKey = "Tutorial.NegativeMoveTile";
    private const string TutorialExitOddKey = "Tutorial.ExitConditionOdd";
    private const string TutorialExitEvenKey = "Tutorial.ExitConditionEven";
    private const string TutorialNumberObstacleKey = "Tutorial.NumberObstacle";
    private const float TutorialSafeMargin = 24f;
    private const float TutorialPointerGap = 12f;
    private const float TutorialFocusSize = 170f;
    private static readonly Vector2 TutorialPanelSize = new Vector2(600f, 266f);

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
    [SerializeField] private GameObject tutorialDialog;
    [SerializeField] private TMP_Text tutorialMessageText;
    [SerializeField] private Button tutorialAdvanceButton;
    [SerializeField] private RectTransform tutorialPanelRect;
    [SerializeField] private Image tutorialPanelImage;
    [SerializeField] private RectTransform tutorialDimmerLeft;
    [SerializeField] private RectTransform tutorialDimmerRight;
    [SerializeField] private RectTransform tutorialDimmerTop;
    [SerializeField] private RectTransform tutorialDimmerBottom;
    [SerializeField] private GameObject resultInputBlocker;
    [SerializeField] private float panelTweenDuration = 0.18f;
    [SerializeField] private Ease panelEase = Ease.OutBack;
    [SerializeField] private float koreanClearStageFontSize = 110f;
    [SerializeField] private TMP_FontAsset koreanClearStageFont;

    private string lobbyButtonDefaultLabel;
    private TMP_Text lobbyButtonText;
    private LocalizeStringEvent hintDialogTitleLocalizer;
    private LocalizeStringEvent hintCancelButtonTextLocalizer;
    private Vector2 hintCancelButtonDefaultAnchoredPosition;
    private float clearStageDefaultFontSize;
    private TMP_FontAsset clearStageDefaultFont;
    private bool isHintCancelButtonPositionCached;
    private bool isNextStageAvailable;
    private bool isShowingClearResult;
    private bool isDismissingResult;
    private Transform tutorialTargetTransform;
    private readonly LocalizedString exitConditionLocalizedString = new LocalizedString(UiStringTableName, ExitConditionOddKey);
    private readonly LocalizedString clearStageLocalizedString = new LocalizedString(UiStringTableName, ClearStageKey);
    private readonly LocalizedString failureStageLocalizedString = new LocalizedString(UiStringTableName, FailureStageKey);
    private readonly LocalizedString movesLeftLocalizedString = new LocalizedString(UiStringTableName, MovesLeftKey);
    private readonly LocalizedString tutorialMessageLocalizedString = new LocalizedString(
        UiStringTableName,
        TutorialPositiveMoveKey);

    public event Action RestartClicked;
    public event Action NextClicked;
    public event Action LobbyClicked;
    public event Action HintClicked;
    public event Action HintConfirmed;
    public event Action HintCanceled;
    public event Action TutorialAdvanced;

    private void Awake()
    {
        DisablePrimaryActionLocalizer();
        CacheHintDialogLocalizers();
        CacheHintCancelButtonPosition();
        CacheButtonLabels();
        CacheClearStageDefaults();
        exitConditionLocalizedString.StringChanged += UpdateExitConditionText;
        clearStageLocalizedString.StringChanged += UpdateClearStageText;
        failureStageLocalizedString.StringChanged += UpdateFailureStageText;
        movesLeftLocalizedString.StringChanged += UpdateMovesLeftText;
        tutorialMessageLocalizedString.StringChanged += UpdateTutorialMessageText;
    }

    private void OnDestroy()
    {
        exitConditionLocalizedString.StringChanged -= UpdateExitConditionText;
        clearStageLocalizedString.StringChanged -= UpdateClearStageText;
        failureStageLocalizedString.StringChanged -= UpdateFailureStageText;
        movesLeftLocalizedString.StringChanged -= UpdateMovesLeftText;
        tutorialMessageLocalizedString.StringChanged -= UpdateTutorialMessageText;
    }

    private void LateUpdate()
    {
        if (tutorialDialog != null && tutorialDialog.activeSelf && tutorialTargetTransform != null)
            UpdateTutorialLayout();
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

        if (tutorialAdvanceButton != null)
            tutorialAdvanceButton.onClick.AddListener(NotifyTutorialAdvanced);
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

        if (tutorialAdvanceButton != null)
            tutorialAdvanceButton.onClick.RemoveListener(NotifyTutorialAdvanced);

        HideHintDialog();
        HideTutorialDialog();
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

        if (isShowingClearResult && !isDismissingResult)
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

        SetHintDialogLocalizersEnabled(true);

        if (hintDialogTitle != null)
            hintDialogTitle.text = "Watch Ad to Reveal Route";

        if (hintConfirmButton != null)
            hintConfirmButton.gameObject.SetActive(true);

        if (hintCancelButtonText != null)
            hintCancelButtonText.text = "Cancel";

        SetHintCancelButtonCentered(false);
        hintDialog.SetActive(true);
        hintDialog.transform.SetAsLastSibling();
    }

    public void ShowHintMessage(string message)
    {
        if (hintDialog == null)
            return;

        SetHintDialogLocalizersEnabled(false);

        if (hintDialogTitle != null)
            hintDialogTitle.text = GetHintMessageText(message);

        if (hintConfirmButton != null)
            hintConfirmButton.gameObject.SetActive(false);

        if (hintCancelButtonText != null)
            hintCancelButtonText.text = IsKoreanLocaleSelected() ? "확인" : "OK";

        SetHintCancelButtonCentered(true);
        hintDialog.SetActive(true);
        hintDialog.transform.SetAsLastSibling();
    }

    public void HideHintDialog()
    {
        if (hintDialog != null)
            hintDialog.SetActive(false);
    }

    public bool ShowTutorialStep(TutorialMessage tutorialMessage, Transform targetTransform)
    {
        if (tutorialDialog == null
            || tutorialMessageText == null
            || tutorialAdvanceButton == null
            || tutorialPanelRect == null
            || tutorialPanelImage == null
            || targetTransform == null)
        {
            return false;
        }

        tutorialTargetTransform = targetTransform;
        tutorialMessageLocalizedString.SetReference(UiStringTableName, GetTutorialMessageKey(tutorialMessage));
        tutorialMessageLocalizedString.RefreshString();

        tutorialDialog.SetActive(true);
        tutorialDialog.transform.SetAsLastSibling();
        if (UpdateTutorialLayout())
            return true;

        tutorialTargetTransform = null;
        tutorialDialog.SetActive(false);
        return false;
    }

    public void HideTutorialDialog()
    {
        tutorialTargetTransform = null;
        if (tutorialDialog != null)
            tutorialDialog.SetActive(false);
    }

    private void CacheHintDialogLocalizers()
    {
        hintDialogTitleLocalizer = hintDialogTitle == null
            ? null
            : hintDialogTitle.GetComponent<LocalizeStringEvent>();
        hintCancelButtonTextLocalizer = hintCancelButtonText == null
            ? null
            : hintCancelButtonText.GetComponent<LocalizeStringEvent>();
    }

    private void CacheHintCancelButtonPosition()
    {
        if (hintCancelButton == null)
            return;

        RectTransform cancelButtonRectTransform = hintCancelButton.transform as RectTransform;
        if (cancelButtonRectTransform == null)
            return;

        hintCancelButtonDefaultAnchoredPosition = cancelButtonRectTransform.anchoredPosition;
        isHintCancelButtonPositionCached = true;
    }

    private void SetHintCancelButtonCentered(bool isCentered)
    {
        if (!isHintCancelButtonPositionCached)
            CacheHintCancelButtonPosition();

        if (hintCancelButton == null || !isHintCancelButtonPositionCached)
            return;

        RectTransform cancelButtonRectTransform = hintCancelButton.transform as RectTransform;
        if (cancelButtonRectTransform == null)
            return;

        Vector2 targetPosition = hintCancelButtonDefaultAnchoredPosition;
        if (isCentered)
            targetPosition.x = 0f;

        cancelButtonRectTransform.anchoredPosition = targetPosition;
    }

    private void SetHintDialogLocalizersEnabled(bool isEnabled)
    {
        if (hintDialogTitleLocalizer != null)
            hintDialogTitleLocalizer.enabled = isEnabled;

        if (hintCancelButtonTextLocalizer != null)
            hintCancelButtonTextLocalizer.enabled = isEnabled;
    }

    private static string GetHintMessageText(string message)
    {
        if (!IsKoreanLocaleSelected())
            return message;

        switch (message)
        {
            case "Ad Not Completed":
                return "광고 시청이 완료되지 않았습니다.";
            case "Restart Recommended":
                return "다시 시작을 권장합니다.";
            default:
                return message;
        }
    }

    private static string GetTutorialMessageKey(TutorialMessage tutorialMessage)
    {
        switch (tutorialMessage)
        {
            case TutorialMessage.PositiveMoveTile:
                return TutorialPositiveMoveKey;
            case TutorialMessage.NegativeMoveTile:
                return TutorialNegativeMoveKey;
            case TutorialMessage.ExitConditionOdd:
                return TutorialExitOddKey;
            case TutorialMessage.ExitConditionEven:
                return TutorialExitEvenKey;
            case TutorialMessage.NumberObstacle:
                return TutorialNumberObstacleKey;
            default:
                return TutorialPositiveMoveKey;
        }
    }

    private bool UpdateTutorialLayout()
    {
        RectTransform tutorialDialogRect = tutorialDialog == null
            ? null
            : tutorialDialog.transform as RectTransform;
        Camera worldCamera = Camera.main;
        if (tutorialDialogRect == null || tutorialTargetTransform == null || worldCamera == null)
            return false;

        Canvas tutorialCanvas = tutorialDialog.GetComponentInParent<Canvas>();
        Camera canvasCamera = tutorialCanvas != null && tutorialCanvas.renderMode != RenderMode.ScreenSpaceOverlay
            ? tutorialCanvas.worldCamera
            : null;
        Vector3 targetScreenPosition = worldCamera.WorldToScreenPoint(tutorialTargetTransform.position);
        if (targetScreenPosition.z < 0f)
            return false;

        Vector2 targetLocalPosition;
        bool converted = RectTransformUtility.ScreenPointToLocalPointInRectangle(
            tutorialDialogRect,
            targetScreenPosition,
            canvasCamera,
            out targetLocalPosition);
        if (!converted)
            return false;

        tutorialPanelRect.sizeDelta = TutorialPanelSize;
        TutorialPanelPlacement panelPlacement = TutorialPanelLayout.Calculate(
            tutorialDialogRect.rect,
            targetLocalPosition,
            TutorialPanelSize,
            TutorialSafeMargin,
            TutorialPointerGap);
        tutorialPanelRect.anchoredPosition = panelPlacement.Position;
        tutorialPanelImage.rectTransform.localScale = panelPlacement.IsAboveTarget
            ? Vector3.one
            : new Vector3(1f, -1f, 1f);
        UpdateTutorialDimmers(tutorialDialogRect.rect, targetLocalPosition);
        return true;
    }

    private void UpdateTutorialDimmers(Rect dialogRect, Vector2 targetPosition)
    {
        float halfFocusSize = TutorialFocusSize * 0.5f;
        float focusLeft = Mathf.Clamp(targetPosition.x - halfFocusSize, dialogRect.xMin, dialogRect.xMax);
        float focusRight = Mathf.Clamp(targetPosition.x + halfFocusSize, dialogRect.xMin, dialogRect.xMax);
        float focusBottom = Mathf.Clamp(targetPosition.y - halfFocusSize, dialogRect.yMin, dialogRect.yMax);
        float focusTop = Mathf.Clamp(targetPosition.y + halfFocusSize, dialogRect.yMin, dialogRect.yMax);

        SetTutorialDimmerBounds(
            tutorialDimmerLeft,
            dialogRect.xMin,
            focusLeft,
            dialogRect.yMin,
            dialogRect.yMax);
        SetTutorialDimmerBounds(
            tutorialDimmerRight,
            focusRight,
            dialogRect.xMax,
            dialogRect.yMin,
            dialogRect.yMax);
        SetTutorialDimmerBounds(
            tutorialDimmerTop,
            focusLeft,
            focusRight,
            focusTop,
            dialogRect.yMax);
        SetTutorialDimmerBounds(
            tutorialDimmerBottom,
            focusLeft,
            focusRight,
            dialogRect.yMin,
            focusBottom);
    }

    private static void SetTutorialDimmerBounds(
        RectTransform dimmerRect,
        float left,
        float right,
        float bottom,
        float top)
    {
        if (dimmerRect == null)
            return;

        float width = Mathf.Max(0f, right - left);
        float height = Mathf.Max(0f, top - bottom);
        dimmerRect.gameObject.SetActive(width > 0f && height > 0f);
        dimmerRect.anchorMin = new Vector2(0.5f, 0.5f);
        dimmerRect.anchorMax = new Vector2(0.5f, 0.5f);
        dimmerRect.pivot = new Vector2(0.5f, 0.5f);
        dimmerRect.anchoredPosition = new Vector2(
            (left + right) * 0.5f,
            (bottom + top) * 0.5f);
        dimmerRect.sizeDelta = new Vector2(width, height);
    }

    private void UpdateTutorialMessageText(string localizedText)
    {
        if (tutorialMessageText != null)
            tutorialMessageText.text = localizedText;
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
        isShowingClearResult = false;
        isDismissingResult = false;
        HidePanel(clearPanel);
        SetResultInputBlockerVisible(false);
    }

    private void SetClearModeObjects()
    {
        isShowingClearResult = true;
        isDismissingResult = false;

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
        isDismissingResult = false;

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
        {
            if (isDismissingResult)
                return;

            isDismissingResult = true;
            if (primaryActionButton != null)
                primaryActionButton.interactable = false;

            NextClicked?.Invoke();
            return;
        }

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

    private void NotifyTutorialAdvanced()
    {
        TutorialAdvanced?.Invoke();
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

public enum TutorialMessage
{
    PositiveMoveTile,
    NegativeMoveTile,
    ExitConditionOdd,
    ExitConditionEven,
    NumberObstacle
}
