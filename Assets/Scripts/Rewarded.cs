using UnityEngine;
using GoogleMobileAds.Api;

public class Rewarded : MonoBehaviour
{
    public BuneLean bl;
    public bool rewardb = false;
    private int adCounter = 0;
    public bool bonus = false, ai = false;
    public string AI = "Ai";
    public string appId = "ca-app-pub-2448682662093474~5899436859";// "ca-app-pub-2448682662093474~5899436859";


#if UNITY_ANDROID
    private string _adUnitId = "ca-app-pub-2448682662093474/2272328246";
#elif UNITY_IPHONE
  private string _adUnitId = "ca-app-pub-2448682662093474/2272328246";
#else
  private string _adUnitId = "unused";
# endif

    RewardedAd rewardedAd;


    private void Start()
    {
        MobileAds.RaiseAdEventsOnUnityMainThread = true;
        MobileAds.Initialize(initStatus => {
            print("Ads Initialised !!");
            LoadRewardedAd();
        });
        adCounter = PlayerPrefs.GetInt("AdCounter", 0);
        if (PlayerPrefs.HasKey(AI))
        {
            ai = true;
        }
    }

    public void LoadRewardedAd()
    {
        if (rewardedAd != null)
        {
            rewardedAd.Destroy();
            rewardedAd = null;
        }
        var adRequest = new AdRequest();

        RewardedAd.Load(_adUnitId, adRequest, (RewardedAd ad, LoadAdError error) =>
        {
            if (error != null || ad == null)
            {
                print("Rewarded failed to load" + error);
                return;
            }

            print("Rewarded ad loaded !!");
            rewardedAd = ad;
            RewardedAdEvents(rewardedAd);
        });
    }

    void SaveAdCounter()
    {
        PlayerPrefs.SetInt("AdCounter", adCounter);
        PlayerPrefs.Save();
    }

    public void ShowRewardedAd()
    {
        if (ai == false)
        {
            ai = true;
            bl.AdInfoFinish();
            PlayerPrefs.SetInt(AI, 1);
            PlayerPrefs.Save();
        }
        if (rewardedAd != null && rewardedAd.CanShowAd())
        {
            rewardedAd.Show((Reward reward) =>
            {
                rewardb = true;
                adCounter++;
                Debug.Log(adCounter);
                // adCounter deðerini kaydet
                SaveAdCounter();
                if (adCounter >= 5)
                {
                    bonus = true;
                    adCounter = 0;

                    // adCounter deðerini sýfýrlayýp kaydet
                    SaveAdCounter();
                }
            });
        }
        else
        {
            print("Rewarded ad not ready");
            LoadRewardedAd();
        }
    }

    public void RewardedAdEvents(RewardedAd ad)
    {
        // Raised when the ad is estimated to have earned money.
        ad.OnAdPaid += (AdValue adValue) =>
        {
            Debug.Log("Rewarded ad paid {0} {1}." +
                adValue.Value +
                adValue.CurrencyCode);
        };
        // Raised when an impression is recorded for an ad.
        ad.OnAdImpressionRecorded += () =>
        {
            Debug.Log("Rewarded ad recorded an impression.");
        };
        // Raised when a click is recorded for an ad.
        ad.OnAdClicked += () =>
        {
            Debug.Log("Rewarded ad was clicked.");
        };
        // Raised when an ad opened full screen content.
        ad.OnAdFullScreenContentOpened += () =>
        {
            Debug.Log("Rewarded ad full screen content opened.");
        };
        // Raised when the ad closed full screen content.
        ad.OnAdFullScreenContentClosed += () =>
        {
            Debug.Log("Rewarded ad full screen content closed.");
        };
        // Raised when the ad failed to open full screen content.
        ad.OnAdFullScreenContentFailed += (AdError error) =>
        {
            Debug.LogError("Rewarded ad failed to open full screen content " +
                           "with error : " + error);
        };
    }
}