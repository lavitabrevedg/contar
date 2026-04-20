using UnityEngine;

/// <summary>
/// 이동 판정 결과. MoveResolver가 반환하며, 실제 상태 변경은 호출자(GameManager)가 수행한다.
/// </summary>
public struct MoveResult
{
    public bool isAllowed;
    public Vector2Int playerTarget;
    public int moveCost;
    public NumberObstacle pushedObstacle;
    public Vector2Int obstacleFrom;
    public Vector2Int obstacleTarget;
    public string blockReason;

    public static MoveResult Blocked(string reason)
    {
        MoveResult result = new MoveResult();
        result.isAllowed = false;
        result.blockReason = reason;
        return result;
    }

    public static MoveResult Allowed(Vector2Int playerTarget, int moveCost)
    {
        MoveResult result = new MoveResult();
        result.isAllowed = true;
        result.playerTarget = playerTarget;
        result.moveCost = moveCost;
        return result;
    }
}

/// <summary>
/// 이동 가능성만 판정. 플레이어 위치/이동횟수 등 상태를 직접 변경하지 않고
/// 결과(MoveResult)만 반환하여 GameManager가 해석/실행하도록 한다.
/// </summary>
public class MoveResolver
{
    private readonly MapGenerator _mapGenerator;

    public MoveResolver(MapGenerator mapGenerator)
    {
        _mapGenerator = mapGenerator;
    }

    public MoveResult Resolve(Vector2Int currentPos, Vector2Int direction, int currentMoveCount)
    {
        if (currentMoveCount <= 0)
            return MoveResult.Blocked("이동 횟수 부족");

        Vector2Int targetGrid = currentPos + direction;
        BaseTile targetTile = _mapGenerator.GetTile(targetGrid.x, targetGrid.y);

        if (targetTile == null)
            return MoveResult.Blocked("맵 바깥");

        if (targetTile is WallTile)
            return MoveResult.Blocked("벽");

        // 장애물 판정 — 뒤 칸이 Empty 타일이어야만 밀기 가능
        if (targetTile is NumberObstacle obstacle)
        {
            Vector2Int behindPos = targetGrid + direction;
            BaseTile behindTile = _mapGenerator.GetTile(behindPos.x, behindPos.y);

            if (behindTile == null)
                return MoveResult.Blocked("장애물을 맵 바깥으로 밀 수 없음");

            if (!(behindTile is EmptyTile))
                return MoveResult.Blocked("장애물은 Empty 타일 위로만 밀 수 있음");

            if (currentMoveCount < obstacle.value)
                return MoveResult.Blocked("이동 횟수 부족 (밀기 비용)");

            // 밀기 성공: 플레이어는 자리 유지, 장애물과 Empty 타일이 위치 교환
            MoveResult result = new MoveResult();
            result.isAllowed = true;
            result.playerTarget = currentPos;
            result.moveCost = obstacle.value;
            result.pushedObstacle = obstacle;
            result.obstacleFrom = targetGrid;
            result.obstacleTarget = behindPos;
            return result;
        }

        return MoveResult.Allowed(targetGrid, 1);
    }
}
