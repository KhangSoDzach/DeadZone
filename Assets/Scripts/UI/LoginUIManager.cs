using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;
using Scripts.API;

public class LoginUIManager : MonoBehaviour
{
    [Header("Welcome Panel")]
    [SerializeField] private GameObject welcomePanel;
    [SerializeField] private Button welcomeLoginButton;
    [SerializeField] private Button welcomeRegisterButton;
    [SerializeField] private Button welcomePlayOfflineButton;
    [SerializeField] private Button welcomeContinueButton;
    [SerializeField] private Button welcomeNewGameButton; // Add new game button
    [SerializeField] private Button welcomeLogoutButton; // Add logout button
    [SerializeField] private Button welcomeSettingsButton; // Add settings button
    [SerializeField] private TMP_Text welcomeVersionText;
    [SerializeField] private TMP_Text welcomeUserText; // Add user info text
    
    [Header("Login Panel")]
    [SerializeField] private GameObject loginPanel;
    [SerializeField] private TMP_InputField loginUsernameInput;
    [SerializeField] private TMP_InputField loginPasswordInput;
    [SerializeField] private Button loginSubmitButton;
    [SerializeField] private Button loginBackButton;
    [SerializeField] private TMP_Text loginErrorText;
    [SerializeField] private Toggle rememberMeToggle;
    
    [Header("Register Panel")]
    [SerializeField] private GameObject registerPanel;
    [SerializeField] private TMP_InputField registerUsernameInput;
    [SerializeField] private TMP_InputField registerEmailInput;
    [SerializeField] private TMP_InputField registerPasswordInput;
    [SerializeField] private TMP_InputField registerConfirmPasswordInput;
    [SerializeField] private Button registerSubmitButton;
    [SerializeField] private Button registerBackButton;
    [SerializeField] private TMP_Text registerErrorText;
    
    [Header("Loading Panel")]
    [SerializeField] private GameObject loadingPanel;
    [SerializeField] private TMP_Text loadingStatusText;
    [SerializeField] private Slider loadingProgressBar;
    
    [Header("Settings Panel")]
    [SerializeField] private GameObject settingsPanel;
    
    [Header("Option Panel")]
    [SerializeField] private GameObject optionPanel;
    [SerializeField] private Button optionButton; // Add option button
    
    [Header("Settings")]
    [SerializeField] private string gameSceneName = "Scene_A";
    [SerializeField] private string menuSceneName = "Menu"; // Thêm cài đặt cho scene menu
    [SerializeField] private string offlineSceneName = "Scene_A";
    [SerializeField] private float autoLoginCheckDelay = 1f;
    [SerializeField] private bool debugMode = true;
    
    private bool isInitialized = false;
      private void Start()
    {
        Initialize();
        // Give GameAPI time to check saved authentication first
        StartCoroutine(DelayedUIUpdate());
    }
    
    private IEnumerator DelayedUIUpdate()
    {
        // Wait for GameAPI to complete its initialization
        yield return new WaitForSeconds(autoLoginCheckDelay);
        
        // Check if GameAPI already logged in the user
        if (GameAPI.Instance.IsLoggedIn && GameAPI.Instance.PlayerData != null)
        {
            DebugLog("User already authenticated by GameAPI");
            UpdateWelcomeUI();
            ShowWelcomePanel();
        }
        else
        {
            // Only check for auto-login if GameAPI hasn't already handled it
            DebugLog("No existing authentication, checking for saved session...");
            CheckAutoLogin();
        }
    }
    
    private void Initialize()
    {
        if (isInitialized) return;
        
        SetupButtonListeners();
        SetupUI();
        
        isInitialized = true;
        DebugLog("LoginUIManager initialized");
    }
    
    private void SetupButtonListeners()
    {
        // Welcome panel buttons
        if (welcomeLoginButton) welcomeLoginButton.onClick.AddListener(ShowLoginPanel);
        if (welcomeRegisterButton) welcomeRegisterButton.onClick.AddListener(ShowRegisterPanel);
        if (welcomePlayOfflineButton) welcomePlayOfflineButton.onClick.AddListener(StartOfflineMode);
        if (welcomeContinueButton) welcomeContinueButton.onClick.AddListener(ContinueGame);
        if (welcomeNewGameButton) welcomeNewGameButton.onClick.AddListener(StartNewGame); // Add new game listener
        if (welcomeLogoutButton) welcomeLogoutButton.onClick.AddListener(LogoutUser); // Add logout listener
        if (welcomeSettingsButton) welcomeSettingsButton.onClick.AddListener(ShowSettingsPanel); // Add settings listener
        if (optionButton) optionButton.onClick.AddListener(ShowOptionPanel); // Add option button listener
        
        // Login panel buttons
        if (loginSubmitButton) loginSubmitButton.onClick.AddListener(OnLoginButtonClicked);
        if (loginBackButton) loginBackButton.onClick.AddListener(ShowWelcomePanel);
        
        // Register panel buttons
        if (registerSubmitButton) registerSubmitButton.onClick.AddListener(OnRegisterButtonClicked);
        if (registerBackButton) registerBackButton.onClick.AddListener(ShowWelcomePanel);
    }
    
    private void SetupUI()
    {
        // Hide error texts
        if (loginErrorText) loginErrorText.gameObject.SetActive(false);
        if (registerErrorText) registerErrorText.gameObject.SetActive(false);
        
        // Set version text
        if (welcomeVersionText) welcomeVersionText.text = "Version " + Application.version;
        
        // Update UI based on login status
        UpdateWelcomeUI();
        
        // Show welcome panel by default
        ShowWelcomePanel();
        
        // Ensure settings panel is hidden
        if (settingsPanel) settingsPanel.SetActive(false);
        // Ensure option panel is hidden
        if (optionPanel) optionPanel.SetActive(false);
    }      private void CheckAutoLogin()
    {
        string savedToken = PlayerPrefs.GetString("AuthToken", "");
        if (!string.IsNullOrEmpty(savedToken))
        {
            DebugLog("Found saved token, attempting auto-login...");
            ShowLoadingPanel();
            
            // First verify the token
            StartCoroutine(GameAPI.Instance.LoginWithToken(savedToken, (success, error) => {
                if (success)
                {
                    DebugLog("Auto-login successful, force loading fresh player data...");
                    
                    // Always force load fresh data after auto-login to ensure we have latest save
                    StartCoroutine(GameAPI.Instance.GetPlayerData((dataSuccess, dataError) => {
                        if (dataSuccess && GameAPI.Instance.PlayerData != null && 
                            !string.IsNullOrEmpty(GameAPI.Instance.PlayerData.id))
                        {
                            DebugLog("Fresh player data loaded after auto-login");
                            UpdateWelcomeUI();
                            ShowWelcomePanel();
                        }
                        else
                        {
                            DebugLog($"Failed to load fresh player data after auto-login: {dataError}");
                            // Check if we have any existing player data to use
                            if (GameAPI.Instance.PlayerData != null && 
                                !string.IsNullOrEmpty(GameAPI.Instance.PlayerData.id))
                            {
                                DebugLog("Using existing player data from auto-login");
                                UpdateWelcomeUI();
                                ShowWelcomePanel();
                            }
                            else
                            {
                                DebugLog("No valid player data available after auto-login");
                                HandleAutoLoginFailure();
                            }
                        }
                    }));
                }
                else
                {
                    DebugLog($"Auto-login failed: {error}");
                    HandleAutoLoginFailure();
                }
            }));
        }
        else
        {
            DebugLog("No saved token found, showing login panel");
            ShowWelcomePanel();
        }
    }
    
    private void HandleAutoLoginFailure()
    {
        // Clear invalid token
        PlayerPrefs.DeleteKey("AuthToken");
        PlayerPrefs.Save();
        
        // Show welcome panel for manual login
        ShowWelcomePanel();
    }
    
    private void OnLoginButtonClicked()
    {
        string username = loginUsernameInput ? loginUsernameInput.text.Trim() : "";
        string password = loginPasswordInput ? loginPasswordInput.text.Trim() : "";
        
        // Enhanced validation for login
        if (string.IsNullOrEmpty(username) && string.IsNullOrEmpty(password))
        {
            ShowLoginError("Vui lòng nhập tên đăng nhập và mật khẩu");
            return;
        }
        
        if (string.IsNullOrEmpty(username))
        {
            ShowLoginError("Vui lòng nhập tên đăng nhập");
            return;
        }
        
        if (string.IsNullOrEmpty(password))
        {
            ShowLoginError("Vui lòng nhập mật khẩu");
            return;
        }
        
        // ShowLoadingPanel("Logging in...");
        StartCoroutine(LoginCoroutine(username, password));
    }
    
    private IEnumerator LoginCoroutine(string username, string password)
    {
        bool loginSuccess = false;
        string errorMessage = "";
        
        ShowLoadingPanel("Logging in...");
        UpdateLoadingStatus("Authenticating...");
        
        yield return StartCoroutine(GameAPI.Instance.Login(username, password, (success, error) => {
            loginSuccess = success;
            errorMessage = error;
        }));
          if (loginSuccess)
        {
            UpdateLoadingStatus("Login successful! Checking user data...");
            
            // Check if player data was already loaded during login
            if (GameAPI.Instance.PlayerData != null && 
                !string.IsNullOrEmpty(GameAPI.Instance.PlayerData.id) && 
                !string.IsNullOrEmpty(GameAPI.Instance.PlayerData.username))
            {                DebugLog($"Player data already available from login: {GameAPI.Instance.PlayerData.username} (ID: {GameAPI.Instance.PlayerData.id})");
                
                // Save login info if remember me is checked
                if (rememberMeToggle && rememberMeToggle.isOn)
                {
                    PlayerPrefs.SetString("LastUsername", username);
                    PlayerPrefs.SetString("AuthToken", GameAPI.Instance.AuthToken);
                    PlayerPrefs.Save();
                }
                
                UpdateLoadingStatus("Login complete!");
                yield return new WaitForSeconds(0.5f);
                
                // Update welcome UI after successful login with forced refresh
                DebugLog("Login successful - forcing UI refresh to check saved game status");
                ShowWelcomePanel();
                StartCoroutine(RefreshPlayerDataAndUpdateUI());
            }
            else
            {
                DebugLog("Player data not available from login response, fetching separately...");
                
                // Explicitly fetch player data after successful login
                bool dataLoaded = false;
                string dataError = "";
                
                UpdateLoadingStatus("Loading user data...");
                
                yield return StartCoroutine(GameAPI.Instance.GetPlayerData((success, error) => {
                    dataLoaded = success;
                    dataError = error;
                }));
                
                if (dataLoaded)
                {                    DebugLog($"User data loaded: {GameAPI.Instance.PlayerData?.username} (ID: {GameAPI.Instance.PlayerData?.id})");
                    
                    // Save login info if remember me is checked
                    if (rememberMeToggle && rememberMeToggle.isOn)
                    {
                        PlayerPrefs.SetString("LastUsername", username);
                        PlayerPrefs.SetString("AuthToken", GameAPI.Instance.AuthToken);
                        PlayerPrefs.Save();
                    }
                    
                    UpdateLoadingStatus("Login complete!");
                    yield return new WaitForSeconds(0.5f);
                    
                    // Update welcome UI after successful login with forced refresh
                    DebugLog("Login successful - forcing UI refresh to check saved game status");
                    ShowWelcomePanel();
                    StartCoroutine(RefreshPlayerDataAndUpdateUI());
                }
                else
                {
                    DebugLog("Login successful but failed to load user data: " + dataError);
                    ShowLoginPanel();
                    ShowLoginError("Login successful but failed to load user data. Please try again.");
                }
            }
        }
        else
        {
            ShowLoginPanel();
            ShowLoginError("Login failed: " + errorMessage);
        }
    }
    
    private void OnRegisterButtonClicked()
    {
        string username = registerUsernameInput ? registerUsernameInput.text.Trim() : "";
        string email = registerEmailInput ? registerEmailInput.text.Trim() : "";
        string password = registerPasswordInput ? registerPasswordInput.text.Trim() : "";
        string confirmPassword = registerConfirmPasswordInput ? registerConfirmPasswordInput.text.Trim() : "";
        
        // Check for empty fields individually with specific messages
        if (string.IsNullOrEmpty(username) && string.IsNullOrEmpty(email) && 
            string.IsNullOrEmpty(password) && string.IsNullOrEmpty(confirmPassword))
        {
            ShowRegisterError("Vui lòng điền đầy đủ tất cả các trường thông tin");
            return;
        }
        
        if (string.IsNullOrEmpty(username))
        {
            ShowRegisterError("Vui lòng nhập tên đăng nhập");
            return;
        }
        
        if (string.IsNullOrEmpty(email))
        {
            ShowRegisterError("Vui lòng nhập email");
            return;
        }
        
        if (string.IsNullOrEmpty(password))
        {
            ShowRegisterError("Vui lòng nhập mật khẩu");
            return;
        }
        
        if (string.IsNullOrEmpty(confirmPassword))
        {
            ShowRegisterError("Vui lòng xác nhận mật khẩu");
            return;
        }
        
        // Additional validation
        if (username.Length < 3)
        {
            ShowRegisterError("Tên đăng nhập phải có ít nhất 3 ký tự");
            return;
        }
        
        if (password.Length < 6)
        {
            ShowRegisterError("Mật khẩu phải có ít nhất 6 ký tự");
            return;
        }
        
        if (password != confirmPassword)
        {
            ShowRegisterError("Mật khẩu xác nhận không khớp");
            return;
        }
        
        // Simple email validation
        if (!IsValidEmail(email))
        {
            ShowRegisterError("Định dạng email không hợp lệ");
            return;
        }
        
        ShowLoadingPanel("Đang tạo tài khoản...");
        StartCoroutine(RegisterCoroutine(username, email, password));
    }
    
    // Simple email validation helper
    private bool IsValidEmail(string email)
    {
        // Basic email validation: contains @ and at least one dot after @
        int atIndex = email.IndexOf('@');
        if (atIndex < 1) return false; // @ not found or at start
        
        int dotIndex = email.IndexOf('.', atIndex);
        if (dotIndex < atIndex + 2 || dotIndex == email.Length - 1) return false; // No dot after @ or dot at end
        
        return true;
    }
    
    private IEnumerator RegisterCoroutine(string username, string email, string password)
    {
        bool registerSuccess = false;
        string errorMessage = "";
        
        yield return StartCoroutine(GameAPI.Instance.Register(username, email, password, (success, error) => {
            registerSuccess = success;
            errorMessage = error;
        }));
        
        if (registerSuccess)
        {
            UpdateLoadingStatus("Tài khoản đã được tạo! Đang tải dữ liệu người dùng...");
            
            // Explicitly fetch player data after successful registration
            bool dataLoaded = false;
            string dataError = "";
            
            yield return StartCoroutine(GameAPI.Instance.GetPlayerData((success, error) => {
                dataLoaded = success;
                dataError = error;
            }));
            
            if (dataLoaded)
            {
                DebugLog($"Đăng ký thành công - Dữ liệu người dùng đã tải: {GameAPI.Instance.PlayerData?.username} (ID: {GameAPI.Instance.PlayerData?.id})");
                
                yield return new WaitForSeconds(0.5f);
                
                // Quay về scene hiện tại (menu)
                UpdateLoadingStatus("Đăng ký thành công! Đang quay về menu...");
                yield return new WaitForSeconds(1.0f);
                DebugLog($"Reloading current scene: {SceneManager.GetActiveScene().name}");
                SceneManager.LoadScene("Menu");
            }
            else
            {
                DebugLog("Đăng ký thành công nhưng không tải được dữ liệu người dùng: " + dataError);
                ShowRegisterPanel();
                ShowRegisterError("Tài khoản đã được tạo nhưng không thể tải dữ liệu người dùng. Vui lòng đăng nhập.");
            }
        }
        else
        {
            ShowRegisterPanel();
            ShowRegisterError("Đăng ký thất bại: " + errorMessage);
        }
    }
      private void ContinueGame()
    {
        if (!GameAPI.Instance.IsLoggedIn)
        {
            DebugLog("Cannot continue game: User not logged in");
            return;
        }
        
        DebugLog("Continuing saved game");
        ShowLoadingPanel("Loading saved game...");
        StartCoroutine(ContinueGameCoroutine());
    }
      private IEnumerator ContinueGameCoroutine()
    {
        UpdateLoadingStatus("Loading latest save data...");        // Force refresh fresh player data from server before continuing
        bool dataLoaded = false;
        string errorMessage = "";
        
        yield return StartCoroutine(GameAPI.Instance.ForceRefreshPlayerData((success, error) => {
            dataLoaded = success;
            errorMessage = error;
        }));
        
        if (!dataLoaded)
        {
            DebugLog($"Failed to load fresh save data: {errorMessage}");
            UpdateLoadingStatus("Failed to load latest save. Loading with cached data...");
            yield return new WaitForSeconds(1f);
        }
        else
        {
            DebugLog("Fresh save data loaded successfully for continue game");
            UpdateLoadingStatus("Latest save data loaded! Starting game...");
            yield return new WaitForSeconds(0.5f);
        }
        
        UpdateLoadingStatus("Loading your saved adventure...");
        yield return new WaitForSeconds(0.5f);
        LoadGameScene();
    }
    
    private void StartOfflineMode()
    {
        ShowLoadingPanel("Starting offline mode...");
        UpdateLoadingStatus("Loading offline game...");
        
        // Remove the SetOfflineMode call since it doesn't exist in GameAPI
        // Just proceed to load the offline game
        
        StartCoroutine(LoadOfflineGame());
    }
    
    private IEnumerator LoadOfflineGame()
    {
        yield return new WaitForSeconds(1f);
        SceneManager.LoadScene(offlineSceneName);
    }
    
    private void CheckForContinueGame()
    {
        // This method is now called within UpdateWelcomeUI()
        DebugLog("Continue game option updated");
    }
    
    private void LoadGameScene()
    {
        DebugLog($"Loading game scene: {gameSceneName}");
        if (string.IsNullOrEmpty(gameSceneName))
        {
            DebugLog("Error: Game scene name is not set!");
            ShowWelcomePanel();
            return;
        }
        // Sử dụng SceneTransitionManager nếu có
        if (SceneTransitionManager.Instance != null)
        {
            SceneTransitionManager.Instance.LoadGameplayScene(gameSceneName);
        }
        else if (Application.CanStreamedLevelBeLoaded(gameSceneName))
        {
            SceneManager.LoadScene(gameSceneName);
        }
        else
        {
            DebugLog($"Error: Scene '{gameSceneName}' not found in build settings!");
            ShowWelcomePanel();
        }
    }
    
    private void ShowLoginError(string message)
    {
        if (loginErrorText)
        {
            loginErrorText.gameObject.SetActive(true);
            loginErrorText.text = message;
        }
        DebugLog("Login Error: " + message);
    }
    
    private void ShowRegisterError(string message)
    {
        if (registerErrorText)
        {
            registerErrorText.gameObject.SetActive(true);
            registerErrorText.text = message;
        }
        DebugLog("Register Error: " + message);
    }
    
    private void DebugLog(string message)
    {
        if (debugMode)
        {
            Debug.Log($"[LoginUIManager] {message}");
        }
    }
    
    private void UpdateLoadingStatus(string status)
    {
        if (loadingStatusText) loadingStatusText.text = status;
        DebugLog("Loading: " + status);
    }
    
    private void ShowLoginPanel()
    {
        SetActivePanel(loginPanel);
        if (loginErrorText) loginErrorText.gameObject.SetActive(false);
        
        // Load saved username if remember me was checked
        if (rememberMeToggle && rememberMeToggle.isOn && loginUsernameInput)
        {
            loginUsernameInput.text = PlayerPrefs.GetString("LastUsername", "");
        }
    }
    
    private void ShowRegisterPanel()
    {
        SetActivePanel(registerPanel);
        if (registerErrorText) registerErrorText.gameObject.SetActive(false);
    }
    
    private void ShowWelcomePanel()
    {
        SetActivePanel(welcomePanel);
        UpdateWelcomeUI(); // Update UI when showing welcome panel
    }
    
    private void ShowLoadingPanel(string status = "Loading...")
    {
        SetActivePanel(loadingPanel);
        UpdateLoadingStatus(status);
        if (loadingProgressBar) loadingProgressBar.value = 0;
    }
    
    private void SetActivePanel(GameObject activePanel)
    {
        if (welcomePanel) welcomePanel.SetActive(activePanel == welcomePanel);
        if (loginPanel) loginPanel.SetActive(activePanel == loginPanel);
        if (registerPanel) registerPanel.SetActive(activePanel == registerPanel);
        if (loadingPanel) loadingPanel.SetActive(activePanel == loadingPanel);
        if (settingsPanel) settingsPanel.SetActive(activePanel == settingsPanel);
        if (optionPanel) optionPanel.SetActive(activePanel == optionPanel);
    }
    
    // Add new methods for UI management
      /// <summary>
    /// Update welcome panel UI based on login status
    /// </summary>
    private void UpdateWelcomeUI()
    {
        bool isLoggedIn = GameAPI.Instance.IsLoggedIn;
        
        // Force refresh player data before checking saved game
        if (isLoggedIn && GameAPI.Instance.PlayerData != null)
        {
            StartCoroutine(RefreshPlayerDataAndUpdateUI());
        }
        else
        {
            UpdateUIBasedOnLoginStatus(isLoggedIn, false);
        }
    }
    
    /// <summary>
    /// Refresh player data from server and update UI
    /// </summary>
    private IEnumerator RefreshPlayerDataAndUpdateUI()
    {
        bool isLoggedIn = GameAPI.Instance.IsLoggedIn;
        bool hasSavedGame = false;
        
        // First check with current data
        hasSavedGame = HasSavedGame();
        
        if (isLoggedIn && !hasSavedGame)
        {
            // Try to refresh data from server to get latest save state
            DebugLog("No saved game detected locally, refreshing from server...");
            bool dataRefreshed = false;
            
            yield return StartCoroutine(GameAPI.Instance.GetPlayerData((success, error) => {
                dataRefreshed = success;
                if (!success)
                {
                    DebugLog($"Failed to refresh player data: {error}");
                }
            }));
            
            if (dataRefreshed)
            {
                // Check again after refresh
                hasSavedGame = HasSavedGame();
                DebugLog($"After data refresh - Has saved game: {hasSavedGame}");
            }
        }
        
        UpdateUIBasedOnLoginStatus(isLoggedIn, hasSavedGame);
    }
    
    /// <summary>
    /// Update UI elements based on login and save status
    /// </summary>
    private void UpdateUIBasedOnLoginStatus(bool isLoggedIn, bool hasSavedGame)
    {
        if (welcomeLoginButton) welcomeLoginButton.gameObject.SetActive(!isLoggedIn);
        if (welcomeRegisterButton) welcomeRegisterButton.gameObject.SetActive(!isLoggedIn);
        if (welcomePlayOfflineButton) welcomePlayOfflineButton.gameObject.SetActive(!isLoggedIn);
        
        if (welcomeNewGameButton) welcomeNewGameButton.gameObject.SetActive(isLoggedIn);
        if (welcomeContinueButton) welcomeContinueButton.gameObject.SetActive(isLoggedIn && hasSavedGame);
        if (welcomeLogoutButton) welcomeLogoutButton.gameObject.SetActive(isLoggedIn);
        

        if (welcomeUserText)
        {
            if (isLoggedIn && GameAPI.Instance.PlayerData != null)
            {
                welcomeUserText.gameObject.SetActive(true);
                welcomeUserText.text = $"Welcome back, {GameAPI.Instance.PlayerData.username}!";
            }
            else
            {
                welcomeUserText.gameObject.SetActive(false);
            }
        }
        
        DebugLog($"Welcome UI updated - Logged in: {isLoggedIn}, Has saved game: {hasSavedGame}");
    }    /// <summary>
    /// Check if user has saved game data
    /// </summary>
    private bool HasSavedGame()
    {
        if (!GameAPI.Instance.IsLoggedIn || GameAPI.Instance.PlayerData == null)
        {
            DebugLog("HasSavedGame: User not logged in or no player data");
            return false;
        }
        
        // Check if player has checkpoint data or meaningful progress
        var playerData = GameAPI.Instance.PlayerData;
        
        // Check for checkpoint data (highest priority)
        bool hasCheckpoint = playerData.checkpoint != null && 
                           !string.IsNullOrEmpty(playerData.checkpoint.sceneId);
        
        // Check for meaningful progress
        bool hasProgress = playerData.level > 1 || 
                          playerData.experience > 0 || 
                          playerData.money > 0;
        
        // Check for weapons (if weapons list exists and has items)
        bool hasWeapons = playerData.weapons != null && playerData.weapons.Count > 0;
        
        // Additional check for any meaningful game state
        // Consider a user with default starting values as having no save
        bool hasDefaultState = playerData.level == 1 && 
                              playerData.experience == 0 && 
                              playerData.money == 0 && 
                              (playerData.weapons == null || playerData.weapons.Count == 0);
        
        bool hasSavedData = hasCheckpoint || hasProgress || hasWeapons;
        
        // Even with default values, if there's a checkpoint, it means they played
        if (hasCheckpoint)
        {
            hasSavedData = true;
        }
        // If they have any progress at all, they have a save
        else if (hasProgress || hasWeapons)
        {
            hasSavedData = true;
        }
        // Complete default state means no save
        else if (hasDefaultState)
        {
            hasSavedData = false;
        }
        // Otherwise, consider it as having data (safety fallback)
        else
        {
            hasSavedData = true;
        }
        
        DebugLog($"HasSavedGame check - Checkpoint: {hasCheckpoint}, Progress: {hasProgress}, Weapons: {hasWeapons}, Default: {hasDefaultState}, Result: {hasSavedData}");
        DebugLog($"Player stats - Level: {playerData.level}, XP: {playerData.experience}, Money: {playerData.money}, Weapons: {playerData.weapons?.Count ?? 0}");
        
        return hasSavedData;
    }
    
    /// <summary>
    /// Start a new game (for logged in users)
    /// </summary>
    private void StartNewGame()
    {
        if (!GameAPI.Instance.IsLoggedIn)
        {
            DebugLog("Cannot start new game: User not logged in");
            return;
        }

        DebugLog("Starting new game for logged in user");
        // ShowLoadingPanel("Starting new game...");

        // Reset dữ liệu local (PlayerPrefs)
        PlayerPrefs.DeleteAll();
        PlayerPrefs.DeleteKey("LastUserData"); // Xóa luôn dữ liệu save cũ trước khi reset PlayerData
        PlayerPrefs.Save();

        // Reset PlayerData trong RAM/cache
        GameAPI.Instance.GetType().GetProperty("PlayerData").SetValue(GameAPI.Instance, null, null);

        // Reset player data for new game
        StartCoroutine(ResetPlayerDataAndStartGame());
    }
    
    /// <summary>
    /// Reset player data and start new game
    /// </summary>
    private System.Collections.IEnumerator ResetPlayerDataAndStartGame()
    {
        DebugLog("Starting new game reset process...");
        
        ShowLoadingPanel("Đang tạo game mới...");
        UpdateLoadingStatus("Đang xác minh trạng thái đăng nhập...");
        
        // Verify user is still logged in
        if (!GameAPI.Instance.IsLoggedIn)
        {
            DebugLog("Error: User not logged in during new game creation");
            ShowWelcomePanel();
            yield break;
        }
        
        // Double-check by fetching fresh player data
        UpdateLoadingStatus("Đang tải dữ liệu người dùng hiện tại...");
        bool dataLoaded = false;
        string errorMsg = "";
        
        yield return StartCoroutine(GameAPI.Instance.GetPlayerData((success, error) => {
            dataLoaded = success;
            errorMsg = error;
        }));
        
        if (!dataLoaded)
        {
            DebugLog("Failed to load player data for new game: " + errorMsg);
            UpdateLoadingStatus("Không thể tải dữ liệu người dùng");
            yield return new WaitForSeconds(1f);
            ShowWelcomePanel();
            yield break;
        }
        
        // Verify we have valid player data
        var playerData = GameAPI.Instance.PlayerData;
        if (playerData == null || string.IsNullOrEmpty(playerData.id) || string.IsNullOrEmpty(playerData.username))
        {
            DebugLog($"Invalid player data - ID: '{playerData?.id}', Username: '{playerData?.username}'");
            UpdateLoadingStatus("Dữ liệu người dùng không hợp lệ");
            yield return new WaitForSeconds(1f);
            ShowWelcomePanel();
            yield break;
        }
        
        UpdateLoadingStatus("Đang xoá tất cả tiến trình game cũ...");
        
        // Lưu lại thông tin người dùng cần giữ
        string originalId = playerData.id;
        string originalUsername = playerData.username;
        string originalEmail = playerData.email;
        
        // Reset triệt để tất cả dữ liệu game - cách tiếp cận mới
        // Chúng ta sẽ cập nhật trực tiếp vào đối tượng PlayerData hiện có
        
        // Reset các thuộc tính game
        playerData.level = 1;
        playerData.experience = 0;
        playerData.money = 0;
        playerData.health = 100f;
        playerData.kills = 0;
        playerData.hasKey = false;
        
        // Reset checkpoint - quan trọng để bảo đảm không có điểm lưu trước đó
        playerData.checkpoint = null;
        
        // Reset danh sách vũ khí
        if (playerData.weapons == null)
        {
            playerData.weapons = new List<WeaponData>();
        }
        else
        {
            playerData.weapons.Clear();
        }
        
        // Bảo đảm thông tin định danh vẫn được giữ nguyên
        playerData.id = originalId;
        playerData.username = originalUsername;
        playerData.email = originalEmail;
        
        // Cập nhật ngày đăng nhập cuối
        playerData.lastLoginDate = System.DateTime.Now.ToString("o");
        
        DebugLog($"Đã reset hoàn toàn dữ liệu game cho người chơi: {playerData.username}");
        
        // Verify login state one more time before saving
        if (!GameAPI.Instance.IsLoggedIn)
        {
            DebugLog("Error: Lost login state during data reset");
            ShowWelcomePanel();
            yield break;
        }
        
        UpdateLoadingStatus("Đang lưu dữ liệu game mới...");
        
        // Sử dụng GameAPI có sẵn để lưu dữ liệu đã reset
        bool saveSuccess = false;
        string errorMessage = "";
        
        yield return StartCoroutine(GameAPI.Instance.SavePlayerData((success, error) => {
            saveSuccess = success;
            errorMessage = error;
        }));
        
        if (saveSuccess)
        {
            UpdateLoadingStatus("Đã tạo game mới! Đang tải...");
            yield return new WaitForSeconds(1f);

            // Force một lần refresh cuối cùng để đảm bảo dữ liệu được cập nhật đúng
            yield return StartCoroutine(GameAPI.Instance.ForceRefreshPlayerData((refreshSuccess, refreshError) => {
                if (!refreshSuccess) {
                    DebugLog("Cảnh báo: Không thể refresh dữ liệu sau khi tạo game mới: " + refreshError);
                }
                else {
                    DebugLog("Dữ liệu game mới đã được tải thành công từ server");
                }
            }));

            // Sau khi refresh, update lại UI để đảm bảo trạng thái đúng
            UpdateWelcomeUI();
            // Load lại scene game
            LoadGameScene();
        }
        else
        {
            DebugLog("Failed to save new game data: " + errorMessage);
            UpdateLoadingStatus("Không thể lưu game mới, vẫn tiếp tục tải game...");
            yield return new WaitForSeconds(1f);
            LoadGameScene();
        }
    }
    
    /// <summary>
    /// Logout current user
    /// </summary>
    private void LogoutUser()
    {
        ShowLoadingPanel("Logging out...");
        
        // Clear API data
        GameAPI.Instance.Logout();
        
        // Clear any local game state - check if GameDataSynchronizer exists
        try
        {
            var synchronizer = FindObjectOfType<UserDataSync>();
            if (synchronizer != null)
            {
                // Clear data if the method exists
                DebugLog("Clearing game data synchronizer");
            }
        }
        catch (System.Exception e)
        {
            DebugLog("Note: GameDataSynchronizer not found or accessible: " + e.Message);
        }
        
        UpdateLoadingStatus("Logged out successfully!");
        
        // Update UI and show welcome panel with proper UI refresh
        StartCoroutine(ShowWelcomePanelAfterLogout(1f));
    }
      /// <summary>
    /// Show welcome panel after a delay
    /// </summary>
    private System.Collections.IEnumerator ShowWelcomePanelAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        ShowWelcomePanel();
    }
      /// <summary>
    /// Show welcome panel after logout with UI refresh
    /// </summary>
    private System.Collections.IEnumerator ShowWelcomePanelAfterLogout(float delay)
    {
        yield return new WaitForSeconds(delay);
        
        // Update UI state after logout to ensure buttons are properly refreshed
        UpdateWelcomeUI();
        ShowWelcomePanel();
        
        DebugLog("UI refreshed after logout - continue button should be hidden");
    }
      // Add this method for debugging
    public void TestAPIConnection()
    {
        if (debugMode)
        {
            DebugLog("Testing API connection...");
            ShowLoadingPanel("Testing API...");
            
            // Log current state first
            GameAPI.Instance.LogCurrentState();
            
            StartCoroutine(GameAPI.Instance.TestAPIEndpoints((success, message) => {
                DebugLog($"API Test Result: {success} - {message}");
                UpdateLoadingStatus($"API Test: {message}");
                
                if (success)
                {
                    StartCoroutine(ShowWelcomePanelAfterDelay(2f));
                }
                else
                {
                    StartCoroutine(ShowWelcomePanelAfterDelay(3f));
                }
            }));
        }
    }
    
    // Add method to manually check player data state
    public void CheckPlayerDataState()
    {
        if (debugMode)
        {
            DebugLog("=== Manual Player Data Check ===");
            GameAPI.Instance.LogCurrentState();
            
            if (GameAPI.Instance.IsLoggedIn)
            {
                DebugLog("User is logged in, testing GetPlayerData...");
                StartCoroutine(GameAPI.Instance.GetPlayerData((success, error) => {
                    DebugLog($"GetPlayerData result: {success}, Error: {error}");
                    if (success)
                    {
                        GameAPI.Instance.LogCurrentState();
                    }
                }));
            }
            else
            {
                DebugLog("User is not logged in");
            }
        }
    }
    
    // Add method to test token verification directly
    public void TestTokenVerification()
    {
        if (debugMode)
        {
            DebugLog("=== Testing Token Verification ===");
            string savedToken = PlayerPrefs.GetString("AuthToken", "");
            
            if (!string.IsNullOrEmpty(savedToken))
            {
                DebugLog($"Found saved token: {savedToken.Substring(0, Math.Min(10, savedToken.Length))}...");
                ShowLoadingPanel("Testing token verification...");
                
                StartCoroutine(GameAPI.Instance.LoginWithToken(savedToken, (success, error) => {
                    if (success)
                    {
                        DebugLog("Token verification successful!");
                        UpdateLoadingStatus("Token verification successful!");
                        GameAPI.Instance.LogCurrentState();
                    }
                    else
                    {
                        DebugLog($"Token verification error: {error}");
                        UpdateLoadingStatus($"Token verification failed: {error}");
                    }
                    
                    StartCoroutine(ShowWelcomePanelAfterDelay(2f));
                }));
            }
            else
            {
                DebugLog("No saved token found to test");
                ShowWelcomePanel();
            }
        }
    }
    
    // Debug method to print raw server response
    public void TestRawServerResponse()
    {
        if (debugMode)
        {
            DebugLog("=== Testing Raw Server Response ===");
            ShowLoadingPanel("Testing raw server response...");
            
            if (string.IsNullOrEmpty(loginUsernameInput.text) || string.IsNullOrEmpty(loginPasswordInput.text))
            {
                DebugLog("Please enter username and password first");
                UpdateLoadingStatus("Please enter username and password");
                StartCoroutine(ShowWelcomePanelAfterDelay(2f));
                return;
            }
            
            string username = loginUsernameInput.text;
            string password = loginPasswordInput.text;
            
            // Use a direct web request to see raw response
            StartCoroutine(TestDirectServerRequest(username, password));
        }
    }
    
    private IEnumerator TestDirectServerRequest(string username, string password)
    {
        string apiUrl = "http://localhost:5000/api"; // Adjust if your URL is different
        
        var loginData = new System.Collections.Generic.Dictionary<string, string>
        {
            { "username", username },
            { "password", password }
        };
        
        string jsonData = Newtonsoft.Json.JsonConvert.SerializeObject(loginData);
        
        using (UnityEngine.Networking.UnityWebRequest request = new UnityEngine.Networking.UnityWebRequest($"{apiUrl}/auth/login", "POST"))
        {
            byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(jsonData);
            request.uploadHandler = new UnityEngine.Networking.UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new UnityEngine.Networking.DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");
            
            UpdateLoadingStatus("Sending direct login request...");
            yield return request.SendWebRequest();
            
            DebugLog($"Test Login - Response Code: {request.responseCode}");
            DebugLog($"Test Login - Raw Response: {request.downloadHandler.text}");
            DebugLog("=== Response Structure Analysis ===");
            
            try
            {
                var responseObj = Newtonsoft.Json.JsonConvert.DeserializeObject<System.Collections.Generic.Dictionary<string, object>>(request.downloadHandler.text);
                
                if (responseObj != null)
                {
                    foreach (var key in responseObj.Keys)
                    {
                        DebugLog($"Key: {key}, Type: {responseObj[key]?.GetType().Name ?? "null"}");
                        
                        // If this is the user data, inspect its structure
                        if (key == "user" && responseObj[key] != null)
                        {
                            var userData = Newtonsoft.Json.JsonConvert.SerializeObject(responseObj[key]);
                            DebugLog($"User data structure: {userData}");
                            
                            var userObj = Newtonsoft.Json.JsonConvert.DeserializeObject<System.Collections.Generic.Dictionary<string, object>>(userData);
                            if (userObj != null)
                            {
                                foreach (var userKey in userObj.Keys)
                                {
                                    DebugLog($"  User.{userKey}: {userObj[userKey]}");
                                }
                            }
                        }
                    }
                }
                else
                {
                    DebugLog("Response could not be parsed as a dictionary");
                }
            }
            catch (System.Exception ex)
            {
                DebugLog($"Error analyzing response: {ex.Message}");
            }
            
            UpdateLoadingStatus("Login test complete");
            StartCoroutine(ShowWelcomePanelAfterDelay(3f));
        }
    }
    
    private void ShowSettingsPanel()
    {
        SetActivePanel(settingsPanel);
    }
    
    private void ShowOptionPanel()
    {
        SetActivePanel(optionPanel);
    }
}
