using System;
using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }
    public bool IsHintRouteVisible => mapGenerator != null && mapGenerator.HasActiveHintRoute;

    [SerializeField] private MapGenerator mapGenerator;
    [SerializeField] private GameStateModel stateModel;
    [SerializeField] private StageProgressService progressService;
    [SerializeField] private AudioService audioService;
    [SerializeField] private HapticService hapticService;

    private PlayerController player;
    private MoveResolver moveResolver;
    private bool isInputBlocked;

    public int CurrentMoveCount => stateModel.MoveCount;
    public MapGenerator MapGenerator => mapGenerator;
    public GameState State => stateModel.State;
    public GameStateModel StateModel => stateModel;
    public StageProgressService ProgressService => progressService;
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
        ResolveHapticService();
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
        if (isInputBlocked) return;
        if (State != GameState.Playing) return;
        if (player == null) return;
        if (player.IsMoving) return;
        if (direction == Vector2Int.zero) return;

        MoveResult result = moveResolver.Resolve(player.GridPosition, direction, CurrentMoveCount);
        if (!result.isAllowed)
        {
            HandleBlockedMove(result);
            return;
        }

        if (mapGenerator != null)
            mapGenerator.StopHintRoute();

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

    public void RestartStage()
    {
        if (mapGenerator == null)
        {
            Debug.LogWarning("[GameManager] Cannot restart stage because MapGenerator is missing.");
            return;
        }

        mapGenerator.StopHintRoute();
        isInputBlocked = false;
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

        mapGenerator.StopHintRoute();
        isInputBlocked = false;
        mapGenerator.SetMapData(mapData, true);
    }

    public bool TrySolveCurrentState(out PuzzleSolveResult solveResult)
    {
        solveResult = null;
        if (player == null || mapGenerator == null || State != GameState.Playing || player.IsMoving)
            return false;

        PuzzleSnapshot puzzleSnapshot;
        if (!mapGenerator.TryCreatePuzzleSnapshot(player.GridPosition, CurrentMoveCount, out puzzleSnapshot))
            return false;

        solveResult = PuzzleSolver.Solve(puzzleSnapshot);
        return !solveResult.HasStructureError;
    }

    public void PlayHintRoute(IReadOnlyList<PuzzleRouteStep> route)
    {
        if (mapGenerator != null)
            mapGenerator.PlayHintRoute(route);
    }

    public void StopHintRoute()
    {
        if (mapGenerator != null)
            mapGenerator.StopHintRoute();
    }

    public void SetInputBlocked(bool shouldBlockInput)
    {
        isInputBlocked = shouldBlockInput;
    }

    public void NotifyStageCleared()
    {
        if (State != GameState.Playing) return;

        int stageIndex = GetCurrentStageIndex();
        if (progressService != null)
            progressService.MarkStageCleared(stageIndex);

        stateModel.Clear();
        if (audioService != null)
            audioService.PlayClear();

        StageCleared?.Invoke(stageIndex);

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

        if (hapticService != null)
            hapticService.PlayBlocked();
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

    private void ResolveHapticService()
    {
        if (hapticService == null)
            hapticService = FindFirstObjectByType<HapticService>();

        if (hapticService == null)
            hapticService = gameObject.AddComponent<HapticService>();
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

        if (hapticService != null)
            hapticService.PlayBlocked();
    }

    private void HandleBlockedMove(MoveResult result)
    {
        if (result.moveCost > 0)
            stateModel.SpendMoveCount(result.moveCost);

        if (result.shakesPushedObstacle && result.pushedObstacle != null)
            result.pushedObstacle.PlayBlockedFeedback();

        NotifyMoveBlocked();

        if (State == GameState.Playing && CurrentMoveCount <= 0)
            Fail();
    }
}
