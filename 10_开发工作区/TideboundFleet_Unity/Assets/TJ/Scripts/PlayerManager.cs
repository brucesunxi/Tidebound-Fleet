using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using DG.Tweening;
using UnityEngine;
using Random = UnityEngine.Random;

namespace TJ.Scripts
{
    public class PlayerManager : MonoBehaviour
    {
        public static PlayerManager instance;
        public List<Player> playersInScene = new();
        public List<Player> totalPlayerList = new();
        public List<Player> activePlayerList = new();
        public GameObject PlayerPrefab;
        public Transform spawnPoint;
        public Transform pickPoint;
        public Vector3 midPoint;
        private System.Random r = new System.Random();

        [Header("Shuffle")] public bool canShuffle = true;
        [Range(0, 1)] public float shuffleIntensity = 0.5f; // Float variable to control shuffle intensity

        public List<Vector3> pointsBetweenMidAndPick;
        public List<Vector3> pointsBetweenMidAndSpawn;
        public List<Vector3> allPoints;

        private void Awake()
        {
            instance = this;
            GeneratePoints();
        }

        private void OnEnable()
        {
            EventManager.OnNewVehArrived += AnyCarColorMatched;
        }

        private void OnDisable()
        {
            EventManager.OnNewVehArrived -= AnyCarColorMatched;
        }

        public void InstantiatePlayers(Vehicle[] vehicles)
        {
            foreach (Vehicle v in vehicles)
            {
                for (int i = 0; i < v.SeatCount; i++)
                {
                    GameObject obj = Instantiate(PlayerPrefab, spawnPoint);
                    Player plyr = obj.GetComponent<Player>();
                    plyr.ChangeColor(v.vehicleColor);
                    playersInScene.Add(plyr);
                }
            }

            StartCoroutine(PlayerMovement());
            //  ShufflePlayerList();
        }

        public void GeneratePoints()
        {
            midPoint = new Vector3(spawnPoint.position.x, pickPoint.position.y, pickPoint.position.z);

            pointsBetweenMidAndPick = GeneratePointsBetween(pickPoint.position, midPoint, 12);

            pointsBetweenMidAndSpawn = GeneratePointsBetween(midPoint, spawnPoint.position, 9);

            allPoints = new();

            allPoints.Add(pickPoint.position);

            allPoints.AddRange(pointsBetweenMidAndPick);

            allPoints.Add(midPoint);

            allPoints.AddRange(pointsBetweenMidAndSpawn);

            allPoints.Add(spawnPoint.position);
        }

        private List<Vector3> GeneratePointsBetween(Vector3 start, Vector3 end, int numberOfPoints)
        {
            List<Vector3> points = new List<Vector3>();
            for (int i = 1; i <= numberOfPoints; i++)
            {
                float t = i / (float)(numberOfPoints + 1);
                Vector3 point = Vector3.Lerp(start, end, t);
                points.Add(point);
            }

            return points;
        }

        public IEnumerator PlayerMovement()
        {
            yield return null;
            //shuffle playersinScene list here
            if (canShuffle)
                 playersInScene = ShufflePlayerListBasedOnColor(playersInScene, shuffleIntensity);
                //ShufflePlayerList();
            totalPlayerList = new List<Player>(playersInScene);
            for (int i = 0; i < totalPlayerList.Count; i++)
            {
                totalPlayerList[i].transform.gameObject.SetActive(false);
            }

            for (int i = 0; i < 24; i++)
            {
                if (totalPlayerList.Count <= 0 || !totalPlayerList[0]) continue;
                activePlayerList.Add(totalPlayerList[0]);
                totalPlayerList.RemoveAt(0);
            }

            var points = allPoints;


            for (int i = 0; i < activePlayerList.Count; i++)
            {
                Player currentPlayer = activePlayerList[i];
                currentPlayer.transform.gameObject.SetActive(true);
                currentPlayer.anim.SetBool(Player.Walk, true);
                if (i < 14)
                {
                    StartCoroutine(currentPlayer.MoveToSlot1(midPoint, pickPoint, points[i], i * .15f));
                }
                else
                {
                    StartCoroutine(currentPlayer.MoveToSlot2(points[i], i * .15f));
                }
            }
        }

        private List<Player> ShufflePlayerListBasedOnColor(List<Player> list, float intensity)
        {
            // Separate players by color
            var colorGroups = list.GroupBy(player => player.color).ToList();

            // Flatten the color groups back to a list, starting with four unique colors
            var firstHalf = new List<Player>();
            var secondHalf = new List<Player>();

            // Add players from the first four unique color groups to the first half
            foreach (var group in colorGroups.Take(4))
            {
                firstHalf.AddRange(group);
            }

            // Add remaining players to the second half
            foreach (var group in colorGroups.Skip(4))
            {
                secondHalf.AddRange(group);
            }

            // Shuffle each half based on intensity
            firstHalf = ShuffleWithIntensity(firstHalf, intensity);
            secondHalf = ShuffleWithIntensity(secondHalf, intensity);

            // Combine first and second halves
            return firstHalf.Concat(secondHalf).ToList();
        }

        private List<Player> ShuffleWithIntensity(List<Player> list, float intensity)
        {
            // Apply Fisher-Yates shuffle with intensity control
            int n = list.Count;
            for (int i = 0; i < n - 1; i++)
            {
                int j = Mathf.Min(i + Mathf.FloorToInt(Random.Range(0f, 1f) * intensity * (n - i)), n - 1);
                (list[i], list[j]) = (list[j], list[i]);
            }

            return list;
        }


        public bool isColormatched;
        private Coroutine _rout;

        public void AnyCarColorMatched()
        {
            var cars = ParkingManager.Instance.parkedVehicles;
            if (cars.Count <= 0)
            {
                return;
            }

            foreach (var car in cars)
            {
                if (activePlayerList.Count > 0 && activePlayerList[0].color == car.vehicleColor && !car.isFull)
                {
                    isColormatched = true;
                    activePlayerList[0].MoveToTruck(car, true);
                    return;
                }
            }
            isColormatched = false;
            if (_rout != null)
                StopCoroutine(_rout);
            _rout = StartCoroutine(GameManager.instance.CheckIfGameOver());
        }


        public IEnumerator RepositionPlayers(Player player)
        {
            /*if (count <= totalPlayerList.Count)
                activePlayerList.Add(totalPlayerList[++count]);*/
            playersInScene.Remove(player);
            activePlayerList.Remove(player);
            totalPlayerList.Remove(player);
            if (totalPlayerList.Count > 0 && activePlayerList.Count < 24)
            {
                activePlayerList.Add(totalPlayerList[0]);
                totalPlayerList[0].gameObject.SetActive(true);
                totalPlayerList.RemoveAt(0);
            }

            for (int i = 0; i < activePlayerList.Count; i++)
            {
                Player currentPlayer = activePlayerList[i];
                currentPlayer.anim.SetBool(Player.Walk, true);
                Vector3 startPosition = currentPlayer.transform.position;
                Vector3 endPosition = allPoints[i];
                currentPlayer.transform.DOMove(endPosition, 0.1f)
                    .OnComplete(() => currentPlayer.anim.SetBool(Player.Walk, false));
                Vector3 direction = (endPosition - startPosition).normalized;
                Quaternion targetRotation = Quaternion.LookRotation(direction);
                currentPlayer.transform.DORotate(targetRotation.eulerAngles, 0.2f);
                currentPlayer.gameObject.SetActive(true);
            }
            yield return new WaitForSeconds(0.1f);
            AnyCarColorMatched();
        }

        public void ShufflePlayerList()
        {
            /*int n = playersInScene.Count;
            for (int i = 0; i < n - 1; i++)
            {
                // Pick a random index from i to n-1
                int j = Random.Range(i, n);
                // Swap the elements at i and j
                (playersInScene[i], playersInScene[j]) = (playersInScene[j], playersInScene[i]);
            }*/
            int n = playersInScene.Count;
            int shuffleCount = Mathf.FloorToInt(n * shuffleIntensity); // Only shuffle a percentage of the list

            for (int i = 0; i < shuffleCount; i++)
            {
                // Pick a random index from i to n-1
                int j = Random.Range(i, n);
                // Swap the elements at i and j
                (playersInScene[i], playersInScene[j]) = (playersInScene[j], playersInScene[i]);
            }

            //Debug.Log($"Shuffled playersInScene Count: {playersInScene.Count}");
            /*// Group players by color
            var groupedByColor = playersInScene
                .GroupBy(p => p.color)
                .OrderBy(x => r.Next())
                .ToList();

            playersInScene.Clear();

            int totalPlayers = groupedByColor.Sum(g => g.Count());
            int segmentSize = Mathf.CeilToInt(totalPlayers / 4f); // Divide into 4 segments

            for (int segment = 0; segment < 4; segment++)
            {
                if (groupedByColor.Count < 3) break;

                // Select three different color groups for this segment
                var selectedGroups = groupedByColor.Take(3).ToList();
                groupedByColor = groupedByColor.Skip(3).ToList();

                // Create a list to hold the players from these three colors
                List<Player> selectedPlayers = new List<Player>();

                // Add players from each selected group to the list
                foreach (var group in selectedGroups)
                {
                    selectedPlayers.AddRange(group.OrderBy(x => r.Next()).Take(segmentSize / 3).ToList());
                }

                // Shuffle the selected players
                selectedPlayers = selectedPlayers.OrderBy(x => r.Next()).ToList();

                // Occasionally insert one or two players from a fourth color
                if (groupedByColor.Count > 0 && r.Next(100) < 25) // 25% chance to insert a fourth color
                {
                    var fourthColorGroup = groupedByColor[0].ToList();
                    int insertCount = r.Next(1, 3); // Randomly insert 1 or 2 players

                    insertCount =
                        Mathf.Min(insertCount,
                            fourthColorGroup.Count); // Ensure we don't take more players than available

                    // Insert players from the fourth color into random positions in selectedPlayers
                    for (int i = 0; i < insertCount; i++)
                    {
                        int insertPosition = r.Next(1, selectedPlayers.Count);
                        selectedPlayers.Insert(insertPosition, fourthColorGroup[i]);
                    }

                    // Remove the inserted players from the fourth color group
                    fourthColorGroup.RemoveRange(0, insertCount);

                    // If there are still players left in the fourth color group, update groupedByColor
                    if (fourthColorGroup.Count > 0)
                    {
                        groupedByColor[0] = fourthColorGroup.GroupBy(p => p.color).First();
                    }
                    else
                    {
                        groupedByColor.RemoveAt(0);
                    }
                }

                // Add the shuffled and possibly enhanced players to the main list
                playersInScene.AddRange(selectedPlayers);
            }

            // If any color groups remain, add them to the end of the list
            foreach (var remainingGroup in groupedByColor)
            {
                playersInScene.AddRange(remainingGroup.OrderBy(x => r.Next()).ToList());
            }

            Debug.Log($"Shuffled playersInScene Count: {playersInScene.Count}");
*/
        }


        public void UpdatePlayerPos(int posCount)
        {
            // Remove the player who has just moved from the queue
            if (activePlayerList.Count > 0)
            {
                // If there are still players left in the totalPlayerList, add the next one to the queue
                if (totalPlayerList.Count > 0)
                {
                    Player newPlayer = totalPlayerList[0];
                    activePlayerList.Add(newPlayer);
                    totalPlayerList.RemoveAt(0);
                    newPlayer.transform.gameObject.SetActive(true);
                }

                // Reposition the remaining players in the queue
                for (int i = posCount; i < activePlayerList.Count; i++)
                {
                    Player currentPlayer = activePlayerList[i];
                    currentPlayer.transform.DOMove(allPoints[i - posCount], 0.1f);

                    // Adjust the rotation for players before the midpoint
                    if (i <= 14 + posCount)
                    {
                        currentPlayer.transform.rotation = pickPoint.rotation;
                    }
                }
            }
        }
    }
}