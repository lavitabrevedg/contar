
public class MoveTile : BaseTile
{
    public int value;
    public override void Init(SerializedTile data)
    {
        value = data.value;
    }
}
