using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [SerializeField] private MapGenerator mapGenerator;

    private PlayerController _player;
    private int _currentMoveCount;

    public int CurrentMoveCount => _currentMoveCount;
    public MapGenerator MapGenerator => mapGenerator;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
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
        if (_player == null) return;
        if (_player.IsMoving) return;
        if (direction == Vector2Int.zero) return;

        Vector2Int targetGrid = _player.GridPosition + direction;

        BaseTile targetTile = mapGenerator.GetTile(targetGrid.x, targetGrid.y);
        if (targetTile == null) return;

        // TODO: Wall / NumberObstacle 판정은 다음 작업에서 추가

        Vector3 targetWorldPos = mapGenerator.GridToWorld(targetGrid.x, targetGrid.y);
        _player.AnimateTo(targetGrid, targetWorldPos);
        _currentMoveCount--;
    }
}
