using System;
using System.Collections;
using System.Collections.Generic;
using GoogleMobileAds.Api;
using GoogleMobileAds.Ump.Api;
using UnityEngine;

public class GoogleAdMobService : MonoBehaviour, IAdService
{
    private const float LoadRetryDelaySeconds = 30f;
    private const string SettingsResourcePath = "SettingDatas/AdMobSettings";

    public static GoogleAdMobService Instance { get; private set; }

    [SerializeField] private AdMobSettings settings;

    private RewardedAd rewardedAd;
    private Coroutine loadRetryCoroutine;
    private Action<bool> showCompleted;
    private readonly Queue<Action> mainThreadActions = new Queue<Action>();
    private readonly object mainThreadActionsLock = new object();
    private bool isInitializing;
    private bool isMobileAdsInitializing;
    private bool isInitialized;
    private bool isLoading;
    private bool isShowing;
    private bool hasEarnedReward;
    private bool isDestroyed;

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
            settings = Resources.Load<AdMobSettings>(SettingsResourcePath);

        Debug.Log(settings == null
            ? "[GoogleAdMobService] Awake completed. AdMobSettings is missing."
            : "[GoogleAdMobService] Awake completed. AdMobSettings loaded.");
    }

    private void Start()
    {
        Debug.Log("[GoogleAdMobService] Start. Requesting consent and ads initialization.");
        RequestConsentAndInitialize();
    }

    private void Update()
    {
        ExecuteQueuedMainThreadActions();
    }

    private void OnDestroy()
    {
        if (Instance != this)
            return;

        isDestroyed = true;
        if (loadRetryCoroutine != null)
            StopCoroutine(loadRetryCoroutine);

        ClearQueuedMainThreadActions();

        Action<bool> pendingShowCompleted = showCompleted;
        showCompleted = null;
        pendingShowCompleted?.Invoke(false);

        DestroyRewardedAd();
        Instance = null;
    }

    public bool IsReady(AdPlacement placement)
    {
        bool canShowAd = rewardedAd != null && rewardedAd.CanShowAd();
        return isInitialized && !isShowing && canShowAd;
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
            bool hasRewardedAd = rewardedAd != null;
            bool canShowAd = rewardedAd != null && rewardedAd.CanShowAd();
            Debug.LogWarning(
                $"[GoogleAdMobService] Rewarded ad is not ready. placement={placement}, " +
                $"isInitialized={isInitialized}, isInitializing={isInitializing}, " +
                $"isMobileAdsInitializing={isMobileAdsInitializing}, isLoading={isLoading}, " +
                $"hasRewardedAd={hasRewardedAd}, canShowAd={canShowAd}");
            LoadRewardedAd();
            completed?.Invoke(false);
            return;
        }

        isShowing = true;
        hasEarnedReward = false;
        showCompleted = completed;
        rewardedAd.Show(reward =>
        {
            RunOnMainThread(() =>
            {
                hasEarnedReward = true;
                Debug.Log($"[GoogleAdMobService] Reward earned. type={reward.Type}, amount={reward.Amount}");
            });
        });
    }

    private void RequestConsentAndInitialize()
    {
        if (isInitializing || isInitialized)
        {
            Debug.Log(
                $"[GoogleAdMobService] Consent request skipped. isInitializing={isInitializing}, isInitialized={isInitialized}");
            return;
        }

        isInitializing = true;
        ConsentRequestParameters requestParameters = new ConsentRequestParameters();
        Debug.Log("[GoogleAdMobService] ConsentInformation.Update started.");
        ConsentInformation.Update(requestParameters, updateError =>
        {
            RunOnMainThread(() =>
            {
                if (updateError != null)
                    Debug.LogWarning($"[GoogleAdMobService] Consent information update failed: {updateError.Message}");
                else
                    Debug.Log("[GoogleAdMobService] Consent information update completed.");

                Debug.Log(
                    $"[GoogleAdMobService] CanRequestAds after consent update: {ConsentInformation.CanRequestAds()}");
                InitializeMobileAds();

                Debug.Log("[GoogleAdMobService] ConsentForm.LoadAndShowConsentFormIfRequired started.");
                ConsentForm.LoadAndShowConsentFormIfRequired(formError =>
                {
                    RunOnMainThread(() =>
                    {
                        if (formError != null)
                            Debug.LogWarning($"[GoogleAdMobService] Consent form failed: {formError.Message}");
                        else
                            Debug.Log("[GoogleAdMobService] Consent form completed or was not required.");

                        Debug.Log(
                            $"[GoogleAdMobService] CanRequestAds after consent form: {ConsentInformation.CanRequestAds()}");
                        InitializeMobileAds();
                    });
                });
            });
        });
    }

    private void InitializeMobileAds()
    {
        if (isMobileAdsInitializing)
        {
            Debug.Log("[GoogleAdMobService] Google Mobile Ads initialization is already in progress.");
            return;
        }

        if (isInitialized)
        {
            Debug.Log("[GoogleAdMobService] Google Mobile Ads initialization skipped because it is already initialized.");
            return;
        }

        if (!ConsentInformation.CanRequestAds())
        {
            isInitializing = false;
            Debug.LogWarning("[GoogleAdMobService] Ads cannot be requested with the current consent status.");
            return;
        }

        isMobileAdsInitializing = true;
        Debug.Log("[GoogleAdMobService] Google Mobile Ads initialization started.");
        MobileAds.Initialize(initializationStatus =>
        {
            RunOnMainThread(() =>
            {
                isInitializing = false;
                isMobileAdsInitializing = false;
                isInitialized = initializationStatus != null;

                if (!isInitialized)
                {
                    Debug.LogWarning("[GoogleAdMobService] Google Mobile Ads initialization failed.");
                    return;
                }

                Debug.Log("[GoogleAdMobService] Google Mobile Ads initialized.");
                LoadRewardedAd();
            });
        });
    }

    private void LoadRewardedAd()
    {
        if (!isInitialized)
        {
            Debug.Log("[GoogleAdMobService] Rewarded load skipped because Mobile Ads is not initialized.");
            return;
        }

        if (isLoading)
        {
            Debug.Log("[GoogleAdMobService] Rewarded load skipped because a load is already in progress.");
            return;
        }

        if (isShowing)
        {
            Debug.Log("[GoogleAdMobService] Rewarded load skipped because an ad is showing.");
            return;
        }

        string adUnitId = GetRewardedAdUnitId();
        if (string.IsNullOrWhiteSpace(adUnitId))
        {
            Debug.LogWarning("[GoogleAdMobService] Rewarded ad unit ID is unavailable.");
            return;
        }

        CancelLoadRetry();
        DestroyRewardedAd();
        isLoading = true;

        Debug.Log($"[GoogleAdMobService] Rewarded ad load started. adUnitId={adUnitId}");
        AdRequest adRequest = new AdRequest();
        RewardedAd.Load(adUnitId, adRequest, (loadedAd, loadError) =>
        {
            RunOnMainThread(() =>
            {
                isLoading = false;

                if (loadError != null || loadedAd == null)
                {
                    string errorMessage = loadError == null ? "Unknown load error" : loadError.ToString();
                    Debug.LogWarning($"[GoogleAdMobService] Rewarded ad load failed: {errorMessage}");
                    ScheduleLoadRetry();
                    return;
                }

                rewardedAd = loadedAd;
                RegisterRewardedEvents(rewardedAd);
                Debug.Log("[GoogleAdMobService] Rewarded ad loaded.");
            });
        });
    }

    private void RegisterRewardedEvents(RewardedAd loadedAd)
    {
        loadedAd.OnAdFullScreenContentClosed += () =>
        {
            Debug.Log("[GoogleAdMobService] Rewarded ad closed.");
            RunOnMainThread(() => CompleteShow(hasEarnedReward));
        };

        loadedAd.OnAdFullScreenContentFailed += showError =>
        {
            RunOnMainThread(() =>
            {
                Debug.LogWarning($"[GoogleAdMobService] Rewarded ad failed to show: {showError}");
                CompleteShow(false);
            });
        };
    }

    private void CompleteShow(bool succeeded)
    {
        if (!isShowing)
            return;

        isShowing = false;
        hasEarnedReward = false;
        Action<bool> completed = showCompleted;
        showCompleted = null;

        DestroyRewardedAd();
        LoadRewardedAd();
        completed?.Invoke(succeeded);
    }

    private string GetRewardedAdUnitId()
    {
        if (settings == null)
            settings = Resources.Load<AdMobSettings>(SettingsResourcePath);

        if (settings == null)
        {
            Debug.LogWarning("[GoogleAdMobService] AdMobSettings asset is missing.");
            return string.Empty;
        }

        return settings.GetAndroidRewardedAdUnitId();
    }

    private void DestroyRewardedAd()
    {
        if (rewardedAd == null)
            return;

        rewardedAd.Destroy();
        rewardedAd = null;
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
        LoadRewardedAd();
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
        if (action == null || isDestroyed)
            return;

        lock (mainThreadActionsLock)
        {
            mainThreadActions.Enqueue(action);
        }
    }

    private void ExecuteQueuedMainThreadActions()
    {
        while (true)
        {
            Action action;
            lock (mainThreadActionsLock)
            {
                if (mainThreadActions.Count <= 0)
                    return;

                action = mainThreadActions.Dequeue();
            }

            action?.Invoke();
        }
    }

    private void ClearQueuedMainThreadActions()
    {
        lock (mainThreadActionsLock)
        {
            mainThreadActions.Clear();
        }
    }
}
