public class ExitTile : BaseTile
{
    public ExitCondition condition;

    public override void Init(SerializedTile data)
    {
        condition = data.exitCondition;
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

        GameManager.Instance.NotifyExitBlocked(condition);
    }

    private bool CanEnter(int moveCount)
    {
        return condition switch
        {
            ExitCondition.Free     => true,
            ExitCondition.OddOnly  => moveCount % 2 != 0,
            ExitCondition.EvenOnly => moveCount % 2 == 0,
            _                      => false
        };
    }
}
