using UnityEngine;

[CreateAssetMenu(fileName = "MapData", menuName = "contar/MapData")]
public class MapData : ScriptableObject
{
    public int width;
    public int height;
    public int startMoveCount;
    public Wrapper<SerializedTile>[] rows; // rows[y].values[x]

    private void Awake()
    {
        if (rows == null)
            ResetGrid();
    }

    public void ResetGrid()
    {
        rows = new Wrapper<SerializedTile>[height];
        for (int y = 0; y < height; y++)
        {
            rows[y] = new Wrapper<SerializedTile>();
            rows[y].values = new SerializedTile[width];
        }
    }
}
