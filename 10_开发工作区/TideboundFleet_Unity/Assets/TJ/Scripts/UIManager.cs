using System.Collections;
using AssetKits.ParticleImage;
using Assets.TJ.Scripts;
using DG.Tweening;
using PluginScripts;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

namespace TJ.Scripts
{
    public class UIManager : MonoBehaviour
    {
        public static UIManager instance;
        public GameObject gameOverPanle, winPanel;

        [SerializeField] private Button restartButtonForTest;
        [SerializeField] private Button btnRestart;
        [SerializeField] private Button btnNext;
        [SerializeField] private Button skipButton;
        [SerializeField] private Button reviveButton;
        [SerializeField] private TextMeshProUGUI levelText;
        [SerializeField] GameObject oneSpaceLeftHint;
        [SerializeField] private ParticleImage coinEffect;
        public int levelNo;
        private void Awake()
        {
            instance = this;
        }

        private void Start()
        {
            //Restart For Testing
            restartButtonForTest.onClick.AddListener(() =>
            {
                Vibration.Vibrate(40);
                SoundController.Instance.PlayOneShot(SoundController.Instance.buttonSound);
                DOVirtual.DelayedCall(0.3f, LevelManager.ReloadLevel);
            });
            //gameover panel restart Button
            btnRestart.onClick.AddListener(() =>
            {
                Vibration.Vibrate(40);
                SoundController.Instance.PlayOneShot(SoundController.Instance.buttonSound);
                DOVirtual.DelayedCall(0.3f, LevelManager.ReloadLevel);
            });
            //next button for WinPanel
            btnNext.onClick.AddListener(() =>
            {
                StartCoroutine(NextLevel());
            });
            skipButton.onClick.AddListener(() =>
            {
                LevelManager.LevelProgressed();
                SoundController.Instance.PlayOneShot(SoundController.Instance.buttonSound);
                DOVirtual.DelayedCall(0.3f, LevelManager.LoadScene);
            });
            reviveButton.onClick.AddListener(() =>
            {
                Vibration.Vibrate(40);
                SoundController.Instance.PlayOneShot(SoundController.Instance.buttonSound);
                ISManager.instance.ShowRewardedVideo(AdState.Revive);
                //GameManager.instance.GameRevive_CallBack();
            });

            levelText.text = "Level " + LevelManager.GetCurrentLeveLNumber();
            SupersonicManager.Instance.LevelStart(LevelManager.GetCurrentLeveLNumber());
        }
        
        private IEnumerator NextLevel()
        {
            btnNext.interactable = false;
            Vibration.Vibrate(40);
            SoundController.Instance.PlayOneShot(SoundController.Instance.buttonSound);
            coinEffect.Play();
            yield return new WaitForSeconds(3f);
            CoinsManager.Instance.AddCoins(50);
            yield return new WaitForSeconds(0.5f);
            LevelManager.LoadScene();
        }

        private bool _canPlayHint = true;
        private Tween _anim;
        public void PlayOneSpaceLeftHint(string hintTxt, bool playInstantly)
        {
            if (playInstantly)
            {
                _anim.Kill();
                oneSpaceLeftHint.transform.localScale = Vector3.zero;
                _canPlayHint = true;
            }
            if (!_canPlayHint)
                return;
            _canPlayHint = false;
            TextMeshProUGUI txt = oneSpaceLeftHint.GetComponentInChildren<TextMeshProUGUI>();
            txt.text = hintTxt;
            oneSpaceLeftHint.SetActive(true);
            _anim = oneSpaceLeftHint.transform.DOScale(Vector3.one, 0.2f).SetDelay(0.1f).OnComplete(() =>
            {
                _anim = oneSpaceLeftHint.transform.DOScale(Vector3.zero, 0.3f).SetDelay(1.5f).OnComplete(() =>
                 {
                     _canPlayHint = true;
                     oneSpaceLeftHint.SetActive(false);
                 });
            });
        }
        public void TogglePanel(GameObject panel, bool value)
        {
            panel.SetActive(value);
        }
        private void Update()
        {
            levelNo = PlayerPrefs.GetInt(PlayerPrefsManager.LevelProgress, 1);
        }
    }
}
