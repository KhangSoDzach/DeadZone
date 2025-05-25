using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DevionGames;
using Scripts.API;

namespace Scripts.UI
{
    /// <summary>
    /// Quản lý UI đăng nhập/đăng ký và phân luồng người dùng
    /// Đặt component này vào Canvas trong scene đăng nhập
    /// </summary>
    public class LoginUIManager : MonoBehaviour
    {
        [Header("Panels")]
        [SerializeField] private GameObject welcomePanel;
        [SerializeField] private GameObject loginPanel;
        [SerializeField] private GameObject registerPanel;
        [SerializeField] private GameObject loadingPanel;
        [SerializeField] private GameObject errorPanel;
          [Header("Welcome UI")]
        [SerializeField] private Button welcomeLoginButton;
        [SerializeField] private Button welcomeRegisterButton;
        [SerializeField] private Button welcomeOfflineButton;
        [SerializeField] private TMP_Text welcomeVersionText;
        
        [Header("Login UI")]
        [SerializeField] private TMP_InputField loginUsernameInput;
        [SerializeField] private TMP_InputField loginPasswordInput;
        [SerializeField] private Button loginSubmitButton;
        [SerializeField] private Button loginBackButton;
        [SerializeField] private Toggle rememberMeToggle;
        [SerializeField] private Button forgotPasswordButton;
        [SerializeField] private TMP_Text loginErrorText;
          [Header("Register UI")]
        [SerializeField] private TMP_InputField registerUsernameInput;
        [SerializeField] private TMP_InputField registerEmailInput;
        [SerializeField] private TMP_InputField registerPasswordInput;
        [SerializeField] private TMP_InputField registerConfirmPasswordInput;
        [SerializeField] private Toggle termsToggle;
        [SerializeField] private Button registerSubmitButton;
        [SerializeField] private Button registerBackButton;
        [SerializeField] private TMP_Text registerErrorText;
        
        [Header("Loading UI")]
        [SerializeField] private TMP_Text loadingStatusText;
        [SerializeField] private Image loadingProgressBar;
        
        [Header("Error UI")]
        [SerializeField] private TMP_Text errorMessageText;
        [SerializeField] private Button errorCloseButton;
        
        [Header("Animation")]
        [SerializeField] private float fadeSpeed = 0.5f;
        [SerializeField] private bool useAnimation = true;
        
        [Header("Settings")]
        [SerializeField] private string gameSceneName = "Scene_A";
        [SerializeField] private string offlineSceneName = "OfflineMode";
        
        // Tham chiếu đến LoginManager
        private LoginManager _loginManager;
        
        private void Start()
        {
            // Khởi tạo LoginManager
            _loginManager = LoginManager.Instance;
            
            // Thiết lập sự kiện nút
            SetupButtonListeners();
            
            // Thiết lập UI
            SetupUI();
            
            // Hiển thị panel welcome mặc định
            ShowPanel(welcomePanel);
        }

        private void SetupButtonListeners()
        {
            // Welcome panel
            if (welcomeLoginButton) welcomeLoginButton.onClick.AddListener(ShowLoginPanel);
            if (welcomeRegisterButton) welcomeRegisterButton.onClick.AddListener(ShowRegisterPanel);
            if (welcomeOfflineButton) welcomeOfflineButton.onClick.AddListener(StartOfflineMode);
            
            // Login panel
            if (loginSubmitButton) loginSubmitButton.onClick.AddListener(HandleLogin);
            if (loginBackButton) loginBackButton.onClick.AddListener(() => ShowPanel(welcomePanel));
            if (forgotPasswordButton) forgotPasswordButton.onClick.AddListener(HandleForgotPassword);
            
            // Register panel
            if (registerSubmitButton) registerSubmitButton.onClick.AddListener(HandleRegister);
            if (registerBackButton) registerBackButton.onClick.AddListener(() => ShowPanel(welcomePanel));
            
            // Error panel
            if (errorCloseButton) errorCloseButton.onClick.AddListener(() => ShowPanel(welcomePanel));
        }
        
        private void SetupUI()
        {
            // Hiển thị phiên bản
            if (welcomeVersionText)
            {
                welcomeVersionText.text = "v" + Application.version;
            }
            
            // Ẩn thông báo lỗi
            if (loginErrorText) loginErrorText.gameObject.SetActive(false);
            if (registerErrorText) registerErrorText.gameObject.SetActive(false);
            
            // Thiết lập giá trị mặc định
            if (rememberMeToggle) rememberMeToggle.isOn = true;
            if (termsToggle) termsToggle.isOn = false;
            
            // Tải username đã lưu
            if (loginUsernameInput)
            {
                string savedUsername = PlayerPrefs.GetString("LastUsername", "");
                loginUsernameInput.text = savedUsername;
            }
        }
        
        public void ShowLoginPanel()
        {
            ShowPanel(loginPanel);
            if (loginErrorText) loginErrorText.gameObject.SetActive(false);
              // Focus vào trường nhập liệu đầu tiên
            if (loginUsernameInput)
            {
                loginUsernameInput.Select();
                loginUsernameInput.ActivateInputField();
            }
        }
        
        public void ShowRegisterPanel()
        {
            ShowPanel(registerPanel);
            if (registerErrorText) registerErrorText.gameObject.SetActive(false);
              // Focus vào trường nhập liệu đầu tiên
            if (registerUsernameInput)
            {
                registerUsernameInput.Select();
                registerUsernameInput.ActivateInputField();
            }
        }
        
        private void ShowPanel(GameObject panelToShow)
        {
            // Ẩn tất cả các panel
            if (welcomePanel) welcomePanel.SetActive(panelToShow == welcomePanel);
            if (loginPanel) loginPanel.SetActive(panelToShow == loginPanel);
            if (registerPanel) registerPanel.SetActive(panelToShow == registerPanel);
            if (loadingPanel) loadingPanel.SetActive(panelToShow == loadingPanel);
            if (errorPanel) errorPanel.SetActive(panelToShow == errorPanel);
            
            // TODO: Thêm hiệu ứng animation nếu được bật
            if (useAnimation)
            {
                // Thực hiện hiệu ứng fade-in cho panel hiện tại
            }
        }
        
        private void HandleLogin()
        {
            if (loginUsernameInput == null || loginPasswordInput == null)
            {
                Debug.LogError("LoginUIManager: Missing username or password input field");
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
            
            // Hiển thị màn hình loading
            ShowPanel(loadingPanel);
            if (loadingStatusText) loadingStatusText.text = "Đang đăng nhập...";
            
            // Gọi API đăng nhập
            StartCoroutine(GameAPI.Instance.Login(username, password, (success, errorMessage) => {
                if (success)
                {
                    // Lưu username nếu chọn remember me
                    if (rememberMeToggle && rememberMeToggle.isOn)
                    {
                        PlayerPrefs.SetString("LastUsername", username);
                        PlayerPrefs.Save();
                    }
                    
                    if (loadingStatusText) loadingStatusText.text = "Đang tải dữ liệu người dùng...";
                    
                    // Tải dữ liệu người dùng
                    StartCoroutine(GameAPI.Instance.GetPlayerData((dataSuccess, dataError) => {
                        if (dataSuccess)
                        {
                            if (loadingStatusText) loadingStatusText.text = "Đăng nhập thành công!";
                            
                            // Chuyển sang scene game chính
                            StartCoroutine(LoadGameScene());
                        }
                        else
                        {
                            ShowLoginError("Đăng nhập thành công nhưng không thể tải dữ liệu: " + dataError);
                        }
                    }));
                }
                else
                {
                    ShowLoginError("Đăng nhập thất bại: " + errorMessage);
                }
            }));
        }
        
        /// <summary>
        /// Xử lý quá trình đăng ký
        /// </summary>
        private void HandleRegister()
        {
            if (registerUsernameInput == null || registerEmailInput == null || 
                registerPasswordInput == null || registerConfirmPasswordInput == null)
            {
                Debug.LogError("LoginUIManager: Missing register input fields");
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
            
            // Kiểm tra định dạng email
            if (!IsValidEmail(email))
            {
                ShowRegisterError("Địa chỉ email không hợp lệ");
                return;
            }
            
            // Kiểm tra mật khẩu trùng khớp
            if (password != confirmPassword)
            {
                ShowRegisterError("Mật khẩu xác nhận không khớp");
                return;
            }
            
            // Kiểm tra đã đồng ý điều khoản chưa
            if (termsToggle && !termsToggle.isOn)
            {
                ShowRegisterError("Vui lòng đồng ý với điều khoản sử dụng");
                return;
            }
            
            // Hiển thị màn hình loading
            ShowPanel(loadingPanel);
            if (loadingStatusText) loadingStatusText.text = "Đang đăng ký...";
            
            // Gọi API đăng ký
            StartCoroutine(GameAPI.Instance.Register(username, email, password, (success, errorMessage) => {
                if (success)
                {
                    if (loadingStatusText) loadingStatusText.text = "Đăng ký thành công! Đang tải dữ liệu...";
                    
                    // Tải dữ liệu người dùng
                    StartCoroutine(GameAPI.Instance.GetPlayerData((dataSuccess, dataError) => {
                        if (dataSuccess)
                        {
                            if (loadingStatusText) loadingStatusText.text = "Đăng ký thành công!";
                            
                            // Chuyển sang scene game chính
                            StartCoroutine(LoadGameScene());
                        }
                        else
                        {
                            ShowRegisterError("Đăng ký thành công nhưng không thể tải dữ liệu: " + dataError);
                        }
                    }));
                }
                else
                {
                    ShowRegisterError("Đăng ký thất bại: " + errorMessage);
                }
            }));
        }
        
        /// <summary>
        /// Xử lý quên mật khẩu
        /// </summary>
        private void HandleForgotPassword()
        {
            // TODO: Triển khai chức năng quên mật khẩu
            // Hiển thị dialog nhập email để khôi phục mật khẩu
            ShowLoginError("Tính năng quên mật khẩu đang được phát triển");
        }
        
        /// <summary>
        /// Chuyển sang chế độ offline
        /// </summary>
        private void StartOfflineMode()
        {
            // TODO: Triển khai chế độ offline
            ShowPanel(loadingPanel);
            if (loadingStatusText) loadingStatusText.text = "Đang khởi tạo chế độ offline...";
            
            // Tải scene offline
            StartCoroutine(LoadOfflineScene());
        }
        
        /// <summary>
        /// Hiển thị lỗi đăng nhập
        /// </summary>
        private void ShowLoginError(string message)
        {
            ShowPanel(loginPanel);
            
            if (loginErrorText)
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
            ShowPanel(registerPanel);
            
            if (registerErrorText)
            {
                registerErrorText.gameObject.SetActive(true);
                registerErrorText.text = message;
            }
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
        /// Tải scene game chính
        /// </summary>
        private IEnumerator LoadGameScene()
        {
            // Hiển thị tiến trình tải
            float progress = 0;
            
            // Giả lập tiến trình tải (có thể thay bằng AsyncOperation thực tế)
            while (progress < 1)
            {
                progress += Time.deltaTime;
                if (loadingProgressBar) loadingProgressBar.fillAmount = progress;
                yield return null;
            }
            
            // Tải scene chính
            UnityEngine.SceneManagement.SceneManager.LoadScene(gameSceneName);
        }
        
        /// <summary>
        /// Tải scene chế độ offline
        /// </summary>
        private IEnumerator LoadOfflineScene()
        {
            // Hiển thị tiến trình tải
            float progress = 0;
            
            // Giả lập tiến trình tải (có thể thay bằng AsyncOperation thực tế)
            while (progress < 1)
            {
                progress += Time.deltaTime * 2; // Tải nhanh hơn
                if (loadingProgressBar) loadingProgressBar.fillAmount = progress;
                yield return null;
            }
            
            // Tải scene chế độ offline
            UnityEngine.SceneManagement.SceneManager.LoadScene(offlineSceneName);
        }
    }
}
