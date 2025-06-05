using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class DataLossTestScript : MonoBehaviour
{
    [Header("UI References")]
    public Button loginButton;
    public Button logoutButton;
    public Button testDataButton;
    public Button validateTokenButton;
    public TMP_InputField usernameInput;
    public TMP_InputField passwordInput;
    public TMP_Text statusText;
    public TMP_Text userDataText;
    
    [Header("Test Credentials")]
    public string testUsername = "testuser";
    public string testPassword = "testpass123";
    
    private void Start()
    {
        SetupUI();
        UpdateUI();
    }
    
    private void SetupUI()
    {
        if (loginButton) loginButton.onClick.AddListener(TestLogin);
        if (logoutButton) logoutButton.onClick.AddListener(TestLogout);
        if (testDataButton) testDataButton.onClick.AddListener(TestDataPersistence);
        if (validateTokenButton) validateTokenButton.onClick.AddListener(TestTokenValidation);
        
        if (usernameInput) usernameInput.text = testUsername;
        if (passwordInput) passwordInput.text = testPassword;
    }
    
    private void UpdateUI()
    {
        bool isLoggedIn = GameAPI.Instance != null && GameAPI.Instance.IsLoggedIn;
        
        if (loginButton) loginButton.interactable = !isLoggedIn;
        if (logoutButton) logoutButton.interactable = isLoggedIn;
        if (testDataButton) testDataButton.interactable = isLoggedIn;
        if (validateTokenButton) validateTokenButton.interactable = isLoggedIn;
        
        UpdateStatusText();
        UpdateUserDataText();
    }
    
    private void UpdateStatusText()
    {
        if (!statusText) return;
        
        if (GameAPI.Instance == null)
        {
            statusText.text = "Status: GameAPI not available";
            statusText.color = Color.red;
            return;
        }
        
        bool isLoggedIn = GameAPI.Instance.IsLoggedIn;
        statusText.text = $"Status: {(isLoggedIn ? "Logged In" : "Not Logged In")}";
        statusText.color = isLoggedIn ? Color.green : Color.red;
    }
    
    private void UpdateUserDataText()
    {
        if (!userDataText) return;
        
        if (GameAPI.Instance == null || !GameAPI.Instance.IsLoggedIn || GameAPI.Instance.PlayerData == null)
        {
            userDataText.text = "User Data: Not available";
            return;
        }
        
        var data = GameAPI.Instance.PlayerData;
        userDataText.text = $"User Data:\n" +
                           $"ID: {data.id}\n" +
                           $"Username: {data.username}\n" +
                           $"Email: {data.email}\n" +
                           $"Level: {data.level}\n" +
                           $"Experience: {data.experience}\n" +
                           $"Money: {data.money}\n" +
                           $"Health: {data.health}\n" +
                           $"Last Login: {data.lastLoginDate}";
    }
    
    public void TestLogin()
    {
        if (GameAPI.Instance == null)
        {
            Debug.LogError("GameAPI instance not found!");
            return;
        }
        
        string username = usernameInput ? usernameInput.text : testUsername;
        string password = passwordInput ? passwordInput.text : testPassword;
        
        Debug.Log($"[DataLossTest] Starting login test for user: {username}");
        
        StartCoroutine(GameAPI.Instance.Login(username, password, (success, error) => {
            if (success)
            {
                Debug.Log("[DataLossTest] Login successful!");
                UpdateUI();
            }
            else
            {
                Debug.LogError($"[DataLossTest] Login failed: {error}");
            }
        }));
    }
    
    public void TestLogout()
    {
        if (GameAPI.Instance == null || !GameAPI.Instance.IsLoggedIn)
        {
            Debug.LogError("Not logged in!");
            return;
        }
        
        Debug.Log("[DataLossTest] Testing logout process...");
        
        // Save current data state for comparison
        var beforeLogout = GameAPI.Instance.PlayerData;
        Debug.Log($"[DataLossTest] Data before logout: {beforeLogout?.username} (ID: {beforeLogout?.id})");
        
        GameAPI.Instance.Logout();
        
        Debug.Log("[DataLossTest] Logout completed");
        UpdateUI();
        
        // Check if data was saved for recovery
        string savedData = PlayerPrefs.GetString("LastUserData", "");
        if (!string.IsNullOrEmpty(savedData))
        {
            Debug.Log("[DataLossTest] ✅ User data was saved during logout for recovery");
        }
        else
        {
            Debug.LogWarning("[DataLossTest] ⚠️ No user data saved during logout");
        }
    }
    
    public void TestDataPersistence()
    {
        if (GameAPI.Instance == null || !GameAPI.Instance.IsLoggedIn)
        {
            Debug.LogError("Not logged in!");
            return;
        }
        
        Debug.Log("[DataLossTest] Testing data persistence...");
        
        var originalData = GameAPI.Instance.PlayerData;
        Debug.Log($"[DataLossTest] Original data: {originalData?.username} (Level: {originalData?.level})");
        
        // Modify some data
        if (originalData != null)
        {
            originalData.experience += 100;
            originalData.money += 500;
            originalData.level += 1;
            
            Debug.Log($"[DataLossTest] Modified data: Experience +100, Money +500, Level +1");
            
            // Save the data
            StartCoroutine(GameAPI.Instance.SavePlayerData((success, error) => {
                if (success)
                {
                    Debug.Log("[DataLossTest] ✅ Data saved successfully");
                    UpdateUI();
                }
                else
                {
                    Debug.LogError($"[DataLossTest] ❌ Data save failed: {error}");
                }
            }));
        }
    }
    
    public void TestTokenValidation()
    {
        if (GameAPI.Instance == null)
        {
            Debug.LogError("GameAPI instance not found!");
            return;
        }
        
        Debug.Log("[DataLossTest] Testing token validation...");
        
        StartCoroutine(GameAPI.Instance.ValidateCurrentToken((valid, error) => {
            if (valid)
            {
                Debug.Log("[DataLossTest] ✅ Token validation successful");
            }
            else
            {
                Debug.LogWarning($"[DataLossTest] ⚠️ Token validation failed: {error}");
            }
            UpdateUI();
        }));
    }
    
    // Call this to test the complete logout/login cycle
    [ContextMenu("Test Complete Logout/Login Cycle")]
    public void TestLogoutLoginCycle()
    {
        if (GameAPI.Instance == null || !GameAPI.Instance.IsLoggedIn)
        {
            Debug.LogError("Must be logged in first to test the cycle!");
            return;
        }
        
        StartCoroutine(LogoutLoginCycleCoroutine());
    }
    
    private IEnumerator LogoutLoginCycleCoroutine()
    {
        Debug.Log("[DataLossTest] === Starting Complete Logout/Login Cycle Test ===");
        
        // Store original data
        var originalData = GameAPI.Instance.PlayerData;
        string originalUsername = originalData?.username;
        int originalLevel = originalData?.level ?? 1;
        int originalMoney = originalData?.money ?? 0;
        
        Debug.Log($"[DataLossTest] Original state: {originalUsername} (Level: {originalLevel}, Money: {originalMoney})");
        
        // Step 1: Logout
        Debug.Log("[DataLossTest] Step 1: Logging out...");
        GameAPI.Instance.Logout();
        yield return new WaitForSeconds(1f);
        
        // Step 2: Login again
        Debug.Log("[DataLossTest] Step 2: Logging back in...");
        string username = usernameInput ? usernameInput.text : testUsername;
        string password = passwordInput ? passwordInput.text : testPassword;
        
        bool loginSuccess = false;
        yield return StartCoroutine(GameAPI.Instance.Login(username, password, (success, error) => {
            loginSuccess = success;
            if (!success)
            {
                Debug.LogError($"[DataLossTest] Re-login failed: {error}");
            }
        }));
        
        if (!loginSuccess)
        {
            Debug.LogError("[DataLossTest] ❌ Logout/Login cycle test failed - could not re-login");
            yield break;
        }
        
        yield return new WaitForSeconds(1f);
        
        // Step 3: Check data integrity
        var newData = GameAPI.Instance.PlayerData;
        if (newData == null)
        {
            Debug.LogError("[DataLossTest] ❌ No player data after re-login!");
            yield break;
        }
        
        Debug.Log($"[DataLossTest] After re-login: {newData.username} (Level: {newData.level}, Money: {newData.money})");
        
        // Compare data
        bool dataIntact = (newData.username == originalUsername && 
                          newData.level == originalLevel && 
                          newData.money == originalMoney);
        
        if (dataIntact)
        {
            Debug.Log("[DataLossTest] ✅ SUCCESS: Data persisted correctly through logout/login cycle!");
        }
        else
        {
            Debug.LogWarning("[DataLossTest] ⚠️ WARNING: Data changed during logout/login cycle");
            Debug.LogWarning($"[DataLossTest] Expected: {originalUsername} (Level: {originalLevel}, Money: {originalMoney})");
            Debug.LogWarning($"[DataLossTest] Got: {newData.username} (Level: {newData.level}, Money: {newData.money})");
        }
        
        UpdateUI();
        Debug.Log("[DataLossTest] === Logout/Login Cycle Test Complete ===");
    }
    
    private void Update()
    {
        // Update UI every few frames to keep it current
        if (Time.frameCount % 30 == 0)
        {
            UpdateUI();
        }
    }
}
