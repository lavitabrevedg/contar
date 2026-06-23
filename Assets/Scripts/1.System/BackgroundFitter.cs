using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public class BackgroundFitter : MonoBehaviour
{
    [SerializeField] private Camera targetCamera;
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private float distanceFromCamera = 50f;
    [SerializeField] private float overscan = 1.02f;
    [SerializeField] private bool preserveAspect = true;
    [SerializeField] private int sortingOrder = -100;

    private void Awake()
    {
        if (spriteRenderer == null)
            spriteRenderer = GetComponent<SpriteRenderer>();
    }

    public void Fit()
    {
        if (targetCamera == null || spriteRenderer == null || spriteRenderer.sprite == null)
            return;

        targetCamera.orthographic = true;

        Transform cameraTransform = targetCamera.transform;
        transform.position = cameraTransform.position + cameraTransform.forward * distanceFromCamera;
        transform.rotation = cameraTransform.rotation;

        float viewHeight = targetCamera.orthographicSize * 2f;
        float viewWidth = viewHeight * targetCamera.aspect;
        Vector2 spriteSize = spriteRenderer.sprite.bounds.size;

        if (spriteSize.x <= 0f || spriteSize.y <= 0f)
            return;

        if (preserveAspect)
        {
            float scale = Mathf.Max(viewWidth / spriteSize.x, viewHeight / spriteSize.y) * overscan;
            transform.localScale = new Vector3(scale, scale, 1f);
        }
        else
        {
            transform.localScale = new Vector3(
                viewWidth / spriteSize.x * overscan,
                viewHeight / spriteSize.y * overscan,
                1f
            );
        }

        spriteRenderer.sortingOrder = sortingOrder;
    }
}
