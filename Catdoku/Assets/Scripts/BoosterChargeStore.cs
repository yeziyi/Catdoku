using UnityEngine;

namespace ColorCubeShooter
{
    public class BoosterChargeStore
    {
        public const int DefaultCharges = 5;

        const string HintChargesKey = "booster_hint_charges";
        const string FindCatChargesKey = "booster_find_cat_charges";

        public int GetHintCharges()
        {
            return PlayerPrefs.GetInt(HintChargesKey, DefaultCharges);
        }

        public int GetFindCatCharges()
        {
            return PlayerPrefs.GetInt(FindCatChargesKey, DefaultCharges);
        }

        public bool TryConsumeHint()
        {
            var charges = GetHintCharges();
            if (charges <= 0) return false;

            SetHintCharges(charges - 1);
            return true;
        }

        public bool TryConsumeFindCat()
        {
            var charges = GetFindCatCharges();
            if (charges <= 0) return false;

            SetFindCatCharges(charges - 1);
            return true;
        }

        public void AddHintCharge()
        {
            SetHintCharges(GetHintCharges() + 1);
        }

        public void AddFindCatCharge()
        {
            SetFindCatCharges(GetFindCatCharges() + 1);
        }

        static void SetHintCharges(int charges)
        {
            PlayerPrefs.SetInt(HintChargesKey, Mathf.Max(0, charges));
            PlayerPrefs.Save();
        }

        static void SetFindCatCharges(int charges)
        {
            PlayerPrefs.SetInt(FindCatChargesKey, Mathf.Max(0, charges));
            PlayerPrefs.Save();
        }
    }
}
