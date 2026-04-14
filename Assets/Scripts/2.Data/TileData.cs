public enum TileType
{
    Empty,
    Start,
    Exit,
    Move,
    NumberObstacle,
    Wall
}

public enum ExitCondition
{
    Free,
    OddOnly,
    EvenOnly
}

[System.Serializable]
public struct SerializedTile
{
    public TileType type;
    public int value;
    public ExitCondition exitCondition;
}

[System.Serializable]
public class Wrapper<T>
{
    public T[] values;
}