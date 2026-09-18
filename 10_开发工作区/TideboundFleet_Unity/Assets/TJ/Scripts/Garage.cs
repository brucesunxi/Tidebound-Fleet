using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace TJ.Scripts
{
    public class Garage : MonoBehaviour
    {
        public bool canMoveNext = true;
        private static readonly int Open = Animator.StringToHash("Open");
        public Transform endPoint;
        public List<Vehicle> vehicles = new();
        public List<Vehicle> obstacle = new();
        public Animator animator;
        public TextMeshPro text;

        private void Awake()
        {
            vehicles = GetComponentsInChildren<Vehicle>(true).ToList();
            foreach (var VARIABLE in vehicles)
            {
                VARIABLE.garage = this;
            }

            foreach (var VARIABLE in obstacle)
            {
                VARIABLE.garage = this;
            }
        }

        private void Start()
        {
            if (obstacle.Count == 0 && vehicles.Count > 0)
                EnableNextVehicle();
            text.text = vehicles.Count.ToString();
        }

        public void RemoveObstacle(Vehicle veh)
        {
            if (obstacle.Contains(veh))
                obstacle.Remove(veh);

            if (obstacle.Count == 0 && vehicles.Count > 0)
                EnableNextVehicle();
        }

        public void EnableNextVehicle()
        {
            if (!canMoveNext)
                return;
            canMoveNext = false;
            animator.SetTrigger(Open);
            DOVirtual.DelayedCall(0.2f, () =>
            {
                vehicles[0].transform.GetChild(0).DOShakeScale(0.3f, new Vector3(0, 0, 0.2f), 1, 1);
                vehicles[0].gameObject.SetActive(true);
                vehicles[0].transform.DOMove(endPoint.position, 0.3f).OnComplete(() =>
                {
                    vehicles[0].SetInitialPosition();
                    obstacle.Add(vehicles[0]);
                    vehicles.RemoveAt(0);
                    canMoveNext = true;
                });
            });
            text.text = (vehicles.Count - 1).ToString();
        }
    }
}