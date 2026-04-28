using TMPro;
using UnityEngine;

public class GameUIController : MonoBehaviour
{
    [SerializeField] private TMP_Text moveCountText;
    [SerializeField] private CanvasGroup clearPanel;
    [SerializeField] private CanvasGroup failPanel;

    private void OnEnable()
    {
        GameManager.Instance.MoveCountChanged += UpdateMoveCount;
        GameManager.Instance.StageCleared += ShowClear;
        GameManager.Instance.StageFailed += ShowFail;
    }

    private void OnDisable()
    {
        GameManager.Instance.MoveCountChanged -= UpdateMoveCount;
        GameManager.Instance.StageCleared -= ShowClear;
        GameManager.Instance.StageFailed -= ShowFail;
    }

    private void UpdateMoveCount(int count)
    {
        moveCountText.text = count.ToString();
    }

    private void ShowClear()
    {

    }

    private void ShowFail()
    {

    }
}
