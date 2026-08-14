using System.Collections;
using UnityEngine;
using UnityEngine.Audio;

public class BackgroundMusicService : MonoBehaviour
{
    private const string ServiceObjectName = "BackgroundMusicService";
    private const string MusicMixerResourcePath = "Audio/MusicMixer";
    private const string MusicMixerGroupName = "Music";

    private static BackgroundMusicService instance;

    private AudioSource firstSource;
    private AudioSource secondSource;
    private AudioSource activeSource;
    private AudioSource standbySource;
    private AudioClip requestedClip;
    private float requestedVolume;
    private bool isMusicEnabled = true;
    private Coroutine crossfadeCoroutine;
    private AudioMixerGroup musicMixerGroup;

    public static BackgroundMusicService GetOrCreate()
    {
        if (instance != null)
            return instance;

        BackgroundMusicService existingService = FindFirstObjectByType<BackgroundMusicService>();
        if (existingService != null)
        {
            instance = existingService;
            return instance;
        }

        GameObject serviceObject = new GameObject(ServiceObjectName);
        return serviceObject.AddComponent<BackgroundMusicService>();
    }

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);
        CreateSources();
    }

    private void OnDestroy()
    {
        if (instance == this)
            instance = null;
    }

    public void SetMusicEnabled(bool isEnabled)
    {
        isMusicEnabled = isEnabled;

        if (!isMusicEnabled)
        {
            StopAllMusic();
            return;
        }

        PlayRequestedMusic();
    }

    public void SetMusic(AudioClip clip, float volume, float crossfadeDuration)
    {
        if (clip == null)
            return;

        requestedClip = clip;
        requestedVolume = Mathf.Clamp01(volume);

        if (!isMusicEnabled)
            return;

        PlayRequestedMusic(crossfadeDuration);
    }

    public void StopCurrentMusic()
    {
        StopAllMusic();
        requestedClip = null;
    }

    private void CreateSources()
    {
        ResolveMusicMixerGroup();
        firstSource = gameObject.AddComponent<AudioSource>();
        secondSource = gameObject.AddComponent<AudioSource>();
        ConfigureSource(firstSource);
        ConfigureSource(secondSource);
        activeSource = firstSource;
        standbySource = secondSource;
    }

    private void ConfigureSource(AudioSource source)
    {
        source.playOnAwake = false;
        source.loop = true;
        source.spatialBlend = 0f;
        source.volume = 0f;
        source.outputAudioMixerGroup = musicMixerGroup;
    }

    private void ResolveMusicMixerGroup()
    {
        AudioMixer musicMixer = Resources.Load<AudioMixer>(MusicMixerResourcePath);
        if (musicMixer == null)
        {
            Debug.LogWarning("[BackgroundMusicService] MusicMixer is missing. Music will use the default audio output.");
            return;
        }

        AudioMixerGroup[] matchingGroups = musicMixer.FindMatchingGroups(MusicMixerGroupName);
        if (matchingGroups == null || matchingGroups.Length == 0)
        {
            Debug.LogWarning("[BackgroundMusicService] Music mixer group is missing. Music will use the default audio output.");
            return;
        }

        musicMixerGroup = matchingGroups[0];
    }

    private void PlayRequestedMusic()
    {
        PlayRequestedMusic(0f);
    }

    private void PlayRequestedMusic(float crossfadeDuration)
    {
        if (requestedClip == null)
            return;

        if (activeSource != null && activeSource.isPlaying && activeSource.clip == requestedClip)
        {
            activeSource.volume = requestedVolume;
            return;
        }

        StopCrossfade();

        AudioSource nextSource = standbySource;
        if (nextSource == null)
            return;

        nextSource.Stop();
        nextSource.clip = requestedClip;
        nextSource.volume = 0f;
        nextSource.Play();

        if (activeSource == null || !activeSource.isPlaying || crossfadeDuration <= 0f)
        {
            if (activeSource != null)
                activeSource.Stop();

            nextSource.volume = requestedVolume;
            SwapSources(nextSource);
            return;
        }

        crossfadeCoroutine = StartCoroutine(Crossfade(activeSource, nextSource, crossfadeDuration));
    }

    private IEnumerator Crossfade(AudioSource outgoingSource, AudioSource incomingSource, float duration)
    {
        float outgoingVolume = outgoingSource.volume;
        float elapsedTime = 0f;

        while (elapsedTime < duration)
        {
            elapsedTime += Time.unscaledDeltaTime;
            float progress = Mathf.Clamp01(elapsedTime / duration);
            outgoingSource.volume = Mathf.Lerp(outgoingVolume, 0f, progress);
            incomingSource.volume = Mathf.Lerp(0f, requestedVolume, progress);
            yield return null;
        }

        outgoingSource.Stop();
        incomingSource.volume = requestedVolume;
        SwapSources(incomingSource);
        crossfadeCoroutine = null;
    }

    private void SwapSources(AudioSource nextActiveSource)
    {
        AudioSource previousActiveSource = activeSource;
        activeSource = nextActiveSource;
        standbySource = previousActiveSource;
    }

    private void StopAllMusic()
    {
        StopCrossfade();

        if (firstSource != null)
            firstSource.Stop();

        if (secondSource != null)
            secondSource.Stop();
    }

    private void StopCrossfade()
    {
        if (crossfadeCoroutine == null)
            return;

        StopCoroutine(crossfadeCoroutine);
        crossfadeCoroutine = null;
    }
}
