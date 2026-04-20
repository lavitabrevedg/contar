using System;
using System.Collections;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    [SerializeField] private float moveDuration = 0.18f;

    private Vector2Int _gridPosition;
    private bool _isMoving;

    public Vector2Int GridPosition => _gridPosition;
    public bool IsMoving => _isMoving;

    public void Init(Vector2Int startGrid, Vector3 startWorldPos)
    {
        _gridPosition = startGrid;
        transform.position = startWorldPos;
    }

    public void AnimateTo(Vector2Int targetGrid, Vector3 targetWorldPos, Action onComplete = null)
    {
        if (_isMoving) return;

        Vector2Int direction = targetGrid - _gridPosition;
        StartCoroutine(RollRoutine(direction, targetGrid, targetWorldPos, onComplete));
    }

    private IEnumerator RollRoutine(Vector2Int direction, Vector2Int targetGrid, Vector3 targetWorldPos, Action onComplete)
    {
        _isMoving = true;

        Vector3 startPos = transform.position;

        // 카메라가 -Z 를 바라보는 2D 뷰 기준, 이동 방향에 수직인 축이 롤링 축이 된다.
        // direction=(1,0) → axis=(0,-1,0), direction=(0,1) → axis=(1,0,0)
        Vector3 rollAxis = new Vector3(direction.y, -direction.x, 0f);
        Quaternion startRot = transform.rotation;
        Quaternion endRot = Quaternion.AngleAxis(90f, rollAxis) * startRot;

        float elapsed = 0f;
        while (elapsed < moveDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / moveDuration);
            transform.position = Vector3.Lerp(startPos, targetWorldPos, t);
            transform.rotation = Quaternion.Slerp(startRot, endRot, t);
            yield return null;
        }

        transform.position = targetWorldPos;
        transform.rotation = endRot;
        _gridPosition = targetGrid;
        _isMoving = false;

        onComplete?.Invoke();
    }
}
