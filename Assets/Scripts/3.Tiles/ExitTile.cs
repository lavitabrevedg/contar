using UnityEngine;

public class ExitTile : BaseTile
{
    public ExitCondition condition;

    [SerializeField] private Sprite freeExitSprite;
    [SerializeField] private Sprite oddExitSprite;
    [SerializeField] private Sprite evenExitSprite;

    public override void Init(SerializedTile data)
    {
        condition = data.exitCondition;
        ApplyConditionSprite();
        SetTileLabel(string.Empty);
    }

    public override void OnPlayerEnter()
    {
        if (GameManager.Instance == null) return;

        int remainingMoves = GameManager.Instance.CurrentMoveCount;
        if (CanEnter(remainingMoves))
        {
            GameManager.Instance.NotifyStageCleared();
            return;
        }

        PlayBlockedFeedback();
        ShowTileLabelFeedback(GetBlockedFeedbackText(), UnityEngine.Color.red);
        GameManager.Instance.NotifyExitBlocked(condition);
    }

    private bool CanEnter(int moveCount)
    {
        switch (condition)
        {
            case ExitCondition.Free:
                return true;
            case ExitCondition.OddOnly:
                return moveCount % 2 != 0;
            case ExitCondition.EvenOnly:
                return moveCount % 2 == 0;
            default:
                return false;
        }
    }

    private string GetBlockedFeedbackText()
    {
        switch (condition)
        {
            case ExitCondition.OddOnly:
                return "Odd Moves";
            case ExitCondition.EvenOnly:
                return "Even Moves";
            default:
                return "Locked";
        }
    }

    private void ApplyConditionSprite()
    {
        SpriteRenderer tileSpriteRenderer = GetComponent<SpriteRenderer>();
        Sprite conditionSprite = GetSpriteForCondition();

        if (tileSpriteRenderer == null || conditionSprite == null)
        {
            return;
        }

        tileSpriteRenderer.sprite = conditionSprite;
    }

    private Sprite GetSpriteForCondition()
    {
        switch (condition)
        {
            case ExitCondition.Free:
                return freeExitSprite;
            case ExitCondition.OddOnly:
                return oddExitSprite;
            case ExitCondition.EvenOnly:
                return evenExitSprite;
            default:
                return freeExitSprite;
        }
    }
}
