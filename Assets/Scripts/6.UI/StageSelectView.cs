using System;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class StageSelectView : MonoBehaviour
{
    [SerializeField] private CanvasGroup panel;
    [SerializeField] private Button continueButton;
    [SerializeField] private Button openStageSelectButton;
    [SerializeField] private Button closeStageSelectButton;
    [SerializeField] private TMP_Text currentStageText;
    [SerializeField] private Button[] stageButtons;
    [SerializeField] private TMP_Text[] stageButtonTexts;
    [SerializeField] private float panelTweenDuration = 0.18f;

    private UnityAction[] stageButtonHandlers;

    public event Action ContinueClicked;
    public event Action OpenStageSelectClicked;
    public event Action CloseStageSelectClicked;
    public event Action<int> StageClicked;

    private void OnEnable()
    {
        if (continueButton != null)
            continueButton.onClick.AddListener(NotifyContinueClicked);

        if (openStageSelectButton != null)
            openStageSelectButton.onClick.AddListener(NotifyOpenStageSelectClicked);

        if (closeStageSelectButton != null)
            closeStageSelectButton.onClick.AddListener(NotifyCloseStageSelectClicked);

        BindStageButtons();
    }

    private void OnDisable()
    {
        if (continueButton != null)
            continueButton.onClick.RemoveListener(NotifyContinueClicked);

        if (openStageSelectButton != null)
            openStageSelectButton.onClick.RemoveListener(NotifyOpenStageSelectClicked);

        if (closeStageSelectButton != null)
            closeStageSelectButton.onClick.RemoveListener(NotifyCloseStageSelectClicked);

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

    public void SetStageButton(int buttonIndex, int stageIndex, bool isAvailable)
    {
        if (stageButtons == null || buttonIndex < 0 || buttonIndex >= stageButtons.Length)
            return;

        Button button = stageButtons[buttonIndex];
        if (button != null)
            button.interactable = isAvailable;

        TMP_Text label = null;
        if (stageButtonTexts != null && buttonIndex < stageButtonTexts.Length)
            label = stageButtonTexts[buttonIndex];

        if (label != null)
            label.text = isAvailable ? $"Stage {stageIndex + 1}" : $"Stage {stageIndex + 1}";
    }

    public int StageButtonCount => stageButtons == null ? 0 : stageButtons.Length;

    private void BindStageButtons()
    {
        if (stageButtons == null)
            return;

        stageButtonHandlers = new UnityAction[stageButtons.Length];
        for (int i = 0; i < stageButtons.Length; i++)
        {
            int stageIndex = i;
            Button button = stageButtons[i];
            if (button == null)
                continue;

            UnityAction handler = () => NotifyStageClicked(stageIndex);
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

    private void NotifyContinueClicked()
    {
        ContinueClicked?.Invoke();
    }

    private void NotifyOpenStageSelectClicked()
    {
        OpenStageSelectClicked?.Invoke();
    }

    private void NotifyCloseStageSelectClicked()
    {
        CloseStageSelectClicked?.Invoke();
    }

    private void NotifyStageClicked(int stageIndex)
    {
        StageClicked?.Invoke(stageIndex);
    }
}
