using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DevionGames.StatSystem;
using DevionGames;

namespace Scripts.API
{
    /// <summary>
    /// This class provides integration between the Dead Zone WebAPI system and the DevionGames StatSystem
    /// It handles synchronizing player stats between the server and the StatsManager
    /// </summary>
    public class StatsManagerIntegration : MonoBehaviour
    {
        [SerializeField]
        private string playerStatsHandlerName = "Player Stats";

        private static StatsManagerIntegration _instance;
        public static StatsManagerIntegration Instance
        {
            get
            {
                if (_instance == null)
                {
                    GameObject go = new GameObject("StatsManagerIntegration");
                    _instance = go.AddComponent<StatsManagerIntegration>();
                    DontDestroyOnLoad(_instance.gameObject);
                }
                return _instance;
            }
        }

        private void Awake()
        {
            if (_instance == null)
            {
                _instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else if (_instance != this)
            {
                Destroy(gameObject);
            }
        }

        private void Start()
        {
            // Register for player data events
            GameDataSynchronizer.Instance.OnPlayerDataUpdated += OnPlayerDataUpdated;
        }

        private void OnDestroy()
        {
            // Unregister from player data events
            if (GameDataSynchronizer.Instance != null)
            {
                GameDataSynchronizer.Instance.OnPlayerDataUpdated -= OnPlayerDataUpdated;
            }
        }
        /// <summary>
        /// Called when player data is updated from the server
        /// Updates the StatsManager with the loaded player stats
        /// </summary>
        private void OnPlayerDataUpdated(PlayerDataModel playerData)
        {
            ApplyPlayerDataToStats(playerData);
        }

        /// <summary>
        /// Applies data from the PlayerDataModel to the StatsManager
        /// </summary>
        public void ApplyPlayerDataToStats(PlayerDataModel playerData)
        {
            if (playerData == null)
            {
                Debug.LogWarning("Cannot apply player data to stats: Data is null");
                return;
            }

            StatsHandler playerStatsHandler = GetStatsHandler();
            if (playerStatsHandler == null) return;

            // Apply health stat
            if (playerStatsHandler.GetStat("Health") is Attribute healthStat)
            {
                healthStat.CurrentValue = playerData.health;
            }

            // Apply money/score stat
            if (playerStatsHandler.GetStat("Money") != null)
            {
                playerStatsHandler.GetStat("Money").Add(playerData.money - playerStatsHandler.GetStatValue("Money"));
            }

            // Apply other stats based on your game's stats configuration
            // Example: Experience, Level, Stamina, etc.
            // if (playerStatsHandler.GetStat("Experience") != null)
            // {
            //     playerStatsHandler.GetStat("Experience").Add(playerData.experience - playerStatsHandler.GetStatValue("Experience"));
            // }

            Debug.Log("Player stats updated from server data");
        }        /// <summary>
        /// Updates the PlayerDataModel with current stats from the StatsManager
        /// </summary>
        public void UpdatePlayerDataFromStats()
        {
            if (GameDataSynchronizer.Instance == null || GameDataSynchronizer.Instance.CurrentPlayerData == null)
            {
                Debug.LogWarning("Cannot update player data from stats: GameDataSynchronizer not initialized");
                return;
            }

            StatsHandler playerStatsHandler = GetStatsHandler();
            if (playerStatsHandler == null) return;

            // Update health
            if (playerStatsHandler.GetStat("Health") is Attribute healthStat)
            {
                GameDataSynchronizer.Instance.CurrentPlayerData.health = (int)healthStat.CurrentValue;
            }

            // Update money/score
            if (playerStatsHandler.GetStat("Money") != null)
            {
                GameDataSynchronizer.Instance.CurrentPlayerData.money = (int)playerStatsHandler.GetStatValue("Money");
            }

            // Update other stats based on your game's stats configuration
            // Example: Experience, Level, Stamina, etc.
            // if (playerStatsHandler.GetStat("Experience") != null)
            // {
            //     PlayerDataManager.Instance.PlayerData.experience = (int)playerStatsHandler.GetStatValue("Experience");
            // }

            Debug.Log("Player data updated from stats");
        }

        /// <summary>
        /// Gets the player's StatsHandler from the StatsManager
        /// </summary>
        public StatsHandler GetStatsHandler()
        {
            StatsHandler handler = StatsManager.GetStatsHandler(playerStatsHandlerName);
            if (handler == null)
            {
                Debug.LogWarning($"Could not find StatsHandler with name '{playerStatsHandlerName}'");
                return null;
            }
            return handler;
        }
    }
}
