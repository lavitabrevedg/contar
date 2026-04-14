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

public class MapGenerator : MonoBehaviour
{
    [Header("Map Settings")]
    public MapData mapData;

    [Header("Tile Prefabs")]
    public GameObject emptyPrefab;
    public GameObject startPrefab;
    public GameObject exitPrefab;
    public GameObject movePrefab;
    public GameObject numberObstaclePrefab;
    public GameObject wallPrefab;

    [Header("Grid Settings")]
    public float tileSize = 1f;

    private BaseTile[,] _grid;

    private void Start()
    {
        GenerateMap();
    }

    public void GenerateMap()
    {
        ClearMap();

        if (mapData == null)
        {
            Debug.LogError("MapData가 없습니다.");
            return;
        }

        _grid = new BaseTile[mapData.width, mapData.height];

        for (int y = 0; y < mapData.height; y++)
        {
            if (mapData.rows[y] == null || mapData.rows[y].values == null) continue;

            for (int x = 0; x < mapData.width; x++)
            {
                SerializedTile tileData = mapData.rows[y].values[x];
                GameObject prefab = GetPrefab(tileData.type);
                if (prefab == null) continue;

                Vector3 pos = new Vector3(x * tileSize, y * tileSize, 0f);
                GameObject go = Instantiate(prefab, pos, Quaternion.identity, transform);
                go.name = $"Tile_{x}_{y}_{tileData.type}";

                BaseTile tile = go.GetComponent<BaseTile>();
                tile.Init(tileData);

                _grid[x, y] = tile;
            }
        }
    }

    public void ClearMap()
    {
        for (int i = transform.childCount - 1; i >= 0; i--)
            DestroyImmediate(transform.GetChild(i).gameObject);

        _grid = null;
    }

    public BaseTile GetTile(int x, int y)
    {
        if (_grid == null || x < 0 || y < 0 || x >= mapData.width || y >= mapData.height)
            return null;
        return _grid[x, y];
    }

    private GameObject GetPrefab(TileType type)
    {
        switch (type)
        {
            case TileType.Empty:          return emptyPrefab;
            case TileType.Start:          return startPrefab;
            case TileType.Exit:           return exitPrefab;
            case TileType.Move:           return movePrefab;
            case TileType.NumberObstacle: return numberObstaclePrefab;
            case TileType.Wall:           return wallPrefab;
            default:                      return null;
        }
    }
}
