using System;
using System.Collections;
using GoogleMobileAds.Api;
using GoogleMobileAds.Common;
using GoogleMobileAds.Ump.Api;
using UnityEngine;

public class GoogleAdMobService : MonoBehaviour, IAdService
{
    private const float LoadRetryDelaySeconds = 30f;

    public static GoogleAdMobService Instance { get; private set; }

    [SerializeField] private AdMobSettings settings;

    private InterstitialAd interstitialAd;
    private Coroutine loadRetryCoroutine;
    private Action<bool> showCompleted;
    private bool isInitializing;
    private bool isInitialized;
    private bool isLoading;
    private bool isShowing;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        if (settings == null)
            settings = Resources.Load<AdMobSettings>("AdMobSettings");
    }

    private void Start()
    {
        RequestConsentAndInitialize();
    }

    private void OnDestroy()
    {
        if (Instance != this)
            return;

        if (loadRetryCoroutine != null)
            StopCoroutine(loadRetryCoroutine);

        Action<bool> pendingShowCompleted = showCompleted;
        showCompleted = null;
        pendingShowCompleted?.Invoke(false);

        DestroyInterstitialAd();
        Instance = null;
    }

    public bool IsReady(AdPlacement placement)
    {
        return isInitialized &&
               !isShowing &&
               interstitialAd != null &&
               interstitialAd.CanShowAd();
    }

    public void Show(AdPlacement placement, Action<bool> completed)
    {
        if (isShowing)
        {
            completed?.Invoke(false);
            return;
        }

        if (!IsReady(placement))
        {
            Debug.LogWarning($"[GoogleAdMobService] Interstitial ad is not ready. placement={placement}");
            LoadInterstitialAd();
            completed?.Invoke(false);
            return;
        }

        isShowing = true;
        showCompleted = completed;
        interstitialAd.Show();
    }

    private void RequestConsentAndInitialize()
    {
        if (isInitializing || isInitialized)
            return;

        isInitializing = true;
        ConsentRequestParameters requestParameters = new ConsentRequestParameters();
        ConsentInformation.Update(requestParameters, updateError =>
        {
            RunOnMainThread(() =>
            {
                if (updateError != null)
                    Debug.LogWarning($"[GoogleAdMobService] Consent information update failed: {updateError.Message}");

                ConsentForm.LoadAndShowConsentFormIfRequired(formError =>
                {
                    RunOnMainThread(() =>
                    {
                        if (formError != null)
                            Debug.LogWarning($"[GoogleAdMobService] Consent form failed: {formError.Message}");

                        InitializeMobileAds();
                    });
                });
            });
        });
    }

    private void InitializeMobileAds()
    {
        if (isInitialized)
            return;

        if (!ConsentInformation.CanRequestAds())
        {
            isInitializing = false;
            Debug.LogWarning("[GoogleAdMobService] Ads cannot be requested with the current consent status.");
            return;
        }

        MobileAds.Initialize(initializationStatus =>
        {
            RunOnMainThread(() =>
            {
                isInitializing = false;
                isInitialized = initializationStatus != null;

                if (!isInitialized)
                {
                    Debug.LogWarning("[GoogleAdMobService] Google Mobile Ads initialization failed.");
                    return;
                }

                Debug.Log("[GoogleAdMobService] Google Mobile Ads initialized.");
                LoadInterstitialAd();
            });
        });
    }

    private void LoadInterstitialAd()
    {
        if (!isInitialized || isLoading || isShowing)
            return;

        string adUnitId = GetInterstitialAdUnitId();
        if (string.IsNullOrWhiteSpace(adUnitId))
        {
            Debug.LogWarning("[GoogleAdMobService] Interstitial ad unit ID is unavailable.");
            return;
        }

        CancelLoadRetry();
        DestroyInterstitialAd();
        isLoading = true;

        AdRequest adRequest = new AdRequest();
        InterstitialAd.Load(adUnitId, adRequest, (loadedAd, loadError) =>
        {
            RunOnMainThread(() =>
            {
                isLoading = false;

                if (loadError != null || loadedAd == null)
                {
                    string errorMessage = loadError == null ? "Unknown load error" : loadError.ToString();
                    Debug.LogWarning($"[GoogleAdMobService] Interstitial ad load failed: {errorMessage}");
                    ScheduleLoadRetry();
                    return;
                }

                interstitialAd = loadedAd;
                RegisterInterstitialEvents(interstitialAd);
                Debug.Log("[GoogleAdMobService] Interstitial ad loaded.");
            });
        });
    }

    private void RegisterInterstitialEvents(InterstitialAd loadedAd)
    {
        loadedAd.OnAdFullScreenContentClosed += () =>
        {
            RunOnMainThread(() => CompleteShow(true));
        };

        loadedAd.OnAdFullScreenContentFailed += showError =>
        {
            RunOnMainThread(() =>
            {
                Debug.LogWarning($"[GoogleAdMobService] Interstitial ad failed to show: {showError}");
                CompleteShow(false);
            });
        };
    }

    private void CompleteShow(bool succeeded)
    {
        if (!isShowing)
            return;

        isShowing = false;
        Action<bool> completed = showCompleted;
        showCompleted = null;

        DestroyInterstitialAd();
        LoadInterstitialAd();
        completed?.Invoke(succeeded);
    }

    private string GetInterstitialAdUnitId()
    {
        if (settings == null)
            settings = Resources.Load<AdMobSettings>("AdMobSettings");

        if (settings == null)
        {
            Debug.LogWarning("[GoogleAdMobService] AdMobSettings asset is missing.");
            return string.Empty;
        }

        return settings.GetAndroidInterstitialAdUnitId();
    }

    private void DestroyInterstitialAd()
    {
        if (interstitialAd == null)
            return;

        interstitialAd.Destroy();
        interstitialAd = null;
    }

    private void ScheduleLoadRetry()
    {
        if (!isActiveAndEnabled || loadRetryCoroutine != null)
            return;

        loadRetryCoroutine = StartCoroutine(RetryLoadAfterDelay());
    }

    private IEnumerator RetryLoadAfterDelay()
    {
        yield return new WaitForSecondsRealtime(LoadRetryDelaySeconds);
        loadRetryCoroutine = null;
        LoadInterstitialAd();
    }

    private void CancelLoadRetry()
    {
        if (loadRetryCoroutine == null)
            return;

        StopCoroutine(loadRetryCoroutine);
        loadRetryCoroutine = null;
    }

    private void RunOnMainThread(Action action)
    {
        if (action == null)
            return;

        MobileAdsEventExecutor.ExecuteInUpdate(action);
    }
}
