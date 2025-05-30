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

            // Start login process
            StartCoroutine(LoginProcess(username, password));
        }

        private IEnumerator LoginProcess(string username, string password)
        {
            yield return StartCoroutine(GameAPI.Instance.Login(username, password, (loginSuccess, errorMessage) =>
            {
                if (loginSuccess)
                {
                    // Store current user data
                    _currentUserData = GameAPI.Instance.PlayerData;

                    // Verify we have user data with identity
                    if (_currentUserData != null)
                    {
                        DebugLog($"Login successful - User: {_currentUserData.username}, ID: {_currentUserData.id}");
                    }
                    else
                    {
                        DebugLog("Warning: Login successful but no player data received");
                    }

                    // Save login info if remember me is checked
                    if (rememberMeToggle != null && rememberMeToggle.isOn)
                    {
                        PlayerPrefs.SetString("LastUsername", username);
                        PlayerPrefs.Save();
                    }

                    // Notify login success
                    OnLoginSuccess?.Invoke(_currentUserData);

                    // Load game scene
                    LoadGameScene();
                }
                else
                {
                    // Show error
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

            string username = registerUsernameInput.text.Trim();
            string email = registerEmailInput.text.Trim();
            string password = registerPasswordInput.text;
            string confirmPassword = registerConfirmPasswordInput.text;

            // Xác thực đầu vào chi tiết
            if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(email) ||
                string.IsNullOrEmpty(password) || string.IsNullOrEmpty(confirmPassword))
            {
                ShowRegisterError("Vui lòng điền đầy đủ thông tin");
                return;
            }

            // Kiểm tra độ dài username
            if (username.Length < 3)
            {
                ShowRegisterError("Tên đăng nhập phải có ít nhất 3 ký tự");
                return;
            }

            if (username.Length > 30)
            {
                ShowRegisterError("Tên đăng nhập không được quá 30 ký tự");
                return;
            }

            // Kiểm tra ký tự đặc biệt trong username
            if (!System.Text.RegularExpressions.Regex.IsMatch(username, @"^[a-zA-Z0-9_]+$"))
            {
                ShowRegisterError("Tên đăng nhập chỉ được chứa chữ cái, số và dấu gạch dưới");
                return;
            }

            // Kiểm tra độ dài password
            if (password.Length < 6)
            {
                ShowRegisterError("Mật khẩu phải có ít nhất 6 ký tự");
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

            // Clear any previous errors
            if (registerErrorText != null)
                registerErrorText.gameObject.SetActive(false);

            // Hiển thị panel đang tải
            ShowLoadingPanel();

            DebugLog($"Starting registration for username: {username}, email: {email}");

            // Start registration process
            StartCoroutine(RegisterProcess(username, email, password));
        }

        private IEnumerator RegisterProcess(string username, string email, string password)
        {
            yield return StartCoroutine(GameAPI.Instance.Register(username, email, password, (registerSuccess, errorMessage) =>
            {
                if (registerSuccess)
                {
                    // Lưu thông tin người dùng
                    _currentUserData = GameAPI.Instance.PlayerData;

                    DebugLog($"Registration successful for user: {username}");

                    // Verify we have user data
                    if (_currentUserData != null)
                    {
                        DebugLog($"User data received - Username: {_currentUserData.username}, ID: {_currentUserData.id}");
                    }
                    else
                    {
                        DebugLog("Warning: Registration successful but no user data received");
                    }

                    // Thông báo đăng ký thành công
                    OnRegisterSuccess?.Invoke(_currentUserData);

                    // Tải scene game
                    LoadGameScene();
                }
                else
                {
                    // Hiển thị lỗi
                    DebugLog($"Registration failed: {errorMessage}");
                    SetActivePanel(registerPanel);
                    ShowRegisterError(ParseRegistrationError(errorMessage));
                }
            }));
        }

        /// <summary>
        /// Parse registration error messages into Vietnamese
        /// </summary>
        private string ParseRegistrationError(string errorMessage)
        {
            if (string.IsNullOrEmpty(errorMessage))
            {
                return "Đăng ký thất bại do lỗi không xác định. Vui lòng thử lại.";
            }

            string lowerError = errorMessage.ToLower();

            // Username already exists
            if (lowerError.Contains("username") && (lowerError.Contains("already") || lowerError.Contains("exists") || lowerError.Contains("taken")))
            {
                return "Tên đăng nhập đã được sử dụng. Vui lòng chọn tên khác.";
            }

            // Email already exists
            if (lowerError.Contains("email") && (lowerError.Contains("already") || lowerError.Contains("exists") || lowerError.Contains("registered")))
            {
                return "Email đã được đăng ký. Vui lòng sử dụng email khác hoặc thử đăng nhập.";
            }

            // Validation errors
            if (lowerError.Contains("validation") || lowerError.Contains("invalid input"))
            {
                return "Thông tin đăng ký không hợp lệ. Vui lòng kiểm tra lại.";
            }

            // Database/server errors
            if (lowerError.Contains("database") || lowerError.Contains("server"))
            {
                return "Lỗi server tạm thời. Vui lòng thử lại sau ít phút.";
            }

            // Connection errors
            if (lowerError.Contains("connection") || lowerError.Contains("connectivity"))
            {
                return "Không thể kết nối đến server. Vui lòng kiểm tra internet và thử lại.";
            }

            // Generic 400 errors
            if (lowerError.Contains("400") || lowerError.Contains("bad request"))
            {
                return "Yêu cầu đăng ký không hợp lệ. Vui lòng kiểm tra thông tin và thử lại.";
            }

            // Return original message if no specific pattern matches
            return $"Đăng ký thất bại: {errorMessage}";
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
            // Đảm bảo không có player nào khác trong scene trước khi load
            CleanupExistingPlayers();

            SceneManager.LoadScene(gameSceneName);
        }

        /// <summary>
        /// Dọn dẹp các player object trùng lặp trước khi load scene mới
        /// </summary>
        private void CleanupExistingPlayers()
        {
            // Tìm tất cả GameObject có tag "Player"
            GameObject[] players = GameObject.FindGameObjectsWithTag("Player");

            if (players.Length > 1)
            {
                Debug.LogWarning($"Found {players.Length} players, cleaning up duplicates...");

                // Giữ lại player đầu tiên, xóa các player khác
                for (int i = 1; i < players.Length; i++)
                {
                    Debug.LogWarning($"Destroying duplicate player: {players[i].name}");
                    Destroy(players[i]);
                }
            }

            // Cũng dọn dẹp các component trùng lặp
            PlayerMovement[] movements = FindObjectsOfType<PlayerMovement>();
            if (movements.Length > 1)
            {
                for (int i = 1; i < movements.Length; i++)
                {
                    Debug.LogWarning($"Destroying duplicate PlayerMovement on: {movements[i].gameObject.name}");
                    Destroy(movements[i].gameObject);
                }
            }
        }


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


        private IEnumerator LoadUserDataCoroutine(Action<bool> onComplete)
        {
            yield return StartCoroutine(GameAPI.Instance.GetPlayerData((success, errorMsg) =>
            {
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
        /// Coroutine tự động lưu dữ liệu theo chu kỳ
        /// </summary>
        private IEnumerator AutoSaveCoroutine()
        {
            while (true)
            {
                yield return new WaitForSeconds(autoSaveInterval);

                if (GameAPI.Instance.IsLoggedIn && !_isSyncing)
                {
                    DebugLog("Đang thực hiện auto save...");
                    // Có thể thêm logic auto save ở đây nếu cần
                    // Ví dụ: SavePlayerData();
                }
            }
        }


        private bool IsValidEmail(string email)
        {
            if (string.IsNullOrEmpty(email))
                return false;

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


        private void DebugLog(string message)
        {
            if (debugMode)
            {
                Debug.Log($"[LoginManager] {message}");
            }
        }
    }
}
