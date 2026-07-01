using DG.Tweening;
using UnityEngine;
using TMPro;

public abstract class BaseTile : MonoBehaviour
{
    [SerializeField] private TextMeshPro labelText;

    public abstract void Init(SerializedTile data);
    public virtual void OnPlayerEnter() { }

    protected void SetTileLabel(string text)
    {
        EnsureLabelText();

        if (labelText == null) return;

        ConfigureLabelText();

        bool hasText = !string.IsNullOrWhiteSpace(text);
        labelText.gameObject.SetActive(hasText);
        labelText.text = hasText ? text : string.Empty;
    }

    protected void ConfigureTileLabelStyle(Color color, float fontSizeMax, Vector2 sizeDelta, Vector3 localPosition)
    {
        EnsureLabelText();

        if (labelText == null) return;

        labelText.color = color;
        labelText.fontStyle = FontStyles.Bold;
        labelText.fontSizeMax = fontSizeMax;

        if (labelText.rectTransform != null)
            labelText.rectTransform.sizeDelta = sizeDelta;

        labelText.transform.localPosition = localPosition;
    }

    protected void ClearTileLabelAnimated()
    {
        EnsureLabelText();

        if (labelText == null)
            return;

        labelText.transform.DOKill();
        labelText.DOKill();

        Vector3 startPosition = labelText.transform.localPosition;
        Color startColor = labelText.color;

        Sequence sequence = DOTween.Sequence();
        sequence.Join(labelText.transform.DOLocalMoveY(startPosition.y + 0.15f, 0.16f));
        sequence.Join(labelText.DOFade(0f, 0.16f));
        sequence.OnComplete(() =>
        {
            labelText.text = string.Empty;
            labelText.gameObject.SetActive(false);
            labelText.transform.localPosition = startPosition;
            labelText.color = startColor;
        });
    }

    public void PlayBlockedFeedback()
    {
        transform.DOKill();
        transform.DOShakePosition(0.18f, 0.08f, 12, 90f)
            .SetEase(Ease.OutQuad);
    }

    private void EnsureLabelText()
    {
        if (labelText != null) return;

        labelText = GetComponentInChildren<TextMeshPro>(true);
    }

    private void ConfigureLabelText()
    {
        if (labelText == null) return;

        if (labelText.font == null && TMP_Settings.defaultFontAsset != null)
            labelText.font = TMP_Settings.defaultFontAsset;

        labelText.alignment = TextAlignmentOptions.Center;
        labelText.color = Color.black;
        labelText.fontSize = 3.4f;
        labelText.enableAutoSizing = true;
        labelText.fontSizeMin = 1.2f;
        labelText.fontSizeMax = 3.4f;

        if (labelText.rectTransform != null)
            labelText.rectTransform.sizeDelta = new Vector2(0.9f, 0.5f);

        MeshRenderer labelRenderer = labelText.GetComponent<MeshRenderer>();
        if (labelRenderer != null)
            labelRenderer.sortingOrder = 10;
    }
}
