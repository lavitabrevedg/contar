#if UNITY_EDITOR
using System;
using UnityEngine;

public sealed class EditorHintAdService : IAdService
{
    public bool IsReady(AdPlacement placement)
    {
        return placement == AdPlacement.HintRoute;
    }

    public void Show(AdPlacement placement, Action<bool> completed)
    {
        if (placement != AdPlacement.HintRoute)
        {
            completed?.Invoke(false);
            return;
        }

        Debug.Log("[EditorHintAdService] Rewarded hint ad simulated in the Unity Editor.");
        completed?.Invoke(true);
    }
}
#endif
