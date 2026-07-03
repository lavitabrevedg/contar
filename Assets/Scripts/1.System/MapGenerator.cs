using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;

public class MapGenerator : MonoBehaviour
{
    [Header("Map Settings")]
    public MapData mapData;
    [SerializeField] private CameraFitter cameraFitter;

    [Header("Tile Prefabs")]
    [SerializeField] private GameObject emptyPrefab;
    [SerializeField] private GameObject startPrefab;
    [SerializeField] private GameObject exitPrefab;
    [SerializeField] private GameObject movePrefab;
    [SerializeField] private GameObject numberObstaclePrefab;
    [SerializeField] private GameObject wallPrefab;
    [FormerlySerializedAs("PlayerPrefab")]
    [SerializeField] private GameObject playerPrefab;

    [Header("Grid Settings")]
    public float tileSize = 1f;
    [SerializeField] private float tileMoveDuration = 0.12f;

    [Header("Obstacle Break Effect")]
    [SerializeField] private float obstacleBreakDuration = 0.18f;
    [SerializeField] private float obstacleBreakScale = 1.16f;
    [SerializeField] private float obstacleBreakShakeStrength = 0.08f;
    [SerializeField] private float obstacleBreakFadeDelay = 0.04f;

    private BaseTile[,] grid;

    private void Start()
    {
        GenerateMap();
    }

    public void SetMapData(MapData nextMapData, bool regenerate)
    {
        if (nextMapData == null)
        {
            Debug.LogWarning("[MapGenerator] Cannot set an empty MapData.");
            return;
        }

        mapData = nextMapData;

        if (regenerate)
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

        grid = new BaseTile[mapData.width, mapData.height];

        for (int y = 0; y < mapData.height; y++)
        {
            if (mapData.rows[y] == null || mapData.rows[y].values == null) continue;

            for (int x = 0; x < mapData.width; x++)
            {
                SerializedTile tileData = mapData.rows[y].values[x];
                CreateTile(x, y, tileData);
            }
        }

        // 맵 생성 후 카메라와 배경을 맵 크기에 맞춰 조정
        if (cameraFitter != null)
            cameraFitter.Fit();
    }

    public void ClearMap()
    {
        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            Transform childTransform = transform.GetChild(i);
            childTransform.DOKill();

            BaseTile[] childTiles = childTransform.GetComponentsInChildren<BaseTile>(true);
            for (int tileIndex = 0; tileIndex < childTiles.Length; tileIndex++)
                childTiles[tileIndex].transform.DOKill();

            SpriteRenderer[] spriteRenderers = childTransform.GetComponentsInChildren<SpriteRenderer>(true);
            for (int rendererIndex = 0; rendererIndex < spriteRenderers.Length; rendererIndex++)
                spriteRenderers[rendererIndex].DOKill();

            TMP_Text[] labelTexts = childTransform.GetComponentsInChildren<TMP_Text>(true);
            for (int labelIndex = 0; labelIndex < labelTexts.Length; labelIndex++)
                labelTexts[labelIndex].DOKill();

            DestroyTileObject(childTransform.gameObject);
        }

        grid = null;
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
        if (grid == null || x < 0 || y < 0 || x >= mapData.width || y >= mapData.height)
            return null;
        return grid[x, y];
    }

    public void SwapTiles(Vector2Int a, Vector2Int b)
    {
        if (grid == null) return;

        BaseTile tileA = grid[a.x, a.y];
        BaseTile tileB = grid[b.x, b.y];

        grid[a.x, a.y] = tileB;
        grid[b.x, b.y] = tileA;

        MoveTileTransform(tileA, GridToWorld(b.x, b.y));
        MoveTileTransform(tileB, GridToWorld(a.x, a.y));
    }

    public void ReplaceTileWithEmpty(Vector2Int position)
    {
        if (grid == null) return;
        if (mapData == null) return;
        if (position.x < 0 || position.y < 0 || position.x >= mapData.width || position.y >= mapData.height)
            return;

        BaseTile previousTile = grid[position.x, position.y];
        if (previousTile != null)
        {
            previousTile.transform.DOKill();

            if (Application.isPlaying && previousTile is NumberObstacle)
                PlayObstacleBreakEffect(previousTile);
            else
                DestroyTileObject(previousTile.gameObject);
        }

        grid[position.x, position.y] = null;

        SerializedTile emptyTile = default;
        emptyTile.type = TileType.Empty;
        CreateTile(position.x, position.y, emptyTile);
    }

    private void PlayObstacleBreakEffect(BaseTile obstacleTile)
    {
        if (obstacleTile == null)
            return;

        Transform obstacleTransform = obstacleTile.transform;
        Vector3 startScale = obstacleTransform.localScale;

        SpriteRenderer[] spriteRenderers = obstacleTile.GetComponentsInChildren<SpriteRenderer>(true);
        TMP_Text[] labelTexts = obstacleTile.GetComponentsInChildren<TMP_Text>(true);

        for (int rendererIndex = 0; rendererIndex < spriteRenderers.Length; rendererIndex++)
            spriteRenderers[rendererIndex].sortingOrder += 20;

        Sequence sequence = DOTween.Sequence();
        sequence.SetTarget(obstacleTransform);
        sequence.Join(obstacleTransform.DOShakePosition(obstacleBreakDuration, obstacleBreakShakeStrength, 14, 90f, false, true));
        sequence.Join(obstacleTransform.DOScale(startScale * obstacleBreakScale, obstacleBreakDuration * 0.45f).SetEase(Ease.OutBack));
        sequence.Append(obstacleTransform.DOScale(Vector3.zero, obstacleBreakDuration * 0.65f).SetEase(Ease.InBack));

        float fadeDuration = Mathf.Max(0.01f, obstacleBreakDuration - obstacleBreakFadeDelay);
        for (int rendererIndex = 0; rendererIndex < spriteRenderers.Length; rendererIndex++)
            sequence.Insert(obstacleBreakFadeDelay, spriteRenderers[rendererIndex].DOFade(0f, fadeDuration));

        for (int labelIndex = 0; labelIndex < labelTexts.Length; labelIndex++)
            sequence.Insert(obstacleBreakFadeDelay, labelTexts[labelIndex].DOFade(0f, fadeDuration));

        sequence.OnComplete(() => DestroyTileObject(obstacleTile.gameObject));
    }

    private void DestroyTileObject(GameObject tileObject)
    {
        if (tileObject == null)
            return;

        if (Application.isPlaying)
            Destroy(tileObject);
        else
            DestroyImmediate(tileObject);
    }

    private void MoveTileTransform(BaseTile tile, Vector3 targetPosition)
    {
        if (tile == null)
            return;

        tile.transform.DOKill();

        if (tile is NumberObstacle)
        {
            tile.transform.DOMove(targetPosition, tileMoveDuration).SetEase(Ease.OutQuad);
            return;
        }

        tile.transform.position = targetPosition;
    }

    private BaseTile CreateTile(int x, int y, SerializedTile tileData)
    {
        GameObject prefab = GetPrefab(tileData.type);
        if (prefab == null) return null;

        Vector3 pos = GridToWorld(x, y);
        GameObject go = Instantiate(prefab, pos, Quaternion.identity, transform);
        go.name = $"Tile_{x}_{y}_{tileData.type}";

        BaseTile tile = go.GetComponent<BaseTile>();
        tile.Init(tileData);
        grid[x, y] = tile;

        if (tileData.type == TileType.Start)
        {
            CreatePlayer(x, y);
        }

        return tile;
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
        if (playerPrefab == null) return;
        if (GameManager.Instance == null) return;

        Vector3 spawnPos = GridToWorld(gridX, gridY);
        GameObject playerGO = Instantiate(playerPrefab, spawnPos, Quaternion.identity, transform);

        PlayerController controller = playerGO.GetComponent<PlayerController>();
        if (controller == null) return;

        GameManager.Instance.RegisterPlayer(controller, new Vector2Int(gridX, gridY));
    }
}
