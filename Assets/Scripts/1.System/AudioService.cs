using UnityEngine;

public class AudioService : MonoBehaviour
{
    private const string MoveClipPath = "Sounds/SFX_Move";
    private const string PushClipPath = "Sounds/SFX_Push";
    private const string BlockedClipPath = "Sounds/SFX_Blocked";
    private const string ClearClipPath = "Sounds/SFX_Clear";
    private const string FailClipPath = "Sounds/SFX_Fail";
    private const string MoveTileClipPath = "Sounds/SFX_MoveTile";
    private const string UiClipPath = "Sounds/SFX_UI";

    [SerializeField] private SettingsService settingsService;
    [SerializeField] private AudioClip moveClip;
    [SerializeField] private AudioClip pushClip;
    [SerializeField] private AudioClip blockedClip;
    [SerializeField] private AudioClip clearClip;
    [SerializeField] private AudioClip failClip;
    [SerializeField] private AudioClip moveTileClip;
    [SerializeField] private AudioClip uiClip;

    private AudioSource audioSource;

    private void Awake()
    {
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
            audioSource = gameObject.AddComponent<AudioSource>();

        audioSource.playOnAwake = false;
        audioSource.spatialBlend = 0f;
        ResolveSettingsService();
        LoadMissingClips();
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

    public void PlayMoveTile()
    {
        Play(moveTileClip);
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

    private void LoadMissingClips()
    {
        moveClip = LoadClipIfMissing(moveClip, MoveClipPath);
        pushClip = LoadClipIfMissing(pushClip, PushClipPath);
        blockedClip = LoadClipIfMissing(blockedClip, BlockedClipPath);
        clearClip = LoadClipIfMissing(clearClip, ClearClipPath);
        failClip = LoadClipIfMissing(failClip, FailClipPath);
        moveTileClip = LoadClipIfMissing(moveTileClip, MoveTileClipPath);
        uiClip = LoadClipIfMissing(uiClip, UiClipPath);
    }

    private AudioClip LoadClipIfMissing(AudioClip clip, string resourcePath)
    {
        if (clip != null)
            return clip;

        return Resources.Load<AudioClip>(resourcePath);
    }
}
