using System;
using UnityEngine;

public class DummyAdService : MonoBehaviour, IAdService
{
    public bool IsReady(AdPlacement placement)
    {
        return true;
    }

    public void Show(AdPlacement placement, Action<bool> completed)
    {
        Debug.Log($"[DummyAdService] Ad completed immediately. placement={placement}");
        completed?.Invoke(true);
    }
}
