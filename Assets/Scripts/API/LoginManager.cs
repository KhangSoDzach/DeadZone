using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DevionGames;
using UnityEngine.SceneManagement;

namespace Scripts.API
{
    /// <summary>
    /// LoginManager - Quản lý quá trình đăng nhập, đăng ký và lưu trữ thông tin người dùng.
    /// Gắn script này vào GameObject trong scene đăng nhập/đăng ký.
    /// </summary>
    public class LoginManager : MonoBehaviour
    {
        private static LoginManager _instance;
        public static LoginManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    GameObject go = new GameObject("LoginManager");
                    _instance = go.AddComponent<LoginManager>();
                    DontDestroyOnLoad(go);
                }
                return _instance;
            }
        }
        
        [Header("Canvas Panels")]
        [SerializeField] private GameObject loginPanel;
        [SerializeField] private GameObject registerPanel;
        [SerializeField] private GameObject loadingPanel;
        [SerializeField] private GameObject errorPanel;
        
        [Header("Login UI")]
        [SerializeField] private TMP_InputField loginUsernameInput;
        [SerializeField] private TMP_InputField loginPasswordInput;
        [SerializeField] private Button loginButton;
        [SerializeField] private TMP_Text loginErrorText;
        [SerializeField] private Toggle rememberMeToggle;
        
        [Header("Register UI")]
        [SerializeField] private TMP_InputField registerUsernameInput;
        [SerializeField] private TMP_InputField registerEmailInput;
        [SerializeField] private TMP_InputField registerPasswordInput;
        [SerializeField] private TMP_InputField registerConfirmPasswordInput;
        [SerializeField] private Button registerButton;
        [SerializeField] private TMP_Text registerErrorText;
        
        [Header("Error Panel")]
        [SerializeField] private TMP_Text errorMessageText;
        [SerializeField] private Button errorCloseButton;
        
        [Header("Settings")]
        [SerializeField] private string gameSceneName = "MainHub";
        [SerializeField] private float autoSaveInterval = 300f; // 5 phút
        [SerializeField] private bool debugMode = false;
        
        // Sự kiện khi đăng nhập thành công
        public delegate void LoginSuccessHandler(PlayerDataModel playerData);
        public event LoginSuccessHandler OnLoginSuccess;
        
        // Sự kiện khi đăng ký thành công
        public delegate void RegisterSuccessHandler(PlayerDataModel playerData);
        public event RegisterSuccessHandler OnRegisterSuccess;
        
        // Thông tin người dùng hiện tại
        private PlayerDataModel _currentUserData;
        public PlayerDataModel CurrentUserData => _currentUserData;
        
        // Biến theo dõi trạng thái đồng bộ
        private bool _isSyncing = false;
        private bool _isInitialized = false;
        
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
            
            Initialize();
        }
        
        /// <summary>
        /// Khởi tạo LoginManager
        /// </summary>
        private void Initialize()
        {
            if (_isInitialized) return;
            
            // Thiết lập sự kiện nút
            if (loginButton != null)
                loginButton.onClick.AddListener(OnLoginButtonClicked);
                
            if (registerButton != null)
                registerButton.onClick.AddListener(OnRegisterButtonClicked);
                
            if (errorCloseButton != null)
                errorCloseButton.onClick.AddListener(() => HideErrorPanel());
            
            // Ẩn các panel lỗi và loading
            if (loginErrorText != null)
                loginErrorText.gameObject.SetActive(false);
                
            if (registerErrorText != null)
                registerErrorText.gameObject.SetActive(false);
                
            if (errorPanel != null)
                errorPanel.SetActive(false);
                
            if (loadingPanel != null)
                loadingPanel.SetActive(false);
            
            // Mặc định hiển thị panel đăng nhập
            ShowLoginPanel();
            
            // Khởi tạo auto save
            StartCoroutine(AutoSaveCoroutine());
            
            _isInitialized = true;
            
            DebugLog("LoginManager đã được khởi tạo");
        }
        
        /// <summary>
        /// Hiển thị panel đăng nhập
        /// </summary>
        public void ShowLoginPanel()
        {
            SetActivePanel(loginPanel);
            if (loginErrorText != null)
                loginErrorText.gameObject.SetActive(false);
        }
        
        /// <summary>
        /// Hiển thị panel đăng ký
        /// </summary>
        public void ShowRegisterPanel()
        {
            SetActivePanel(registerPanel);
            if (registerErrorText != null)
                registerErrorText.gameObject.SetActive(false);
        }
        
        /// <summary>
        /// Hiển thị panel đang tải
        /// </summary>
        public void ShowLoadingPanel()
        {
            SetActivePanel(loadingPanel);
        }
        
        /// <summary>
        /// Hiển thị panel lỗi với thông báo tùy chỉnh
        /// </summary>
        public void ShowErrorPanel(string message)
        {
            if (errorPanel != null)
            {
                errorPanel.SetActive(true);
                if (errorMessageText != null)
                    errorMessageText.text = message;
            }
        }
        
        /// <summary>
        /// Ẩn panel lỗi
        /// </summary>
        public void HideErrorPanel()
        {
            if (errorPanel != null)
                errorPanel.SetActive(false);
        }
        
        /// <summary>
        /// Thiết lập panel nào được hiển thị
        /// </summary>
        private void SetActivePanel(GameObject activePanel)
        {
            if (loginPanel != null)
                loginPanel.SetActive(activePanel == loginPanel);
                
            if (registerPanel != null)
                registerPanel.SetActive(activePanel == registerPanel);
                
            if (loadingPanel != null)
                loadingPanel.SetActive(activePanel == loadingPanel);
                
            // Không thay đổi errorPanel vì nó có thể hiển thị cùng với các panel khác
        }
        
        /// <summary>
        /// Xử lý khi nút đăng nhập được nhấn
        /// </summary>
        public void OnLoginButtonClicked()
        {
            if (loginUsernameInput == null || loginPasswordInput == null)
            {
                DebugLog("Lỗi: Thiếu InputField đăng nhập");
                return;
            }
            
            string username = loginUsernameInput.text;
            string password = loginPasswordInput.text;
            
            // Xác thực đầu vào
            if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
            {
                ShowLoginError("Vui lòng nhập tên đăng nhập và mật khẩu");
                return;
            }
            
            // Hiển thị panel đang tải
            ShowLoadingPanel();
            
            // Gọi API đăng nhập
            StartCoroutine(GameAPI.Instance.Login(username, password, (success, errorMessage) => {
                if (success)
                {
                    // Lưu thông tin người dùng
                    _currentUserData = GameAPI.Instance.PlayerData;
                    
                    // Lưu thông tin đăng nhập nếu được chọn
                    if (rememberMeToggle != null && rememberMeToggle.isOn)
                    {
                        PlayerPrefs.SetString("LastUsername", username);
                        PlayerPrefs.Save();
                    }
                    
                    // Thông báo đăng nhập thành công
                    OnLoginSuccess?.Invoke(_currentUserData);
                    
                    // Tải scene game
                    LoadGameScene();
                }
                else
                {
                    // Hiển thị lỗi
                    SetActivePanel(loginPanel);
                    ShowLoginError("Đăng nhập thất bại: " + errorMessage);
                }
            }));
        }
        
        /// <summary>
        /// Xử lý khi nút đăng ký được nhấn
        /// </summary>
        public void OnRegisterButtonClicked()
        {
            if (registerUsernameInput == null || registerEmailInput == null || 
                registerPasswordInput == null || registerConfirmPasswordInput == null)
            {
                DebugLog("Lỗi: Thiếu InputField đăng ký");
                return;
            }
            
            string username = registerUsernameInput.text;
            string email = registerEmailInput.text;
            string password = registerPasswordInput.text;
            string confirmPassword = registerConfirmPasswordInput.text;
            
            // Xác thực đầu vào
            if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(email) || 
                string.IsNullOrEmpty(password) || string.IsNullOrEmpty(confirmPassword))
            {
                ShowRegisterError("Vui lòng điền đầy đủ thông tin");
                return;
            }
            
            // Kiểm tra mật khẩu xác nhận
            if (password != confirmPassword)
            {
                ShowRegisterError("Mật khẩu xác nhận không khớp");
                return;
            }
            
            // Kiểm tra định dạng email
            if (!IsValidEmail(email))
            {
                ShowRegisterError("Email không hợp lệ");
                return;
            }
            
            // Hiển thị panel đang tải
            ShowLoadingPanel();
            
            // Gọi API đăng ký
            StartCoroutine(GameAPI.Instance.Register(username, email, password, (success, errorMessage) => {
                if (success)
                {
                    // Lưu thông tin người dùng
                    _currentUserData = GameAPI.Instance.PlayerData;
                    
                    // Thông báo đăng ký thành công
                    OnRegisterSuccess?.Invoke(_currentUserData);
                    
                    // Tải scene game
                    LoadGameScene();
                }
                else
                {
                    // Hiển thị lỗi
                    SetActivePanel(registerPanel);
                    ShowRegisterError("Đăng ký thất bại: " + errorMessage);
                }
            }));
        }
        
        /// <summary>
        /// Hiển thị lỗi đăng nhập
        /// </summary>
        private void ShowLoginError(string message)
        {
            if (loginErrorText != null)
            {
                loginErrorText.gameObject.SetActive(true);
                loginErrorText.text = message;
            }
        }
        
        /// <summary>
        /// Hiển thị lỗi đăng ký
        /// </summary>
        private void ShowRegisterError(string message)
        {
            if (registerErrorText != null)
            {
                registerErrorText.gameObject.SetActive(true);
                registerErrorText.text = message;
            }
        }
        
        /// <summary>
        /// Tải scene game chính
        /// </summary>
        private void LoadGameScene()
        {
            SceneManager.LoadScene(gameSceneName);
        }
        
        /// <summary>
        /// Kiểm tra định dạng email
        /// </summary>
        private bool IsValidEmail(string email)
        {
            try
            {
                var addr = new System.Net.Mail.MailAddress(email);
                return addr.Address == email;
            }
            catch
            {
                return false;
            }
        }
        
        /// <summary>
        /// Đăng xuất người dùng hiện tại
        /// </summary>
        public void Logout()
        {
            // Xóa dữ liệu người dùng
            _currentUserData = null;
            
            // Gọi hàm đăng xuất của GameAPI
            GameAPI.Instance.Logout();
        }
        
        /// <summary>
        /// Tự động lưu dữ liệu người dùng theo định kỳ
        /// </summary>
        private IEnumerator AutoSaveCoroutine()
        {
            while (true)
            {
                yield return new WaitForSeconds(autoSaveInterval);
                
                // Chỉ lưu khi đã đăng nhập
                if (GameAPI.Instance.IsLoggedIn && _currentUserData != null)
                {
                    SaveUserData();
                }
            }
        }
        
        /// <summary>
        /// Lưu dữ liệu người dùng lên server
        /// </summary>
        public void SaveUserData(Action<bool> onComplete = null)
        {
            if (!GameAPI.Instance.IsLoggedIn || _currentUserData == null)
            {
                DebugLog("Không thể lưu dữ liệu: Người dùng chưa đăng nhập");
                onComplete?.Invoke(false);
                return;
            }
            
            if (_isSyncing)
            {
                DebugLog("Đang trong quá trình đồng bộ. Bỏ qua yêu cầu lưu.");
                onComplete?.Invoke(false);
                return;
            }
            
            _isSyncing = true;
            
            // TODO: Thêm code để cập nhật dữ liệu người dùng từ game
            
            // Bắt đầu lưu dữ liệu
            StartCoroutine(SaveUserDataCoroutine(onComplete));
        }
        
        /// <summary>
        /// Coroutine để lưu dữ liệu người dùng
        /// </summary>
        private IEnumerator SaveUserDataCoroutine(Action<bool> onComplete)
        {
            yield return StartCoroutine(GameAPI.Instance.SavePlayerData((success, errorMsg) => {
                if (success)
                {
                    DebugLog("Đã lưu dữ liệu người dùng thành công");
                }
                else
                {
                    DebugLog("Lỗi khi lưu dữ liệu: " + errorMsg);
                }
                
                _isSyncing = false;
                onComplete?.Invoke(success);
            }));
        }
        
        /// <summary>
        /// Tải dữ liệu người dùng từ server
        /// </summary>
        public void LoadUserData(Action<bool> onComplete = null)
        {
            if (!GameAPI.Instance.IsLoggedIn)
            {
                DebugLog("Không thể tải dữ liệu: Người dùng chưa đăng nhập");
                onComplete?.Invoke(false);
                return;
            }
            
            if (_isSyncing)
            {
                DebugLog("Đang trong quá trình đồng bộ. Bỏ qua yêu cầu tải.");
                onComplete?.Invoke(false);
                return;
            }
            
            _isSyncing = true;
            
            StartCoroutine(LoadUserDataCoroutine(onComplete));
        }
        
        /// <summary>
        /// Coroutine để tải dữ liệu người dùng
        /// </summary>
        private IEnumerator LoadUserDataCoroutine(Action<bool> onComplete)
        {
            yield return StartCoroutine(GameAPI.Instance.GetPlayerData((success, errorMsg) => {
                if (success)
                {
                    _currentUserData = GameAPI.Instance.PlayerData;
                    DebugLog("Đã tải dữ liệu người dùng thành công");
                }
                else
                {
                    DebugLog("Lỗi khi tải dữ liệu: " + errorMsg);
                }
                
                _isSyncing = false;
                onComplete?.Invoke(success);
            }));
        }
        
        /// <summary>
        /// Ghi log debug
        /// </summary>
        private void DebugLog(string message)
        {
            if (debugMode)
            {
                Debug.Log($"[LoginManager] {message}");
            }
        }
    }
}
