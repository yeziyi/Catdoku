using System;
using UnityEngine;
#if ADS_ENABLED
using GoogleMobileAds.Api;
using UnityEngine.Advertisements;
#endif

namespace ColorCubeShooter
{
    public static class RewardAdsService
    {
        static Action<bool> _pendingCallback;

        public static void ShowRewardedAd(Action<bool> onComplete)
        {
#if ADS_ENABLED
            if (AdsControl.Instance == null)
            {
                Debug.LogWarning("RewardAdsService: AdsControl.Instance is missing.");
                onComplete?.Invoke(false);
                return;
            }

            _pendingCallback = onComplete;

            var ads = AdsControl.Instance;
            switch (ads.currentAdsType)
            {
                case AdsControl.ADS_TYPE.ADMOB:
                    TryShowAdMobReward(ads);
                    break;
                case AdsControl.ADS_TYPE.UNITY:
                    TryShowUnityReward(ads);
                    break;
                case AdsControl.ADS_TYPE.MEDIATION:
                    if (ads.rewardedAd != null && ads.rewardedAd.CanShowAd())
                        ads.ShowRewardAd(OnAdMobReward);
                    else
                        TryShowUnityReward(ads);
                    break;
                default:
                    Complete(false);
                    break;
            }
#else
            Debug.Log("RewardAdsService: ADS_ENABLED off — grant reward (dev).");
            onComplete?.Invoke(true);
#endif
        }

#if ADS_ENABLED
        static void TryShowAdMobReward(AdsControl ads)
        {
            if (ads.rewardedAd != null && ads.rewardedAd.CanShowAd())
                ads.ShowRewardAd(OnAdMobReward);
            else
                Complete(false);
        }

        static void OnAdMobReward(Reward reward)
        {
            Complete(true);
        }

        static void TryShowUnityReward(AdsControl ads)
        {
            ads.PlayUnityVideoAd((string id, UnityAdsShowCompletionState state) =>
            {
                if (!id.Equals(ads.adUnityRWUnitId))
                    return;

                Complete(state == UnityAdsShowCompletionState.COMPLETED);
            });
        }

        static void Complete(bool success)
        {
            var callback = _pendingCallback;
            _pendingCallback = null;
            callback?.Invoke(success);
        }
#endif
    }
}
