using DG.Tweening;
using UnityEngine;

[RequireComponent(typeof(RectTransform))]
public class LobbyLogoAnimator : MonoBehaviour
{
    [SerializeField] private RectTransform logoTransform;
    [SerializeField] private float introStartScale = 0.72f;
    [SerializeField] private float introOvershootScale = 1.06f;
    [SerializeField] private float introGrowDuration = 0.39f;
    [SerializeField] private float introSettleDuration = 0.16f;
    [SerializeField] private float floatingDistance = 12f;
    [SerializeField] private float floatingCycleDuration = 3.2f;

    private Sequence introSequence;
    private Tween floatingTween;
    private Vector3 baseScale;
    private Vector2 baseAnchoredPosition;
    private bool hasDefaults;

    private void Reset()
    {
        logoTransform = GetComponent<RectTransform>();
    }

    private void Awake()
    {
        ResolveLogoTransform();
        CacheDefaults();
    }

    private void OnEnable()
    {
        PlayAnimation();
    }

    private void OnDisable()
    {
        StopAndRestore();
    }

    private void OnDestroy()
    {
        KillTweens();
    }

    private void ResolveLogoTransform()
    {
        if (logoTransform == null)
            logoTransform = GetComponent<RectTransform>();
    }

    private void CacheDefaults()
    {
        if (hasDefaults || logoTransform == null)
            return;

        baseScale = logoTransform.localScale;
        baseAnchoredPosition = logoTransform.anchoredPosition;
        hasDefaults = true;
    }

    private void PlayAnimation()
    {
        ResolveLogoTransform();
        CacheDefaults();

        if (logoTransform == null)
            return;

        KillTweens();
        logoTransform.localScale = baseScale * introStartScale;
        logoTransform.anchoredPosition = baseAnchoredPosition;

        introSequence = DOTween.Sequence();
        introSequence.SetUpdate(true);
        introSequence.SetLink(gameObject, LinkBehaviour.KillOnDestroy);
        introSequence.Append(logoTransform.DOScale(baseScale * introOvershootScale, introGrowDuration)
            .SetEase(Ease.OutBack));
        introSequence.Append(logoTransform.DOScale(baseScale, introSettleDuration)
            .SetEase(Ease.InOutSine));
        introSequence.OnComplete(StartFloatingAnimation);
    }

    private void StartFloatingAnimation()
    {
        floatingTween = DOVirtual.Float(0f, Mathf.PI * 2f, floatingCycleDuration, UpdateFloatingPosition)
            .SetEase(Ease.Linear)
            .SetLoops(-1)
            .SetUpdate(true)
            .SetLink(gameObject, LinkBehaviour.KillOnDestroy);
    }

    private void UpdateFloatingPosition(float angle)
    {
        if (logoTransform == null)
            return;

        Vector2 floatingPosition = baseAnchoredPosition;
        floatingPosition.y += Mathf.Sin(angle) * floatingDistance;
        logoTransform.anchoredPosition = floatingPosition;
    }

    private void StopAndRestore()
    {
        KillTweens();

        if (!hasDefaults || logoTransform == null)
            return;

        logoTransform.localScale = baseScale;
        logoTransform.anchoredPosition = baseAnchoredPosition;
    }

    private void KillTweens()
    {
        if (introSequence != null)
        {
            introSequence.Kill();
            introSequence = null;
        }

        if (floatingTween != null)
        {
            floatingTween.Kill();
            floatingTween = null;
        }

        if (logoTransform != null)
            logoTransform.DOKill();
    }
}
