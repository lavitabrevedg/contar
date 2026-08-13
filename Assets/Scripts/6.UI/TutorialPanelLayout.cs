using UnityEngine;

public readonly struct TutorialPanelPlacement
{
    public Vector2 Position { get; }
    public bool IsAboveTarget { get; }

    public TutorialPanelPlacement(Vector2 position, bool isAboveTarget)
    {
        Position = position;
        IsAboveTarget = isAboveTarget;
    }
}

public static class TutorialPanelLayout
{
    public static TutorialPanelPlacement Calculate(
        Rect canvasRect,
        Vector2 targetPosition,
        Vector2 panelSize,
        float safeMargin,
        float pointerGap)
    {
        float halfPanelWidth = panelSize.x * 0.5f;
        float halfPanelHeight = panelSize.y * 0.5f;
        float aboveY = targetPosition.y + halfPanelHeight + pointerGap;
        float belowY = targetPosition.y - halfPanelHeight - pointerGap;
        bool fitsAbove = aboveY + halfPanelHeight <= canvasRect.yMax - safeMargin;
        bool fitsBelow = belowY - halfPanelHeight >= canvasRect.yMin + safeMargin;
        bool placeAbove = fitsAbove || !fitsBelow;

        Vector2 panelPosition = new Vector2(
            targetPosition.x,
            placeAbove ? aboveY : belowY);
        panelPosition.x = Mathf.Clamp(
            panelPosition.x,
            canvasRect.xMin + safeMargin + halfPanelWidth,
            canvasRect.xMax - safeMargin - halfPanelWidth);
        panelPosition.y = Mathf.Clamp(
            panelPosition.y,
            canvasRect.yMin + safeMargin + halfPanelHeight,
            canvasRect.yMax - safeMargin - halfPanelHeight);

        return new TutorialPanelPlacement(panelPosition, placeAbove);
    }
}
