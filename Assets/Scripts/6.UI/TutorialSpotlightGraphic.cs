using UnityEngine;
using UnityEngine.UI;

public class TutorialSpotlightGraphic : MaskableGraphic
{
    private Rect focusRect;
    private bool hasFocus;

    protected override void Awake()
    {
        base.Awake();
        raycastTarget = false;
    }

    public void SetFocus(Rect nextFocusRect)
    {
        focusRect = nextFocusRect;
        hasFocus = true;
        SetVerticesDirty();
    }

    public void ClearFocus()
    {
        hasFocus = false;
        SetVerticesDirty();
    }

    protected override void OnPopulateMesh(VertexHelper vertexHelper)
    {
        vertexHelper.Clear();
        Rect outerRect = rectTransform.rect;
        if (!hasFocus)
        {
            AddQuad(vertexHelper, outerRect);
            return;
        }

        float focusLeft = Mathf.Clamp(focusRect.xMin, outerRect.xMin, outerRect.xMax);
        float focusRight = Mathf.Clamp(focusRect.xMax, outerRect.xMin, outerRect.xMax);
        float focusBottom = Mathf.Clamp(focusRect.yMin, outerRect.yMin, outerRect.yMax);
        float focusTop = Mathf.Clamp(focusRect.yMax, outerRect.yMin, outerRect.yMax);

        AddQuad(vertexHelper, new Rect(
            outerRect.xMin,
            focusTop,
            outerRect.width,
            outerRect.yMax - focusTop));
        AddQuad(vertexHelper, new Rect(
            outerRect.xMin,
            outerRect.yMin,
            outerRect.width,
            focusBottom - outerRect.yMin));
        AddQuad(vertexHelper, new Rect(
            outerRect.xMin,
            focusBottom,
            focusLeft - outerRect.xMin,
            focusTop - focusBottom));
        AddQuad(vertexHelper, new Rect(
            focusRight,
            focusBottom,
            outerRect.xMax - focusRight,
            focusTop - focusBottom));
    }

    private void AddQuad(VertexHelper vertexHelper, Rect quadRect)
    {
        if (quadRect.width <= 0f || quadRect.height <= 0f)
            return;

        int vertexStartIndex = vertexHelper.currentVertCount;
        Color32 vertexColor = color;
        vertexHelper.AddVert(new Vector3(quadRect.xMin, quadRect.yMin), vertexColor, Vector2.zero);
        vertexHelper.AddVert(new Vector3(quadRect.xMin, quadRect.yMax), vertexColor, Vector2.up);
        vertexHelper.AddVert(new Vector3(quadRect.xMax, quadRect.yMax), vertexColor, Vector2.one);
        vertexHelper.AddVert(new Vector3(quadRect.xMax, quadRect.yMin), vertexColor, Vector2.right);
        vertexHelper.AddTriangle(vertexStartIndex, vertexStartIndex + 1, vertexStartIndex + 2);
        vertexHelper.AddTriangle(vertexStartIndex + 2, vertexStartIndex + 3, vertexStartIndex);
    }
}
