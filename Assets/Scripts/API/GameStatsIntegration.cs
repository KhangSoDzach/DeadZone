using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DevionGames.StatSystem;
using DevionGames;


    public class GameStatsIntegration : MonoBehaviour
    {
        [SerializeField]
        private string playerStatsHandlerName = "Player Stats";
        
        private static GameStatsIntegration _instance;
        public static GameStatsIntegration Instance
        {
            get
            {
                if (_instance == null)
                {
                    GameObject go = new GameObject("GameStatsIntegration");
                    _instance = go.AddComponent<GameStatsIntegration>();
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
            if (GameDataSynchronizer.Instance != null)
            {
                GameDataSynchronizer.Instance.OnPlayerDataUpdated += OnPlayerDataUpdated;
            }
        }

        private void OnDestroy()
        {
            // Unregister from player data events
            if (GameDataSynchronizer.Instance != null)
            {
                GameDataSynchronizer.Instance.OnPlayerDataUpdated -= OnPlayerDataUpdated;
            }
        }        /// <summary>
        /// Called when player data is updated from the server
        /// Updates the StatsManager with the loaded player stats
        /// </summary>
        private void OnPlayerDataUpdated()
        {
            // Get player data from GameAPI and validate it
            var playerData = GameAPI.Instance.PlayerData;
            if (playerData != null && !string.IsNullOrEmpty(playerData.id) && !string.IsNullOrEmpty(playerData.username))
            {
                UpdateGameStatsFromPlayerData(playerData);
            }
            else
            {
                Debug.LogWarning("Cannot update stats: Invalid or incomplete player data");
                
                // Try to reload player data
                StartCoroutine(GameAPI.Instance.GetPlayerData((success, error) => {
                    if (success && GameAPI.Instance.PlayerData != null)
                    {
                        UpdateGameStatsFromPlayerData(GameAPI.Instance.PlayerData);
                    }
                    else
                    {
                        Debug.LogError($"Failed to reload player data for stats update: {error}");
                    }
                }));
            }
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
        }

        /// <summary>
        /// Updates the PlayerDataModel with current stats from the StatsManager
        /// </summary>
        public void UpdatePlayerDataFromStats()
        {
            // Replace GameDataSynchronizer.Instance.CurrentPlayerData with GameAPI.Instance.PlayerData
            var playerData = GameAPI.Instance.PlayerData;
            if (playerData == null)
            {
                Debug.LogWarning("Cannot update player data from stats: No player data available");
                return;
            }

            StatsHandler playerStatsHandler = GetStatsHandler();
            if (playerStatsHandler == null) return;

            // Update health
            if (playerStatsHandler.GetStat("Health") is Attribute healthStat)
            {
                playerData.health = (int)healthStat.CurrentValue;
            }

            // Update money/score
            if (playerStatsHandler.GetStat("Money") != null)
            {
                playerData.money = (int)playerStatsHandler.GetStatValue("Money");
            }

            // Update other stats based on your game's stats configuration
            // Example: Experience, Level, Stamina, etc.
            // if (playerStatsHandler.GetStat("Experience") != null)
            // {
            //     playerData.experience = (int)playerStatsHandler.GetStatValue("Experience");
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

        private void UpdateGameStatsFromPlayerData(PlayerDataModel playerData)
        {
            // Replace GameDataSynchronizer.Instance.CurrentPlayerData with GameAPI.Instance.PlayerData
            var currentData = GameAPI.Instance.PlayerData;
            if (currentData == null)
            {
                Debug.LogWarning("No player data available for stats integration");
                return;
            }

            StatsHandler playerStatsHandler = GetStatsHandler();
            if (playerStatsHandler == null) return;

            // Update health
            if (playerStatsHandler.GetStat("Health") is Attribute healthStat)
            {
                healthStat.CurrentValue = playerData.health;
            }

            // Update money/score
            if (playerStatsHandler.GetStat("Money") != null)
            {
                playerStatsHandler.GetStat("Money").Add(playerData.money - playerStatsHandler.GetStatValue("Money"));
            }

            // Update other stats based on your game's stats configuration
            // Example: Experience, Level, Stamina, etc.
            // if (playerStatsHandler.GetStat("Experience") != null)
            // {
            //     playerStatsHandler.GetStat("Experience").Add(playerData.experience - playerStatsHandler.GetStatValue("Experience"));
            // }

            Debug.Log("Player stats updated from server data");
        }

        private void CollectStatsToPlayerData()
        {
            // Replace GameDataSynchronizer.Instance.CurrentPlayerData with GameAPI.Instance.PlayerData
            var playerData = GameAPI.Instance.PlayerData;
            if (playerData == null)
            {
                Debug.LogWarning("No player data available to update from stats");
                return;
            }

            StatsHandler playerStatsHandler = GetStatsHandler();
            if (playerStatsHandler == null) return;

            // Collect current stats and update player data
            if (playerStatsHandler.GetStat("Health") is Attribute healthStat)
            {
                playerData.health = (int)healthStat.CurrentValue;
            }

            if (playerStatsHandler.GetStat("Money") != null)
            {
                playerData.money = (int)playerStatsHandler.GetStatValue("Money");
            }

            Debug.Log("Player data updated from stats");
        }
    }
