
public class MoveTile : BaseTile
{
    public int value;

    public override void Init(SerializedTile data)
    {
        value = data.value;
    }

    public override void OnPlayerEnter()
    {
        // value가 양수면 +타일, 음수면 -타일 역할
        if (GameManager.Instance == null) return;
        GameManager.Instance.AddMoveCount(value);
    }
}
