using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using TJ.Scripts;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Map
{
    public class LevelMapInstantiator : Singleton<LevelMapInstantiator>
    {
        public GameObject levelImages;

        public GameObject normalLevel;
        public GameObject prevLevel;
        public GameObject currLevel;
        public GameObject bossLevel;
        public GameObject giftLevel;

        public GameObject powerUpRef;
        public List<Sprite> powerUpsSprites;
        public List<int> refLevels;

        public Sprite prevSprite;
        public Sprite currSprite;
        public Color prevCol;

        public bool isNewLevel = false;

        public int totalLevelCount = 0;
        public int level = 0;
        public int startLevel = 0;
        public Vector3 firstPos;
        public Vector3 firstPos02;

        bool isGiftBox = false;
        public Vector3 secondPos;
        public Vector3 secondPos02;
        [SerializeField] private GiftBox _giftBox;
        [SerializeField] private Button _playButton;
        Vector3 startPos;
        Vector3 endPos;

        [SerializeField] public TextMeshProUGUI coins;

        void Start()
        {
            Vibration.Init();
            _playButton.onClick.AddListener(() =>
            {
                Vibration.Vibrate(40);
                SoundController.Instance.PlayOneShot(SoundController.Instance.buttonSound);
                DOVirtual.DelayedCall(0.2f, LevelManager.LoadLevel);
            });
            /*Vibration.Init();
        coins.text = PlayerPrefs.GetInt(PlayerPrefsManager.TotalCoins, 0)+"";
        _playButton.onClick.AddListener(()=>
        {
            Vibration.Vibrate(20);
            SoundController.Instance.PlayAudio(SoundController.Instance.buttonClick);
            DOVirtual.DelayedCall(0.2f, LevelManager.LoadLevel);
        });*/
            isNewLevel = false;
            level = PlayerPrefs.GetInt(PlayerPrefsManager.LevelProgress, 1);
            if (!PlayerPrefs.HasKey("RoadLevel"))
            {
                PlayerPrefs.SetInt("RoadLevel", 1);
            }

            if (level != PlayerPrefs.GetInt("RoadLevel"))
            {
                if ((level - 1) % 5 == 0 && (level - 1) % 10 != 0 && level > 4)
                {
                }

                isNewLevel = true;
                PlayerPrefs.SetInt("RoadLevel", level);
            }

            if (level < 21)
            {
                startLevel = 1;
                totalLevelCount += level;
            }
            else
            {
                startLevel = level - 20;
                totalLevelCount += 20;
            }

            totalLevelCount += 60;

            if (!isGiftBox)
            {
                SetContentHeight();
            }

            if ((level - 1) % 10 == 0 && PlayerPrefs.GetInt(PlayerPrefsManager.giftClaimed) == 0 && level != 1)
            {
                OpenGiftBox();
            }
        }

        private void OpenGiftBox()
        {
            print(PlayerPrefs.GetInt(PlayerPrefsManager.LevelProgress, 1) + "  ll" + _giftBox.transform.parent.name);
            _giftBox.transform.parent.gameObject.SetActive(true);
            PlayerPrefs.SetInt(PlayerPrefsManager.giftClaimed, 1);
        }

        public void SetContentHeight()
        {
            var totalContentCount = (float)totalLevelCount;
            if (totalContentCount > 75.0f)
            {
                totalContentCount = 75;
            }

            if (level < 16)
            {
                totalContentCount -= 1.5f;
            }


            var vert = GetComponent<VerticalLayoutGroup>();

            var child = levelImages.transform as RectTransform;

            float scrollHeight = 0f;
            if (child != null) scrollHeight = (child.rect.height + vert.spacing) * (totalContentCount);


            var rect = GetComponent<RectTransform>().sizeDelta;
            GetComponent<RectTransform>().sizeDelta = new Vector2(rect.x, scrollHeight);


            if (level < 21)
            {
                GetComponent<RectTransform>().anchoredPosition = firstPos;
                startPos = firstPos02;
                endPos = firstPos;
            }
            else
            {
                GetComponent<RectTransform>().anchoredPosition = secondPos;
                startPos = secondPos02;
                endPos = secondPos;
            }

            SetLevelImages();
        }

        private void SetLevelImages()
        {
            Transform oldLevel = null;
            var imagesNoToInstantiate = totalLevelCount;
            for (int i = 0; i < imagesNoToInstantiate; i++)
            {
                levelImages = normalLevel;
                if ((i + startLevel) % 10 == 0 && (i + startLevel) >= level)
                {
                    levelImages = giftLevel;
                }

                if (this == null) continue;
                var image = Instantiate(levelImages, transform, true);
                image.name = levelImages.name + (i + startLevel).ToString();
                //image.transform.localScale = Vector3.one * 3;
                image.GetComponentInChildren<TextMeshProUGUI>().text = (i + startLevel).ToString();
                if (level == i + startLevel)
                {
                    if (level % 10 != 0)
                    {
                        image.transform.GetChild(1).GetComponent<Image>().sprite = currSprite;
                        image.transform.GetComponentInChildren<TextMeshProUGUI>().color = Color.white;
                    }

                    if (isNewLevel)
                    {
                        StartCoroutine(NextLevelAnimation(oldLevel, image.transform));
                    }
                    else
                    {
                        image.transform.GetChild(1).GetComponent<RectTransform>().localScale = Vector3.one * 2f;
                    }
                }
            }
        }

        private IEnumerator NextLevelAnimation(Transform oldLevel, Transform newLevel)
        {
            GetComponent<RectTransform>().anchoredPosition = startPos;
            if (oldLevel) oldLevel.GetChild(1).GetComponent<RectTransform>().localScale = Vector3.one * 2f;
            newLevel.GetChild(1).GetComponent<RectTransform>().localScale = Vector3.one * 1.4f;
            yield return new WaitForSeconds(0.4f);
            float elapsedTime = 0f;

            while (elapsedTime < 1f)
            {
                elapsedTime += Time.deltaTime;
                if (oldLevel)
                    oldLevel.GetChild(1).GetComponent<RectTransform>().localScale =
                        Vector3.Lerp(Vector3.one * 2f, Vector3.one * 1.4f, elapsedTime);
                newLevel.GetChild(1).GetComponent<RectTransform>().localScale =
                    Vector3.Lerp(Vector3.one * 1.4f, Vector3.one * 2f, elapsedTime);
                GetComponent<RectTransform>().anchoredPosition = Vector3.Lerp(startPos, endPos, elapsedTime);

                yield return null;
            }

            GetComponent<RectTransform>().anchoredPosition = endPos;
        }
    }
}