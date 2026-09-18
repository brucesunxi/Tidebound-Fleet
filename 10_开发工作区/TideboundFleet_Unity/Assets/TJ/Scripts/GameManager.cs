using System.Collections;
using DG.Tweening;
using PluginScripts;
using UnityEngine;

namespace TJ.Scripts
{
    public class GameManager : MonoBehaviour
    {
        public static GameManager instance;

        public MaterialHolder vehMaterialHolder;
        public MaterialHolder stickmanMaterialHolder;
        public Transform train;
        public int winCount = 0;

        public bool gameOver = false;

        // Start is called before the first frame update
        private void Awake()
        {
            instance = this;
            gameOver = true;
            //MaterialHolder.InitializeMaterialDictionary();
            train.DOLocalMoveX(-50f,0.15f).SetEase(Ease.InOutSine);
        }

        private void Start()
        {
            Application.targetFrameRate = 120;
            Time.timeScale = 1;
            DOVirtual.DelayedCall(0.3f, ()=> gameOver = false);
        }

        private bool IfSameColorVehicleParked()
        {
            var vehicles = ParkingManager.Instance.parkedVehicles;
            if (vehicles.Count > 0 && PlayerManager.instance.activePlayerList.Count > 0)
            {
                foreach (var VARIABLE in vehicles)
                {
                    if (VARIABLE.vehicleColor == PlayerManager.instance.activePlayerList[0].color)
                    {
                        return true;
                    }
                }
            }
            else if (vehicles.Count <= 0)
            {
                return true;
            }
            else if (PlayerManager.instance.activePlayerList.Count <= 0)
            {
                return true;
            }

            return false;
        }

        public bool ChekIfSlotFull(bool canPlayOnceSpaceLeftAnim)
        {
            var vehicles = ParkingManager.Instance.parkedVehicles;
            if (vehicles.Count == ParkingManager.Instance.slots.Count && !canPlayOnceSpaceLeftAnim)
            {
                return true;
            }
            else if (vehicles.Count == ParkingManager.Instance.slots.Count - 1 && canPlayOnceSpaceLeftAnim && !IfSameColorVehicleParked())
            {
                UIManager.instance.PlayOneSpaceLeftHint("Only one space left!", false);
                //Debug.Log("<color=yellow>Warning: Only One Slot Left</color>");
                return true;
            }
            else if (vehicles.Count == ParkingManager.Instance.slots.Count && canPlayOnceSpaceLeftAnim && !IfSameColorVehicleParked())
            {
                UIManager.instance.PlayOneSpaceLeftHint("Out of Space!", true);
                return true;
            }
            return false;
        }

        public IEnumerator CheckIfGameOver()
        {
            if (gameOver)
                yield break;

            yield return new WaitForSeconds(3f);

            if (ChekIfSlotFull(false) && IfSameColorVehicleParked() == false && !gameOver)
            {
                gameOver = true;
                SoundController.Instance.PlayOneShot(SoundController.Instance.fail);
                UIManager.instance.TogglePanel(UIManager.instance.gameOverPanle, true);
                SupersonicManager.Instance.LevelFail(LevelManager.GetCurrentLeveLNumber());
                //Debug.Log("<color=red>Warning: Game Over</color>");
            }
        }

        private bool alreaduCalled;

        public void CheckGameWin()
        {
            if (alreaduCalled)
                return;

            winCount++;
            if (winCount == VehicleController.instance.totalVehicles)
            {
                gameOver = true;
                alreaduCalled = true;

                DOVirtual.DelayedCall(1.5f, () => SoundController.Instance.PlayOneShot(SoundController.Instance.win));
                DOVirtual.DelayedCall(2f, () =>
                {
                    UIManager.instance.TogglePanel(UIManager.instance.winPanel, true);
                });
                SupersonicManager.Instance.LevelCompleted(LevelManager.GetCurrentLeveLNumber());
                LevelManager.LevelProgressed();
                //Debug.Log("<color=Green>Success: Game Win</color>");
            }
        }

        public void GameRevive_CallBack()
        {
            UIManager.instance.TogglePanel(UIManager.instance.gameOverPanle, false);
            ParkingManager.Instance.UnlockSlot();
            PowerUps.Instance.PlayerSort_CallBack();
            gameOver = false;
            SupersonicManager.Instance.LevelRevived(LevelManager.GetCurrentLeveLNumber());
        }
    }
}