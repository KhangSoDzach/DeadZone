using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro; // Add TextMeshPro namespace

namespace Scripts.API
{

    public class PauseMenu : MonoBehaviour
    {
        [Header("Pause Menu UI")]
        [SerializeField] private GameObject pauseMenuPanel;
        [SerializeField] private Button continueButton;
        [SerializeField] private Button optionButton;
        [SerializeField] private Button saveGameButton;
        [SerializeField] private Button mainMenuButton;
        [SerializeField] private Button quitGameButton;

        [Header("Option Panel")]
        [SerializeField] private GameObject optionPanel;
        [SerializeField] private TMP_Text optionTitleText;
        [SerializeField] private Slider optionVolumeSlider;
        [SerializeField] private TMP_Text optionVolumeValueText;
        [SerializeField] private TMP_Dropdown optionResolutionDropdown;
        [SerializeField] private Toggle optionFullscreenToggle;
        [SerializeField] private Button optionSaveButton;
        [SerializeField] private Button optionCancelButton;
        [SerializeField] private Button optionBackButton; // Thêm back button

        // Thêm các button và panel cho từng mục (template từ SettingMenu)
        [Header("Option Subpanels")]
        [SerializeField] private Button videoButton;
        [SerializeField] private Button audioButton;
        [SerializeField] private Button gameplayButton;
        [SerializeField] private GameObject videoPanel;
        [SerializeField] private GameObject audioPanel;
        [SerializeField] private GameObject gameplayPanel;

        [Header("Save Notification")]
        [SerializeField] private GameObject saveNotificationPanel;
        [SerializeField] private TextMeshProUGUI saveNotificationText;
        [SerializeField] private float saveNotificationDuration = 2f;

        [Header("Confirmation Dialog")]
        [SerializeField] private GameObject confirmationPanel;
        [SerializeField] private TextMeshProUGUI confirmationText;
        [SerializeField] private Button confirmYesButton;
        [SerializeField] private Button confirmNoButton;

        [Header("Settings")]
        [SerializeField] private string mainMenuSceneName = "Menu";
        [SerializeField] private KeyCode pauseKey = KeyCode.Escape;
        [SerializeField] private bool canPauseInShop = false;

        // State tracking
        private bool isPaused = false;
        private bool wasTimeScaleZero = false;
        private System.Action pendingConfirmAction;

        // Components references
        private ShopManagement shopManager;

        // Static property to check pause state from other scripts
        public static bool IsGamePaused { get; private set; } = false;

        private Resolution[] resolutions;
        private int currentResolutionIndex = 0;

        private void Start()
        {
            // Find shop manager
            shopManager = FindObjectOfType<ShopManagement>();

            // Setup button listeners
            SetupButtonListeners();

            // Initialize UI state
            InitializeUI();
        }

        private void Update()
        {
            // Handle pause input
            if (Input.GetKeyDown(pauseKey))
            {
                TogglePause();
            }
        }

        private void SetupButtonListeners()
        {
            // Main pause menu buttons
            if (continueButton) continueButton.onClick.AddListener(ContinueGame);
            if (optionButton) optionButton.onClick.AddListener(ShowOptionPanel);
            if (saveGameButton) saveGameButton.onClick.AddListener(SaveGame);
            if (mainMenuButton) mainMenuButton.onClick.AddListener(() => ShowConfirmation("Return to Main Menu?", ReturnToMainMenu));
            if (quitGameButton) quitGameButton.onClick.AddListener(() => ShowConfirmation("Quit Game?", QuitGame));

            // Option panel buttons
            if (optionSaveButton) optionSaveButton.onClick.AddListener(SaveOptionSettings);
            if (optionCancelButton) optionCancelButton.onClick.AddListener(CancelOptionSettings);
            if (optionBackButton) optionBackButton.onClick.AddListener(OnBackToPauseMenu); // Gắn back button

            // Các button chuyển panel (template từ SettingMenu)
            if (videoButton) videoButton.onClick.AddListener(ShowVideoPanel);
            if (audioButton) audioButton.onClick.AddListener(ShowAudioPanel);
            if (gameplayButton) gameplayButton.onClick.AddListener(ShowGameplayPanel);

            // Confirmation dialog buttons
            if (confirmYesButton) confirmYesButton.onClick.AddListener(ConfirmAction);
            if (confirmNoButton) confirmNoButton.onClick.AddListener(CancelConfirmation);
        }

        private void InitializeUI()
        {
            // Hide all panels initially
            if (pauseMenuPanel) pauseMenuPanel.SetActive(false);
            if (optionPanel) optionPanel.SetActive(false);
            if (saveNotificationPanel) saveNotificationPanel.SetActive(false);
            if (confirmationPanel) confirmationPanel.SetActive(false);

            // Disable save button if not logged in
            if (saveGameButton && GameAPI.Instance != null)
            {
                saveGameButton.interactable = GameAPI.Instance.IsLoggedIn;
            }

            // Option panel UI setup
            SetupOptionPanelUI();

            // Ẩn các subpanel khi khởi tạo
            if (videoPanel) videoPanel.SetActive(false);
            if (audioPanel) audioPanel.SetActive(false);
            if (gameplayPanel) gameplayPanel.SetActive(false);
        }

        private void SetupOptionPanelUI()
        {
            if (optionPanel == null) return;

            // Volume
            if (optionVolumeSlider)
            {
                optionVolumeSlider.minValue = 0f;
                optionVolumeSlider.maxValue = 1f;
                optionVolumeSlider.value = PlayerPrefs.GetFloat("Volume", 1f);
                optionVolumeSlider.onValueChanged.AddListener(OnOptionVolumeChanged);
                OnOptionVolumeChanged(optionVolumeSlider.value);
            }

            // Resolution
            if (optionResolutionDropdown)
            {
                optionResolutionDropdown.ClearOptions();
                resolutions = Screen.resolutions;
                var options = new System.Collections.Generic.List<string>();
                currentResolutionIndex = 0;
                for (int i = 0; i < resolutions.Length; i++)
                {
                    string option = resolutions[i].width + " x " + resolutions[i].height;
                    options.Add(option);
                    if (resolutions[i].width == Screen.currentResolution.width &&
                        resolutions[i].height == Screen.currentResolution.height)
                    {
                        currentResolutionIndex = i;
                    }
                }
                optionResolutionDropdown.AddOptions(options);
                optionResolutionDropdown.value = PlayerPrefs.GetInt("ResolutionIndex", currentResolutionIndex);
                optionResolutionDropdown.RefreshShownValue();
            }

            // Fullscreen
            if (optionFullscreenToggle)
            {
                optionFullscreenToggle.isOn = PlayerPrefs.GetInt("Fullscreen", 1) == 1;
            }

            // Title
            if (optionTitleText) optionTitleText.text = "Options";
        }

        private void OnOptionVolumeChanged(float value)
        {
            if (optionVolumeValueText)
                optionVolumeValueText.text = Mathf.RoundToInt(value * 100f) + "%";
            AudioListener.volume = value;
        }

        private void TogglePause()
        {
            // Check if we can pause (not in shop unless allowed)
            if (!canPauseInShop && shopManager != null && shopManager.IsShopOpen())
            {
                return;
            }

            if (isPaused)
            {
                ContinueGame();
            }
            else
            {
                PauseGame();
            }
        }

        public void PauseGame()
        {
            if (isPaused) return;

            isPaused = true;
            IsGamePaused = true; // Set static flag

            // Store current time scale
            wasTimeScaleZero = (Time.timeScale == 0f);

            // Pause the game
            Time.timeScale = 0f;

            // Show pause menu
            if (pauseMenuPanel) pauseMenuPanel.SetActive(true);

            // Enable cursor
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;

            // Update save button state
            if (saveGameButton && GameAPI.Instance != null)
            {
                saveGameButton.interactable = GameAPI.Instance.IsLoggedIn;
            }

            Debug.Log("Game Paused");
        }

        public void ContinueGame()
        {
            if (!isPaused) return;

            isPaused = false;
            IsGamePaused = false; // Clear static flag

            // Hide all pause-related panels
            if (pauseMenuPanel) pauseMenuPanel.SetActive(false);
            if (optionPanel) optionPanel.SetActive(false);
            if (confirmationPanel) confirmationPanel.SetActive(false);

            // Restore time scale only if it wasn't zero before
            if (!wasTimeScaleZero)
            {
                Time.timeScale = 1f;
            }

            // Restore cursor state for gameplay
            Cursor.visible = false;
            Cursor.lockState = CursorLockMode.Locked;

            Debug.Log("Game Resumed");
        }

        public void ShowOptionPanel()
        {
            if (pauseMenuPanel) pauseMenuPanel.SetActive(false);
            if (optionPanel) optionPanel.SetActive(true);

            // Khi mở option panel, mặc định mở videoPanel (hoặc panel nào bạn muốn)
            ShowVideoPanel();

            if (optionVolumeSlider) optionVolumeSlider.value = PlayerPrefs.GetFloat("Volume", 1f);
            if (optionResolutionDropdown) optionResolutionDropdown.value = PlayerPrefs.GetInt("ResolutionIndex", currentResolutionIndex);
            if (optionFullscreenToggle) optionFullscreenToggle.isOn = PlayerPrefs.GetInt("Fullscreen", 1) == 1;
        }

        // Hàm back về pause menu từ option panel
        private void OnBackToPauseMenu()
        {
            if (optionPanel) optionPanel.SetActive(false);
            if (pauseMenuPanel) pauseMenuPanel.SetActive(true);
            
        }

        // Các hàm chuyển panel (template từ SettingMenu)
        private void ShowVideoPanel()
        {
            if (videoPanel) videoPanel.SetActive(true);
            if (audioPanel) audioPanel.SetActive(false);
            if (gameplayPanel) gameplayPanel.SetActive(false);
        }

        private void ShowAudioPanel()
        {
            if (videoPanel) videoPanel.SetActive(false);
            if (audioPanel) audioPanel.SetActive(true);
            if (gameplayPanel) gameplayPanel.SetActive(false);
        }

        private void ShowGameplayPanel()
        {
            if (videoPanel) videoPanel.SetActive(false);
            if (audioPanel) audioPanel.SetActive(false);
            if (gameplayPanel) gameplayPanel.SetActive(true);
        }

        public void SaveOptionSettings()
        {
            if (optionVolumeSlider)
            {
                PlayerPrefs.SetFloat("Volume", optionVolumeSlider.value);
                AudioListener.volume = optionVolumeSlider.value;
            }

            if (optionResolutionDropdown && resolutions != null && resolutions.Length > 0)
            {
                int resIndex = optionResolutionDropdown.value;
                PlayerPrefs.SetInt("ResolutionIndex", resIndex);
                Resolution res = resolutions[resIndex];
                Screen.SetResolution(res.width, res.height, optionFullscreenToggle && optionFullscreenToggle.isOn);
            }

            if (optionFullscreenToggle)
            {
                PlayerPrefs.SetInt("Fullscreen", optionFullscreenToggle.isOn ? 1 : 0);
                Screen.fullScreen = optionFullscreenToggle.isOn;
            }

            PlayerPrefs.Save();
            if (optionPanel) optionPanel.SetActive(false);
            if (pauseMenuPanel) pauseMenuPanel.SetActive(true);
        }

        public void CancelOptionSettings()
        {
            if (optionPanel) optionPanel.SetActive(false);
            if (pauseMenuPanel) pauseMenuPanel.SetActive(true);
        }

        public void OpenOptions()
        {
            if (optionPanel)
            {
                optionPanel.SetActive(true);
                if (pauseMenuPanel) pauseMenuPanel.SetActive(false);
            }
            else
            {
                // If no option panel exists, show a simple message
                ShowSaveNotification("Options panel not configured");
            }
        }

        public void CloseOptions()
        {
            if (optionPanel) optionPanel.SetActive(false);
            if (pauseMenuPanel) pauseMenuPanel.SetActive(true);
        }

        public void SaveGame()
        {
            // Check if user is logged in first
            if (!GameAPI.Instance.IsLoggedIn)
            {
                ShowMessage("You must be logged in to save the game.");
                return;
            }
            
            // Check if data is loaded
            if (!GameDataSynchronizer.Instance.IsDataLoaded)
            {
                ShowMessage("Loading player data...");
                GameDataSynchronizer.Instance.LoadGameData((success, error) => {
                    if (success)
                    {
                        PerformSave();
                    }
                    else
                    {
                        ShowMessage("Failed to load player data: " + error);
                    }
                });
                return;
            }
            
            PerformSave();
        }
        
        private void PerformSave()
        {
            ShowMessage("Saving game...");
            GameDataSynchronizer.Instance.SaveGameData((success, error) => {
                if (success)
                {
                    ShowMessage("Game saved successfully!");
                }
                else
                {
                    ShowMessage("Failed to save game: " + error);
                }
            });
        }
        
        private void ShowMessage(string message)
        {
            Debug.Log($"[PauseMenu] {message}");
            // You can add UI feedback here if needed
        }
        
        private void ShowConfirmation(string message, System.Action confirmAction)
        {
            if (confirmationPanel && confirmationText)
            {
                confirmationText.text = message;
                confirmationPanel.SetActive(true);
                pendingConfirmAction = confirmAction;
            }
            else
            {
                // If no confirmation panel, execute action directly
                confirmAction?.Invoke();
            }
            
        }

        private void ConfirmAction()
        {
            if (confirmationPanel) confirmationPanel.SetActive(false);
            pendingConfirmAction?.Invoke();
            pendingConfirmAction = null;
        }

        private void CancelConfirmation()
        {
            if (confirmationPanel) confirmationPanel.SetActive(false);
            pendingConfirmAction = null;
        }        
        public void ReturnToMainMenu()
        {
            // Clear pause state before leaving
            IsGamePaused = false;
            
            // Save game before leaving if logged in
            if (GameAPI.Instance != null && GameAPI.Instance.IsLoggedIn)
            {
                if (GameSaveManager.Instance != null)
                {
                    GameSaveManager.Instance.SaveGame();
                }
            }
            

            // Restore time scale
            Time.timeScale = 1f;
            
            DataPersistenceManager.instance.SaveGame();
            
            // Use SceneTransitionManager for proper cleanup
            if (SceneTransitionManager.Instance != null)
            {
                SceneTransitionManager.Instance.LoadMenuScene(mainMenuSceneName);
            }
            else
            {
                // Fallback to direct scene loading
                SceneManager.LoadScene(mainMenuSceneName);
            }
        }

        public void QuitGame()
        {
            // Clear pause state before quitting
            IsGamePaused = false;
            
            // Save game before quitting if logged in
            if (GameAPI.Instance != null && GameAPI.Instance.IsLoggedIn)
            {
                if (GameSaveManager.Instance != null)
                {
                    GameSaveManager.Instance.SaveGame();
                }
            }

            // Restore time scale
            Time.timeScale = 1f;
            DataPersistenceManager.instance.SaveGame();

            Debug.Log("Quitting Game");

#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        private void ShowSaveNotification(string message)
        {
            if (saveNotificationPanel && saveNotificationText)
            {
                saveNotificationText.text = message;
                saveNotificationPanel.SetActive(true);

                // Hide notification after delay
                StopAllCoroutines();
                StartCoroutine(HideNotificationAfterDelay());
            }
            else
            {
                Debug.Log($"Save Notification: {message}");
            }
        }

        private IEnumerator HideNotificationAfterDelay()
        {
            yield return new WaitForSecondsRealtime(saveNotificationDuration);

            if (saveNotificationPanel)
            {
                saveNotificationPanel.SetActive(false);
            }
        }

        // Public methods for external access
        public bool IsPaused => isPaused;

        public void SetPauseMenuActive(bool active)
        {
            if (active)
            {
                PauseGame();
            }
            else
            {
                ContinueGame();
            }
        }
    }
}
