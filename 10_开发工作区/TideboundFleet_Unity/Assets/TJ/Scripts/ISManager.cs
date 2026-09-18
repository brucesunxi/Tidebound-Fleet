using Map;
using System;
using TJ.Scripts;
using UnityEngine;


public enum AdState
{
    None,
    ParkingSlot,
    PlayerSort,
    CarShuffle,
    claim3XCoins,
    Heli_VIP,
    Revive
}

public class ISManager : MonoBehaviour
{
    public static ISManager instance;
    public AdState adState;

    public bool canShowAds, isTesting;
    public bool isInterstitialAdsLoaded, isRewardedAdsLoaded;

    public float timeGap;
    public string appKey, admobID;


    private float _interstitialLoadTime = 0.2f;

    private void Awake()
    {
        if (!instance)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            DestroyImmediate(gameObject);
        }
    }

    private void OnEnable()
    {
        IronSourceEvents.onSdkInitializationCompletedEvent += SdkInitializationCompletedEvent;

        // Interstitial Events Callback
        IronSourceInterstitialEvents.onAdReadyEvent += InterstitialOnAdReadyEvent;
        IronSourceInterstitialEvents.onAdLoadFailedEvent += InterstitialOnAdLoadFailed;
        IronSourceInterstitialEvents.onAdOpenedEvent += InterstitialOnAdOpenedEvent;
        IronSourceInterstitialEvents.onAdClickedEvent += InterstitialOnAdClickedEvent;
        IronSourceInterstitialEvents.onAdShowSucceededEvent += InterstitialOnAdShowSucceededEvent;
        IronSourceInterstitialEvents.onAdShowFailedEvent += InterstitialOnAdShowFailedEvent;
        IronSourceInterstitialEvents.onAdClosedEvent += InterstitialOnAdClosedEvent;

        // Rewarded Video Events Callback
        IronSourceRewardedVideoEvents.onAdUnavailableEvent += RewardedVideoOnAdUnavailable;
        IronSourceRewardedVideoEvents.onAdAvailableEvent += RewardedVideoOnAdAvailable;
        IronSourceRewardedVideoEvents.onAdShowFailedEvent += RewardedVideoOnAdShowFailedEvent;
        IronSourceRewardedVideoEvents.onAdOpenedEvent += RewardedVideoOnAdOpenedEvent;
        IronSourceRewardedVideoEvents.onAdClickedEvent += RewardedVideoOnAdClickedEvent;
        IronSourceRewardedVideoEvents.onAdRewardedEvent += RewardedVideoOnAdRewardedEvent;
        IronSourceRewardedVideoEvents.onAdClosedEvent += RewardedVideoOnAdClosedEvent;
    }

    private void OnDisable()
    {
        // Interstitial Events Callback
        IronSourceInterstitialEvents.onAdReadyEvent -= InterstitialOnAdReadyEvent;
        IronSourceInterstitialEvents.onAdLoadFailedEvent -= InterstitialOnAdLoadFailed;
        IronSourceInterstitialEvents.onAdOpenedEvent -= InterstitialOnAdOpenedEvent;
        IronSourceInterstitialEvents.onAdClickedEvent -= InterstitialOnAdClickedEvent;
        IronSourceInterstitialEvents.onAdShowSucceededEvent -= InterstitialOnAdShowSucceededEvent;
        IronSourceInterstitialEvents.onAdShowFailedEvent -= InterstitialOnAdShowFailedEvent;
        IronSourceInterstitialEvents.onAdClosedEvent -= InterstitialOnAdClosedEvent;

        // Rewarded Video Events Callback
        IronSourceRewardedVideoEvents.onAdOpenedEvent -= RewardedVideoOnAdOpenedEvent;
        IronSourceRewardedVideoEvents.onAdClosedEvent -= RewardedVideoOnAdClosedEvent;
        IronSourceRewardedVideoEvents.onAdAvailableEvent -= RewardedVideoOnAdAvailable;
        IronSourceRewardedVideoEvents.onAdUnavailableEvent -= RewardedVideoOnAdUnavailable;
        IronSourceRewardedVideoEvents.onAdShowFailedEvent -= RewardedVideoOnAdShowFailedEvent;
        IronSourceRewardedVideoEvents.onAdRewardedEvent -= RewardedVideoOnAdRewardedEvent;
        IronSourceRewardedVideoEvents.onAdClickedEvent -= RewardedVideoOnAdClickedEvent;
    }

    private void SdkInitializationCompletedEvent()
    {
        if (isTesting) IronSource.Agent.launchTestSuite();

        if (ConnectedToInternet())
        {
            Invoke(nameof(LoadAds), 1f);
        }
    }

    private void Init()
    {
        IronSource.Agent.init(appKey, IronSourceAdUnits.REWARDED_VIDEO);
        IronSource.Agent.init(appKey, IronSourceAdUnits.INTERSTITIAL);
        IronSource.Agent.init(appKey, IronSourceAdUnits.BANNER);
    }

    private void Start()
    {
        canShowAds = Connectivity();

        if (isTesting)
        {
            IronSource.Agent.validateIntegration();
            IronSource.Agent.shouldTrackNetworkState(true);
            //IronSource.Agent.setMetaData("is_test_suite", "enable"); 
            //IronSource.Agent.setMetaData("Chartboost_Coppa","false");
        }

        Init();
    }

    private void LoadAds()
    {
        LoadBannerAds();
        LoadInterstitialAds();
        ShowBannerAds();
    }

    private void Update()
    {
        canShowAds = Connectivity();
    }

    private bool ConnectedToInternet()
    {
        return Application.internetReachability != NetworkReachability.NotReachable;
    }

    private bool Connectivity()
    {
        return ConnectedToInternet() && TimeGapFinished();
    }

    private bool TimeGapFinished()
    {
        if (timeGap <= 0) return true;

        timeGap -= Time.deltaTime;
        return timeGap <= 0;
    }

    public void ResetAdsTimeLimit(float val) => timeGap = val;

    public void ShowRewardedVideo(AdState AdState)
    {
        if (!ConnectedToInternet())
        {
            Time.timeScale = 1;
            print("No internet");
            UIManager.instance.PlayOneSpaceLeftHint("No Internet!", true);
            return;
        }

        if (!IronSource.Agent.isRewardedVideoAvailable())
        {
            Time.timeScale = 1;
            UIManager.instance.PlayOneSpaceLeftHint("Not available!", true);
            print("Rewarded Ads are Not Ready");
            return;
        }

        print("ShowRewardedVideo::" + IronSource.Agent.isRewardedVideoAvailable());

        timeGap = 5;
        Time.timeScale = 0;
        adState = AdState;
        IronSource.Agent.SetPauseGame(true);
        IronSource.Agent.showRewardedVideo();
    }


    public void RewardCallBacks()
    {
        Time.timeScale = 1;
        var powerUps = FindObjectOfType<PowerUps>();
        switch (adState)
        {
            case AdState.None:
                break;
            case AdState.ParkingSlot:
                ParkingManager.Instance.parkingSlot_Rv.UnlockSlot_Callback();
                ParkingManager.Instance.parkingSlot_Rv.UnlockSlot_Callback();
                break;
            case AdState.PlayerSort:
                powerUps.PlayerSort_CallBack();
                break;
            case AdState.CarShuffle:
                powerUps.CarShuffle_CallBack();
                break;
            case AdState.Heli_VIP:
                powerUps.Helicopte_Vip_CallBack();
                break;
            case AdState.claim3XCoins:
                GiftBox.instance.ClaimWithAds();
                break; 
            case AdState.Revive:
                GameManager.instance.GameRevive_CallBack();
                break; 
      
            default:
                throw new ArgumentOutOfRangeException();
        }

        adState = AdState.None;
    }

    public void LoadInterstitialAds()
    {
        if (!ConnectedToInternet())
            return;

        print("LoadInterstitialAds::" + IronSource.Agent.isInterstitialReady());

        IronSource.Agent.loadInterstitial();
    }

    public void ShowInterstitialAds()
    {
        if (!canShowAds)
        {
            Time.timeScale = 1;
            print("No internet or Still Time to Show Ads");
            if (!IronSource.Agent.isInterstitialReady()) LoadInterstitialAds();
            return;
        }

        if (!IronSource.Agent.isInterstitialReady())
        {
            Time.timeScale = 1;
            LoadInterstitialAds();
            print("Interstitial Ads are not ready");
            return;
        }

        print("ShowInterstitialAds::" + IronSource.Agent.isInterstitialReady());

        Time.timeScale = 0;
        IronSource.Agent.showInterstitial();
        IronSource.Agent.SetPauseGame(true);
    }

    private void LoadBannerAds()
    {
        if (!ConnectedToInternet())
            return;

        print("LoadBannerAds");
        IronSource.Agent.loadBanner(IronSourceBannerSize.SMART, IronSourceBannerPosition.BOTTOM);
    }

    public void ShowBannerAds()
    {
        if (!ConnectedToInternet())
            return;

        IronSource.Agent.displayBanner();
    }

    private void HideBannerAds()
    {
        if (!ConnectedToInternet())
            return;

        IronSource.Agent.hideBanner();
    }


    private void OnApplicationPause(bool pauseStatus)
    {
        IronSource.Agent.onApplicationPause(pauseStatus);
    }

    private void InterstitialOnAdReadyEvent(IronSourceAdInfo adInfo)
    {
        isInterstitialAdsLoaded = true;
        // Invoked when the interstitial ad was loaded Successfully.  
    }

    private void InterstitialOnAdLoadFailed(IronSourceError ironSourceError)
    {
        // Invoked when the initialization process has failed.
        print(ironSourceError);
        Time.timeScale = 1;
        Invoke(nameof(LoadInterstitialAds), _interstitialLoadTime);
        _interstitialLoadTime += 0.1f;
        _interstitialLoadTime = _interstitialLoadTime >= 5f ? 5f : _interstitialLoadTime;
        IronSource.Agent.SetPauseGame(false);
    }

    private static void InterstitialOnAdOpenedEvent(IronSourceAdInfo adInfo)
    {
        // Invoked when the Interstitial Ad Unit has opened. This is the impression indication. 
    }

    private void InterstitialOnAdClickedEvent(IronSourceAdInfo adInfo)
    {
        // Invoked when end user clicked on the interstitial ad
    }

    private void InterstitialOnAdShowFailedEvent(IronSourceError ironSourceError, IronSourceAdInfo adInfo)
    {
        // Invoked when the ad failed to show.
        Time.timeScale = 1;
        print(ironSourceError);
        Invoke(nameof(LoadInterstitialAds), _interstitialLoadTime);
        _interstitialLoadTime += 0.1f;
        _interstitialLoadTime = _interstitialLoadTime >= 5f ? 5f : _interstitialLoadTime;
        IronSource.Agent.SetPauseGame(false);
    }

    private void InterstitialOnAdClosedEvent(IronSourceAdInfo adInfo)
    {
        Time.timeScale = 1;
        LoadInterstitialAds();
        _interstitialLoadTime = 0;
        ResetAdsTimeLimit(30);
        // Invoked when the interstitial ad closed and the user went back to the application screen.
    }

    private void InterstitialOnAdShowSucceededEvent(IronSourceAdInfo adInfo)
    {
        // Invoked before the interstitial ad was opened, and before the InterstitialOnAdOpenedEvent is reported.
        // This callback is not supported by all networks, and we recommend using it only if  
        // it's supported by all networks you included in your build.
    }


    private void RewardedVideoOnAdAvailable(IronSourceAdInfo adInfo)
    {
        // Indicates that there’s an available ad.
        // The adInfo object includes information about the ad that was loaded successfully
        // This replaces the RewardedVideoAvailabilityChangedEvent(true) event
        print(adInfo);
        isRewardedAdsLoaded = true;
    }


    private void RewardedVideoOnAdUnavailable()
    {
        // Indicates that no ads are available to be displayed
        // This replaces the RewardedVideoAvailabilityChangedEvent(false) event
        isRewardedAdsLoaded = false;
        print("Rewarded Ad Unavailable+::");
    }


    private void RewardedVideoOnAdOpenedEvent(IronSourceAdInfo adInfo)
    {
        // The Rewarded Video ad view has opened. Your activity will loose focus.
        print(adInfo + "Opened");
    }


    private void RewardedVideoOnAdClosedEvent(IronSourceAdInfo adInfo)
    {
        Time.timeScale = 1;
        print(adInfo + "About to be closed::");
        // The Rewarded Video ad view is about to be closed. Your activity will regain its focus.
    }


    private void RewardedVideoOnAdRewardedEvent(IronSourcePlacement placement, IronSourceAdInfo adInfo)
    {
        // place rewards
        // The user completed to watch the video, and should be rewarded.
        // The placement parameter will include the reward data.
        // When using server-to-server callbacks, you may ignore this event and wait for the ironSource server callback.

        RewardCallBacks();
        IronSource.Agent.SetPauseGame(false);
    }


    private void RewardedVideoOnAdShowFailedEvent(IronSourceError error, IronSourceAdInfo adInfo)
    {
        print(error);
        Time.timeScale = 1;
        IronSource.Agent.SetPauseGame(false);
        // The rewarded video ad was failed to show.
    }

    private static void RewardedVideoOnAdClickedEvent(IronSourcePlacement placement, IronSourceAdInfo adInfo)
    {
        // Invoked when the video ad was clicked.
        // This callback is not supported by all networks, and we recommend using it only if
        // it’s supported by all networks you included in your build.
    }
}