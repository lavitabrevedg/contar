using UnityEngine;

public class AudioService : MonoBehaviour
{
    [SerializeField] private SettingsService settingsService;

    private AudioSource audioSource;
    private AudioClip moveClip;
    private AudioClip pushClip;
    private AudioClip blockedClip;
    private AudioClip clearClip;
    private AudioClip failClip;
    private AudioClip rewardClip;
    private AudioClip uiClip;

    private void Awake()
    {
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
            audioSource = gameObject.AddComponent<AudioSource>();

        audioSource.playOnAwake = false;
        audioSource.spatialBlend = 0f;
        ResolveSettingsService();
        BuildClips();
    }

    public void PlayMove()
    {
        Play(moveClip);
    }

    public void PlayPush()
    {
        Play(pushClip);
    }

    public void PlayBlocked()
    {
        Play(blockedClip);
    }

    public void PlayClear()
    {
        Play(clearClip);
    }

    public void PlayFail()
    {
        Play(failClip);
    }

    public void PlayReward()
    {
        Play(rewardClip);
    }

    public void PlayUi()
    {
        Play(uiClip);
    }

    private void Play(AudioClip clip)
    {
        ResolveSettingsService();

        if (settingsService != null && !settingsService.IsSoundEnabled)
            return;

        if (clip == null || audioSource == null)
            return;

        audioSource.PlayOneShot(clip);
    }

    private void ResolveSettingsService()
    {
        if (settingsService == null)
            settingsService = FindFirstObjectByType<SettingsService>();
    }

    private void BuildClips()
    {
        moveClip = CreateTone("contar_move", 440f, 0.045f, 0.12f);
        pushClip = CreateTone("contar_push", 220f, 0.08f, 0.18f);
        blockedClip = CreateTone("contar_blocked", 120f, 0.08f, 0.14f);
        clearClip = CreateTone("contar_clear", 660f, 0.14f, 0.2f);
        failClip = CreateTone("contar_fail", 90f, 0.16f, 0.18f);
        rewardClip = CreateTone("contar_reward", 880f, 0.12f, 0.18f);
        uiClip = CreateTone("contar_ui", 520f, 0.04f, 0.1f);
    }

    private AudioClip CreateTone(string clipName, float frequency, float duration, float volume)
    {
        int sampleRate = 44100;
        int sampleCount = Mathf.Max(1, Mathf.RoundToInt(sampleRate * duration));
        float[] samples = new float[sampleCount];

        for (int i = 0; i < sampleCount; i++)
        {
            float progress = (float)i / sampleCount;
            float fade = 1f - progress;
            samples[i] = Mathf.Sin(2f * Mathf.PI * frequency * i / sampleRate) * volume * fade;
        }

        AudioClip clip = AudioClip.Create(clipName, sampleCount, 1, sampleRate, false);
        clip.SetData(samples, 0);
        return clip;
    }
}
