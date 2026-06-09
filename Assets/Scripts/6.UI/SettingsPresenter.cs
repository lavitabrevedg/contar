using UnityEngine;

public class SettingsPresenter : MonoBehaviour
{
    [SerializeField] private SettingsView view;
    [SerializeField] private SettingsService settingsService;
    [SerializeField] private StageProgressService progressService;
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

        if (progressService == null)
            progressService = FindFirstObjectByType<StageProgressService>();

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
        view.SoundClicked -= OnSoundClicked;
        view.ResetProgressClicked -= OnResetProgressClicked;

        view.OpenClicked += OnOpenClicked;
        view.CloseClicked += OnCloseClicked;
        view.SoundClicked += OnSoundClicked;
        view.ResetProgressClicked += OnResetProgressClicked;

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
        view.SoundClicked -= OnSoundClicked;
        view.ResetProgressClicked -= OnResetProgressClicked;

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

    private void OnSoundClicked()
    {
        ResolveReferences();
        if (settingsService == null)
            return;

        settingsService.SetSoundEnabled(!settingsService.IsSoundEnabled);
        if (audioService != null)
            audioService.PlayUi();

        Refresh();
    }

    private void OnResetProgressClicked()
    {
        ResolveReferences();
        if (progressService != null)
            progressService.ResetProgress();

        if (audioService != null)
            audioService.PlayUi();

        Refresh();
    }

    private void Refresh()
    {
        ResolveReferences();
        if (view == null || settingsService == null)
            return;

        view.SetSoundEnabled(settingsService.IsSoundEnabled);
    }
}
