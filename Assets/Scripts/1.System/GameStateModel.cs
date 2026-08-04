using System;
using UnityEngine;

public enum GameState
{
    Playing,
    Cleared,
    Failed
}

public class GameStateModel : MonoBehaviour
{
    public int MoveCount { get; private set; }
    public GameState State { get; private set; } = GameState.Playing;

    public event Action<int> MoveCountChanged;
    public event Action<GameState> StateChanged;

    public void StartStage(int startMoveCount)
    {
        MoveCount = startMoveCount;
        State = GameState.Playing;

        MoveCountChanged?.Invoke(MoveCount);
        StateChanged?.Invoke(State);
    }

    public void SpendMoveCount(int cost)
    {
        SetMoveCount(MoveCount - cost);
    }

    public void AddMoveCount(int delta)
    {
        SetMoveCount(MoveCount + delta);
    }

    public void Clear()
    {
        SetState(GameState.Cleared);
    }

    public void Fail()
    {
        SetState(GameState.Failed);
    }

    private void SetMoveCount(int moveCount)
    {
        MoveCount = moveCount;
        MoveCountChanged?.Invoke(MoveCount);
    }

    private void SetState(GameState state)
    {
        if (State == state) return;

        State = state;
        StateChanged?.Invoke(State);
    }
}
