using System;
using DG.Tweening;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    [SerializeField] private float moveDuration = 0.18f;
    [SerializeField] private Ease moveEase = Ease.OutQuad;

    private Vector2Int _gridPosition;
    private bool _isMoving;
    private Tween activeMoveTween;

    public Vector2Int GridPosition => _gridPosition;
    public bool IsMoving => _isMoving;

    public void Init(Vector2Int startGrid, Vector3 startWorldPos)
    {
        activeMoveTween?.Kill();
        activeMoveTween = null;
        _isMoving = false;
        _gridPosition = startGrid;
        transform.position = startWorldPos;
    }

    public void AnimateTo(Vector2Int targetGrid, Vector3 targetWorldPos, Action onComplete = null)
    {
        if (_isMoving) return;

        Vector2Int direction = targetGrid - _gridPosition;

        // 카메라가 -Z 를 바라보는 2D 뷰 기준, 이동 방향에 수직인 축이 롤링 축이 된다.
        // direction=(1,0) → axis=(0,-1,0), direction=(0,1) → axis=(1,0,0)
        Vector3 rollAxis = new Vector3(direction.y, -direction.x, 0f);
        Quaternion startRotation = transform.rotation;
        Quaternion endRotation = Quaternion.AngleAxis(90f, rollAxis) * startRotation;

        _isMoving = true;

        Sequence sequence = DOTween.Sequence();
        activeMoveTween = sequence;

        sequence.Join(transform.DOMove(targetWorldPos, moveDuration).SetEase(moveEase));
        sequence.Join(transform.DORotateQuaternion(endRotation, moveDuration).SetEase(moveEase));
        sequence.OnComplete(() =>
        {
            transform.position = targetWorldPos;
            transform.rotation = endRotation;
            _gridPosition = targetGrid;
            _isMoving = false;
            activeMoveTween = null;
            onComplete?.Invoke();
        });
    }

    private void OnDisable()
    {
        activeMoveTween?.Kill();
        activeMoveTween = null;
        _isMoving = false;
    }
}
