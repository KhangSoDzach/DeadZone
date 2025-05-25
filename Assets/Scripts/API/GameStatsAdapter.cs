using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DevionGames.StatSystem;
using DevionGames; // For DevionGames types

namespace Scripts.API
{
   public class GameStatsAdapter : MonoBehaviour
    {
        [SerializeField]
        private string playerStatsHandlerName = "Player Stats";
        
        private static GameStatsAdapter _instance;
        public static GameStatsAdapter Instance
        {
            get
            {
                if (_instance == null)
                {
                    GameObject go = new GameObject("GameStatsAdapter");
                    _instance = go.AddComponent<GameStatsAdapter>();
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
                return;
            }
        }
        
        private void Start()
        {
            // Register for player data events
            if (GameDataSynchronizer.Instance != null)
            {
                GameDataSynchronizer.Instance.OnPlayerDataUpdated += HandlePlayerDataUpdated;
            }
        }

        private void OnDestroy()
        {
            // Unregister from player data events
            if (GameDataSynchronizer.Instance != null)
            {
                GameDataSynchronizer.Instance.OnPlayerDataUpdated -= HandlePlayerDataUpdated;
            }
        }
        
        /// <summary>
        /// Called when player data is updated from the server
        /// </summary>
        private void HandlePlayerDataUpdated(PlayerDataModel playerData)
        {
            ApplyPlayerDataToStats(playerData);
        }

        /// <summary>
        /// Applies data from the DevionGames PlayerDataModel to the StatsManager
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

            Debug.Log("Player stats updated from server data");
        }

        /// <summary>
        /// Updates the PlayerDataModel with current stats from the StatsManager
        /// </summary>
        public void UpdatePlayerDataFromStats(PlayerDataModel playerData)
        {
            if (playerData == null)
            {
                Debug.LogWarning("Cannot update player data from stats: Data is null");
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

            Debug.Log("Player data updated from stats");
        }

        /// <summary>
        /// Gets the player's StatsHandler from the StatsManager
        /// </summary>
        private StatsHandler GetStatsHandler()
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
