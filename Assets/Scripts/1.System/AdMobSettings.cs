using UnityEngine;

[CreateAssetMenu(fileName = "AdMobSettings", menuName = "contar/AdMob Settings")]
public class AdMobSettings : ScriptableObject
{
    private const string AndroidTestInterstitialAdUnitId = "ca-app-pub-3940256099942544/1033173712";

    [SerializeField] private string androidInterstitialAdUnitId;
    [SerializeField] private bool useTestAdsInDevelopmentBuild = true;

    public string GetAndroidInterstitialAdUnitId()
    {
#if UNITY_EDITOR
        return AndroidTestInterstitialAdUnitId;
#else
        if (Debug.isDebugBuild && useTestAdsInDevelopmentBuild)
            return AndroidTestInterstitialAdUnitId;

        if (string.IsNullOrWhiteSpace(androidInterstitialAdUnitId))
        {
            Debug.LogWarning("[AdMobSettings] Android interstitial ad unit ID is empty. Using the Google test ad unit ID.");
            return AndroidTestInterstitialAdUnitId;
        }

        return androidInterstitialAdUnitId.Trim();
#endif
    }
}
