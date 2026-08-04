using UnityEngine;

public class SettingsPresenter : MonoBehaviour
{
    [SerializeField] private SettingsView view;
    [SerializeField] private SettingsService settingsService;
    [SerializeField] private AudioService audioService;

    private void Awake()
    {
        ResolveReferences();
    }

    private void OnEnable()
    {
        Bind();
        Refresh();
    }

    private void OnDisable()
    {
        Unbind();
    }

    private void ResolveReferences()
    {
        if (view == null)
            view = GetComponent<SettingsView>();

        if (settingsService == null)
            settingsService = FindFirstObjectByType<SettingsService>();

        if (audioService == null)
            audioService = FindFirstObjectByType<AudioService>();
    }

    private void Bind()
    {
        ResolveReferences();
        if (view == null)
            return;

        view.OpenClicked -= OnOpenClicked;
        view.CloseClicked -= OnCloseClicked;
        view.SoundEffectClicked -= OnSoundEffectClicked;
        view.MusicClicked -= OnMusicClicked;
        view.VibrationClicked -= OnVibrationClicked;

        view.OpenClicked += OnOpenClicked;
        view.CloseClicked += OnCloseClicked;
        view.SoundEffectClicked += OnSoundEffectClicked;
        view.MusicClicked += OnMusicClicked;
        view.VibrationClicked += OnVibrationClicked;

        if (settingsService != null)
        {
            settingsService.SettingsChanged -= Refresh;
            settingsService.SettingsChanged += Refresh;
        }
    }

    private void Unbind()
    {
        if (view == null)
        {
            if (settingsService != null)
                settingsService.SettingsChanged -= Refresh;

            return;
        }

        view.OpenClicked -= OnOpenClicked;
        view.CloseClicked -= OnCloseClicked;
        view.SoundEffectClicked -= OnSoundEffectClicked;
        view.MusicClicked -= OnMusicClicked;
        view.VibrationClicked -= OnVibrationClicked;

        if (settingsService != null)
            settingsService.SettingsChanged -= Refresh;
    }

    private void OnOpenClicked()
    {
        if (audioService != null)
            audioService.PlayUi();

        if (view != null)
            view.SetPanelVisible(true);

        Refresh();
    }

    private void OnCloseClicked()
    {
        if (audioService != null)
            audioService.PlayUi();

        if (view != null)
            view.SetPanelVisible(false);
    }

    private void OnSoundEffectClicked()
    {
        ResolveReferences();
        if (settingsService == null)
            return;

        settingsService.SetSoundEffectEnabled(!settingsService.IsSoundEffectEnabled);
        if (audioService != null)
            audioService.PlayUi();

        Refresh();
    }

    private void OnMusicClicked()
    {
        ResolveReferences();
        if (settingsService == null)
            return;

        settingsService.SetMusicEnabled(!settingsService.IsMusicEnabled);
        if (audioService != null)
            audioService.PlayUi();

        Refresh();
    }

    private void OnVibrationClicked()
    {
        ResolveReferences();
        if (settingsService == null)
            return;

        settingsService.SetVibrationEnabled(!settingsService.IsVibrationEnabled);
        if (audioService != null)
            audioService.PlayUi();

        Refresh();
    }

    private void Refresh()
    {
        ResolveReferences();
        if (view == null || settingsService == null)
            return;

        view.SetSoundEffectEnabled(settingsService.IsSoundEffectEnabled);
        view.SetMusicEnabled(settingsService.IsMusicEnabled);
        view.SetVibrationEnabled(settingsService.IsVibrationEnabled);
    }
}
