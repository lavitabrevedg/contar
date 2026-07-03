#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

public static class StageSolver
{
    private const string StagesFolder = "Assets/Data/Stages";
    private const int DestroyedObstacleIndex = 0x7F;
    private const int MaxPackedCellCount = DestroyedObstacleIndex;
    private const int MaxMoveTileCount = 31;
    private const int MaxObstacleCount = 9;

    [MenuItem("contar/Validate All Stages")]
    public static void ValidateAll()
    {
        string[] guids = AssetDatabase.FindAssets("t:MapData", new[] { StagesFolder });
        List<StageAssetInfo> stageAssets = new List<StageAssetInfo>();

        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            string stageName = Path.GetFileNameWithoutExtension(path);
            if (!stageName.StartsWith("Stage_", StringComparison.Ordinal))
                continue;

            int stageNumber = int.MaxValue;
            TryParseStageNumber(stageName, out stageNumber);

            StageAssetInfo stageAssetInfo = new StageAssetInfo
            {
                name = stageName,
                number = stageNumber,
                map = AssetDatabase.LoadAssetAtPath<MapData>(path),
            };
            stageAssets.Add(stageAssetInfo);
        }

        stageAssets.Sort(CompareStageAssetInfo);

        int okCount = 0;
        int failCount = 0;
        int errorCount = 0;
        List<string> stageLines = new List<string>();

        foreach (StageAssetInfo stageAssetInfo in stageAssets)
        {
            SolveResult solveResult;
            if (stageAssetInfo.map == null)
            {
                solveResult = new SolveResult
                {
                    hasStructureError = true,
                    errorMessage = "MapData load failed.",
                };
            }
            else
            {
                solveResult = Solve(stageAssetInfo.map);
            }

            if (solveResult.hasStructureError)
            {
                errorCount++;
                stageLines.Add($"{stageAssetInfo.name}: ERROR, {solveResult.errorMessage}");
            }
            else if (solveResult.isSolvable)
            {
                okCount++;
                stageLines.Add($"{stageAssetInfo.name}: OK, remaining={solveResult.maxRemaining}, states={solveResult.statesExplored}");
            }
            else
            {
                failCount++;
                stageLines.Add($"{stageAssetInfo.name}: FAIL, states={solveResult.statesExplored}");
            }
        }

        StringBuilder logBuilder = new StringBuilder();
        logBuilder.AppendLine("===== Stage Validation Result =====");
        logBuilder.AppendLine($"Total: {stageAssets.Count} | OK: {okCount} | FAIL: {failCount} | ERROR: {errorCount}");
        logBuilder.AppendLine();
        logBuilder.AppendLine("[All Stages]");

        foreach (string stageLine in stageLines)
            logBuilder.AppendLine(stageLine);

        Debug.Log(logBuilder.ToString());
    }

    public struct SolveResult
    {
        public bool isSolvable;
        public bool hasStructureError;
        public int maxRemaining;
        public int statesExplored;
        public string errorMessage;
    }

    private struct StageAssetInfo
    {
        public string name;
        public int number;
        public MapData map;
    }

    private struct StageMapInfo
    {
        public Vector2Int start;
        public TileType[,] types;
        public int[,] values;
        public Dictionary<int, ExitCondition> exitConditions;
        public Dictionary<int, int> moveBit;
        public List<int> obstacleStart;
        public List<int> obstacleValue;
    }

    private struct State : IEquatable<State>
    {
        public int posIndex;
        public int remaining;
        public int moveMask;
        public long obstaclePacked;
        public bool canResolveExit;

        public bool Equals(State otherState)
        {
            return posIndex == otherState.posIndex
                && remaining == otherState.remaining
                && moveMask == otherState.moveMask
                && obstaclePacked == otherState.obstaclePacked
                && canResolveExit == otherState.canResolveExit;
        }

        public override bool Equals(object obj)
        {
            return obj is State otherState && Equals(otherState);
        }

        public override int GetHashCode()
        {
            int hashCode = posIndex;
            hashCode = hashCode * 397 ^ remaining;
            hashCode = hashCode * 397 ^ moveMask;
            hashCode = hashCode * 397 ^ obstaclePacked.GetHashCode();
            hashCode = hashCode * 397 ^ canResolveExit.GetHashCode();
            return hashCode;
        }
    }

    public static SolveResult Solve(MapData map)
    {
        StageMapInfo mapInfo;
        string errorMessage;
        if (!TryBuildMapInfo(map, out mapInfo, out errorMessage))
        {
            return new SolveResult
            {
                hasStructureError = true,
                errorMessage = errorMessage,
            };
        }

        int width = map.width;
        int height = map.height;
        int obstacleCount = mapInfo.obstacleStart.Count;
        long initialObstaclePacked = 0;
        for (int obstacleIndex = 0; obstacleIndex < obstacleCount; obstacleIndex++)
        {
            initialObstaclePacked |= ((long)mapInfo.obstacleStart[obstacleIndex]) << (obstacleIndex * 7);
        }

        State initialState = new State
        {
            posIndex = mapInfo.start.y * width + mapInfo.start.x,
            remaining = map.startMoveCount,
            moveMask = 0,
            obstaclePacked = initialObstaclePacked,
            canResolveExit = true,
        };

        HashSet<State> visited = new HashSet<State>();
        Queue<State> queue = new Queue<State>();
        queue.Enqueue(initialState);
        visited.Add(initialState);

        int maxRemaining = -1;
        bool solved = false;
        int[] directionX = { 1, -1, 0, 0 };
        int[] directionY = { 0, 0, 1, -1 };
        int[] obstacleBuffer = new int[obstacleCount];

        while (queue.Count > 0)
        {
            State currentState = queue.Dequeue();
            ExitCondition exitCondition;
            if (currentState.canResolveExit
                && mapInfo.exitConditions.TryGetValue(currentState.posIndex, out exitCondition)
                && IsExitConditionMet(exitCondition, currentState.remaining))
            {
                solved = true;
                if (currentState.remaining > maxRemaining)
                    maxRemaining = currentState.remaining;

                continue;
            }

            if (currentState.remaining <= 0)
                continue;

            int currentX = currentState.posIndex % width;
            int currentY = currentState.posIndex / width;

            UnpackObstacles(currentState.obstaclePacked, obstacleCount, obstacleBuffer);

            for (int directionIndex = 0; directionIndex < 4; directionIndex++)
            {
                int nextX = currentX + directionX[directionIndex];
                int nextY = currentY + directionY[directionIndex];
                if (!IsInside(nextX, nextY, width, height))
                    continue;

                int nextIndex = nextY * width + nextX;
                if (mapInfo.types[nextX, nextY] == TileType.Wall)
                    continue;

                int obstacleIndex = FindObstacleAt(obstacleBuffer, obstacleCount, nextIndex);
                if (obstacleIndex >= 0)
                {
                    TryPushObstacle(
                        currentState,
                        obstacleIndex,
                        currentX,
                        currentY,
                        directionX[directionIndex],
                        directionY[directionIndex],
                        width,
                        height,
                        mapInfo,
                        obstacleBuffer,
                        visited,
                        queue
                    );
                }
                else
                {
                    TryMovePlayer(
                        currentState,
                        nextX,
                        nextY,
                        nextIndex,
                        mapInfo,
                        visited,
                        queue
                    );
                }
            }
        }

        return new SolveResult
        {
            isSolvable = solved,
            maxRemaining = maxRemaining,
            statesExplored = visited.Count,
            errorMessage = string.Empty,
        };
    }

    private static bool TryBuildMapInfo(MapData map, out StageMapInfo mapInfo, out string errorMessage)
    {
        mapInfo = new StageMapInfo
        {
            start = new Vector2Int(-1, -1),
            types = null,
            values = null,
            exitConditions = new Dictionary<int, ExitCondition>(),
            moveBit = new Dictionary<int, int>(),
            obstacleStart = new List<int>(),
            obstacleValue = new List<int>(),
        };
        errorMessage = string.Empty;

        if (map == null)
        {
            errorMessage = "MapData is null.";
            return false;
        }

        if (map.width <= 0 || map.height <= 0)
        {
            errorMessage = $"Invalid size. width={map.width}, height={map.height}.";
            return false;
        }

        int cellCount = map.width * map.height;
        if (cellCount > MaxPackedCellCount)
        {
            errorMessage = $"Grid has {cellCount} cells. Solver supports up to {MaxPackedCellCount}.";
            return false;
        }

        if (map.rows == null)
        {
            errorMessage = "rows is null.";
            return false;
        }

        if (map.rows.Length != map.height)
        {
            errorMessage = $"rows length mismatch. rows={map.rows.Length}, height={map.height}.";
            return false;
        }

        if (map.startMoveCount < 0)
        {
            errorMessage = $"startMoveCount must be 0 or higher. current={map.startMoveCount}.";
            return false;
        }

        mapInfo.types = new TileType[map.width, map.height];
        mapInfo.values = new int[map.width, map.height];

        int startCount = 0;
        int exitCount = 0;

        for (int y = 0; y < map.height; y++)
        {
            if (map.rows[y] == null)
            {
                errorMessage = $"row {y} is null.";
                return false;
            }

            if (map.rows[y].values == null)
            {
                errorMessage = $"row {y}.values is null.";
                return false;
            }

            if (map.rows[y].values.Length != map.width)
            {
                errorMessage = $"row {y} width mismatch. values={map.rows[y].values.Length}, width={map.width}.";
                return false;
            }

            for (int x = 0; x < map.width; x++)
            {
                SerializedTile tile = map.rows[y].values[x];
                if (!Enum.IsDefined(typeof(TileType), tile.type))
                {
                    errorMessage = $"Undefined TileType at ({x},{y}). value={(int)tile.type}.";
                    return false;
                }

                if (!Enum.IsDefined(typeof(ExitCondition), tile.exitCondition))
                {
                    errorMessage = $"Undefined ExitCondition at ({x},{y}). value={(int)tile.exitCondition}.";
                    return false;
                }

                int tileIndex = y * map.width + x;
                mapInfo.types[x, y] = tile.type;
                mapInfo.values[x, y] = tile.value;

                if (tile.type == TileType.Start)
                {
                    startCount++;
                    mapInfo.start = new Vector2Int(x, y);
                }
                else if (tile.type == TileType.Exit)
                {
                    exitCount++;
                    mapInfo.exitConditions[tileIndex] = tile.exitCondition;
                }
                else if (tile.type == TileType.Move)
                {
                    mapInfo.moveBit[tileIndex] = mapInfo.moveBit.Count;
                }
                else if (tile.type == TileType.NumberObstacle)
                {
                    mapInfo.obstacleStart.Add(tileIndex);
                    mapInfo.obstacleValue.Add(tile.value);
                }
            }
        }

        if (startCount != 1)
        {
            errorMessage = $"Start count must be exactly 1. current={startCount}.";
            return false;
        }

        if (exitCount < 1)
        {
            errorMessage = "Exit count must be 1 or higher.";
            return false;
        }

        if (mapInfo.moveBit.Count > MaxMoveTileCount)
        {
            errorMessage = $"MoveTile count {mapInfo.moveBit.Count} exceeds solver limit {MaxMoveTileCount}.";
            return false;
        }

        if (mapInfo.obstacleStart.Count > MaxObstacleCount)
        {
            errorMessage = $"NumberObstacle count {mapInfo.obstacleStart.Count} exceeds solver limit {MaxObstacleCount}.";
            return false;
        }

        return true;
    }

    private static void TryPushObstacle(
        State currentState,
        int obstacleIndex,
        int currentX,
        int currentY,
        int directionX,
        int directionY,
        int width,
        int height,
        StageMapInfo mapInfo,
        int[] obstacleBuffer,
        HashSet<State> visited,
        Queue<State> queue)
    {
        int nextX = currentX + directionX;
        int nextY = currentY + directionY;
        int behindX = nextX + directionX;
        int behindY = nextY + directionY;
        if (!IsInside(behindX, behindY, width, height))
            return;

        bool destroysObstacle = mapInfo.types[behindX, behindY] == TileType.Wall;
        if (mapInfo.types[behindX, behindY] != TileType.Empty && !destroysObstacle)
            return;

        int behindIndex = behindY * width + behindX;
        if (FindObstacleAt(obstacleBuffer, mapInfo.obstacleStart.Count, behindIndex) >= 0)
            return;

        int pushCost = mapInfo.obstacleValue[obstacleIndex];
        if (currentState.remaining < pushCost)
            return;

        long nextObstaclePacked = currentState.obstaclePacked;
        nextObstaclePacked &= ~(((long)DestroyedObstacleIndex) << (obstacleIndex * 7));

        int nextObstacleIndex = destroysObstacle ? DestroyedObstacleIndex : behindIndex;
        nextObstaclePacked |= ((long)nextObstacleIndex) << (obstacleIndex * 7);

        State nextState = new State
        {
            posIndex = currentState.posIndex,
            remaining = currentState.remaining - pushCost,
            moveMask = currentState.moveMask,
            obstaclePacked = nextObstaclePacked,
            canResolveExit = false,
        };

        if (visited.Add(nextState))
            queue.Enqueue(nextState);
    }

    private static void TryMovePlayer(
        State currentState,
        int nextX,
        int nextY,
        int nextIndex,
        StageMapInfo mapInfo,
        HashSet<State> visited,
        Queue<State> queue)
    {
        int nextRemaining = currentState.remaining - 1;
        int nextMoveMask = currentState.moveMask;

        int moveBitIndex;
        if (mapInfo.types[nextX, nextY] == TileType.Move && mapInfo.moveBit.TryGetValue(nextIndex, out moveBitIndex))
        {
            int moveFlag = 1 << moveBitIndex;
            if ((currentState.moveMask & moveFlag) == 0)
            {
                nextRemaining += mapInfo.values[nextX, nextY];
                nextMoveMask |= moveFlag;
            }
        }

        if (nextRemaining < 0)
            return;

        State nextState = new State
        {
            posIndex = nextIndex,
            remaining = nextRemaining,
            moveMask = nextMoveMask,
            obstaclePacked = currentState.obstaclePacked,
            canResolveExit = true,
        };

        if (visited.Add(nextState))
            queue.Enqueue(nextState);
    }

    private static void UnpackObstacles(long obstaclePacked, int obstacleCount, int[] obstacleBuffer)
    {
        for (int obstacleIndex = 0; obstacleIndex < obstacleCount; obstacleIndex++)
            obstacleBuffer[obstacleIndex] = (int)((obstaclePacked >> (obstacleIndex * 7)) & DestroyedObstacleIndex);
    }

    private static int FindObstacleAt(int[] obstacleBuffer, int obstacleCount, int tileIndex)
    {
        for (int obstacleIndex = 0; obstacleIndex < obstacleCount; obstacleIndex++)
        {
            if (obstacleBuffer[obstacleIndex] != DestroyedObstacleIndex && obstacleBuffer[obstacleIndex] == tileIndex)
                return obstacleIndex;
        }

        return -1;
    }

    private static bool IsInside(int x, int y, int width, int height)
    {
        return x >= 0 && x < width && y >= 0 && y < height;
    }

    private static bool IsExitConditionMet(ExitCondition exitCondition, int remaining)
    {
        switch (exitCondition)
        {
            case ExitCondition.Free:
                return true;
            case ExitCondition.OddOnly:
                return remaining % 2 != 0;
            case ExitCondition.EvenOnly:
                return remaining % 2 == 0;
            default:
                return false;
        }
    }

    private static bool TryParseStageNumber(string stageName, out int stageNumber)
    {
        stageNumber = int.MaxValue;
        string numberText = stageName.Substring("Stage_".Length);
        int parsedStageNumber;
        if (int.TryParse(numberText, out parsedStageNumber))
        {
            stageNumber = parsedStageNumber;
            return true;
        }

        return false;
    }

    private static int CompareStageAssetInfo(StageAssetInfo leftStage, StageAssetInfo rightStage)
    {
        int numberCompare = leftStage.number.CompareTo(rightStage.number);
        if (numberCompare != 0)
            return numberCompare;

        return string.CompareOrdinal(leftStage.name, rightStage.name);
    }
}
#endif
