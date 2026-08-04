using UnityEngine;

public class HapticService : MonoBehaviour
{
    [SerializeField] private SettingsService settingsService;
    [SerializeField] private bool isEnabled = true;

    private void Awake()
    {
        ResolveReferences();
    }

    public void PlayBlocked()
    {
        if (!CanVibrate())
            return;

        Handheld.Vibrate();
    }

    private void ResolveReferences()
    {
        if (settingsService == null)
            settingsService = FindFirstObjectByType<SettingsService>();
    }

    private bool CanVibrate()
    {
        if (!isEnabled)
            return false;

        ResolveReferences();
        if (settingsService != null && !settingsService.IsVibrationEnabled)
            return false;

#if UNITY_ANDROID || UNITY_IOS
        return Application.isMobilePlatform;
#else
        return false;
#endif
    }
}
