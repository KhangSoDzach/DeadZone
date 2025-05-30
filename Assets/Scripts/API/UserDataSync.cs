using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using DevionGames;
using DevionGames.StatSystem;
public class UserDataSync : MonoBehaviour
    {
        [Header("Đồng bộ tự động")]
        [SerializeField] private bool enableAutoSync = true;
        [SerializeField] private float syncInterval = 300f; // 5 phút
        [SerializeField] private bool syncOnSceneChange = true;
        [SerializeField] private bool syncOnApplicationPause = true;
        [SerializeField] private bool syncOnApplicationQuit = true;
        
        [Header("Debug")]
        [SerializeField] private bool debugMode = false;
        [SerializeField] private bool showNotifications = false;
        
        // Biến theo dõi trạng thái đồng bộ
        private bool _isSyncing = false;
        private DateTime _lastSyncTime;
        
        private void Start()
        {
            // Đăng ký sự kiện thay đổi scene
            if (syncOnSceneChange)
            {
                SceneManager.sceneLoaded += OnSceneLoaded;
            }
            
            // Bắt đầu đồng bộ tự động
            if (enableAutoSync)
            {
                StartCoroutine(AutoSyncCoroutine());
            }
            
            // Thực hiện đồng bộ khi bắt đầu scene
            SyncFromServer();
        }
        
        private void OnDestroy()
        {
            // Hủy đăng ký sự kiện thay đổi scene
            if (syncOnSceneChange)
            {
                SceneManager.sceneLoaded -= OnSceneLoaded;
            }
        }
        
        private void OnApplicationPause(bool paused)
        {
            if (syncOnApplicationPause)
            {
                if (paused)
                {
                    // Khi game bị tạm dừng, lưu dữ liệu lên server
                    SyncToServer();
                }
                else
                {
                    // Khi game tiếp tục, tải dữ liệu mới từ server
                    SyncFromServer();
                }
            }
        }
        
        private void OnApplicationQuit()
        {
            if (syncOnApplicationQuit)
            {
                // Lưu dữ liệu lên server khi thoát game
                SyncToServer(true);
            }
        }
        
        /// <summary>
        /// Xử lý khi scene được tải
        /// </summary>
        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            // Đồng bộ dữ liệu khi chuyển scene
            SyncFromServer();
        }
        
        /// <summary>
        /// Coroutine để đồng bộ tự động theo định kỳ
        /// </summary>
        private IEnumerator AutoSyncCoroutine()
        {
            while (enableAutoSync)
            {
                yield return new WaitForSeconds(syncInterval);
                
                // Kiểm tra đã đăng nhập chưa
                if (GameAPI.Instance.IsLoggedIn)
                {
                    SyncToServer();
                }
            }
        }
        
        /// <summary>
        /// Đồng bộ dữ liệu từ server xuống game
        /// </summary>
        public void SyncFromServer(Action<bool> onComplete = null)
        {
            if (!GameAPI.Instance.IsLoggedIn)
            {
                DebugLog("Không thể đồng bộ từ server: Người dùng chưa đăng nhập");
                onComplete?.Invoke(false);
                return;
            }
            
            if (_isSyncing)
            {
                DebugLog("Đang trong quá trình đồng bộ. Bỏ qua yêu cầu.");
                onComplete?.Invoke(false);
                return;
            }
            
            _isSyncing = true;
            StartCoroutine(SyncFromServerCoroutine(onComplete));
        }
        
        /// <summary>
        /// Coroutine để đồng bộ dữ liệu từ server với improved error handling
        /// </summary>
        private IEnumerator SyncFromServerCoroutine(Action<bool> onComplete)
        {
            DebugLog("Bắt đầu đồng bộ dữ liệu từ server...");
            
            yield return StartCoroutine(GameAPI.Instance.GetPlayerData((success, errorMsg) => {
                if (success)
                {
                    _lastSyncTime = DateTime.Now;
                    DebugLog("Đã đồng bộ dữ liệu từ server thành công");
                    
                    if (showNotifications)
                    {
                        // TODO: Hiển thị thông báo thành công
                    }
                    
                    ApplyUserDataToGame();
                }
                else
                {
                    // Only log as warning if it's not a connectivity issue during startup
                    if (errorMsg.Contains("Cannot reach server") || errorMsg.Contains("connectivity"))
                    {
                        DebugLog($"Server không khả dụng khi khởi động: {errorMsg}");
                    }
                    else
                    {
                        DebugLog($"Lỗi khi đồng bộ từ server: {errorMsg}");
                    }
                    
                    if (showNotifications && !errorMsg.Contains("Cannot reach server"))
                    {
                        // TODO: Hiển thị thông báo lỗi chỉ khi không phải lỗi kết nối
                    }
                }
                
                _isSyncing = false;
                onComplete?.Invoke(success);
            }));
        }
        
        /// <summary>
        /// Đồng bộ dữ liệu từ game lên server
        /// </summary>
        public void SyncToServer(bool immediate = false, Action<bool> onComplete = null)
        {
            if (!GameAPI.Instance.IsLoggedIn)
            {
                DebugLog("Không thể đồng bộ lên server: Người dùng chưa đăng nhập");
                onComplete?.Invoke(false);
                return;
            }
            
            // Verify player data exists
            if (GameAPI.Instance.PlayerData == null)
            {
                DebugLog("Không có dữ liệu người chơi để đồng bộ");
                onComplete?.Invoke(false);
                return;
            }
            
            if (_isSyncing && !immediate)
            {
                DebugLog("Đang trong quá trình đồng bộ. Bỏ qua yêu cầu.");
                onComplete?.Invoke(false);
                return;
            }
            
            _isSyncing = true;
            
            // Trước khi lưu, cập nhật dữ liệu từ game
            UpdateUserDataFromGame();
            
            if (immediate)
            {
                // Lưu đồng bộ (chỉ sử dụng khi thoát game)
                SaveUserDataImmediate();
                _isSyncing = false;
                onComplete?.Invoke(true);
            }
            else
            {
                StartCoroutine(SyncToServerCoroutine(onComplete));
            }
        }
        
        /// <summary>
        /// Coroutine để đồng bộ dữ liệu lên server
        /// </summary>
        private IEnumerator SyncToServerCoroutine(Action<bool> onComplete)
        {
            DebugLog("Bắt đầu đồng bộ dữ liệu lên server...");
            
            yield return StartCoroutine(GameAPI.Instance.SavePlayerData((success, errorMsg) => {
                if (success)
                {
                    _lastSyncTime = DateTime.Now;
                    DebugLog("Đã đồng bộ dữ liệu lên server thành công");
                    
                    if (showNotifications)
                    {
                        // TODO: Hiển thị thông báo thành công
                    }
                }
                else
                {
                    DebugLog("Lỗi khi đồng bộ lên server: " + errorMsg);
                    
                    if (showNotifications)
                    {
                        // TODO: Hiển thị thông báo lỗi
                    }
                }
                
                _isSyncing = false;
                onComplete?.Invoke(success);
            }));
        }
          /// <summary>
        /// Cập nhật dữ liệu từ game vào UserData
        /// </summary>
        private void UpdateUserDataFromGame()
        {
            if (GameAPI.Instance.PlayerData == null)
            {
                DebugLog("Không thể cập nhật dữ liệu: PlayerData is null");
                return;
            }

            DebugLog("Bắt đầu cập nhật dữ liệu từ game...");

            try
            {
                // 1. Cập nhật tiền từ ScoreManager
                if (ScoreManager.Instance != null)
                {
                    int currentMoney = ScoreManager.Score;
                    GameAPI.Instance.PlayerData.money = currentMoney;
                    DebugLog($"Cập nhật tiền từ ScoreManager: {currentMoney}");
                }

                // 2. Cập nhật máu từ StatsHandler
                StatsHandler playerStatsHandler = FindObjectOfType<StatsHandler>();
                if (playerStatsHandler != null)
                {
                    var healthStat = playerStatsHandler.GetStat("Health");
                    if (healthStat is DevionGames.StatSystem.Attribute healthAttribute)
                    {
                        GameAPI.Instance.PlayerData.health = healthAttribute.CurrentValue;
                        DebugLog($"Cập nhật máu từ StatsHandler: {GameAPI.Instance.PlayerData.health}");
                    }
                }

                // 3. Cập nhật vũ khí và đạn
                WeaponManager weaponManager = FindObjectOfType<WeaponManager>();
                if (weaponManager != null)
                {
                    // Sử dụng extension method để sync weapons
                    GameAPI.Instance.PlayerData.SyncWeaponsFromGame();
                    DebugLog("Đã đồng bộ dữ liệu vũ khí");
                }

                // 4. Cập nhật timestamp
                GameAPI.Instance.PlayerData.lastLoginDate = System.DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

                DebugLog("Hoàn thành cập nhật dữ liệu từ game");
            }
            catch (System.Exception ex)
            {
                DebugLog($"Lỗi khi cập nhật dữ liệu từ game: {ex.Message}");
            }
        }
          /// <summary>
        /// Áp dụng dữ liệu từ UserData vào game
        /// </summary>
        private void ApplyUserDataToGame()
        {
            if (GameAPI.Instance.PlayerData == null)
            {
                DebugLog("Không thể áp dụng dữ liệu: PlayerData is null");
                return;
            }

            DebugLog("Bắt đầu áp dụng dữ liệu vào game...");

            try
            {
                // 1. Áp dụng tiền vào ScoreManager
                if (ScoreManager.Instance != null)
                {
                    ScoreManager.Score = GameAPI.Instance.PlayerData.money;
                    DebugLog($"Áp dụng tiền vào ScoreManager: {GameAPI.Instance.PlayerData.money}");
                }

                // 2. Áp dụng máu vào StatsHandler
                StatsHandler playerStatsHandler = FindObjectOfType<StatsHandler>();
                if (playerStatsHandler != null)
                {
                    var healthStat = playerStatsHandler.GetStat("Health");
                    if (healthStat is DevionGames.StatSystem.Attribute healthAttribute)
                    {
                        healthAttribute.CurrentValue = GameAPI.Instance.PlayerData.health;
                        DebugLog($"Áp dụng máu vào StatsHandler: {GameAPI.Instance.PlayerData.health}");
                    }
                }

                // 3. Áp dụng dữ liệu vũ khí
                if (GameAPI.Instance.PlayerData.weapons != null)
                {
                    GameAPI.Instance.PlayerData.ApplyWeaponsToGame();
                    DebugLog("Đã áp dụng dữ liệu vũ khí vào game");
                }

                DebugLog("Hoàn thành áp dụng dữ liệu vào game");
            }
            catch (System.Exception ex)
            {
                DebugLog($"Lỗi khi áp dụng dữ liệu vào game: {ex.Message}");
            }
        }
          /// <summary>
        /// Lưu dữ liệu người dùng đồng bộ (sử dụng khi thoát game)
        /// </summary>
        private void SaveUserDataImmediate()
        {
            DebugLog("Thực hiện lưu dữ liệu ngay lập tức");
            
            // Trước khi lưu, cập nhật dữ liệu từ game
            UpdateUserDataFromGame();
            
            // Sử dụng coroutine để save data
            StartCoroutine(GameAPI.Instance.SavePlayerData((success, msg) => {
                DebugLog($"Kết quả lưu dữ liệu: {(success ? "Thành công" : "Thất bại - " + msg)}");
            }));
        }
        
        /// <summary>
        /// Ghi log debug
        /// </summary>
        private void DebugLog(string message)
        {
            if (debugMode)
            {
                Debug.Log($"[UserDataSync] {message}");
            }
        }
        
        /// <summary>
        /// Lấy thời gian từ lần đồng bộ cuối cùng
        /// </summary>
        public TimeSpan GetTimeSinceLastSync()
        {
            return DateTime.Now - _lastSyncTime;
        }
    }

