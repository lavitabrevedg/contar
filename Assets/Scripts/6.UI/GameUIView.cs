using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class GameUIView : MonoBehaviour
{
    [SerializeField] private TMP_Text moveCountText;
    [SerializeField] private CanvasGroup clearPanel;
    [SerializeField] private CanvasGroup failPanel;
    [SerializeField] private Button retryButton;
    [SerializeField] private Button nextButton;

    public event Action RetryClicked;
    public event Action NextClicked;

    private void OnEnable()
    {
        if (retryButton != null)
            retryButton.onClick.AddListener(NotifyRetryClicked);

        if (nextButton != null)
            nextButton.onClick.AddListener(NotifyNextClicked);
    }

    private void OnDisable()
    {
        if (retryButton != null)
            retryButton.onClick.RemoveListener(NotifyRetryClicked);

        if (nextButton != null)
            nextButton.onClick.RemoveListener(NotifyNextClicked);
    }

    public void SetMoveCount(int moveCount)
    {
        if (moveCountText == null) return;

        moveCountText.text = moveCount.ToString();
    }

    public void ShowClear()
    {
        HidePanel(failPanel);
        ShowPanel(clearPanel);
    }

    public void ShowFail()
    {
        HidePanel(clearPanel);
        ShowPanel(failPanel);
    }

    public void HideResultPanels()
    {
        HidePanel(clearPanel);
        HidePanel(failPanel);
    }

    private void NotifyRetryClicked()
    {
        RetryClicked?.Invoke();
    }

    private void NotifyNextClicked()
    {
        NextClicked?.Invoke();
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
}
