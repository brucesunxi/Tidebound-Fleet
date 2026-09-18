using System.Collections;
using System.Collections.Generic;
using System.Linq;
using DG.Tweening;
using UnityEngine;
using UnityEngine.EventSystems;

namespace TJ.Scripts
{
    public class Vehicle : MonoBehaviour
    {
        public ColorEnum vehicleColor;
        public List<Transform> seats;
        [SerializeField] private List<MeshRenderer> vehMesh;
        public Tween movingZdir;
        private float distance = 30f;
        private bool isCollided = false;
        public bool isFull = false;
        public Vector3 originalPosition;
        public List<GameObject> removableParts;
        public Vector3 ogScale;
        public Vector3 newScale;
        public int playersInSeat = 0;
        public ParkingSlots slot;
        private bool canCollideWitOtherVehicle = true;
        public static bool isMovingStraight = false;
        public static ColorEnum LastTouchedCarcolor;
        public bool isMovingForward = false;
        public Garage garage;
        public Transform pickUPPoint;
        public int SeatCount => seats.Count;

        void Start()
        {
            transform.position = new Vector3(transform.position.x, 1f, transform.position.z);
            SetInitialPosition();
            ogScale = transform.localScale;
            isMovingStraight = false;
            Vector3 currentRotation = transform.rotation.eulerAngles;
            transform.rotation = Quaternion.Euler(0, currentRotation.y, 0);

        }

        private void OnValidate()
        {
            transform.position = new Vector3(transform.position.x, 1f, transform.position.z);
            Vector3 currentRotation = transform.rotation.eulerAngles;
            transform.rotation = Quaternion.Euler(0, currentRotation.y, 0);
        }

        public void SetInitialPosition()
        {
            originalPosition = transform.position;
        }

        private void OnMouseDown()
        {

            if (isMovingForward || isMovingStraight || GameManager.instance.gameOver || EventSystem.current.IsPointerOverGameObject() || PowerUps.Instance.panel.activeSelf)
                return;
            if (garage != null && !garage.canMoveNext)
                return;

            if (Helper.instance)
                Helper.instance.MoveHand();

            if (PowerUps.Instance.currentPowerUp == PowerUp.Helicopter)
            {
                GetComponent<Collider>().enabled = false;
                HelicopterController heli = FindObjectOfType<HelicopterController>();
                VehicleController.instance.RemoveVehicle(this);
                heli.PickUP(this);
                return;
            }

            LastTouchedCarcolor = vehicleColor;
            if (CheckForVehicleInFront(out RaycastHit hitInfo))
            {
                isMovingStraight = true;
                isMovingForward = true;
                Vibration.Vibrate(40);

                Vector3 targetPosition =
                    transform.position +
                    transform.forward * (hitInfo.distance + 1);
                movingZdir = transform.DOMove(targetPosition, 0.2f).SetEase(Ease.InQuad);

                return;
            }

            slot = ParkingManager.Instance.CheckForFreeSlot();
            if (slot == null)
            {
                return;
            }

            if (garage)
                garage.RemoveObstacle(this);
            MoveCarStraight();

        }


        public void FillUpVehicle()
        {
            var players = PlayerManager.instance.playersInScene;
            for (int i = 0; i < players.Count; i++)
            {
                if (playersInSeat >= SeatCount)
                    break;

                if (players[i].color == vehicleColor)
                {
                    players[i].MoveToTruck(this, false);
                    i--;
                }
            }
            SoundController.Instance.PlayOneShot(SoundController.Instance.sort);
        }
        public void ChangeScale(bool shift)
        {
            newScale = Vector3.one;
            if (shift)
            {
                transform.localScale = newScale;
            }
            else
            {
                transform.localScale = ogScale;
            }
        }

        public Transform GetFreeSeat()
        {
            for (int i = seats.Count - 1; i >= 0; i--)
            {
                if (seats[i].childCount == 0)
                {
                    playersInSeat++;
                    IsVehicleFull();
                    return seats[i];
                }
            }

            return null;
        }

        public void IsVehicleFull()
        {
            if (playersInSeat == seats.Count)
            {
                isFull = true;
                DOVirtual.DelayedCall(1f, () =>
                {
                    VehicleGoing();
                    GameManager.instance.CheckGameWin();
                });
            }
        }

        public void VehicleGoing()
        {

            VehicleController.instance.vehicles = VehicleController.instance.vehicles
                .Where(v => v != this.transform)
                .ToArray();
            transform.DORotateQuaternion(ParkingManager.Instance.exitPoint.rotation, 0.2f);
            transform.DOMove(
                new Vector3(slot.enterPoint.transform.position.x, transform.position.y,
                    slot.enterPoint.transform.position.z), 30f).SetSpeedBased().OnComplete(() =>
            {
                slot.isOccupied = false;
                canCollideWitOtherVehicle = false;
                ParkingManager.Instance.parkedVehicles.Remove(this);
                transform.parent = null;
                transform.DOMove(ParkingManager.Instance.exitPoint.transform.position, 30f).SetSpeedBased()
                    .OnComplete(() => { transform.gameObject.SetActive(false); });
            });
            SoundController.Instance.PlayFullSound();
            SoundController.Instance.PlayOneShot(SoundController.Instance.moving);
        }

        public void ChangeColor(ColorEnum colorEnum)
        {
            this.vehicleColor = colorEnum;
            Material mats = VehicleController.instance.VehiclesMaterialHolder.FindMaterialByName(colorEnum);
            if (mats != null)
            {
                for (int i = 0; i < vehMesh.Count; i++)
                {
                    vehMesh[i].material = mats;
                }
            }
        }


        public void MoveCarStraight()
        {
            Vibration.Vibrate(40);
            SoundController.Instance.PlayOneShot(SoundController.Instance.tapSound, 0.5f);
            slot.isOccupied = true;
            isMovingStraight = true;
            isMovingForward = true;
            Vector3 localPosition = transform.localPosition;
            Vector3 localForwardDirection = transform.localRotation * Vector3.forward;

            Vector3 pointAtDistance = localPosition + localForwardDirection * distance;

            Vector3 worldPoint = transform.parent.TransformPoint(pointAtDistance);

            Debug.DrawLine(transform.position, worldPoint, Color.green);
            movingZdir = transform.DOMove(worldPoint, 12f).SetSpeedBased();
            GetComponent<AudioSource>().enabled = true;
        }
        public bool CheckForObstacles()
        {

            float offset = 1.0f;
            float rayDistance = Mathf.Infinity;


            Vector3 forward = transform.TransformDirection(Vector3.forward);


            Vector3 leftRayDirection = transform.TransformDirection(Vector3.forward + Vector3.left * offset);
            Vector3 rightRayDirection = transform.TransformDirection(Vector3.forward + Vector3.right * offset);


            if (Physics.Raycast(transform.position, leftRayDirection, out RaycastHit hitInfoLeft, rayDistance) &&
                Physics.Raycast(transform.position, rightRayDirection, out RaycastHit hitInfoRight, rayDistance))
            {
                if (hitInfoLeft.collider != null && hitInfoLeft.collider.TryGetComponent(out Vehicle vehicleLeft) &&
                    vehicleLeft.canCollideWitOtherVehicle && !vehicleLeft.isMovingForward)
                {

                    return true;
                }

                if (hitInfoRight.collider != null && hitInfoRight.collider.TryGetComponent(out Vehicle vehicleRight) &&
                    vehicleRight.canCollideWitOtherVehicle && !vehicleRight.isMovingForward)
                {

                    return true;
                }
            }

            return false;
        }


        private bool CheckForVehicleInFront(out RaycastHit hitInfo)
        {
            Vector3 forward = transform.TransformDirection(Vector3.forward);
            float rayDistance = Mathf.Infinity;

            if (Physics.Raycast(transform.position, forward, out hitInfo, rayDistance))
            {
                if (hitInfo.collider.TryGetComponent(out Vehicle vehicle) && vehicle.canCollideWitOtherVehicle &&
                    !vehicle.isMovingForward)
                {

                    return true;
                }
            }

            return false;


        }

        private void StrikeAndMoveBack(Vehicle targetVehicle)
        {
            Vector3 targetPosition = targetVehicle.transform.position;

            transform.DOMove(targetPosition, 0.5f).OnComplete(() =>
            {
                targetVehicle.ShakeVehicle();

                transform.DOMove(originalPosition, 0.5f);
            });
        }

        public void ShakeVehicle()
        {
            transform.DOShakeRotation(0.2f, transform.forward * 2, vibrato: 10, randomness: 90).SetEase(Ease.InBounce);
        }

        private static int counter = 0;
        private bool toggle;

        private void OnTriggerEnter(Collider other)
        {
            if (other.gameObject.CompareTag("Down"))
            {
                canCollideWitOtherVehicle = false;
                movingZdir.Pause();

                toggle = !toggle;

                MoveToSideBorder(VehicleController.instance.leftCollider, -20f);

                if (!toggle)
                {
                }

                return;
            }

            if (canCollideWitOtherVehicle && other.TryGetComponent(out Vehicle vehicle) &&
                vehicle.canCollideWitOtherVehicle)
            {
                if (slot && canCollideWitOtherVehicle)
                    slot.isOccupied = false;
                movingZdir.Pause();

                if (!isCollided)
                {
                    if (isMovingStraight && counter == 0 && isMovingForward)
                    {
                        counter++;

                        GetComponent<AudioSource>().enabled = false;
                        SoundController.Instance.PlayOneShot(SoundController.Instance.hitSound);
                        EffectsManager.instance.PlayEffect(EffectsManager.instance.hitEffect,
                            other.ClosestPoint(transform.position + new Vector3(0, 0.25f, 0)), Quaternion.identity);
                    }

                    vehicle.ShakeVehicle();
                    transform.DOMove(originalPosition, 0.3f).SetEase(Ease.OutBack)
                        .OnComplete(() =>
                        {
                            counter = 0;
                            isMovingStraight = false;
                            isMovingForward = false;
                        });
                }
            }

            if (other.gameObject.CompareTag("Border") && !isCollided)
            {
                isCollided = true;
                isMovingStraight = false;
                canCollideWitOtherVehicle = false;

                MoveToTargetFromBorder();
                VehicleController.instance.RemoveVehicle(this);
                movingZdir.Pause();
            }

            if (other.gameObject.CompareTag("Upborder") && !isCollided)
            {
                isCollided = true;
                isMovingStraight = false;
                canCollideWitOtherVehicle = false;
                MoveToTargetFromUpBorder();
                VehicleController.instance.RemoveVehicle(this);
                movingZdir.Pause();
            }
        }

        public void MoveToSideBorder(Transform collider, float distance)
        {
            isMovingStraight = false;
            canCollideWitOtherVehicle = false;

            Transform cube = collider.transform;
            Vector3 cubePos = cube.position;

            Vector3 directionToCube = new Vector3(cubePos.x - transform.position.x, 0, 0);

            Quaternion targetRotation = Quaternion.LookRotation(directionToCube, Vector3.up);

            transform.DORotateQuaternion(targetRotation, 0.1f);
            transform.DOLocalMoveX(distance, 0.8f);
            VehicleController.instance.RemoveVehicle(this);
        }

        public void MoveToTargetFromBorder()
        {
            Transform road = VehicleController.instance.Road;
            Vector3 roadPos = road.position;

            Vector3[] path = new Vector3[]
            {
                transform.position,
                new Vector3(transform.position.x, transform.position.y, road.position.z)
            };

            transform.DORotate(Vector3.zero, 0.1f);
            transform.DOPath(path, 0.3f, PathType.Linear).SetEase(Ease.Linear).OnComplete(() =>
            {
                transform.DOLookAt(roadPos, 0.1f);

                MoveToSlot();
                DeactivateRoof();
            });
        }

        public void DeactivateRoof()
        {
            foreach (var parts in removableParts)
            {
                parts.SetActive(false);
            }
        }
        public void MoveToTargetFromUpBorder()
        {
            transform.DOLookAt(slot.transform.position, 0.1f);
            MoveToSlot();
            DeactivateRoof();
        }

        public void MoveToSlot()
        {

            Vector3[] waypoints = new Vector3[]
            {
                new(slot.enterPoint.position.x, transform.position.y, slot.enterPoint.position.z),
                new(slot.stopPoint.position.x, transform.position.y, slot.stopPoint.position.z),
            };
            ChangeScale(true);
            transform.DOPath(waypoints, .5f, PathType.CatmullRom).OnWaypointChange(waypointindex =>
                {
                    if (waypointindex == 1)
                    {
                        transform.DORotateQuaternion(slot.stopPoint.rotation, 0.2f);
                    }
                })
                .OnComplete(() =>
                {
                    isMovingStraight = false;
                    ParkingManager.Instance.parkedVehicles.Add(this);

                    transform.parent = slot.transform;
                    GetComponent<BoxCollider>().enabled = false;

                    DOVirtual.DelayedCall(0.2f, () => GameManager.instance.ChekIfSlotFull(true));
                    if (!PlayerManager.instance.isColormatched)
                        EventManager.OnNewVehArrived?.Invoke();
                    GetComponent<AudioSource>().enabled = false;
                });
        }
    }
}