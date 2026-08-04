using System;
using System.Collections.Generic;
using UnityEngine;

public readonly struct PuzzleObstacleState
{
    public PuzzleObstacleState(Vector2Int position, int moveCost)
    {
        Position = position;
        MoveCost = moveCost;
    }

    public Vector2Int Position { get; }
    public int MoveCost { get; }
}

public readonly struct PuzzleRouteStep
{
    public PuzzleRouteStep(Vector2Int direction, Vector2Int highlightPosition, bool pushesObstacle)
    {
        Direction = direction;
        HighlightPosition = highlightPosition;
        PushesObstacle = pushesObstacle;
    }

    public Vector2Int Direction { get; }
    public Vector2Int HighlightPosition { get; }
    public bool PushesObstacle { get; }
}

public sealed class PuzzleSnapshot
{
    internal int width;
    internal int height;
    internal Vector2Int playerPosition;
    internal int remainingMoves;
    internal TileType[,] terrainTypes;
    internal int[,] tileValues;
    internal Dictionary<int, ExitCondition> exitConditions;
    internal Dictionary<int, int> moveBits;
    internal int consumedMoveMask;
    internal List<int> obstaclePositions;
    internal List<int> obstacleMoveCosts;
}

public sealed class PuzzleSolveResult
{
    public bool IsSolvable { get; internal set; }
    public bool HasStructureError { get; internal set; }
    public int RemainingMoves { get; internal set; }
    public int StatesExplored { get; internal set; }
    public string ErrorMessage { get; internal set; }
    public IReadOnlyList<PuzzleRouteStep> Route { get; internal set; }
}

public static class PuzzleSolver
{
    private const int DestroyedObstacleIndex = 0x7F;
    private const int MaxPackedCellCount = DestroyedObstacleIndex;
    private const int MaxMoveTileCount = 31;
    private const int MaxObstacleCount = 9;

    private static readonly Vector2Int[] Directions =
    {
        Vector2Int.right,
        Vector2Int.left,
        Vector2Int.up,
        Vector2Int.down
    };

    private struct SolverState : IEquatable<SolverState>
    {
        public int playerIndex;
        public int remainingMoves;
        public int consumedMoveMask;
        public long obstaclePositions;
        public bool canResolveExit;

        public bool Equals(SolverState otherState)
        {
            return playerIndex == otherState.playerIndex
                && remainingMoves == otherState.remainingMoves
                && consumedMoveMask == otherState.consumedMoveMask
                && obstaclePositions == otherState.obstaclePositions
                && canResolveExit == otherState.canResolveExit;
        }

        public override bool Equals(object otherObject)
        {
            return otherObject is SolverState otherState && Equals(otherState);
        }

        public override int GetHashCode()
        {
            int hashCode = playerIndex;
            hashCode = hashCode * 397 ^ remainingMoves;
            hashCode = hashCode * 397 ^ consumedMoveMask;
            hashCode = hashCode * 397 ^ obstaclePositions.GetHashCode();
            hashCode = hashCode * 397 ^ canResolveExit.GetHashCode();
            return hashCode;
        }
    }

    private readonly struct ParentState
    {
        public ParentState(SolverState previousState, PuzzleRouteStep routeStep)
        {
            PreviousState = previousState;
            RouteStep = routeStep;
        }

        public SolverState PreviousState { get; }
        public PuzzleRouteStep RouteStep { get; }
    }

    public static PuzzleSolveResult SolveInitial(MapData mapData)
    {
        PuzzleSnapshot initialSnapshot;
        string errorMessage;
        bool created = TryCreateSnapshot(
            mapData,
            FindStartPosition(mapData),
            mapData == null ? 0 : mapData.startMoveCount,
            null,
            null,
            out initialSnapshot,
            out errorMessage);

        if (!created)
            return CreateStructureError(errorMessage);

        return Solve(initialSnapshot);
    }

    public static bool TryCreateSnapshot(
        MapData mapData,
        Vector2Int playerPosition,
        int remainingMoves,
        IReadOnlyList<Vector2Int> consumedMoveTiles,
        IReadOnlyList<PuzzleObstacleState> currentObstacles,
        out PuzzleSnapshot puzzleSnapshot,
        out string errorMessage)
    {
        puzzleSnapshot = null;
        errorMessage = string.Empty;

        if (mapData == null)
        {
            errorMessage = "MapData is null.";
            return false;
        }

        if (mapData.width <= 0 || mapData.height <= 0)
        {
            errorMessage = $"Invalid size. width={mapData.width}, height={mapData.height}.";
            return false;
        }

        int cellCount = mapData.width * mapData.height;
        if (cellCount > MaxPackedCellCount)
        {
            errorMessage = $"Grid has {cellCount} cells. Solver supports up to {MaxPackedCellCount}.";
            return false;
        }

        if (!IsInside(playerPosition.x, playerPosition.y, mapData.width, mapData.height))
        {
            errorMessage = $"Player position is outside the map. position={playerPosition}.";
            return false;
        }

        if (remainingMoves < 0)
        {
            errorMessage = $"Remaining moves must be 0 or higher. current={remainingMoves}.";
            return false;
        }

        if (mapData.rows == null || mapData.rows.Length != mapData.height)
        {
            errorMessage = "Map rows do not match the map height.";
            return false;
        }

        PuzzleSnapshot createdSnapshot = new PuzzleSnapshot
        {
            width = mapData.width,
            height = mapData.height,
            playerPosition = playerPosition,
            remainingMoves = remainingMoves,
            terrainTypes = new TileType[mapData.width, mapData.height],
            tileValues = new int[mapData.width, mapData.height],
            exitConditions = new Dictionary<int, ExitCondition>(),
            moveBits = new Dictionary<int, int>(),
            consumedMoveMask = 0,
            obstaclePositions = new List<int>(),
            obstacleMoveCosts = new List<int>()
        };

        List<PuzzleObstacleState> initialObstacles = new List<PuzzleObstacleState>();
        int startCount = 0;

        for (int y = 0; y < mapData.height; y++)
        {
            Wrapper<SerializedTile> row = mapData.rows[y];
            if (row == null || row.values == null || row.values.Length != mapData.width)
            {
                errorMessage = $"Row {y} does not match the map width.";
                return false;
            }

            for (int x = 0; x < mapData.width; x++)
            {
                SerializedTile tile = row.values[x];
                if (!Enum.IsDefined(typeof(TileType), tile.type)
                    || !Enum.IsDefined(typeof(ExitCondition), tile.exitCondition))
                {
                    errorMessage = $"Undefined tile data at ({x},{y}).";
                    return false;
                }

                int tileIndex = GetIndex(x, y, mapData.width);
                createdSnapshot.terrainTypes[x, y] = tile.type == TileType.NumberObstacle
                    ? TileType.Empty
                    : tile.type;
                createdSnapshot.tileValues[x, y] = tile.value;

                if (tile.type == TileType.Start)
                    startCount++;
                else if (tile.type == TileType.Exit)
                    createdSnapshot.exitConditions[tileIndex] = tile.exitCondition;
                else if (tile.type == TileType.Move)
                    createdSnapshot.moveBits[tileIndex] = createdSnapshot.moveBits.Count;
                else if (tile.type == TileType.NumberObstacle)
                    initialObstacles.Add(new PuzzleObstacleState(new Vector2Int(x, y), tile.value));
            }
        }

        if (startCount != 1)
        {
            errorMessage = $"Start count must be exactly 1. current={startCount}.";
            return false;
        }

        if (createdSnapshot.exitConditions.Count < 1)
        {
            errorMessage = "Exit count must be 1 or higher.";
            return false;
        }

        if (createdSnapshot.moveBits.Count > MaxMoveTileCount)
        {
            errorMessage = $"MoveTile count exceeds solver limit {MaxMoveTileCount}.";
            return false;
        }

        IReadOnlyList<PuzzleObstacleState> obstacles = currentObstacles ?? initialObstacles;
        if (obstacles.Count > MaxObstacleCount)
        {
            errorMessage = $"NumberObstacle count exceeds solver limit {MaxObstacleCount}.";
            return false;
        }

        HashSet<int> occupiedObstacleCells = new HashSet<int>();
        for (int obstacleIndex = 0; obstacleIndex < obstacles.Count; obstacleIndex++)
        {
            PuzzleObstacleState obstacle = obstacles[obstacleIndex];
            if (!IsInside(obstacle.Position.x, obstacle.Position.y, mapData.width, mapData.height))
            {
                errorMessage = $"Obstacle position is outside the map. position={obstacle.Position}.";
                return false;
            }

            if (obstacle.MoveCost <= 0)
            {
                errorMessage = $"Obstacle move cost must be positive. position={obstacle.Position}, cost={obstacle.MoveCost}.";
                return false;
            }

            int obstacleCell = GetIndex(obstacle.Position.x, obstacle.Position.y, mapData.width);
            if (!occupiedObstacleCells.Add(obstacleCell))
            {
                errorMessage = $"Multiple obstacles occupy {obstacle.Position}.";
                return false;
            }

            createdSnapshot.obstaclePositions.Add(obstacleCell);
            createdSnapshot.obstacleMoveCosts.Add(obstacle.MoveCost);
        }

        if (consumedMoveTiles != null)
        {
            for (int consumedIndex = 0; consumedIndex < consumedMoveTiles.Count; consumedIndex++)
            {
                Vector2Int consumedPosition = consumedMoveTiles[consumedIndex];
                int consumedCell = GetIndex(consumedPosition.x, consumedPosition.y, mapData.width);
                int moveBitIndex;
                if (createdSnapshot.moveBits.TryGetValue(consumedCell, out moveBitIndex))
                    createdSnapshot.consumedMoveMask |= 1 << moveBitIndex;
            }
        }

        puzzleSnapshot = createdSnapshot;
        return true;
    }

    public static PuzzleSolveResult Solve(PuzzleSnapshot puzzleSnapshot)
    {
        if (puzzleSnapshot == null)
            return CreateStructureError("Puzzle snapshot is null.");

        int obstacleCount = puzzleSnapshot.obstaclePositions.Count;
        long initialObstaclePositions = 0;
        for (int obstacleIndex = 0; obstacleIndex < obstacleCount; obstacleIndex++)
            initialObstaclePositions |= ((long)puzzleSnapshot.obstaclePositions[obstacleIndex]) << (obstacleIndex * 7);

        SolverState initialState = new SolverState
        {
            playerIndex = GetIndex(
                puzzleSnapshot.playerPosition.x,
                puzzleSnapshot.playerPosition.y,
                puzzleSnapshot.width),
            remainingMoves = puzzleSnapshot.remainingMoves,
            consumedMoveMask = puzzleSnapshot.consumedMoveMask,
            obstaclePositions = initialObstaclePositions,
            canResolveExit = true
        };

        HashSet<SolverState> visitedStates = new HashSet<SolverState>();
        Dictionary<SolverState, ParentState> parentStates = new Dictionary<SolverState, ParentState>();
        Queue<SolverState> pendingStates = new Queue<SolverState>();
        int[] obstacleBuffer = new int[obstacleCount];

        visitedStates.Add(initialState);
        pendingStates.Enqueue(initialState);

        while (pendingStates.Count > 0)
        {
            SolverState currentState = pendingStates.Dequeue();
            ExitCondition exitCondition;
            if (currentState.canResolveExit
                && puzzleSnapshot.exitConditions.TryGetValue(currentState.playerIndex, out exitCondition)
                && IsExitConditionMet(exitCondition, currentState.remainingMoves))
            {
                return new PuzzleSolveResult
                {
                    IsSolvable = true,
                    RemainingMoves = currentState.remainingMoves,
                    StatesExplored = visitedStates.Count,
                    ErrorMessage = string.Empty,
                    Route = RestoreRoute(initialState, currentState, parentStates)
                };
            }

            if (currentState.remainingMoves <= 0)
                continue;

            int currentX = currentState.playerIndex % puzzleSnapshot.width;
            int currentY = currentState.playerIndex / puzzleSnapshot.width;
            UnpackObstacles(currentState.obstaclePositions, obstacleCount, obstacleBuffer);

            for (int directionIndex = 0; directionIndex < Directions.Length; directionIndex++)
            {
                Vector2Int direction = Directions[directionIndex];
                int nextX = currentX + direction.x;
                int nextY = currentY + direction.y;
                if (!IsInside(nextX, nextY, puzzleSnapshot.width, puzzleSnapshot.height))
                    continue;

                int nextIndex = GetIndex(nextX, nextY, puzzleSnapshot.width);
                if (puzzleSnapshot.terrainTypes[nextX, nextY] == TileType.Wall)
                    continue;

                int obstacleIndex = FindObstacleAt(obstacleBuffer, obstacleCount, nextIndex);
                if (obstacleIndex >= 0)
                {
                    TryEnqueuePush(
                        currentState,
                        obstacleIndex,
                        direction,
                        puzzleSnapshot,
                        obstacleBuffer,
                        visitedStates,
                        parentStates,
                        pendingStates);
                    continue;
                }

                TryEnqueueMove(
                    currentState,
                    direction,
                    nextX,
                    nextY,
                    nextIndex,
                    puzzleSnapshot,
                    visitedStates,
                    parentStates,
                    pendingStates);
            }
        }

        return new PuzzleSolveResult
        {
            IsSolvable = false,
            RemainingMoves = -1,
            StatesExplored = visitedStates.Count,
            ErrorMessage = string.Empty,
            Route = Array.Empty<PuzzleRouteStep>()
        };
    }

    private static void TryEnqueuePush(
        SolverState currentState,
        int obstacleIndex,
        Vector2Int direction,
        PuzzleSnapshot puzzleSnapshot,
        int[] obstacleBuffer,
        HashSet<SolverState> visitedStates,
        Dictionary<SolverState, ParentState> parentStates,
        Queue<SolverState> pendingStates)
    {
        int pushCost = puzzleSnapshot.obstacleMoveCosts[obstacleIndex];
        if (currentState.remainingMoves < pushCost)
            return;

        int currentX = currentState.playerIndex % puzzleSnapshot.width;
        int currentY = currentState.playerIndex / puzzleSnapshot.width;
        Vector2Int obstaclePosition = new Vector2Int(currentX + direction.x, currentY + direction.y);
        Vector2Int behindPosition = obstaclePosition + direction;
        bool behindIsOutside = !IsInside(
            behindPosition.x,
            behindPosition.y,
            puzzleSnapshot.width,
            puzzleSnapshot.height);

        long nextObstaclePositions = currentState.obstaclePositions;
        if (!behindIsOutside)
        {
            TileType behindTerrain = puzzleSnapshot.terrainTypes[behindPosition.x, behindPosition.y];
            bool destroysObstacle = behindTerrain == TileType.Wall;
            if (behindTerrain != TileType.Empty && !destroysObstacle)
                return;

            int behindIndex = GetIndex(behindPosition.x, behindPosition.y, puzzleSnapshot.width);
            if (FindObstacleAt(obstacleBuffer, puzzleSnapshot.obstaclePositions.Count, behindIndex) >= 0)
                return;

            nextObstaclePositions &= ~(((long)DestroyedObstacleIndex) << (obstacleIndex * 7));
            int nextObstacleIndex = destroysObstacle ? DestroyedObstacleIndex : behindIndex;
            nextObstaclePositions |= ((long)nextObstacleIndex) << (obstacleIndex * 7);
        }

        SolverState nextState = new SolverState
        {
            playerIndex = currentState.playerIndex,
            remainingMoves = currentState.remainingMoves - pushCost,
            consumedMoveMask = currentState.consumedMoveMask,
            obstaclePositions = nextObstaclePositions,
            canResolveExit = false
        };

        PuzzleRouteStep routeStep = new PuzzleRouteStep(direction, obstaclePosition, true);
        EnqueueState(currentState, nextState, routeStep, visitedStates, parentStates, pendingStates);
    }

    private static void TryEnqueueMove(
        SolverState currentState,
        Vector2Int direction,
        int nextX,
        int nextY,
        int nextIndex,
        PuzzleSnapshot puzzleSnapshot,
        HashSet<SolverState> visitedStates,
        Dictionary<SolverState, ParentState> parentStates,
        Queue<SolverState> pendingStates)
    {
        int nextRemainingMoves = currentState.remainingMoves - 1;
        int nextConsumedMoveMask = currentState.consumedMoveMask;
        int moveBitIndex;
        if (puzzleSnapshot.terrainTypes[nextX, nextY] == TileType.Move
            && puzzleSnapshot.moveBits.TryGetValue(nextIndex, out moveBitIndex))
        {
            int moveFlag = 1 << moveBitIndex;
            if ((currentState.consumedMoveMask & moveFlag) == 0)
            {
                nextRemainingMoves += puzzleSnapshot.tileValues[nextX, nextY];
                nextConsumedMoveMask |= moveFlag;
            }
        }

        if (nextRemainingMoves < 0)
            return;

        SolverState nextState = new SolverState
        {
            playerIndex = nextIndex,
            remainingMoves = nextRemainingMoves,
            consumedMoveMask = nextConsumedMoveMask,
            obstaclePositions = currentState.obstaclePositions,
            canResolveExit = true
        };

        PuzzleRouteStep routeStep = new PuzzleRouteStep(direction, new Vector2Int(nextX, nextY), false);
        EnqueueState(currentState, nextState, routeStep, visitedStates, parentStates, pendingStates);
    }

    private static void EnqueueState(
        SolverState currentState,
        SolverState nextState,
        PuzzleRouteStep routeStep,
        HashSet<SolverState> visitedStates,
        Dictionary<SolverState, ParentState> parentStates,
        Queue<SolverState> pendingStates)
    {
        if (!visitedStates.Add(nextState))
            return;

        parentStates[nextState] = new ParentState(currentState, routeStep);
        pendingStates.Enqueue(nextState);
    }

    private static IReadOnlyList<PuzzleRouteStep> RestoreRoute(
        SolverState initialState,
        SolverState solvedState,
        Dictionary<SolverState, ParentState> parentStates)
    {
        List<PuzzleRouteStep> reversedRoute = new List<PuzzleRouteStep>();
        SolverState currentState = solvedState;

        while (!currentState.Equals(initialState))
        {
            ParentState parentState;
            if (!parentStates.TryGetValue(currentState, out parentState))
                return Array.Empty<PuzzleRouteStep>();

            reversedRoute.Add(parentState.RouteStep);
            currentState = parentState.PreviousState;
        }

        reversedRoute.Reverse();
        return reversedRoute;
    }

    private static void UnpackObstacles(long packedPositions, int obstacleCount, int[] obstacleBuffer)
    {
        for (int obstacleIndex = 0; obstacleIndex < obstacleCount; obstacleIndex++)
        {
            obstacleBuffer[obstacleIndex] =
                (int)((packedPositions >> (obstacleIndex * 7)) & DestroyedObstacleIndex);
        }
    }

    private static int FindObstacleAt(int[] obstacleBuffer, int obstacleCount, int tileIndex)
    {
        for (int obstacleIndex = 0; obstacleIndex < obstacleCount; obstacleIndex++)
        {
            int obstaclePosition = obstacleBuffer[obstacleIndex];
            if (obstaclePosition != DestroyedObstacleIndex && obstaclePosition == tileIndex)
                return obstacleIndex;
        }

        return -1;
    }

    private static Vector2Int FindStartPosition(MapData mapData)
    {
        if (mapData == null || mapData.rows == null)
            return new Vector2Int(-1, -1);

        for (int y = 0; y < mapData.rows.Length; y++)
        {
            Wrapper<SerializedTile> row = mapData.rows[y];
            if (row == null || row.values == null)
                continue;

            for (int x = 0; x < row.values.Length; x++)
            {
                if (row.values[x].type == TileType.Start)
                    return new Vector2Int(x, y);
            }
        }

        return new Vector2Int(-1, -1);
    }

    private static PuzzleSolveResult CreateStructureError(string errorMessage)
    {
        return new PuzzleSolveResult
        {
            IsSolvable = false,
            HasStructureError = true,
            RemainingMoves = -1,
            StatesExplored = 0,
            ErrorMessage = errorMessage,
            Route = Array.Empty<PuzzleRouteStep>()
        };
    }

    private static int GetIndex(int x, int y, int width)
    {
        return y * width + x;
    }

    private static bool IsInside(int x, int y, int width, int height)
    {
        return x >= 0 && x < width && y >= 0 && y < height;
    }

    private static bool IsExitConditionMet(ExitCondition exitCondition, int remainingMoves)
    {
        switch (exitCondition)
        {
            case ExitCondition.Free:
                return true;
            case ExitCondition.OddOnly:
                return remainingMoves % 2 != 0;
            case ExitCondition.EvenOnly:
                return remainingMoves % 2 == 0;
            default:
                return false;
        }
    }
}
