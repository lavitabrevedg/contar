using System;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [SerializeField] private MapGenerator mapGenerator;
    [SerializeField] private GameStateModel stateModel;
    [SerializeField] private StageProgressService progressService;
    [SerializeField] private AudioService audioService;

    private PlayerController player;
    private MoveResolver moveResolver;

    public int CurrentMoveCount => stateModel.MoveCount;
    public MapGenerator MapGenerator => mapGenerator;
    public GameState State => stateModel.State;
    public GameStateModel StateModel => stateModel;
    public StageProgressService ProgressService => progressService;
    public StageClearProgressResult LastStageClearProgressResult { get; private set; }
    public int LastFailureCount { get; private set; }

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
        ResolveAudioService();
        moveResolver = new MoveResolver(mapGenerator);
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    public void RegisterPlayer(PlayerController player, Vector2Int startGrid)
    {
        this.player = player;

        int startMoveCount = 0;
        if (mapGenerator != null && mapGenerator.mapData != null)
            startMoveCount = mapGenerator.mapData.startMoveCount;

        stateModel.StartStage(startMoveCount);

        Vector3 startWorldPos = mapGenerator.GridToWorld(startGrid.x, startGrid.y);
        this.player.Init(startGrid, startWorldPos);
    }

    public void OnSwipe(Vector2Int direction)
    {
        if (State != GameState.Playing) return;
        if (player == null) return;
        if (player.IsMoving) return;
        if (direction == Vector2Int.zero) return;

        MoveResult result = moveResolver.Resolve(player.GridPosition, direction, CurrentMoveCount);
        if (!result.isAllowed)
        {
            NotifyMoveBlocked();
            return;
        }

        stateModel.SpendMoveCount(result.moveCost);

        if (result.pushedObstacle != null)
        {
            if (audioService != null)
                audioService.PlayPush();

            if (result.destroysPushedObstacle)
                mapGenerator.ReplaceTileWithEmpty(result.obstacleFrom);
            else
                mapGenerator.SwapTiles(result.obstacleFrom, result.obstacleTarget);

            if (State == GameState.Playing && CurrentMoveCount <= 0)
                Fail();

            return;
        }

        Vector3 targetWorldPos = mapGenerator.GridToWorld(result.playerTarget.x, result.playerTarget.y);
        Vector2Int landedGrid = result.playerTarget;
        if (audioService != null)
            audioService.PlayMove();

        player.AnimateTo(result.playerTarget, targetWorldPos, () => OnPlayerLanded(landedGrid));
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

    public bool ContinueWithBonusMoves(int bonusMoveCount)
    {
        if (State != GameState.Failed)
            return false;

        stateModel.ContinueWithBonusMoves(bonusMoveCount);
        Debug.Log($"[GameManager] Continued with bonus moves. bonusMoveCount={bonusMoveCount}");
        return true;
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

        LastStageClearProgressResult = progressResult;
        stateModel.Clear();
        if (audioService != null)
            audioService.PlayClear();

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

        LastFailureCount = 0;
        stateModel.Fail();
        if (audioService != null)
            audioService.PlayFail();

        StageFailed?.Invoke(stageIndex, LastFailureCount);
        Debug.Log($"[GameManager] Stage failed. stageIndex={stageIndex}");
    }

    public void NotifyExitBlocked(ExitCondition condition)
    {
        if (audioService != null)
            audioService.PlayBlocked();
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

    private void ResolveAudioService()
    {
        if (audioService == null)
            audioService = FindFirstObjectByType<AudioService>();
    }

    private int GetCurrentStageIndex()
    {
        if (progressService == null)
            ResolveProgressService();

        if (progressService == null)
            return 0;

        return progressService.CurrentStageIndex;
    }

    private void NotifyMoveBlocked()
    {
        if (audioService != null)
            audioService.PlayBlocked();
    }
}
