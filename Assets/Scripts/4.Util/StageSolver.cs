using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Text;
using System.IO;

public static class StageSolver
{
    private const string StagesFolder = "Assets/Scripts/2.Data/Stages";
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
            MapData map = AssetDatabase.LoadAssetAtPath<MapData>(path);
            if (map == null) continue;

            string name = Path.GetFileNameWithoutExtension(path);
            SolveResult r = Solve(map);

            if (r.isSolvable)
            {
                solvable++;
                if (r.maxRemaining == 0)
                    tightList.Add($"{name} (여유 0)");
                else if (r.maxRemaining >= LooseThreshold)
                    looseList.Add($"{name} (여유 {r.maxRemaining})");
            }
            else
            {
                unsolvable++;
                failList.Add($"{name} (탐색 {r.statesExplored})");
            }
        }

        StringBuilder sb = new StringBuilder();
        sb.AppendLine("===== 스테이지 검증 결과 =====");
        sb.AppendLine($"총 {solvable + unsolvable}개 | ✅ {solvable} | ❌ {unsolvable}");
        sb.AppendLine();

        if (failList.Count > 0)
        {
            sb.AppendLine($"❌ 해결 불가 ({failList.Count}):");
            foreach (string s in failList) sb.AppendLine($"  - {s}");
            sb.AppendLine();
        }
        if (tightList.Count > 0)
        {
            sb.AppendLine($"⚠️ 타이트 — 여유 0 ({tightList.Count}):");
            foreach (string s in tightList) sb.AppendLine($"  - {s}");
            sb.AppendLine();
        }
        if (looseList.Count > 0)
        {
            sb.AppendLine($"💤 여유 {LooseThreshold}+ ({looseList.Count}):");
            foreach (string s in looseList) sb.AppendLine($"  - {s}");
        }

        Debug.Log(sb.ToString());
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
        int W = map.width, H = map.height;
        if (W <= 0 || H <= 0 || map.rows == null)
            return new SolveResult();

        Vector2Int start     = new Vector2Int(-1, -1);
        HashSet<int> exitSet = new HashSet<int>();
        ExitCondition exitCond = ExitCondition.Free;

        TileType[,] types  = new TileType[W, H];
        int[,]      values = new int[W, H];

        Dictionary<int, int> moveBit = new Dictionary<int, int>();
        List<int> obsStart = new List<int>();
        List<int> obsValue = new List<int>();

        for (int y = 0; y < H; y++)
        {
            if (map.rows[y] == null || map.rows[y].values == null) continue;
            for (int x = 0; x < W; x++)
            {
                SerializedTile t = map.rows[y].values[x];
                int idx = y * W + x;
                types[x, y]  = t.type;
                values[x, y] = t.value;

                switch (t.type)
                {
                    case TileType.Start:
                        start = new Vector2Int(x, y);
                        break;
                    case TileType.Exit:
                        exitSet.Add(idx);
                        exitCond = t.exitCondition;
                        break;
                    case TileType.Move:
                        moveBit[idx] = moveBit.Count;
                        break;
                    case TileType.NumberObstacle:
                        obsStart.Add(idx);
                        obsValue.Add(t.value);
                        break;
                }
            }
        }

        if (start.x < 0 || exitSet.Count == 0)
            return new SolveResult { isSolvable = false };

        int nObs = obsStart.Count;
        long initialObsPacked = 0;
        for (int i = 0; i < nObs; i++)
            initialObsPacked |= ((long)obsStart[i]) << (i * 7); // 7bit per obstacle (최대 9개까지 long에 수납)

        State initial = new State
        {
            posIndex       = start.y * W + start.x,
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
        int[] obsBuf = new int[nObs];

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

            int curX = cur.posIndex % W;
            int curY = cur.posIndex / W;

            for (int i = 0; i < nObs; i++)
                obsBuf[i] = (int)((cur.obstaclePacked >> (i * 7)) & 0x7F);

            for (int d = 0; d < 4; d++)
            {
                int nx = curX + dx[d];
                int ny = curY + dy[d];
                if (nx < 0 || nx >= W || ny < 0 || ny >= H) continue;
                int nidx = ny * W + nx;

                if (types[nx, ny] == TileType.Wall) continue;

                int obsAt = -1;
                for (int i = 0; i < nObs; i++)
                    if (obsBuf[i] == nidx) { obsAt = i; break; }

                if (obsAt >= 0)
                {
                    // 밀기 시도
                    int bx = nx + dx[d];
                    int by = ny + dy[d];
                    if (bx < 0 || bx >= W || by < 0 || by >= H) continue;
                    if (types[bx, by] != TileType.Empty) continue;

                    int bidx = by * W + bx;
                    bool behindBlocked = false;
                    for (int i = 0; i < nObs; i++)
                        if (obsBuf[i] == bidx) { behindBlocked = true; break; }
                    if (behindBlocked) continue;

                    int cost = obsValue[obsAt];
                    if (cur.remaining < cost) continue;

                    long newPacked = cur.obstaclePacked;
                    newPacked &= ~(((long)0x7F) << (obsAt * 7));
                    newPacked |=  ((long)bidx)  << (obsAt * 7);

                    State next = new State
                    {
                        posIndex       = cur.posIndex, // 플레이어는 제자리
                        remaining      = cur.remaining - cost,
                        moveMask       = cur.moveMask,
                        obstaclePacked = newPacked,
                    };
                    if (visited.Add(next)) queue.Enqueue(next);
                }
                else
                {
                    int newRem  = cur.remaining - 1;
                    int newMask = cur.moveMask;

                    if (types[nx, ny] == TileType.Move && moveBit.TryGetValue(nidx, out int bit))
                    {
                        int flag = 1 << bit;
                        if ((cur.moveMask & flag) == 0)
                        {
                            newRem  += values[nx, ny];
                            newMask |= flag;
                        }
                    }

                    if (newRem < 0) continue;

                    State next = new State
                    {
                        posIndex       = nidx,
                        remaining      = newRem,
                        moveMask       = newMask,
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
