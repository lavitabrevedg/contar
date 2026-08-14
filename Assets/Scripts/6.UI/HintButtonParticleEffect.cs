using UnityEngine;

public class HintButtonParticleEffect : MonoBehaviour
{
    [SerializeField] private GameObject particlePrefab;
    [SerializeField, Min(0.01f)] private float particleScale = 1f;
    [SerializeField] private Vector2 screenOffset;
    [SerializeField, Min(0.31f)] private float distanceFromCamera = 2f;
    [SerializeField] private int sortingOrder = 1000;

    private RectTransform hintButtonRect;
    private Canvas parentCanvas;
    private Camera effectWorldCamera;
    private GameObject particleInstance;
    private ParticleSystem[] particleSystems;
    private readonly Vector3[] hintButtonWorldCorners = new Vector3[4];
    private bool isPlaying;

    private void Awake()
    {
        hintButtonRect = transform as RectTransform;
        parentCanvas = GetComponentInParent<Canvas>();
        EnsureEffectInstance();
        StopParticles();
    }

    private void OnEnable()
    {
        if (!Application.isPlaying)
            return;

        EnsureEffectInstance();
        UpdateEffectPosition();
        ApplyPlaybackState();
    }

    private void OnDisable()
    {
        StopParticles();
    }

    private void OnDestroy()
    {
        StopParticles();
        particleSystems = null;

        if (particleInstance != null)
        {
            Destroy(particleInstance);
            particleInstance = null;
        }
    }

    private void LateUpdate()
    {
        if (!isPlaying || particleInstance == null)
            return;

        UpdateEffectPosition();
    }

    public void SetPlaying(bool shouldPlay)
    {
        if (isPlaying == shouldPlay)
            return;

        isPlaying = shouldPlay;
        if (!Application.isPlaying)
            return;

        EnsureEffectInstance();
        ApplyPlaybackState();
    }

    private void EnsureEffectInstance()
    {
        if (particleInstance != null || particlePrefab == null)
            return;

        particleInstance = Instantiate(particlePrefab);
        particleInstance.name = $"{particlePrefab.name} (Hint Button)";
        particleInstance.transform.localScale = Vector3.one * particleScale;
        particleSystems = particleInstance.GetComponentsInChildren<ParticleSystem>(true);

        ParticleSystemRenderer[] particleRenderers =
            particleInstance.GetComponentsInChildren<ParticleSystemRenderer>(true);
        for (int rendererIndex = 0; rendererIndex < particleRenderers.Length; rendererIndex++)
            particleRenderers[rendererIndex].sortingOrder = sortingOrder;
    }

    private void ApplyPlaybackState()
    {
        if (isPlaying && isActiveAndEnabled)
        {
            UpdateEffectPosition();
            PlayParticles();
            return;
        }

        StopParticles();
    }

    private void PlayParticles()
    {
        if (particleSystems == null)
            return;

        for (int particleIndex = 0; particleIndex < particleSystems.Length; particleIndex++)
        {
            ParticleSystem particleSystem = particleSystems[particleIndex];
            if (particleSystem == null)
                continue;

            if (!particleSystem.isPlaying)
                particleSystem.Play(true);
        }
    }

    private void StopParticles()
    {
        if (particleSystems == null)
            return;

        for (int particleIndex = 0; particleIndex < particleSystems.Length; particleIndex++)
        {
            ParticleSystem particleSystem = particleSystems[particleIndex];
            if (particleSystem == null)
                continue;

            particleSystem.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }
    }

    private void UpdateEffectPosition()
    {
        if (particleInstance == null || hintButtonRect == null)
            return;

        if (effectWorldCamera == null)
            effectWorldCamera = Camera.main;

        if (effectWorldCamera == null)
            return;

        Camera canvasCamera = null;
        if (parentCanvas != null && parentCanvas.renderMode != RenderMode.ScreenSpaceOverlay)
            canvasCamera = parentCanvas.worldCamera;

        Vector2 buttonScreenPosition = GetButtonScreenCenter(canvasCamera);
        Vector3 effectScreenPosition = new Vector3(
            buttonScreenPosition.x + screenOffset.x,
            buttonScreenPosition.y + screenOffset.y,
            Mathf.Max(distanceFromCamera, effectWorldCamera.nearClipPlane + 0.01f));
        particleInstance.transform.position = effectWorldCamera.ScreenToWorldPoint(effectScreenPosition);
    }

    private Vector2 GetButtonScreenCenter(Camera canvasCamera)
    {
        if (hintButtonRect == null)
            hintButtonRect = transform as RectTransform;

        if (hintButtonRect == null)
            return Vector2.zero;

        hintButtonRect.GetWorldCorners(hintButtonWorldCorners);
        Vector3 buttonWorldCenter =
            (hintButtonWorldCorners[0] + hintButtonWorldCorners[2]) * 0.5f;
        return RectTransformUtility.WorldToScreenPoint(canvasCamera, buttonWorldCenter);
    }
}
