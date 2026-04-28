using TMPro;
using UnityEngine;

public class GameUIView : MonoBehaviour
{
    [SerializeField] private TMP_Text moveCountText;
    [SerializeField] private CanvasGroup clearPanel;
    [SerializeField] private CanvasGroup failPanel;

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
