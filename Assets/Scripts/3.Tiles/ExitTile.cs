public class ExitTile : BaseTile
{
    public ExitCondition condition;

    public override void Init(SerializedTile data)
    {
        condition = data.exitCondition;
    }

    public override void OnPlayerEnter()
    {
        // if(CanEnter()) 흠...이거는 system에서 확인해줘야하나
        //@TODO Exit Player
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
