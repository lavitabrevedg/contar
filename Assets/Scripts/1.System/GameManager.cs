using UnityEngine;

public enum GameState
{
    Playing,
    Cleared,
    Failed
}

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [SerializeField] private MapGenerator mapGenerator;

    private PlayerController _player;
    private int _currentMoveCount;
    private MoveResolver _moveResolver;

    public int CurrentMoveCount => _currentMoveCount;
    public MapGenerator MapGenerator => mapGenerator;
    public GameState State { get; private set; } = GameState.Playing;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        _moveResolver = new MoveResolver(mapGenerator);
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    public void RegisterPlayer(PlayerController player, Vector2Int startGrid)
    {
        _player = player;

        if (mapGenerator != null && mapGenerator.mapData != null)
            _currentMoveCount = mapGenerator.mapData.startMoveCount;

        Vector3 startWorldPos = mapGenerator.GridToWorld(startGrid.x, startGrid.y);
        _player.Init(startGrid, startWorldPos);
    }

    public void OnSwipe(Vector2Int direction)
    {
        if (State != GameState.Playing) return;
        if (_player == null) return;
        if (_player.IsMoving) return;
        if (direction == Vector2Int.zero) return;

        MoveResult result = _moveResolver.Resolve(_player.GridPosition, direction, _currentMoveCount);
        if (!result.isAllowed) return;

        _currentMoveCount -= result.moveCost;

        if (result.pushedObstacle != null)
        {
            // 장애물 밀기 — 플레이어는 자리 유지, 장애물과 Empty 타일이 위치 교환
            mapGenerator.SwapTiles(result.obstacleFrom, result.obstacleTarget);

            // 밀기로 이동횟수 0 되면 실패 판정
            if (State == GameState.Playing && _currentMoveCount <= 0)
                Fail();
            return;
        }

        // 일반 이동
        Vector3 targetWorldPos = mapGenerator.GridToWorld(result.playerTarget.x, result.playerTarget.y);
        Vector2Int landedGrid = result.playerTarget;
        _player.AnimateTo(result.playerTarget, targetWorldPos, () => OnPlayerLanded(landedGrid));
    }

    private void OnPlayerLanded(Vector2Int grid)
    {
        BaseTile tile = mapGenerator.GetTile(grid.x, grid.y);
        if (tile != null)
            tile.OnPlayerEnter();

        // 이동 후 클리어/실패 판정
        if (State == GameState.Playing && _currentMoveCount <= 0)
        {
            // 출구 위에서 끝났으면 ExitTile.OnPlayerEnter에서 이미 Cleared로 바뀜 → 여기선 실패만
            // 실제 실패 판정은 "출구에 도달 못하고 이동횟수 소진" → 추후 고도화
            // 현재는 단순히 0이 되고 Playing이면 실패로 본다
            Fail();
        }
    }

    /// <summary>
    /// MoveTile 등 타일 효과에서 이동 횟수를 증감시킬 때 사용.
    /// </summary>
    public void AddMoveCount(int delta)
    {
        _currentMoveCount += delta;
    }

    /// <summary>
    /// ExitTile에서 클리어 조건을 만족했을 때 호출.
    /// </summary>
    public void NotifyStageCleared()
    {
        if (State != GameState.Playing) return;
        State = GameState.Cleared;
        Debug.Log("[GameManager] 스테이지 클리어!");
    }

    private void Fail()
    {
        State = GameState.Failed;
        Debug.Log("[GameManager] 스테이지 실패 (이동 횟수 소진)");
    }
}
