#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Text;
using System.IO;

public static class StageSolver
{
    private const string StagesFolder = "Assets/Data/Stages";
    private const int DestroyedObstacleIndex = 0x7F;
    private const int LooseThreshold = 5; // 여유 이 값 이상이면 "너무 후함"

    [MenuItem("contar/Validate All Stages")]
    public static void ValidateAll()
    {
        string[] guids = AssetDatabase.FindAssets("t:MapData", new[] { StagesFolder });
        System.Array.Sort(guids, (a, b) =>
            string.CompareOrdinal(AssetDatabase.GUIDToAssetPath(a), AssetDatabase.GUIDToAssetPath(b)));

        int solvable = 0, unsolvable = 0;
        List<string> tightList = new List<string>();
        List<string> looseList = new List<string>();
        List<string> failList = new List<string>();

        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            string name = Path.GetFileNameWithoutExtension(path);
            if (!name.StartsWith("Stage_", System.StringComparison.Ordinal)) continue;

            MapData map = AssetDatabase.LoadAssetAtPath<MapData>(path);
            if (map == null) continue;

            SolveResult solveResult = Solve(map);

            if (solveResult.isSolvable)
            {
                solvable++;
                if (solveResult.maxRemaining == 0)
                    tightList.Add($"{name} (여유 0)");
                else if (solveResult.maxRemaining >= LooseThreshold)
                    looseList.Add($"{name} (여유 {solveResult.maxRemaining})");
            }
            else
            {
                unsolvable++;
                failList.Add($"{name} (탐색 {solveResult.statesExplored})");
            }
        }

        StringBuilder logBuilder = new StringBuilder();
        logBuilder.AppendLine("===== 스테이지 검증 결과 =====");
        logBuilder.AppendLine($"총 {solvable + unsolvable}개 | 성공 {solvable} | 실패 {unsolvable}");
        logBuilder.AppendLine();

        if (failList.Count > 0)
        {
            logBuilder.AppendLine($"해결 불가 ({failList.Count}):");
            foreach (string failItem in failList) logBuilder.AppendLine($"  - {failItem}");
            logBuilder.AppendLine();
        }
        if (tightList.Count > 0)
        {
            logBuilder.AppendLine($"타이트한 스테이지 - 여유 0 ({tightList.Count}):");
            foreach (string tightItem in tightList) logBuilder.AppendLine($"  - {tightItem}");
            logBuilder.AppendLine();
        }
        if (looseList.Count > 0)
        {
            logBuilder.AppendLine($"여유 {LooseThreshold}+ ({looseList.Count}):");
            foreach (string looseItem in looseList) logBuilder.AppendLine($"  - {looseItem}");
        }

        Debug.Log(logBuilder.ToString());
    }

    public struct SolveResult
    {
        public bool isSolvable;
        public int  maxRemaining;
        public int  statesExplored;
    }

    private struct State : System.IEquatable<State>
    {
        public int  posIndex;
        public int  remaining;
        public int  moveMask;
        public long obstaclePacked;

        public bool Equals(State o) =>
            posIndex == o.posIndex && remaining == o.remaining &&
            moveMask == o.moveMask && obstaclePacked == o.obstaclePacked;

        public override bool Equals(object obj) => obj is State s && Equals(s);

        public override int GetHashCode()
        {
            int h = posIndex;
            h = h * 397 ^ remaining;
            h = h * 397 ^ moveMask;
            h = h * 397 ^ obstaclePacked.GetHashCode();
            return h;
        }
    }

    public static SolveResult Solve(MapData map)
    {
        int width = map.width;
        int height = map.height;
        if (width <= 0 || height <= 0 || map.rows == null)
            return new SolveResult();

        Vector2Int start     = new Vector2Int(-1, -1);
        HashSet<int> exitSet = new HashSet<int>();
        ExitCondition exitCond = ExitCondition.Free;

        TileType[,] types  = new TileType[width, height];
        int[,]      values = new int[width, height];

        Dictionary<int, int> moveBit = new Dictionary<int, int>();
        List<int> obsStart = new List<int>();
        List<int> obsValue = new List<int>();

        for (int y = 0; y < height; y++)
        {
            if (map.rows[y] == null || map.rows[y].values == null) continue;
            for (int x = 0; x < width; x++)
            {
                SerializedTile t = map.rows[y].values[x];
                int index = y * width + x;
                types[x, y]  = t.type;
                values[x, y] = t.value;

                switch (t.type)
                {
                    case TileType.Start:
                        start = new Vector2Int(x, y);
                        break;
                    case TileType.Exit:
                        exitSet.Add(index);
                        exitCond = t.exitCondition;
                        break;
                    case TileType.Move:
                        moveBit[index] = moveBit.Count;
                        break;
                    case TileType.NumberObstacle:
                        obsStart.Add(index);
                        obsValue.Add(t.value);
                        break;
                }
            }
        }

        if (start.x < 0 || exitSet.Count == 0)
            return new SolveResult { isSolvable = false };

        int obstacleCount = obsStart.Count;
        long initialObsPacked = 0;
        for (int i = 0; i < obstacleCount; i++)
            initialObsPacked |= ((long)obsStart[i]) << (i * 7); // 7bit per obstacle (최대 9개까지 long에 수납)

        State initial = new State
        {
            posIndex       = start.y * width + start.x,
            remaining      = map.startMoveCount,
            moveMask       = 0,
            obstaclePacked = initialObsPacked,
        };

        HashSet<State> visited = new HashSet<State>();
        Queue<State>   queue   = new Queue<State>();
        queue.Enqueue(initial);
        visited.Add(initial);

        int  maxRemaining = -1;
        bool solved       = false;
        int[] dx = { 1, -1, 0, 0 };
        int[] dy = { 0, 0, 1, -1 };
        int[] obstacleBuffer = new int[obstacleCount];

        while (queue.Count > 0)
        {
            State cur = queue.Dequeue();

            // Exit 도달 판정
            if (exitSet.Contains(cur.posIndex))
            {
                bool condOK = exitCond == ExitCondition.Free
                           || (exitCond == ExitCondition.OddOnly  && cur.remaining % 2 == 1)
                           || (exitCond == ExitCondition.EvenOnly && cur.remaining % 2 == 0);
                if (condOK)
                {
                    solved = true;
                    if (cur.remaining > maxRemaining) maxRemaining = cur.remaining;
                    continue; // Exit 조건 만족 → 게임 종료
                }
                // 조건 불만족이면 걸어다니는 타일처럼 취급하여 계속 탐색
            }

            if (cur.remaining <= 0) continue;

            int currentX = cur.posIndex % width;
            int currentY = cur.posIndex / width;

            for (int i = 0; i < obstacleCount; i++)
                obstacleBuffer[i] = (int)((cur.obstaclePacked >> (i * 7)) & 0x7F);

            for (int d = 0; d < 4; d++)
            {
                int nextX = currentX + dx[d];
                int nextY = currentY + dy[d];
                if (nextX < 0 || nextX >= width || nextY < 0 || nextY >= height) continue;
                int nextIndex = nextY * width + nextX;

                if (types[nextX, nextY] == TileType.Wall) continue;

                int obstacleIndex = -1;
                for (int i = 0; i < obstacleCount; i++)
                    if (obstacleBuffer[i] != DestroyedObstacleIndex && obstacleBuffer[i] == nextIndex) { obstacleIndex = i; break; }

                if (obstacleIndex >= 0)
                {
                    // 밀기 시도
                    int behindX = nextX + dx[d];
                    int behindY = nextY + dy[d];
                    if (behindX < 0 || behindX >= width || behindY < 0 || behindY >= height) continue;
                    bool destroysObstacle = types[behindX, behindY] == TileType.Wall;
                    if (types[behindX, behindY] != TileType.Empty && !destroysObstacle) continue;

                    int behindIndex = behindY * width + behindX;
                    bool behindBlocked = false;
                    for (int i = 0; i < obstacleCount; i++)
                        if (obstacleBuffer[i] != DestroyedObstacleIndex && obstacleBuffer[i] == behindIndex) { behindBlocked = true; break; }
                    if (behindBlocked) continue;

                    int cost = obsValue[obstacleIndex];
                    if (cur.remaining < cost) continue;

                    long nextObstaclePacked = cur.obstaclePacked;
                    nextObstaclePacked &= ~(((long)0x7F) << (obstacleIndex * 7));
                    int nextObstacleIndex = destroysObstacle ? DestroyedObstacleIndex : behindIndex;
                    nextObstaclePacked |=  ((long)nextObstacleIndex)  << (obstacleIndex * 7);

                    State next = new State
                    {
                        posIndex       = cur.posIndex, // 플레이어는 제자리
                        remaining      = cur.remaining - cost,
                        moveMask       = cur.moveMask,
                        obstaclePacked = nextObstaclePacked,
                    };
                    if (visited.Add(next)) queue.Enqueue(next);
                }
                else
                {
                    int nextRemaining  = cur.remaining - 1;
                    int nextMoveMask = cur.moveMask;

                    if (types[nextX, nextY] == TileType.Move && moveBit.TryGetValue(nextIndex, out int moveBitIndex))
                    {
                        int flag = 1 << moveBitIndex;
                        if ((cur.moveMask & flag) == 0)
                        {
                            nextRemaining  += values[nextX, nextY];
                            nextMoveMask |= flag;
                        }
                    }

                    if (nextRemaining < 0) continue;

                    State next = new State
                    {
                        posIndex       = nextIndex,
                        remaining      = nextRemaining,
                        moveMask       = nextMoveMask,
                        obstaclePacked = cur.obstaclePacked,
                    };
                    if (visited.Add(next)) queue.Enqueue(next);
                }
            }
        }

        return new SolveResult
        {
            isSolvable     = solved,
            maxRemaining   = maxRemaining,
            statesExplored = visited.Count,
        };
    }
}
#endif
