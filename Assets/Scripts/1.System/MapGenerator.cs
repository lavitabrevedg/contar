using UnityEngine;

[CreateAssetMenu(fileName = "MapData", menuName = "contar/MapData")]
public class MapData : ScriptableObject
{
    public int width;
    public int height;
    public int startMoveCount;
    public SerializedTile[] tiles;  // 길이 = width * height
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
            for (int x = 0; x < mapData.width; x++)
            {
                int index = y * mapData.width + x;
                if (index >= mapData.tiles.Length) break;

                SerializedTile tileData = mapData.tiles[index];
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
        return type switch
        {
            TileType.Empty          => emptyPrefab,
            TileType.Start          => startPrefab,
            TileType.Exit           => exitPrefab,
            TileType.Move           => movePrefab,
            TileType.NumberObstacle => numberObstaclePrefab,
            TileType.Wall           => wallPrefab,
            _                       => null
        };
    }
}
