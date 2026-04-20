using UnityEngine;

[RequireComponent(typeof(Camera))]
public class CameraFitter : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private MapGenerator mapGenerator;
    [SerializeField] private Camera targetCamera;

    [Header("Settings")]
    [SerializeField] private float padding = 1f;

    [Tooltip("카메라를 기울였을 때 맵이 화면 중앙에서 벗어나는 것을 보정. 보통 기울기 각도에 비례(20° 기울임 → 0.5~1.0 정도)")]
    [SerializeField] private float verticalOffset = 0f;

    [Tooltip("카메라 기울기로 인한 세로 시야 축소를 보정. 큰 기울기일수록 값 증가.")]
    [SerializeField] private float tiltSizeMultiplier = 1.05f;

    private int _lastScreenWidth;
    private int _lastScreenHeight;

    private void Awake()
    {
        if (targetCamera == null)
            targetCamera = GetComponent<Camera>();
    }

    private void Update()
    {
        // 기기 회전/해상도 변경 대응
        if (Screen.width != _lastScreenWidth || Screen.height != _lastScreenHeight)
        {
            Fit();
        }
    }

    public void Fit()
    {
        if (mapGenerator == null || mapGenerator.mapData == null || targetCamera == null)
            return;

        MapData data = mapGenerator.mapData;
        float tileSize = mapGenerator.tileSize;

        float worldWidth = data.width * tileSize + padding * 2f;
        float worldHeight = data.height * tileSize + padding * 2f;

        float screenAspect = (float)Screen.width / Screen.height;
        float sizeByHeight = worldHeight / 2f;
        float sizeByWidth = worldWidth / 2f / screenAspect;

        targetCamera.orthographicSize = Mathf.Max(sizeByHeight, sizeByWidth) * tiltSizeMultiplier;

        Vector3 centerPos = new Vector3(
            (data.width - 1) * tileSize * 0.5f,
            (data.height - 1) * tileSize * 0.5f - verticalOffset,
            -10f
        );
        targetCamera.transform.position = centerPos;

        _lastScreenWidth = Screen.width;
        _lastScreenHeight = Screen.height;
    }
}
