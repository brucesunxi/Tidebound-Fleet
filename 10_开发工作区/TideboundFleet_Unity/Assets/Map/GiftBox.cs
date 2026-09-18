using System.Collections.Generic;
using Assets.TJ.Scripts;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Random = UnityEngine.Random;

namespace Map
{
    public class GiftBox : MonoBehaviour
    {
        public static GiftBox instance;
        public TextMeshProUGUI coinText;

        int coinCount = 0;

        //PowerUps powerup = PowerUps.Helicopter;
        int powerUpCount = 0;

        public List<Sprite> powerUpSprites;
        public Image powerUpImage;
        public TextMeshProUGUI powerUpCountText;
        public AudioSource audioSource;
        public AudioClip openBoxClip;
        public AudioClip itemDropClip;
        public AudioClip buttonClickClip;
        [SerializeField] private int coins;
        [SerializeField] Button claim3XCoinsbtn;

        bool claimed = false;
        private void Awake()
        {
            instance = this;
        }

        private void Start()
        {
            audioSource = transform.parent.GetComponent<AudioSource>();
            gameObject.transform.parent.localScale = Vector3.one;
            coins = Random.Range(25, 50);
            coinText.text = coins + "";
            claim3XCoinsbtn.onClick.AddListener(()=>ISManager.instance.ShowRewardedVideo(AdState.claim3XCoins));
        }
        public void ClaimGiftBox()
        {
            if (claimed)
                return;
            claimed = true;
            // AddCoins(coinCount);
            audioSource.PlayOneShot(buttonClickClip);
            Vibration.Vibrate(30);
            LevelMapInstantiator.Instance.SetContentHeight();
            DOVirtual.DelayedCall(0.2f, () =>
            {
                CoinsManager.Instance.AddCoins(coins);
                LevelMapInstantiator.Instance.coins.text = CoinsManager.Instance.GetTotalCoins() + "";
            });
            DOVirtual.DelayedCall(0.6f, () =>
            {
                transform.parent.gameObject.SetActive(false);
            });
        }
        
        public void ClaimWithAds()
        {
            if (claimed)
                return;
            claimed = true;

            // call rewarded ads here 

            // give 3X coins to the player after the add
            audioSource.PlayOneShot(buttonClickClip);
            Vibration.Vibrate(30);
            LevelMapInstantiator.Instance.SetContentHeight();
            DOVirtual.DelayedCall(0.2f, () =>
            {
                CoinsManager.Instance.AddCoins(coins * 3);
                LevelMapInstantiator.Instance.coins.text = CoinsManager.Instance.GetTotalCoins() + "";
            });
            DOVirtual.DelayedCall(0.6f, () =>
            {
                transform.parent.gameObject.SetActive(false);
            });
        }
        public void OpenBoxSoundPlay()
        {
            audioSource.PlayOneShot(openBoxClip);
        }
        public void DropItemSoundPlay()
        {
            audioSource.PlayOneShot(itemDropClip);
        }
    }
}
