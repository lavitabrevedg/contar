using UnityEngine;

public class AudioService : MonoBehaviour
{
    private const string MoveClipPath = "Sounds/SFX_Move";
    private const string PushClipPath = "Sounds/SFX_Push";
    private const string BlockedClipPath = "Sounds/SFX_Blocked";
    private const string ClearClipPath = "Sounds/SFX_Clear";
    private const string FailClipPath = "Sounds/SFX_Fail";
    private const string PositiveMoveTileClipPath = "Sounds/SFX_MoveTile_Positive";
    private const string NegativeMoveTileClipPath = "Sounds/SFX_MoveTile_Negative";
    private const string UiClipPath = "Sounds/SFX_UI";

    [SerializeField] private SettingsService settingsService;
    [SerializeField] private AudioClip moveClip;
    [SerializeField] private AudioClip pushClip;
    [SerializeField] private AudioClip blockedClip;
    [SerializeField] private AudioClip clearClip;
    [SerializeField] private AudioClip failClip;
    [SerializeField] private AudioClip positiveMoveTileClip;
    [SerializeField] private AudioClip negativeMoveTileClip;
    [SerializeField] private AudioClip uiClip;
    [SerializeField, Range(0f, 3f)] private float sfxVolume = 1.8f;
    [SerializeField] private AudioClip backgroundMusicClip;
    [SerializeField, Range(0f, 1f)] private float backgroundMusicVolume = 0.35f;
    [SerializeField, Min(0f)] private float backgroundMusicCrossfadeDuration = 0.5f;

    private AudioSource audioSource;
    private BackgroundMusicService backgroundMusicService;

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

    private void OnEnable()
    {
        BindSettingsService();
    }

    private void Start()
    {
        RefreshBackgroundMusic();
    }

    private void OnDisable()
    {
        UnbindSettingsService();
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

    public void PlayMoveTile(int moveValue)
    {
        AudioClip selectedClip = moveValue >= 0 ? positiveMoveTileClip : negativeMoveTileClip;
        Play(selectedClip);
    }

    public void PlayUi()
    {
        Play(uiClip);
    }

    private void Play(AudioClip clip)
    {
        ResolveSettingsService();

        if (settingsService != null && !settingsService.IsSoundEffectEnabled)
            return;

        if (clip == null || audioSource == null)
            return;

        audioSource.PlayOneShot(clip, sfxVolume);
    }

    private void ResolveSettingsService()
    {
        if (settingsService == null)
            settingsService = FindFirstObjectByType<SettingsService>();
    }

    private void BindSettingsService()
    {
        ResolveSettingsService();
        if (settingsService == null)
            return;

        settingsService.SettingsChanged -= RefreshBackgroundMusic;
        settingsService.SettingsChanged += RefreshBackgroundMusic;
    }

    private void UnbindSettingsService()
    {
        if (settingsService != null)
            settingsService.SettingsChanged -= RefreshBackgroundMusic;
    }

    private void RefreshBackgroundMusic()
    {
        ResolveSettingsService();
        backgroundMusicService = BackgroundMusicService.GetOrCreate();

        bool isMusicEnabled = settingsService == null || settingsService.IsMusicEnabled;
        backgroundMusicService.SetMusicEnabled(isMusicEnabled);

        if (backgroundMusicClip != null)
        {
            backgroundMusicService.SetMusic(
                backgroundMusicClip,
                backgroundMusicVolume,
                backgroundMusicCrossfadeDuration);
        }
    }

    private void LoadMissingClips()
    {
        moveClip = LoadClipIfMissing(moveClip, MoveClipPath);
        pushClip = LoadClipIfMissing(pushClip, PushClipPath);
        blockedClip = LoadClipIfMissing(blockedClip, BlockedClipPath);
        clearClip = LoadClipIfMissing(clearClip, ClearClipPath);
        failClip = LoadClipIfMissing(failClip, FailClipPath);
        positiveMoveTileClip = LoadClipIfMissing(positiveMoveTileClip, PositiveMoveTileClipPath);
        negativeMoveTileClip = LoadClipIfMissing(negativeMoveTileClip, NegativeMoveTileClipPath);
        uiClip = LoadClipIfMissing(uiClip, UiClipPath);
    }

    private AudioClip LoadClipIfMissing(AudioClip clip, string resourcePath)
    {
        if (clip != null)
            return clip;

        return Resources.Load<AudioClip>(resourcePath);
    }
}
