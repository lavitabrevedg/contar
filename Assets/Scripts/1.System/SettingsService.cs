using System;
using UnityEngine;

public class SettingsService : MonoBehaviour
{
    private const string SoundEnabledKey = "contar.settings.soundEnabled";
    private const int EnabledValue = 1;
    private const int DisabledValue = 0;

    public bool IsSoundEnabled { get; private set; } = true;

    public event Action SettingsChanged;

    private void Awake()
    {
        Load();
    }

    public void Load()
    {
        IsSoundEnabled = PlayerPrefs.GetInt(SoundEnabledKey, EnabledValue) == EnabledValue;
        NotifySettingsChanged();
    }

    public void SetSoundEnabled(bool isEnabled)
    {
        if (IsSoundEnabled == isEnabled)
            return;

        IsSoundEnabled = isEnabled;
        Save();
    }

    private void Save()
    {
        PlayerPrefs.SetInt(SoundEnabledKey, IsSoundEnabled ? EnabledValue : DisabledValue);
        PlayerPrefs.Save();
        NotifySettingsChanged();
    }

    private void NotifySettingsChanged()
    {
        SettingsChanged?.Invoke();
    }
}
