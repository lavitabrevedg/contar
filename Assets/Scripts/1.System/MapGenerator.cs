using UnityEngine;

public class MapGenerator : MonoBehaviour
{
    [Header("Map Settings")]
    public MapData mapData;

    [Header("Tile Prefabs")]
    [SerializeField] private GameObject emptyPrefab;
    [SerializeField] private GameObject startPrefab;
    [SerializeField] private GameObject exitPrefab;
    [SerializeField] private GameObject movePrefab;
    [SerializeField] private GameObject numberObstaclePrefab;
    [SerializeField] private GameObject wallPrefab;
    [SerializeField] private GameObject PlayerPrefab;

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

                Vector3 pos = GridToWorld(x, y);
                GameObject go = Instantiate(prefab, pos, Quaternion.identity, transform);
                go.name = $"Tile_{x}_{y}_{tileData.type}";

                BaseTile tile = go.GetComponent<BaseTile>();
                tile.Init(tileData);
                _grid[x, y] = tile;

                if(tileData.type == TileType.Start)
                {
                    CreatePlayer(x, y);
                }
            }
        }
    }

    public void ClearMap()
    {
        for (int i = transform.childCount - 1; i >= 0; i--)
            DestroyImmediate(transform.GetChild(i).gameObject);

        _grid = null;
    }

    public Vector3 GridToWorld(int x, int y)
    {
        return new Vector3(x * tileSize , y * tileSize , 0f);
    }

    public Vector2Int WorldToGrid(Vector3 worldPos)
    {
        int x = Mathf.RoundToInt(worldPos.x / tileSize);
        int y = Mathf.RoundToInt(worldPos.y / tileSize);
        return new Vector2Int(x, y);
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
            case TileType.Empty: return emptyPrefab;
            case TileType.Start: return startPrefab;
            case TileType.Exit: return exitPrefab;
            case TileType.Move: return movePrefab;
            case TileType.NumberObstacle: return numberObstaclePrefab;
            case TileType.Wall: return wallPrefab;
            default: return null;
        }
    }

    private void CreatePlayer(int gridX, int gridY)
    {
        if (PlayerPrefab == null) return;
        if (GameManager.Instance == null) return;

        Vector3 spawnPos = GridToWorld(gridX, gridY);
        GameObject playerGO = Instantiate(PlayerPrefab, spawnPos, Quaternion.identity, transform);

        PlayerController controller = playerGO.GetComponent<PlayerController>();
        if (controller == null) return;

        GameManager.Instance.RegisterPlayer(controller, new Vector2Int(gridX, gridY));
    }
}
