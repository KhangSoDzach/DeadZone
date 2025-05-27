using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro; // Add TextMeshPro namespace
using Scripts.API; // Ensure GameAPI is accessible
using DevionGames;
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

        [Header("Option Panel (Optional)")]
        [SerializeField] private GameObject optionPanel;
        [SerializeField] private Button optionBackButton;

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
        [SerializeField] private string mainMenuSceneName = "MainMenu";
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
            if (optionButton) optionButton.onClick.AddListener(OpenOptions);
            if (saveGameButton) saveGameButton.onClick.AddListener(SaveGame);
            if (mainMenuButton) mainMenuButton.onClick.AddListener(() => ShowConfirmation("Return to Main Menu?", ReturnToMainMenu));
            if (quitGameButton) quitGameButton.onClick.AddListener(() => ShowConfirmation("Quit Game?", QuitGame));

            // Option panel buttons
            if (optionBackButton) optionBackButton.onClick.AddListener(CloseOptions);

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
        }

        public void TogglePause()
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
            if (GameAPI.Instance == null || !GameAPI.Instance.IsLoggedIn)
            {
                ShowSaveNotification("Not logged in - Cannot save game");
                return;
            }

            ShowSaveNotification("Saving game...");

            // Try to save through GameSaveManager if available
            if (GameSaveManager.Instance != null)
            {
                GameSaveManager.Instance.SaveGame();
                ShowSaveNotification("Game saved successfully!");
            }
            // Fallback to direct API save
            else if (GameDataSynchronizer.Instance != null)
            {
                GameDataSynchronizer.Instance.SaveGameData((success, message) =>
                {
                    if (success)
                    {
                        ShowSaveNotification("Game saved successfully!");
                    }
                    else
                    {
                        ShowSaveNotification("Save failed: " + message);
                    }
                });
            }
            else
            {
                ShowSaveNotification("Save system not available");
            }
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

            // Load main menu scene
            SceneManager.LoadScene(mainMenuSceneName);
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
