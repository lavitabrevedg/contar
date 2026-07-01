public class ExitTile : BaseTile
{
    public ExitCondition condition;

    public override void Init(SerializedTile data)
    {
        condition = data.exitCondition;
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
}
