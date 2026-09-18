using UnityEngine;
using TJ.Scripts;
using TMPro;

namespace Assets.TJ.Scripts
{
    public class CoinsManager : Singleton<CoinsManager>
    {
        [SerializeField] TextMeshProUGUI coinTxt;

        private int totalCoins;
        int defaultcoins = 0;

        private void Start()
        {
            defaultcoins = 10000;
/*#if UNITY_EDITOR
            defaultcoins = 999999;
#endif*/
            UpdateCoinTxt();
        }
        public int GetTotalCoins()
        {
            return PlayerPrefs.GetInt(PlayerPrefsManager.TotalCoins, defaultcoins);
        }

        public void AddCoins(int amount)
        {
            int coins = GetTotalCoins();
            coins += amount;
            PlayerPrefs.SetInt(PlayerPrefsManager.TotalCoins, coins);
            UpdateCoinTxt();
        }
        public void DeductCoins(int amount)
        {
            int coins = GetTotalCoins();
            coins -= amount;
            PlayerPrefs.SetInt(PlayerPrefsManager.TotalCoins, coins);
            UpdateCoinTxt();
        }
        public void UpdateCoinTxt()
        {
            totalCoins = GetTotalCoins();
            coinTxt.text = totalCoins.ToString();
        }
    }
}