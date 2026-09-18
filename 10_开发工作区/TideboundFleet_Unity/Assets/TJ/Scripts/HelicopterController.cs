using DG.Tweening;
using TJ.Scripts;
using UnityEngine;

public class HelicopterController : Singleton<HelicopterController>
{
    private int hoverID = Animator.StringToHash("Howering");
    public Vector3 idlePoint;
    public Quaternion rot;
    public Collider _collider;
    public GameObject pwPanel;
    [SerializeField] Animator _animator;
    [SerializeField] GameObject adsIcon;
    public ParkingSlots vipSlot;
    public GameObject hint;
    protected void OnEnable()
    {
        rot = transform.rotation;
        idlePoint = transform.position;
        transform.position = new Vector3(transform.position.x + 10, transform.position.y, transform.position.z);
        _collider.enabled = false;
    }

    private void Start()
    {
        StartWork();
    }

    public void StartWork()
    {
        _collider.enabled = false;
        transform.DOMove(idlePoint, 1f).SetEase(Ease.InOutSine).OnComplete(() =>
        {
            _animator.SetBool(hoverID, true);
            _collider.enabled = true;
        }).SetDelay(2.5f);
    }

    private void OnMouseDown()
    {
        if (pwPanel.activeSelf || GameManager.instance.gameOver || PowerUps.Instance.currentPowerUp == PowerUp.Helicopter) return;

        PowerUps.Instance.ShowHelicopterPanel();
        SoundController.Instance.PlayOneShot(SoundController.Instance.buttonSound, 0.5f);
        Vibration.Vibrate(40);
    }
    public void ToggleHint(bool value)
    {
        hint.SetActive(value);
        vipSlot.gameObject.SetActive(value);
    }

    public void PickUP(Vehicle veh)
    {
        PowerUps.Instance.currentPowerUp = PowerUp.None;
        veh.slot = vipSlot;
        _collider.enabled = false;
        _animator.SetBool(hoverID, false);
        hint.SetActive(false);
        Vector3 target = new Vector3(veh.pickUPPoint.position.x, transform.position.y, veh.pickUPPoint.position.z);
        Vector3 direction = (target - transform.position).normalized;
        Quaternion lookRotation = Quaternion.LookRotation(direction);

        transform.DORotateQuaternion(lookRotation, 0.2f).SetEase(Ease.InOutQuad);

      
        transform.DOMove(target, 2f).SetEase(Ease.InOutSine).OnComplete(() =>
        {
           
            transform.DORotateQuaternion(veh.pickUPPoint.rotation, 0.15f);
            transform.DOMove(veh.pickUPPoint.position, 1.5f).SetEase(Ease.InOutSine).OnComplete(() =>
            {
                veh.transform.parent = transform; 
               
                transform.DOMoveY(transform.position.y + 5, 10f).SetSpeedBased().SetEase(Ease.OutCubic).OnComplete(() =>
                {
                    
                    Vector3 pos = new Vector3(vipSlot.transform.position.x, transform.position.y, vipSlot.transform.position.z);
                    if (veh.garage)
                        veh.garage.RemoveObstacle(veh);

                    transform.DORotateQuaternion(vipSlot.transform.rotation, 0.3f).SetEase(Ease.InOutQuad);
                    transform.DOMove(pos, 10f).SetSpeedBased().SetEase(Ease.InOutSine).OnComplete(() =>
                    {
                        
                        transform.DOMoveY(transform.position.y - 5, 10f).SetSpeedBased().SetEase(Ease.OutQuad).OnComplete(() =>
                        {
                            veh.transform.parent = null;
                            veh.DeactivateRoof();
                            veh.ChangeScale(true);
                            veh.FillUpVehicle();
                            ToggleHint(false);
                            transform.DOMoveY(transform.position.y + 5, 10f).SetSpeedBased().SetEase(Ease.OutCubic).OnComplete(() =>
                            {
                                transform.DOMoveX(-60, 10f).SetSpeedBased().SetEase(Ease.InOutSine).OnComplete(() =>
                                {
                                    transform.position = new Vector3(idlePoint.x + 10, idlePoint.y + 20, idlePoint.z);
                                    transform.rotation = rot;
                                    StartWork();
                                });
                            });
                        });
                    });
                });
            });
        });
    }
   
}
