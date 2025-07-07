using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using DevionGames;
using UnityEngine.SceneManagement;

public class AuthUIManager : MonoBehaviour
{
    [Header("Panels")]
    public GameObject loginPanel;
    public GameObject registerPanel;
    public GameObject loadingPanel;
    
    [Header("Login UI")]
    public InputField loginUsernameInput;
    public InputField loginPasswordInput;
    public Button loginButton;
    public Text loginErrorText;
    
    [Header("Register UI")]
    public InputField registerUsernameInput;
    public InputField registerEmailInput;
    public InputField registerPasswordInput;
    public InputField registerConfirmPasswordInput;
    public Button registerButton;
    public Text registerErrorText;
    
    [Header("Settings")]
    public string gameSceneName = "MainHub";
    public string startSceneName = "Cutscene";
    
    private void Start()
    {
        // Set active panel
        SetActivePanel(loginPanel);
        
        // Hide error texts
        loginErrorText.gameObject.SetActive(false);
        registerErrorText.gameObject.SetActive(false);
        
        // Add button listeners
        loginButton.onClick.AddListener(OnLoginButtonClicked);
        registerButton.onClick.AddListener(OnRegisterButtonClicked);
        
        // Check if user is already logged in with proper data validation
        if (GameAPI.Instance.IsLoggedIn)
        {
            loadingPanel.SetActive(true);
            
            // Verify we have complete player data
            if (GameAPI.Instance.PlayerData != null && 
                !string.IsNullOrEmpty(GameAPI.Instance.PlayerData.id) && 
                !string.IsNullOrEmpty(GameAPI.Instance.PlayerData.username))
            {
                // Complete data available, load game
                LoadGameScene();
            }
            else
            {
                // Try to get complete player data
                StartCoroutine(GameAPI.Instance.GetPlayerData((success, errorMessage) => {
                    if (success && GameAPI.Instance.PlayerData != null && 
                        !string.IsNullOrEmpty(GameAPI.Instance.PlayerData.id))
                    {
                        // Load game scene
                        LoadGameScene();
                    }
                    else
                    {
                        // Show login panel - data incomplete
                        loadingPanel.SetActive(false);
                        Debug.LogWarning($"Failed to load complete player data: {errorMessage}");
                    }
                }));
            }
        }
    }
    
    public void OnLoginButtonClicked()
    {
        string username = loginUsernameInput.text;
        string password = loginPasswordInput.text;
        
        // Validate input
        if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
        {
            ShowLoginError("Please enter username and password");
            return;
        }
        
        // Show loading panel
        SetActivePanel(loadingPanel);
        
        // Call login API
        StartCoroutine(GameAPI.Instance.Login(username, password, (success, errorMessage) => {
            if (success)
            {
                // Save token for auto-login
                if (!string.IsNullOrEmpty(GameAPI.Instance.AuthToken))
                {
                    PlayerPrefs.SetString("AuthToken", GameAPI.Instance.AuthToken);
                    PlayerPrefs.SetString("LastCheckpoint", UnityEngine.SceneManagement.SceneManager.GetActiveScene().name);
                    PlayerPrefs.Save();
                }
                
                // Load game scene
                LoadGameScene();
            }
            else
            {
                // Show error
                SetActivePanel(loginPanel);
                ShowLoginError("Login failed: " + errorMessage);
            }
        }));
    }
    
    public void OnRegisterButtonClicked()
    {
        string username = registerUsernameInput.text;
        string email = registerEmailInput.text;
        string password = registerPasswordInput.text;
        string confirmPassword = registerConfirmPasswordInput.text;
        
        // Validate input
        if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(email) || 
            string.IsNullOrEmpty(password) || string.IsNullOrEmpty(confirmPassword))
        {
            ShowRegisterError("Please fill in all fields");
            return;
        }
        
        if (password != confirmPassword)
        {
            ShowRegisterError("Passwords do not match");
            return;
        }
        
        // Validate email format
        if (!IsValidEmail(email))
        {
            ShowRegisterError("Please enter a valid email address");
            return;
        }
        
        // Show loading panel
        SetActivePanel(loadingPanel);
        
        // Call register API
        StartCoroutine(GameAPI.Instance.Register(username, email, password, (success, errorMessage) => {
            if (success)
            {
                // Load game scene
                LoadGameScene();
            }
            else
            {
                // Show error
                SetActivePanel(registerPanel);
                ShowRegisterError("Registration failed: " + errorMessage);
            }
        }));
    }
    
    public void SwitchToRegisterPanel()
    {
        SetActivePanel(registerPanel);
        registerErrorText.gameObject.SetActive(false);
    }
    
    public void SwitchToLoginPanel()
    {
        SetActivePanel(loginPanel);
        loginErrorText.gameObject.SetActive(false);
    }
    
    private void SetActivePanel(GameObject activePanel)
    {
        loginPanel.SetActive(activePanel == loginPanel);
        registerPanel.SetActive(activePanel == registerPanel);
        loadingPanel.SetActive(activePanel == loadingPanel);
    }
    
    private void ShowLoginError(string message)
    {
        loginErrorText.gameObject.SetActive(true);
        loginErrorText.text = message;
    }
    
    private void ShowRegisterError(string errorMessage)
    {
        registerErrorText.text = errorMessage;
        registerErrorText.gameObject.SetActive(true);
    }
    
    private bool IsValidEmail(string email)
    {
        // Simple email validation
        return email.Contains("@") && email.Contains(".");
    }
    
    private void LoadGameScene()
    {
        // Load start scene (cutscene) for new game experience
        string sceneToLoad = !string.IsNullOrEmpty(startSceneName) ? startSceneName : gameSceneName;
        UnityEngine.SceneManagement.SceneManager.LoadScene(sceneToLoad);
    }
}
