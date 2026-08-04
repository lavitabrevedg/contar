using UnityEngine;

[CreateAssetMenu(fileName = "AdMobSettings", menuName = "contar/AdMob Settings")]
public class AdMobSettings : ScriptableObject
{
    private const string AndroidTestRewardedAdUnitId = "ca-app-pub-3940256099942544/5224354917";

    [SerializeField] private string androidRewardedAdUnitId;
    [SerializeField] private bool useTestAdsInDevelopmentBuild = true;

    public string GetAndroidRewardedAdUnitId()
    {
#if UNITY_EDITOR
        if (!useTestAdsInDevelopmentBuild)
            Debug.LogWarning("[AdMobSettings] The Unity Editor always uses Google's rewarded test ad unit ID.");

        return AndroidTestRewardedAdUnitId;
#else
        if (Debug.isDebugBuild && useTestAdsInDevelopmentBuild)
            return AndroidTestRewardedAdUnitId;

        if (string.IsNullOrWhiteSpace(androidRewardedAdUnitId))
        {
            Debug.LogWarning("[AdMobSettings] Android rewarded ad unit ID is empty. Using the Google test ad unit ID.");
            return AndroidTestRewardedAdUnitId;
        }

        return androidRewardedAdUnitId.Trim();
#endif
    }
}
