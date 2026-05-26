
public class MoveTile : BaseTile
{
    public int value;

    public bool IsConsumed { get; private set; }

    public override void Init(SerializedTile data)
    {
        value = data.value;
        IsConsumed = false;
        SetTileLabel(FormatValue(value));
    }

    public override void OnPlayerEnter()
    {
        if (IsConsumed) return;

        // value가 양수면 +타일, 음수면 -타일 역할
        if (GameManager.Instance == null) return;

        IsConsumed = true;
        GameManager.Instance.AddMoveCount(value);
        SetTileLabel(string.Empty);
    }

    private string FormatValue(int moveValue)
    {
        if (moveValue > 0)
            return $"+{moveValue}";

        return moveValue.ToString();
    }
}
