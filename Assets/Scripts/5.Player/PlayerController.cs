using System;
using DG.Tweening;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    [SerializeField] private float moveDuration = 0.18f;
    [SerializeField] private Ease moveEase = Ease.OutQuad;
    [SerializeField] private Transform visualRoot;
    [SerializeField] private SpriteRenderer visualRenderer;
    [SerializeField] private float hopHeight = 0.12f;
    [SerializeField] private float squashX = 1.08f;
    [SerializeField] private float squashY = 0.92f;

    private Vector2Int gridPosition;
    private bool isMoving;
    private Tween activeMoveTween;
    private Vector3 visualStartLocalPosition;
    private Vector3 visualStartLocalScale;

    public Vector2Int GridPosition => gridPosition;
    public bool IsMoving => isMoving;

    private void Awake()
    {
        ResolveVisualReferences();
        CacheVisualDefaults();
    }

    public void Init(Vector2Int startGrid, Vector3 startWorldPos)
    {
        activeMoveTween?.Kill();
        activeMoveTween = null;
        isMoving = false;
        gridPosition = startGrid;
        transform.position = startWorldPos;
        transform.rotation = Quaternion.identity;
        ResetVisual();
    }

    public void AnimateTo(Vector2Int targetGrid, Vector3 targetWorldPos, Action onComplete = null)
    {
        if (isMoving) return;

        Vector2Int direction = targetGrid - gridPosition;
        UpdateFacing(direction);

        isMoving = true;

        Sequence sequence = DOTween.Sequence();
        activeMoveTween = sequence;

        sequence.Join(transform.DOMove(targetWorldPos, moveDuration).SetEase(moveEase));
        AddVisualStepTween(sequence);
        sequence.OnComplete(() =>
        {
            transform.position = targetWorldPos;
            ResetVisual();
            gridPosition = targetGrid;
            isMoving = false;
            activeMoveTween = null;
            onComplete?.Invoke();
        });
    }

    private void OnDisable()
    {
        activeMoveTween?.Kill();
        activeMoveTween = null;
        isMoving = false;
        ResetVisual();
    }

    private void ResolveVisualReferences()
    {
        if (visualRoot == null)
            visualRoot = transform.Find("Visual");

        if (visualRenderer == null && visualRoot != null)
            visualRenderer = visualRoot.GetComponent<SpriteRenderer>();

        if (visualRenderer == null)
            visualRenderer = GetComponentInChildren<SpriteRenderer>();
    }

    private void CacheVisualDefaults()
    {
        if (visualRoot == null)
            return;

        visualStartLocalPosition = visualRoot.localPosition;
        visualStartLocalScale = visualRoot.localScale;
    }

    private void ResetVisual()
    {
        if (visualRoot == null)
            return;

        visualRoot.DOKill();
        visualRoot.localPosition = visualStartLocalPosition;
        visualRoot.localScale = visualStartLocalScale;
    }

    private void UpdateFacing(Vector2Int direction)
    {
        if (visualRenderer == null || direction.x == 0)
            return;

        visualRenderer.flipX = direction.x < 0;
    }

    private void AddVisualStepTween(Sequence sequence)
    {
        if (visualRoot == null)
            return;

        visualRoot.DOKill();
        visualRoot.localPosition = visualStartLocalPosition;
        visualRoot.localScale = visualStartLocalScale;

        Vector3 hopTarget = visualStartLocalPosition + Vector3.up * hopHeight;
        Vector3 squashTarget = new Vector3(
            visualStartLocalScale.x * squashX,
            visualStartLocalScale.y * squashY,
            visualStartLocalScale.z
        );

        sequence.Join(visualRoot.DOLocalMoveY(hopTarget.y, moveDuration * 0.5f)
            .SetEase(Ease.OutQuad)
            .SetLoops(2, LoopType.Yoyo));

        sequence.Join(visualRoot.DOScale(squashTarget, moveDuration * 0.5f)
            .SetEase(Ease.InOutSine)
            .SetLoops(2, LoopType.Yoyo));
    }
}
