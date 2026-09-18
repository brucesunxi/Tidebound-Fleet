using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace TJ.Scripts
{
    public class LevelManager : MonoBehaviour
    {
       
        public static int randomLevelStartIDX = 20;
        private static List<int> loadedLevels = new List<int>();
        private void Start()
        {
            Vibration.Init();
            //LoadScene();
        }

        public static void LoadScene()
        {
            if (GetCurrentLeveLNumber() > 2)
            {
                LoadLevel(1); // Map scene
            }
            else
            {
                LoadLevel(GetCurrentLeveLNumber() + 1); // Levels
            }
        }

       

        // use this to get the currecnt level number, you can use this for the UI
        public static int GetCurrentLeveLNumber()
        {
            int progress = PlayerPrefs.GetInt(PlayerPrefsManager.LevelProgress, 1);
           
            return progress;
        }

        public static int GetCurrentSceneBuildIndex()
        {
            return SceneManager.GetActiveScene().buildIndex;
        }

        public static void ReloadLevel()
        {
            Debug.Log("Reloading Level");
            SceneManager.LoadScene(GetCurrentSceneBuildIndex());
        }

        public static void LoadLevel(int levelIDX)
        {
            SceneManager.LoadScene(levelIDX);
        }

        // call this methode whenver a level is won
        public static void LevelProgressed()
        {
            int progress = PlayerPrefs.GetInt(PlayerPrefsManager.LevelProgress, 1);
            progress++;

            PlayerPrefs.SetInt(PlayerPrefsManager.LevelProgress, progress);
            PlayerPrefs.SetInt(PlayerPrefsManager.giftClaimed, 0);
        }

        // call this to load the next level.
        public static void LoadLevel()
        {
            int progress = PlayerPrefs.GetInt(PlayerPrefsManager.LevelProgress, 1) + 1;

            if (progress >= SceneManager.sceneCountInBuildSettings - 1)
            {
                int randomLVL = GetUniqueRandomLevel();
                LoadLevel(randomLVL);
            }
            else
            {
                LoadLevel(progress);
            }
        }

        private static int GetUniqueRandomLevel()
        {
            string loadedLevelsStr = PlayerPrefs.GetString(PlayerPrefsManager.LoadedLevels, "");
            if (!string.IsNullOrEmpty(loadedLevelsStr))
            {
                loadedLevels = new List<int>(Array.ConvertAll(loadedLevelsStr.Split(','), int.Parse));
            }

            int levelCount = SceneManager.sceneCountInBuildSettings;
            List<int> availableLevels = new List<int>();

            for (int i = randomLevelStartIDX; i < levelCount; i++)
            {
                if (!loadedLevels.Contains(i))
                {
                    availableLevels.Add(i);
                }
            }

            if (availableLevels.Count == 0)
            {
                loadedLevels.Clear();
                for (int i = randomLevelStartIDX; i < levelCount; i++)
                {
                    availableLevels.Add(i);
                }
            }

            int randomIndex = UnityEngine.Random.Range(0, availableLevels.Count);
            int randomLevel = availableLevels[randomIndex];
            loadedLevels.Add(randomLevel);
            PlayerPrefs.SetString(PlayerPrefsManager.LoadedLevels, string.Join(",", loadedLevels));
            return randomLevel;
        }
    }
}