using System;
using UnityEngine;

public class SettingsService : MonoBehaviour
{
    private const string SoundEffectEnabledKey = "contar.settings.soundEnabled";
    private const string MusicEnabledKey = "contar.settings.musicEnabled";
    private const string VibrationEnabledKey = "contar.settings.vibrationEnabled";
    private const int EnabledValue = 1;
    private const int DisabledValue = 0;

    public bool IsSoundEffectEnabled { get; private set; } = true;
    public bool IsMusicEnabled { get; private set; } = true;
    public bool IsVibrationEnabled { get; private set; } = true;

    public event Action SettingsChanged;

    private void Awake()
    {
        Load();
    }

    public void Load()
    {
        IsSoundEffectEnabled = PlayerPrefs.GetInt(SoundEffectEnabledKey, EnabledValue) == EnabledValue;
        IsMusicEnabled = PlayerPrefs.GetInt(MusicEnabledKey, EnabledValue) == EnabledValue;
        IsVibrationEnabled = PlayerPrefs.GetInt(VibrationEnabledKey, EnabledValue) == EnabledValue;
        NotifySettingsChanged();
    }

    public void SetSoundEffectEnabled(bool isEnabled)
    {
        if (IsSoundEffectEnabled == isEnabled)
            return;

        IsSoundEffectEnabled = isEnabled;
        Save();
    }

    public void SetMusicEnabled(bool isEnabled)
    {
        if (IsMusicEnabled == isEnabled)
            return;

        IsMusicEnabled = isEnabled;
        Save();
    }

    public void SetVibrationEnabled(bool isEnabled)
    {
        if (IsVibrationEnabled == isEnabled)
            return;

        IsVibrationEnabled = isEnabled;
        Save();
    }

    private void Save()
    {
        PlayerPrefs.SetInt(SoundEffectEnabledKey, IsSoundEffectEnabled ? EnabledValue : DisabledValue);
        PlayerPrefs.SetInt(MusicEnabledKey, IsMusicEnabled ? EnabledValue : DisabledValue);
        PlayerPrefs.SetInt(VibrationEnabledKey, IsVibrationEnabled ? EnabledValue : DisabledValue);
        PlayerPrefs.Save();
        NotifySettingsChanged();
    }

    private void NotifySettingsChanged()
    {
        SettingsChanged?.Invoke();
    }
}
