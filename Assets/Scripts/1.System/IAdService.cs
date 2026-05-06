using System;

public interface IAdService
{
    bool IsReady(AdPlacement placement);
    void Show(AdPlacement placement, Action<bool> completed);
}
