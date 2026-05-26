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
