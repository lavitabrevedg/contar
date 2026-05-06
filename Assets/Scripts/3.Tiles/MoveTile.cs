
public class MoveTile : BaseTile
{
    public int value;

    public bool IsConsumed { get; private set; }

    public override void Init(SerializedTile data)
    {
        value = data.value;
        IsConsumed = false;
    }

    public override void OnPlayerEnter()
    {
        if (IsConsumed) return;

        // value가 양수면 +타일, 음수면 -타일 역할
        if (GameManager.Instance == null) return;

        IsConsumed = true;
        GameManager.Instance.AddMoveCount(value);
    }
}
