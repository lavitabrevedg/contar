using System;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class StageSelectView : MonoBehaviour
{
    private const float pageButtonOffset = 64f;
    private const float pageButtonSize = 52f;

    [SerializeField] private CanvasGroup panel;
    [SerializeField] private Button openStageSelectButton;
    [SerializeField] private Button closeStageSelectButton;
    [SerializeField] private Button previousPageButton;
    [SerializeField] private Button nextPageButton;
    [SerializeField] private TMP_Text currentStageText;
    [SerializeField] private Button playCurrentStageButton;
    [SerializeField] private TMP_Text currentStageButtonText;
    [SerializeField] private Button[] stageButtons;
    [SerializeField] private TMP_Text[] stageButtonTexts;
    [SerializeField] private float panelTweenDuration = 0.18f;

    private UnityAction[] stageButtonHandlers;
    private int pageStartStageIndex;

    public event Action OpenStageSelectClicked;
    public event Action CloseStageSelectClicked;
    public event Action PreviousPageClicked;
    public event Action NextPageClicked;
    public event Action PlayCurrentStageClicked;
    public event Action<int> StageClicked;

    private void OnEnable()
    {
        EnsurePageButtons();

        if (openStageSelectButton != null)
            openStageSelectButton.onClick.AddListener(NotifyOpenStageSelectClicked);

        if (closeStageSelectButton != null)
            closeStageSelectButton.onClick.AddListener(NotifyCloseStageSelectClicked);

        if (previousPageButton != null)
            previousPageButton.onClick.AddListener(NotifyPreviousPageClicked);

        if (nextPageButton != null)
            nextPageButton.onClick.AddListener(NotifyNextPageClicked);

        if (playCurrentStageButton != null)
            playCurrentStageButton.onClick.AddListener(NotifyPlayCurrentStageClicked);

        BindStageButtons();
    }

    private void OnDisable()
    {
        if (openStageSelectButton != null)
            openStageSelectButton.onClick.RemoveListener(NotifyOpenStageSelectClicked);

        if (closeStageSelectButton != null)
            closeStageSelectButton.onClick.RemoveListener(NotifyCloseStageSelectClicked);

        if (previousPageButton != null)
            previousPageButton.onClick.RemoveListener(NotifyPreviousPageClicked);

        if (nextPageButton != null)
            nextPageButton.onClick.RemoveListener(NotifyNextPageClicked);

        if (playCurrentStageButton != null)
            playCurrentStageButton.onClick.RemoveListener(NotifyPlayCurrentStageClicked);

        UnbindStageButtons();
    }

    public void SetPanelVisible(bool isVisible)
    {
        if (panel == null)
            return;

        panel.DOKill();
        panel.transform.DOKill();

        if (isVisible)
        {
            panel.gameObject.SetActive(true);
            panel.alpha = 0f;
            panel.transform.localScale = Vector3.one * 0.96f;
            panel.interactable = false;
            panel.blocksRaycasts = true;

            panel.DOFade(1f, panelTweenDuration);
            panel.transform.DOScale(Vector3.one, panelTweenDuration)
                .SetEase(Ease.OutBack)
                .OnComplete(() => panel.interactable = true);
            return;
        }

        panel.interactable = false;
        panel.blocksRaycasts = false;

        panel.DOFade(0f, panelTweenDuration);
        panel.transform.DOScale(Vector3.one * 0.98f, panelTweenDuration)
            .SetEase(Ease.InQuad)
            .OnComplete(() => panel.gameObject.SetActive(false));
    }

    public void SetCurrentStageText(int currentStageIndex, int stageCount)
    {
        if (currentStageText == null)
            return;

        if (stageCount <= 0)
        {
            currentStageText.text = "스테이지 없음";
            return;
        }

        currentStageText.text = $"스테이지 {currentStageIndex + 1}/{stageCount}";
    }

    public void SetCurrentStageButton(int currentStageIndex, int stageCount, bool isAvailable)
    {
        if (playCurrentStageButton != null)
            playCurrentStageButton.interactable = isAvailable;

        if (currentStageButtonText == null)
            return;

        if (stageCount <= 0 || !isAvailable)
        {
            currentStageButtonText.text = "Stage -";
            return;
        }

        currentStageButtonText.text = $"Stage {currentStageIndex + 1}";
    }

    public void SetPageStartStageIndex(int stageIndex)
    {
        pageStartStageIndex = Mathf.Max(0, stageIndex);
    }

    public void SetPageButtons(bool canMovePrevious, bool canMoveNext)
    {
        EnsurePageButtons();

        if (previousPageButton != null)
            previousPageButton.interactable = canMovePrevious;

        if (nextPageButton != null)
            nextPageButton.interactable = canMoveNext;
    }

    public void SetStageButton(int buttonIndex, int stageIndex, bool isVisible, bool isAvailable)
    {
        if (stageButtons == null || buttonIndex < 0 || buttonIndex >= stageButtons.Length)
            return;

        Button button = stageButtons[buttonIndex];
        if (button != null)
        {
            button.gameObject.SetActive(isVisible);
            button.interactable = isVisible && isAvailable;
        }

        TMP_Text label = null;
        if (stageButtonTexts != null && buttonIndex < stageButtonTexts.Length)
            label = stageButtonTexts[buttonIndex];

        if (label != null)
            label.text = $"Stage {stageIndex + 1}";
    }

    public int StageButtonCount => stageButtons == null ? 0 : stageButtons.Length;

    private void BindStageButtons()
    {
        if (stageButtons == null)
            return;

        stageButtonHandlers = new UnityAction[stageButtons.Length];
        for (int i = 0; i < stageButtons.Length; i++)
        {
            int buttonIndex = i;
            Button button = stageButtons[i];
            if (button == null)
                continue;

            UnityAction handler = () => NotifyStageClicked(pageStartStageIndex + buttonIndex);
            stageButtonHandlers[i] = handler;
            button.onClick.AddListener(handler);
        }
    }

    private void UnbindStageButtons()
    {
        if (stageButtons == null || stageButtonHandlers == null)
            return;

        for (int i = 0; i < stageButtons.Length; i++)
        {
            Button button = stageButtons[i];
            UnityAction handler = stageButtonHandlers[i];
            if (button != null && handler != null)
                button.onClick.RemoveListener(handler);
        }

        stageButtonHandlers = null;
    }

    private void NotifyOpenStageSelectClicked()
    {
        OpenStageSelectClicked?.Invoke();
    }

    private void NotifyCloseStageSelectClicked()
    {
        CloseStageSelectClicked?.Invoke();
    }

    private void NotifyPreviousPageClicked()
    {
        PreviousPageClicked?.Invoke();
    }

    private void NotifyNextPageClicked()
    {
        NextPageClicked?.Invoke();
    }

    private void NotifyPlayCurrentStageClicked()
    {
        PlayCurrentStageClicked?.Invoke();
    }

    private void NotifyStageClicked(int stageIndex)
    {
        StageClicked?.Invoke(stageIndex);
    }

    private void EnsurePageButtons()
    {
        if (panel == null)
            return;

        if (previousPageButton == null)
            previousPageButton = CreatePageButton("PreviousPageButton", "<", -1f);

        if (nextPageButton == null)
            nextPageButton = CreatePageButton("NextPageButton", ">", 1f);
    }

    private Button CreatePageButton(string objectName, string labelText, float direction)
    {
        GameObject buttonObject = new GameObject(objectName, typeof(RectTransform), typeof(Image), typeof(Button));
        buttonObject.transform.SetParent(panel.transform, false);

        RectTransform buttonRect = buttonObject.GetComponent<RectTransform>();
        buttonRect.anchorMin = new Vector2(0.5f, 0.5f);
        buttonRect.anchorMax = new Vector2(0.5f, 0.5f);
        buttonRect.pivot = new Vector2(0.5f, 0.5f);
        buttonRect.sizeDelta = new Vector2(pageButtonSize, pageButtonSize);

        RectTransform panelRect = panel.transform as RectTransform;
        float panelHalfWidth = panelRect == null ? 220f : panelRect.rect.width * 0.5f;
        if (panelHalfWidth <= 0f)
            panelHalfWidth = 220f;

        buttonRect.anchoredPosition = new Vector2(direction * (panelHalfWidth + pageButtonOffset), 0f);

        Image image = buttonObject.GetComponent<Image>();
        image.color = new Color(0.22f, 0.18f, 0.42f, 0.95f);

        Button button = buttonObject.GetComponent<Button>();
        button.targetGraphic = image;

        ColorBlock colors = button.colors;
        colors.disabledColor = new Color(0.22f, 0.18f, 0.42f, 0.35f);
        colors.highlightedColor = new Color(0.32f, 0.27f, 0.58f, 1f);
        colors.pressedColor = new Color(0.16f, 0.13f, 0.32f, 1f);
        button.colors = colors;

        GameObject labelObject = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
        labelObject.transform.SetParent(buttonObject.transform, false);

        RectTransform labelRect = labelObject.GetComponent<RectTransform>();
        labelRect.anchorMin = Vector2.zero;
        labelRect.anchorMax = Vector2.one;
        labelRect.offsetMin = Vector2.zero;
        labelRect.offsetMax = Vector2.zero;

        TextMeshProUGUI label = labelObject.GetComponent<TextMeshProUGUI>();
        label.text = labelText;
        label.alignment = TextAlignmentOptions.Center;
        label.fontSize = 34f;
        label.fontStyle = FontStyles.Bold;
        label.color = Color.white;
        label.raycastTarget = false;

        return button;
    }
}
