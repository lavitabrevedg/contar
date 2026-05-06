using System;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [SerializeField] private MapGenerator mapGenerator;
    [SerializeField] private GameStateModel stateModel;
    [SerializeField] private StageProgressService progressService;

    private PlayerController _player;
    private MoveResolver _moveResolver;

    public int CurrentMoveCount => stateModel.MoveCount;
    public MapGenerator MapGenerator => mapGenerator;
    public GameState State => stateModel.State;
    public GameStateModel StateModel => stateModel;
    public StageProgressService ProgressService => progressService;

    public event Action<int> StageCleared;
    public event Action<int, int> StageFailed;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        if (stateModel == null)
            stateModel = GetComponent<GameStateModel>();

        if (stateModel == null)
            stateModel = gameObject.AddComponent<GameStateModel>();

        ResolveProgressService();
        _moveResolver = new MoveResolver(mapGenerator);
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    public void RegisterPlayer(PlayerController player, Vector2Int startGrid)
    {
        _player = player;

        int startMoveCount = 0;
        if (mapGenerator != null && mapGenerator.mapData != null)
            startMoveCount = mapGenerator.mapData.startMoveCount;

        stateModel.StartStage(startMoveCount);

        Vector3 startWorldPos = mapGenerator.GridToWorld(startGrid.x, startGrid.y);
        _player.Init(startGrid, startWorldPos);
    }

    public void OnSwipe(Vector2Int direction)
    {
        if (State != GameState.Playing) return;
        if (_player == null) return;
        if (_player.IsMoving) return;
        if (direction == Vector2Int.zero) return;

        MoveResult result = _moveResolver.Resolve(_player.GridPosition, direction, CurrentMoveCount);
        if (!result.isAllowed) return;

        stateModel.SpendMoveCount(result.moveCost);

        if (result.pushedObstacle != null)
        {
            mapGenerator.SwapTiles(result.obstacleFrom, result.obstacleTarget);

            if (State == GameState.Playing && CurrentMoveCount <= 0)
                Fail();

            return;
        }

        Vector3 targetWorldPos = mapGenerator.GridToWorld(result.playerTarget.x, result.playerTarget.y);
        Vector2Int landedGrid = result.playerTarget;
        _player.AnimateTo(result.playerTarget, targetWorldPos, () => OnPlayerLanded(landedGrid));
    }

    private void OnPlayerLanded(Vector2Int grid)
    {
        BaseTile tile = mapGenerator.GetTile(grid.x, grid.y);
        if (tile != null)
            tile.OnPlayerEnter();

        if (State == GameState.Playing && CurrentMoveCount <= 0)
            Fail();
    }

    public void AddMoveCount(int delta)
    {
        stateModel.AddMoveCount(delta);
    }

    public void RestartStage()
    {
        if (mapGenerator == null)
        {
            Debug.LogWarning("[GameManager] Cannot restart stage because MapGenerator is missing.");
            return;
        }

        mapGenerator.GenerateMap();
    }

    public void SetStage(MapData mapData, int stageIndex)
    {
        if (mapGenerator == null)
        {
            Debug.LogWarning("[GameManager] Cannot set stage because MapGenerator is missing.");
            return;
        }

        if (progressService == null)
            ResolveProgressService();

        if (progressService != null)
            progressService.SetCurrentStage(stageIndex);

        mapGenerator.SetMapData(mapData, true);
    }

    public void NotifyStageCleared()
    {
        if (State != GameState.Playing) return;

        int stageIndex = GetCurrentStageIndex();
        StageClearProgressResult progressResult = new StageClearProgressResult(false, false, 0);

        if (progressService != null)
            progressResult = progressService.MarkStageCleared(stageIndex);

        stateModel.Clear();
        StageCleared?.Invoke(stageIndex);

        if (progressResult.GrantedSkipTicket)
            Debug.Log($"[GameManager] Stage cleared. stageIndex={stageIndex}, skipTickets={progressResult.SkipTicketCount}");
        else
            Debug.Log($"[GameManager] Stage cleared. stageIndex={stageIndex}");
    }

    private void Fail()
    {
        if (State != GameState.Playing) return;

        int stageIndex = GetCurrentStageIndex();
        int failureCount = 0;

        if (progressService != null)
            failureCount = progressService.RecordFailure(stageIndex);

        stateModel.Fail();
        StageFailed?.Invoke(stageIndex, failureCount);
        Debug.Log($"[GameManager] Stage failed. stageIndex={stageIndex}, failureCount={failureCount}");
    }

    private void ResolveProgressService()
    {
        if (progressService == null)
            progressService = GetComponent<StageProgressService>();

        if (progressService == null)
            progressService = FindFirstObjectByType<StageProgressService>();

        if (progressService == null)
            progressService = gameObject.AddComponent<StageProgressService>();
    }

    private int GetCurrentStageIndex()
    {
        if (progressService == null)
            ResolveProgressService();

        if (progressService == null)
            return 0;

        return progressService.CurrentStageIndex;
    }
}
