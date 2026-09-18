using Assets.TJ.Scripts;
using DG.Tweening;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace TJ.Scripts
{
    public class PowerUps : Singleton<PowerUps>
    {
        public PowerUp currentPowerUp = PowerUp.None;
        public int shuffleCarCost;
        public int sortPlayerCost;
        public int vip_Heli_Cost;

        [SerializeField] TextMeshProUGUI title;
        [SerializeField] TextMeshProUGUI info;
        [SerializeField] Image icon;
        public GameObject panel;
        [SerializeField] GameObject background;
        [Header("Panel BTN")]
        [SerializeField] Button panelCloseButton;
        [SerializeField] Button useWithCoinsButton;
        [SerializeField] Button useWithAdsButton;
        [Header("PW BTN")]
        public Button btn_ShuffleVehicles;
        public Button btn_ShufflePlayers;
        [Header("Panel Sprites")]
        [SerializeField] Sprite carShuffleSprite;
        [SerializeField] Sprite playerSortSprite;
        [SerializeField] Sprite vipVehicleSprite;
        [Header("")]
        public GameObject notEnoughCoinsPopup;

        private bool isPanelClosed = false;
        private bool isInfoPlaying = false;

        public Vector3 heliIdlePosition;
        public GameObject heli;

        private void Start()
        {
            InitializeUI();

            btn_ShuffleVehicles.onClick.AddListener(() =>
            {
                ShowCarShufflePanel();
                SoundController.Instance.PlayOneShot(SoundController.Instance.buttonSound, 0.5f);
                Vibration.Vibrate(40);
            });
            btn_ShufflePlayers.onClick.AddListener(() =>
            {
                ShowPlayerSortPanel();
                SoundController.Instance.PlayOneShot(SoundController.Instance.buttonSound, 0.5f);
                Vibration.Vibrate(40);
            });
            panelCloseButton.onClick.AddListener(() =>
            {
                ClosePanel();
                SoundController.Instance.PlayOneShot(SoundController.Instance.buttonSound, 0.5f);
                Vibration.Vibrate(40);
            });

        }
        private void InitializeUI()
        {
            panel.SetActive(false);
            background.SetActive(false);
            panel.transform.localScale = Vector3.zero;
            notEnoughCoinsPopup.transform.localScale = Vector3.zero;
        }

        private void ShowCarShufflePanel()
        {
            if (currentPowerUp != PowerUp.None)
                return;
            SetPowerUpPanel(PowerUp.ShuffleCar, "Shuffle", "Rearrange the <color=green>COLOR</color> of the Vehicles in the parking lot", carShuffleSprite);
            useWithCoinsButton.onClick.AddListener(() => UsePowerUpWithCoins(shuffleCarCost, VehicleController.instance.RandomVehicleColors));
            useWithAdsButton.onClick.AddListener(() =>
            {
                //call the ads here
                ISManager.instance.ShowRewardedVideo(AdState.CarShuffle);
                // call below lines after the ad
            });
        }
        public void CarShuffle_CallBack()
        {
            ClosePanel();
            VehicleController.instance.RandomVehicleColors();
            SoundController.Instance.PlayOneShot(SoundController.Instance.buttonSound, 0.5f);
            Vibration.Vibrate(40);
        }

        private void ShowPlayerSortPanel()
        {
            if (currentPowerUp != PowerUp.None)
                return;
            SetPowerUpPanel(PowerUp.SortPlayers, "Sort", "Sort the <color=green>PASSENGERS</color> according to Vehicle Colors", playerSortSprite);
            useWithCoinsButton.onClick.AddListener(() => UsePowerUpWithCoins(sortPlayerCost, ShufflePlayersPowerUp));
            useWithAdsButton.onClick.AddListener(() =>
            {
                //call the ads
                ISManager.instance.ShowRewardedVideo(AdState.PlayerSort);
                //callback for he powerUp

            });
        }
        public void PlayerSort_CallBack()
        {
            ClosePanel();
            ShufflePlayersPowerUp();
            SoundController.Instance.PlayOneShot(SoundController.Instance.buttonSound, 0.5f);
            Vibration.Vibrate(40);
        }

        public void ShowHelicopterPanel()
        {
            if (currentPowerUp != PowerUp.None)
                return;
            SetPowerUpPanel(PowerUp.Helicopter, "VIP Space", "Choose a <color=green>VEHICLE</color> to the VIP space. It can pick up reqired passengers instantly", vipVehicleSprite);
            useWithCoinsButton.onClick.AddListener(() => UsePowerUpWithCoins(vip_Heli_Cost, VipHeli));
            useWithAdsButton.onClick.AddListener(() =>
            {
                //call the ads
                ISManager.instance.ShowRewardedVideo(AdState.Heli_VIP);
                //callback for he powerUp

            });
        }
        public void Helicopte_Vip_CallBack()
        {
            ClosePanel();
            VipHeli();
            SoundController.Instance.PlayOneShot(SoundController.Instance.buttonSound, 0.5f);
            Vibration.Vibrate(40);
        }


        private void SetPowerUpPanel(PowerUp powerUp, string titleText, string infoText, Sprite iconSprite)
        {
            currentPowerUp = powerUp;
            title.text = titleText;
            info.text = infoText;
            icon.sprite = iconSprite;
            OpenPanel();
        }

        Tween panelTween;

        private void OpenPanel()
        {
            /* if (panelTween != null && panelTween.IsPlaying())
                 return;
 */
            panel.SetActive(true);
            background.SetActive(true);
            panelTween = panel.transform.DOScale(Vector3.one, 0.3f)
                .OnStart(() => { isPanelClosed = false; });
        }

        private void ClosePanel()
        {
            /*if (panelTween != null && panelTween.IsPlaying())
                return;*/

            isPanelClosed = true;
            currentPowerUp = PowerUp.None;
            panelTween = panel.transform.DOScale(Vector3.zero, 0.3f)
                .OnStart(() => { ResetButtonListeners(); })
                .OnComplete(() =>
                {
                    background.SetActive(false);
                    panel.SetActive(false);
                    isPanelClosed = false;
                });
        }

        private void UsePowerUpWithCoins(int cost, System.Action powerUpAction)
        {
            int coins = CoinsManager.Instance.GetTotalCoins();
            if (coins >= cost)
            {
                CoinsManager.Instance.DeductCoins(cost);
                ClosePanel();
                powerUpAction.Invoke();
            }
            else
            {
                currentPowerUp = PowerUp.None;
                PlayInfoPopup("Not Enough Coins!");
                return;
            }
            SoundController.Instance.PlayOneShot(SoundController.Instance.buttonSound, 0.5f);
            Vibration.Vibrate(40);
        }

        private void ShufflePlayersPowerUp()
        {
            var cars = new List<Vehicle>(ParkingManager.Instance.parkedVehicles);
            var players = PlayerManager.instance.playersInScene;
            int totalRemainingSeats = cars.Sum(car => car.SeatCount - car.playersInSeat);

            if (totalRemainingSeats < 24)
            {
                var additionalCars = VehicleController.instance.vehicles
                                    .Where(car => !car.CheckForObstacles())
                                    .ToList();

                foreach (var car in additionalCars)
                {
                    cars.Add(car);
                    totalRemainingSeats += car.SeatCount - car.playersInSeat;
                    if (totalRemainingSeats >= 24) break;
                }
            }

            int playersMatched = 0;
            for (int i = 0; i < cars.Count && playersMatched < 24; i++)
            {
                int remainingSeats = cars[i].SeatCount - cars[i].playersInSeat;
                for (int j = playersMatched; j < players.Count && remainingSeats > 0 && playersMatched < 24; j++)
                {
                    if (cars[i].vehicleColor == players[j].color)
                    {
                        SwapPlayerColors(playersMatched, j);
                        playersMatched++;
                        remainingSeats--;
                    }
                }
            }

            if (!PlayerManager.instance.isColormatched)
                EventManager.OnNewVehArrived?.Invoke();

            currentPowerUp = PowerUp.None;
        }


        private void SwapPlayerColors(int playerIndex1, int playerIndex2)
        {
            var players = PlayerManager.instance.playersInScene;
            var tempColor = players[playerIndex1].color;
            players[playerIndex1].ChangeColor(players[playerIndex2].color);
            players[playerIndex2].ChangeColor(tempColor);
        }

        private void VipHeli()
        {
            currentPowerUp = PowerUp.Helicopter;
            HelicopterController.Instance.ToggleHint(true);
        }
        private void PlayInfoPopup(string message)
        {
            if (isInfoPlaying)
                return;

            isInfoPlaying = true;
            var infoText = notEnoughCoinsPopup.GetComponent<TextMeshProUGUI>();
            infoText.text = message;
            notEnoughCoinsPopup.transform.DOScale(Vector3.one, 0.2f);
            DOVirtual.DelayedCall(2f, () =>
            {
                notEnoughCoinsPopup.transform.DOScale(Vector3.zero, 0.2f).OnComplete(() => isInfoPlaying = false);
            });
            SoundController.Instance.PlayOneShot(SoundController.Instance.nocoinPOP, 0.5f);
            Vibration.Vibrate(40);
        }
        private void ResetButtonListeners()
        {
            useWithCoinsButton.onClick.RemoveAllListeners();
            useWithAdsButton.onClick.RemoveAllListeners();
        }
    }

    public enum PowerUp
    {
        None,
        ShuffleCar,
        SortPlayers,
        Helicopter
    }
}
