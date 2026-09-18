using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;

namespace TJ.Scripts
{
    public class VehicleController : MonoBehaviour
    {
        public static VehicleController instance;
        public Vehicle[] vehicles;
        public int totalPlayersCount;
        public int playerCount;
        public TextMeshPro totalPlayerDisplay;
        public Transform Road;
        public PlayerManager playerManager;
        public MaterialHolder VehiclesMaterialHolder;
        public MaterialHolder stickmanMaterialHolder;
        public Transform rightCollider;
        public Transform leftCollider;
        public int totalSeats;
        public int totalVehicles;
        public bool shuffle = true;

        // Start is called before the first frame update
        private void Awake()
        {
            instance = this;
            VehiclesMaterialHolder.InitializeMaterialDictionary();
            stickmanMaterialHolder.InitializeMaterialDictionary();
            if (shuffle == true)
                vehicles = GetComponentsInChildren<Vehicle>(true);
            RandomVehColor();
            CalculatePlayersCount();
            CalculateTotalSeat();
        }

        private void CalculateTotalSeat()
        {
            totalSeats = vehicles.Sum(v => v.SeatCount);
        }

        private void Start()
        {
            playerCount = totalPlayersCount;
            totalPlayerDisplay.text = playerCount.ToString();
            totalVehicles = vehicles.Length;
        }

        public void UpdatePlayerCount()
        {
            playerCount--;
            totalPlayerDisplay.text = playerCount.ToString();
        }

        [ContextMenu("random")]
        public void RandomVehColor()
        {
            System.Random r = new System.Random();
            ColorEnum[] values = (ColorEnum[])Enum.GetValues(typeof(ColorEnum));
            List<ColorEnum> colors = new(values);
            colors.Remove(ColorEnum.none);
            //colors.RemoveAt(UnityEngine.Random.Range(0, colors.Count));
            //colors.RemoveAt(UnityEngine.Random.Range(0, colors.Count));
            colors = colors.OrderBy(x => r.Next()).ToList();
            // if (shuffle == true)
            //  vehicles = vehicles.OrderBy(x => r.Next()).ToArray();

            int colorIndex = 0;
            for (int i = 0; i < vehicles.Length; i++)
            {
                if (colorIndex >= colors.Count)
                {
                    colorIndex = 0;
                }

                ColorEnum color = colors[0];
                vehicles[i].ChangeColor(colors[colorIndex]);
                colorIndex++;
            }
        }

        public void RandomVehicleColors()
        {
            var groupedVehicles = vehicles.GroupBy(v => v.SeatCount);

            foreach (var group in groupedVehicles)
            {
                List<ColorEnum> existingColors = new List<ColorEnum>();
                existingColors.Remove(ColorEnum.none);
                //existingColors.RemoveAt(UnityEngine.Random.Range(0, existingColors.Count));
                //existingColors.RemoveAt(UnityEngine.Random.Range(0, existingColors.Count));
                foreach (var vehicle in group)
                {
                    existingColors.Add(vehicle.vehicleColor);
                }

                System.Random r = new System.Random();
                existingColors = existingColors.OrderBy(x => r.Next()).ToList();
                int index = 0;
                foreach (var vehicle in group)
                {
                    vehicle.ChangeColor(existingColors[index]);
                    index++;
                }
            }
        }

        [ContextMenu("changecolor of vehicles")]
        public void ChangeParkingCarsColor()
        {
            List<Vehicle> parkingVehicles = new List<Vehicle>();

            // Add all vehicles to the parkingVehicles list
            for (int i = 0; i < vehicles.Length; i++)
            {
                parkingVehicles.Add(vehicles[i]);
            }

            // Remove vehicles that are in parkedVehicles from parkingVehicles
            parkingVehicles.RemoveAll(v => ParkingManager.Instance.parkedVehicles.Contains(v));

            // Group vehicles by SeatCount
            var groupedVehicles = parkingVehicles.GroupBy(v => v.SeatCount).ToList();

            System.Random r = new System.Random();

            // Interchange colors within each group
            foreach (var group in groupedVehicles)
            {
                var vehicleGroup = group.ToList();

                // Shuffle the group
                vehicleGroup = vehicleGroup.OrderBy(x => r.Next()).ToList();

                // Save the first vehicle's color to be used at the end
                ColorEnum firstVehicleColor = vehicleGroup[0].vehicleColor;

                // Interchange colors in a circular manner
                for (int i = 0; i < vehicleGroup.Count - 1; i++)
                {
                    vehicleGroup[i].ChangeColor(vehicleGroup[i + 1].vehicleColor);
                }

                // Assign the first vehicle's color to the last vehicle
                vehicleGroup[^1].ChangeColor(firstVehicleColor);
            }
        }

        public void RemoveVehicle(Vehicle vehicleToRemove)
        {
            // Convert the array to a list for easier manipulation
            List<Vehicle> vehicleList = vehicles.ToList();

            // Remove the specified vehicle
            vehicleList.Remove(vehicleToRemove);

            // Convert the list back to an array and update the vehicles array
            vehicles = vehicleList.ToArray();
        }

        public void CalculatePlayersCount()
        {
            for (int i = 0; i < vehicles.Length; i++)
            {
                totalPlayersCount += vehicles[i].SeatCount;
            }

            playerManager.InstantiatePlayers(vehicles);
        }
        // ReSharper disable Unity.PerformanceAnalysis
        /*public IEnumerator JumpToSeat(List<Player> players, Vehicle veh, float delay, PlayerManager manager)
        {
            yield return new WaitForSeconds(delay);
            int totalCount = players.Count;
            if (veh.GetFreeSeat() != null)
            {
                for (int i = 0; i < players.Count; i++)
                {
                    Player player = players[i];
                    PlayerManager.instance.playersInScene.Remove(player);
                    player.StartCoroutine(player.MoveToTruck(player, veh, walkingpoint, i * .15f, totalCount, players));
                }
            }
            else
            {
                Debug.Log("vehicle is full");
                PlayerManager.instance.CheckColor(0);
            }

        }*/
    }
}