using System;
using System.Collections;
using System.Collections.Generic;
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

    [Header("Hint Route")]
    [SerializeField] private float hintStepInterval = 0.18f;
    [SerializeField] private GameObject hintEffectPrefab;
    [SerializeField] private float hintEffectScale = 0.1f;
    [SerializeField] private int hintEffectSortingOrder = 15;

    [Header("Initial Map Reveal")]
    [SerializeField] private float tileRevealDuration = 0.18f;
    [SerializeField] private float playerRevealDuration = 0.18f;
    [SerializeField] private float totalRevealDuration = 0.8f;
    [SerializeField] private Ease tileRevealEase = Ease.OutBack;

    private BaseTile[,] grid;
    private Coroutine hintRouteCoroutine;
    private readonly List<GameObject> activeHintEffects = new List<GameObject>();
    private readonly Dictionary<Transform, Vector3> initialRevealScales = new Dictionary<Transform, Vector3>();
    private Transform generatedPlayerTransform;
    private Sequence initialRevealSequence;
    private AudioService audioService;

    public bool HasActiveHintRoute => hintRouteCoroutine != null || activeHintEffects.Count > 0;

    private void OnDisable()
    {
        StopInitialReveal(true);
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

    public void PlayInitialReveal(Action completed)
    {
        StopInitialReveal(true);

        if (grid == null || mapData == null)
        {
            completed?.Invoke();
            return;
        }

        List<Transform> tileTransforms = GetTileTransformsInRevealOrder();
        if (tileTransforms.Count == 0)
        {
            completed?.Invoke();
            return;
        }

        PlayMapRevealSound();

        for (int tileIndex = 0; tileIndex < tileTransforms.Count; tileIndex++)
            HideRevealTarget(tileTransforms[tileIndex]);

        if (generatedPlayerTransform != null)
            HideRevealTarget(generatedPlayerTransform);

        float safeTileDuration = Mathf.Max(0.01f, tileRevealDuration);
        float safePlayerDuration = Mathf.Max(0.01f, playerRevealDuration);
        float safeTotalDuration = Mathf.Max(safeTileDuration + safePlayerDuration, totalRevealDuration);
        float tileStartWindow = Mathf.Max(0f, safeTotalDuration - safeTileDuration - safePlayerDuration);
        float tileInterval = tileTransforms.Count <= 1
            ? 0f
            : tileStartWindow / (tileTransforms.Count - 1);

        initialRevealSequence = DOTween.Sequence();
        initialRevealSequence.SetUpdate(true);
        initialRevealSequence.SetTarget(transform);

        for (int tileIndex = 0; tileIndex < tileTransforms.Count; tileIndex++)
        {
            Transform tileTransform = tileTransforms[tileIndex];
            Vector3 targetScale = initialRevealScales[tileTransform];
            initialRevealSequence.Insert(
                tileIndex * tileInterval,
                tileTransform.DOScale(targetScale, safeTileDuration).SetEase(tileRevealEase));
        }

        float playerStartTime = tileStartWindow + safeTileDuration;
        if (generatedPlayerTransform != null && initialRevealScales.ContainsKey(generatedPlayerTransform))
        {
            Vector3 playerTargetScale = initialRevealScales[generatedPlayerTransform];
            initialRevealSequence.Insert(
                playerStartTime,
                generatedPlayerTransform.DOScale(playerTargetScale, safePlayerDuration).SetEase(tileRevealEase));
        }
        else
        {
            initialRevealSequence.AppendInterval(safePlayerDuration);
        }

        initialRevealSequence.OnComplete(() =>
        {
            initialRevealSequence = null;
            initialRevealScales.Clear();
            completed?.Invoke();
        });
    }

    public void StopInitialReveal(bool restoreScale)
    {
        if (initialRevealSequence != null)
        {
            initialRevealSequence.Kill(false);
            initialRevealSequence = null;
        }

        if (restoreScale)
        {
            foreach (KeyValuePair<Transform, Vector3> revealScale in initialRevealScales)
            {
                if (revealScale.Key != null)
                    revealScale.Key.localScale = revealScale.Value;
            }
        }

        initialRevealScales.Clear();
    }

    public void ClearMap()
    {
        StopInitialReveal(true);
        StopHintRoute();
        generatedPlayerTransform = null;

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

    public bool TryCreatePuzzleSnapshot(
        Vector2Int playerPosition,
        int remainingMoves,
        out PuzzleSnapshot puzzleSnapshot)
    {
        puzzleSnapshot = null;
        if (mapData == null || grid == null)
            return false;

        List<Vector2Int> consumedMoveTiles = new List<Vector2Int>();
        List<PuzzleObstacleState> currentObstacles = new List<PuzzleObstacleState>();

        for (int y = 0; y < mapData.height; y++)
        {
            for (int x = 0; x < mapData.width; x++)
            {
                BaseTile tile = grid[x, y];
                MoveTile moveTile = tile as MoveTile;
                if (moveTile != null && moveTile.IsConsumed)
                    consumedMoveTiles.Add(new Vector2Int(x, y));

                NumberObstacle obstacle = tile as NumberObstacle;
                if (obstacle != null)
                {
                    currentObstacles.Add(new PuzzleObstacleState(
                        new Vector2Int(x, y),
                        obstacle.value));
                }
            }
        }

        string errorMessage;
        bool created = PuzzleSolver.TryCreateSnapshot(
            mapData,
            playerPosition,
            remainingMoves,
            consumedMoveTiles,
            currentObstacles,
            out puzzleSnapshot,
            out errorMessage);

        if (!created)
            Debug.LogWarning($"[MapGenerator] Could not create hint snapshot: {errorMessage}");

        return created;
    }

    public void PlayHintRoute(IReadOnlyList<PuzzleRouteStep> route)
    {
        StopHintRoute();
        if (route == null || route.Count == 0)
            return;

        hintRouteCoroutine = StartCoroutine(PlayHintRouteCoroutine(route));
    }

    public void StopHintRoute()
    {
        if (hintRouteCoroutine != null)
        {
            StopCoroutine(hintRouteCoroutine);
            hintRouteCoroutine = null;
        }

        for (int effectIndex = 0; effectIndex < activeHintEffects.Count; effectIndex++)
        {
            GameObject hintEffect = activeHintEffects[effectIndex];
            if (hintEffect != null)
                DestroyTileObject(hintEffect);
        }

        activeHintEffects.Clear();
    }

    private IEnumerator PlayHintRouteCoroutine(IReadOnlyList<PuzzleRouteStep> route)
    {
        WaitForSecondsRealtime stepWait = new WaitForSecondsRealtime(Mathf.Max(0.05f, hintStepInterval));

        for (int stepIndex = 0; stepIndex < route.Count; stepIndex++)
        {
            Vector2Int highlightPosition = route[stepIndex].HighlightPosition;
            BaseTile tile = GetTile(highlightPosition.x, highlightPosition.y);
            if (tile == null)
                continue;

            PlayHintEffect(tile.transform.position);
            yield return stepWait;
        }

        hintRouteCoroutine = null;
    }

    private void PlayHintEffect(Vector3 worldPosition)
    {
        if (hintEffectPrefab == null)
            return;

        GameObject hintEffect = Instantiate(hintEffectPrefab, worldPosition, Quaternion.identity);
        hintEffect.transform.localScale = Vector3.one * Mathf.Max(0.01f, hintEffectScale);

        ParticleSystemRenderer[] particleRenderers = hintEffect.GetComponentsInChildren<ParticleSystemRenderer>(true);
        for (int rendererIndex = 0; rendererIndex < particleRenderers.Length; rendererIndex++)
            particleRenderers[rendererIndex].sortingOrder = hintEffectSortingOrder;

        activeHintEffects.Add(hintEffect);
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
        generatedPlayerTransform = playerGO.transform;

        PlayerController controller = playerGO.GetComponent<PlayerController>();
        if (controller == null) return;

        GameManager.Instance.RegisterPlayer(controller, new Vector2Int(gridX, gridY));
    }

    private List<Transform> GetTileTransformsInRevealOrder()
    {
        List<Transform> tileTransforms = new List<Transform>();

        for (int y = 0; y < mapData.height; y++)
        {
            for (int x = 0; x < mapData.width; x++)
            {
                BaseTile tile = grid[x, y];
                if (tile != null)
                    tileTransforms.Add(tile.transform);
            }
        }

        return tileTransforms;
    }

    private void HideRevealTarget(Transform target)
    {
        if (target == null)
            return;

        target.DOKill();
        initialRevealScales[target] = target.localScale;
        target.localScale = Vector3.zero;
    }

    private void PlayMapRevealSound()
    {
        if (audioService == null)
            audioService = FindFirstObjectByType<AudioService>();

        if (audioService != null)
            audioService.PlayMapReveal();
    }
}
