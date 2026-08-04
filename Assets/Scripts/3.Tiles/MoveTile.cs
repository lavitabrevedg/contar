using UnityEngine;

public class MoveTile : BaseTile
{
    private static readonly Color PositiveLabelColor = new Color(0.18f, 0.82f, 0.52f);
    private static readonly Color NegativeLabelColor = new Color(1f, 0.28f, 0.36f);

    public int value;

    public bool IsConsumed { get; private set; }

    public override void Init(SerializedTile data)
    {
        value = data.value;
        IsConsumed = false;
        SetTileLabel(FormatValue(value));
        ConfigureMoveLabel();
    }

    public override void OnPlayerEnter()
    {
        if (IsConsumed) return;

        // value가 양수면 +타일, 음수면 -타일 역할
        if (GameManager.Instance == null) return;

        IsConsumed = true;
        GameManager.Instance.AddMoveCount(value);

        AudioService audioService = FindFirstObjectByType<AudioService>();
        if (audioService != null)
            audioService.PlayMoveTile(value);

        ClearTileLabelAnimated();
    }

    private string FormatValue(int moveValue)
    {
        if (moveValue > 0)
            return $"+{moveValue}";

        return moveValue.ToString();
    }

    private void ConfigureMoveLabel()
    {
        Color labelColor = value >= 0 ? PositiveLabelColor : NegativeLabelColor;
        ConfigureTileLabelStyle(
            labelColor,
            3.8f,
            new Vector2(0.95f, 0.48f),
            new Vector3(0f, 0.08f, -0.1f)
        );
    }
}
