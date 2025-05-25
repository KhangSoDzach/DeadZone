using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using DevionGames;

namespace Scripts.API
{
    /// <summary>
    /// Đồng bộ dữ liệu người dùng giữa client và server
    /// Có thể đặt component này vào scene chính của game để đảm bảo dữ liệu luôn được đồng bộ
    /// </summary>
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
        /// Coroutine để đồng bộ dữ liệu từ server
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
                    
                    // TODO: Cập nhật dữ liệu vào game
                    ApplyUserDataToGame();
                }
                else
                {
                    DebugLog("Lỗi khi đồng bộ từ server: " + errorMsg);
                    
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
            // TODO: Cập nhật dữ liệu từ game vào PlayerData
            // Thực hiện cập nhật từ các thành phần khác nhau của game
            // ScoreManager, HealthManager, WeaponManager, etc.
            
            // Ví dụ:
            // if (ScoreManager.Instance != null)
            // {
            //     GameAPI.Instance.PlayerData.score = ScoreManager.Instance.Score;
            // }
            
            DebugLog("Đã cập nhật dữ liệu từ game");
        }
        
        /// <summary>
        /// Áp dụng dữ liệu từ UserData vào game
        /// </summary>
        private void ApplyUserDataToGame()
        {
            // TODO: Áp dụng dữ liệu từ PlayerData vào game
            // Cập nhật các thành phần khác nhau của game từ dữ liệu người dùng
            
            // Ví dụ:
            // if (ScoreManager.Instance != null)
            // {
            //     ScoreManager.Instance.SetScore(GameAPI.Instance.PlayerData.score);
            // }
            
            DebugLog("Đã áp dụng dữ liệu vào game");
        }
        
        /// <summary>
        /// Lưu dữ liệu người dùng đồng bộ (sử dụng khi thoát game)
        /// </summary>
        private void SaveUserDataImmediate()
        {
            DebugLog("Thực hiện lưu dữ liệu ngay lập tức");
            
            // Lưu ý: API thực tế vẫn là bất đồng bộ, chúng ta chỉ cập nhật dữ liệu cục bộ
            // và bắt đầu quá trình lưu, không đợi kết quả
            
            // TODO: Thêm code để lưu dữ liệu người dùng đồng bộ nếu cần
            GameAPI.Instance.SavePlayerData((success, msg) => {
                DebugLog($"Kết quả lưu dữ liệu: {(success ? "Thành công" : "Thất bại - " + msg)}");
            });
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
}
