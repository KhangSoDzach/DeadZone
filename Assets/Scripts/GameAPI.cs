using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using Newtonsoft.Json;

public class GameAPI : MonoBehaviour
{
    private static GameAPI _instance;
    public static GameAPI Instance
    {
        get
        {
            if (_instance == null)
            {
                GameObject go = new GameObject("GameAPI");
                _instance = go.AddComponent<GameAPI>();
                DontDestroyOnLoad(go);
            }
            return _instance;
        }
    }
    
    [Header("API Settings")]
    private const string API_URL = "http://localhost:5000/api";
    [SerializeField] private float requestTimeout = 30f;
    [SerializeField] private bool debugMode = true;
    
    // Authentication
    public string AuthToken { get; private set; }
    public PlayerDataModel PlayerData { get; private set; }
    public bool IsLoggedIn => !string.IsNullOrEmpty(AuthToken);
    
    // Events
    public delegate void PlayerDataLoadedHandler(PlayerDataModel playerData);
    public event PlayerDataLoadedHandler OnPlayerDataLoaded;
      private void Awake()
    {
        if (_instance == null)
        {
            _instance = this;
            DontDestroyOnLoad(gameObject);
            DebugLog("GameAPI instance created");
        }
        else if (_instance != this)
        {
            Destroy(gameObject);
        }
    }
    
    private void Start()
    {
        DebugLog("GameAPI started - checking for saved authentication...");
        CheckSavedAuthentication();
    }
      private void CheckSavedAuthentication()
    {
        string savedToken = PlayerPrefs.GetString("AuthToken", "");
        if (!string.IsNullOrEmpty(savedToken))
        {
            DebugLog($"Found saved token: {savedToken.Substring(0, Math.Min(10, savedToken.Length))}...");
            StartCoroutine(LoginWithToken(savedToken, (success, error) => {
                if (success)
                {
                    DebugLog("Automatic authentication successful");
                      // Check if we have player data, if not, try to fetch it
                    if (PlayerData == null || string.IsNullOrEmpty(PlayerData.id) || string.IsNullOrEmpty(PlayerData.username))
                    {
                        DebugLog("Player data missing after token verification, fetching...");
                        StartCoroutine(GetPlayerData((dataSuccess, dataError) => {
                            if (dataSuccess)
                            {
                                DebugLog("Player data loaded successfully after token verification");
                            }
                            else
                            {
                                DebugLog($"Failed to load player data after token verification: {dataError}");
                                // Clear invalid token
                                AuthToken = null;
                                PlayerPrefs.DeleteKey("AuthToken");
                                PlayerPrefs.Save();
                            }
                        }));
                    }
                    else
                    {
                        // Always refresh player data on startup to ensure we have latest saved data
                        DebugLog("Player data exists but refreshing from server to get latest save state...");
                        StartCoroutine(GetPlayerData((dataSuccess, dataError) => {
                            if (dataSuccess)
                            {
                                DebugLog("Fresh player data loaded successfully on startup");
                            }
                            else
                            {
                                DebugLog($"Failed to refresh player data on startup: {dataError}");
                                // Keep existing data if refresh fails
                            }
                        }));
                    }
                }                else
                {
                    DebugLog($"Automatic authentication failed: {error}");
                    // Only clear token if it's definitely an authentication error (401)
                    // Don't clear for server connectivity issues, 404s, or other errors
                    if (error.Contains("Token is invalid") || error.Contains("401") || error.Contains("Authorization expired"))
                    {
                        DebugLog("Token is definitely invalid - clearing saved authentication");
                        PlayerPrefs.DeleteKey("AuthToken");
                        PlayerPrefs.Save();
                    }
                    else
                    {
                        DebugLog("Server connectivity issue - keeping token for later retry");
                    }
                }
            }));
        }
        else
        {
            DebugLog("No saved authentication token found");
        }
    }
    
    // Diagnostic method to check current state
    public void LogCurrentState()
    {
        DebugLog("=== GameAPI Current State ===");
        DebugLog($"IsLoggedIn: {IsLoggedIn}");
        DebugLog($"AuthToken exists: {!string.IsNullOrEmpty(AuthToken)}");
        DebugLog($"AuthToken: {AuthToken?.Substring(0, Math.Min(10, AuthToken?.Length ?? 0))}...");
        DebugLog($"PlayerData exists: {PlayerData != null}");
        if (PlayerData != null)
        {
            DebugLog($"PlayerData.id: '{PlayerData.id}'");
            DebugLog($"PlayerData.username: '{PlayerData.username}'");
            DebugLog($"PlayerData.email: '{PlayerData.email}'");
            DebugLog($"PlayerData.level: {PlayerData.level}");
            DebugLog($"PlayerData.experience: {PlayerData.experience}");
            DebugLog($"PlayerData.money: {PlayerData.money}");
            DebugLog($"PlayerData.health: {PlayerData.health}");
        }
        DebugLog("=== End State ===");
    }
    
    public IEnumerator Login(string username, string password, Action<bool, string> onComplete)
    {
        // Enhanced client-side validation with specific error messages
        if (string.IsNullOrEmpty(username.Trim()) && string.IsNullOrEmpty(password.Trim()))
        {
            onComplete?.Invoke(false, "Vui lòng nhập tên đăng nhập và mật khẩu");
            yield break;
        }
        
        if (string.IsNullOrEmpty(username.Trim()))
        {
            onComplete?.Invoke(false, "Vui lòng nhập tên đăng nhập");
            yield break;
        }
        
        if (string.IsNullOrEmpty(password.Trim()))
        {
            onComplete?.Invoke(false, "Vui lòng nhập mật khẩu");
            yield break;
        }
        
        if (username.Length < 3)
        {
            onComplete?.Invoke(false, "Tên đăng nhập phải có ít nhất 3 ký tự");
            yield break;
        }
        
        if (password.Length < 6)
        {
            onComplete?.Invoke(false, "Mật khẩu phải có ít nhất 6 ký tự");
            yield break;
        }
        
        var loginData = new
        {
            username = username.Trim(),
            password = password
        };
        
        string jsonData = JsonConvert.SerializeObject(loginData);
        
        using (UnityWebRequest request = new UnityWebRequest($"{API_URL}/auth/login", "POST"))
        {
            byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(jsonData);
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");
            request.timeout = (int)requestTimeout;
            
            yield return request.SendWebRequest();
        
            if (request.result == UnityWebRequest.Result.Success)
            {
                bool parseSuccess = false;
                string parseError = "";
                
                try
                {
                    DebugLog($"Login response received: {request.downloadHandler.text}");
                    
                    // Try to parse as our standard LoginResponse first
                    var response = JsonConvert.DeserializeObject<LoginResponse>(request.downloadHandler.text);
                    
                    if (response == null)
                    {
                        DebugLog("Standard login response format not detected, trying alternative format...");
                        // Try to parse the response in a more flexible way
                        var dynamicResponse = JsonConvert.DeserializeObject<Dictionary<string, object>>(request.downloadHandler.text);
                        
                        if (dynamicResponse == null)
                        {
                            DebugLog("Error: Login response could not be parsed in any format");
                            parseError = "Invalid server response structure";
                        }
                        else if (!dynamicResponse.TryGetValue("token", out object tokenObj) || tokenObj == null)
                        {
                            DebugLog("Error: No token found in dynamic response");
                            parseError = "No authentication token received";
                        }
                        else
                        {
                            // Extract the token
                            string token = tokenObj.ToString();
                            if (string.IsNullOrEmpty(token))
                            {
                                DebugLog("Error: Empty token received in login response");
                                parseError = "Empty authentication token received";
                            }
                            else
                            {
                                AuthToken = token;
                                
                                // Try to extract user data if it exists
                                if (dynamicResponse.TryGetValue("user", out object userObj) && userObj != null)
                                {
                                    try
                                    {
                                        string userJson = JsonConvert.SerializeObject(userObj);
                                        PlayerData = JsonConvert.DeserializeObject<PlayerDataModel>(userJson);
                                        
                                        // If user data is missing critical fields, try different property names
                                        if (PlayerData != null && (string.IsNullOrEmpty(PlayerData.id) || string.IsNullOrEmpty(PlayerData.username)))
                                        {
                                            var userDict = JsonConvert.DeserializeObject<Dictionary<string, object>>(userJson);
                                            if (userDict != null)
                                            {
                                                // Check for alternative field names
                                                if (string.IsNullOrEmpty(PlayerData.id) && userDict.TryGetValue("_id", out object idObj))
                                                    PlayerData.id = idObj.ToString();
                                                
                                                if (string.IsNullOrEmpty(PlayerData.username) && userDict.TryGetValue("name", out object nameObj))
                                                    PlayerData.username = nameObj.ToString();
                                            }
                                        }
                                    }
                                    catch (Exception userEx)
                                    {
                                        DebugLog($"Warning: Failed to parse user data: {userEx.Message}");
                                        PlayerData = null; // Clear invalid data
                                    }
                                }
                                else
                                {
                                    DebugLog("Warning: No user data found in response");
                                    PlayerData = null;
                                }
                                parseSuccess = true;
                            }
                        }
                    }
                    else
                    {
                        // Standard response format was parsed successfully
                        if (string.IsNullOrEmpty(response.token))
                        {
                            DebugLog("Error: No token received in login response");
                            parseError = "No authentication token received";
                        }
                        else
                        {
                            AuthToken = response.token;
                            PlayerData = response.user;
                            parseSuccess = true;
                        }
                    }
                }
                catch (Exception ex)
                {
                    DebugLog($"Login response parse error: {ex.Message}");
                    DebugLog($"Raw response: {request.downloadHandler.text}");
                    parseError = "Invalid server response";
                }
            
                if (parseSuccess)
                {
                    // Save token to PlayerPrefs for persistence
                    PlayerPrefs.SetString("AuthToken", AuthToken);
                    PlayerPrefs.Save();
                      DebugLog($"Login successful for user: {username}");
                    DebugLog($"Token received and saved: {AuthToken?.Substring(0, Math.Min(10, AuthToken?.Length ?? 0))}...");
                    
                    // Validate the token immediately to catch any issues
                    yield return StartCoroutine(ValidateCurrentToken((tokenValid, tokenError) => {
                        if (!tokenValid)
                        {
                            DebugLog($"Token validation failed immediately after login: {tokenError}");
                            // Don't fail login here, just log it - we'll try to get player data anyway
                        }
                        else
                        {
                            DebugLog("Token validation successful after login");
                        }
                    }));
                    
                      // Check if we have complete player data
                    if (PlayerData != null && !string.IsNullOrEmpty(PlayerData.id) && !string.IsNullOrEmpty(PlayerData.username))
                    {
                        DebugLog($"Complete user data received - ID: '{PlayerData.id}', Username: '{PlayerData.username}', Email: '{PlayerData.email}'");
                        OnPlayerDataLoaded?.Invoke(PlayerData);
                        onComplete?.Invoke(true, "");
                    }
                    else
                    {
                        DebugLog("Incomplete user data in login response, fetching from server...");
                        DebugLog($"Current PlayerData state: ID='{PlayerData?.id}', Username='{PlayerData?.username}'");
                        
                        // Wait a moment before fetching (some servers need time to process)
                        yield return new WaitForSeconds(0.5f);
                        
                        // Use a separate coroutine to fetch player data
                        StartCoroutine(FetchPlayerDataAfterLogin(onComplete));
                    }
                }
                else
                {
                    onComplete?.Invoke(false, parseError);
                }
            }
            else
            {
                string errorMsg = GetErrorMessage(request);
                DebugLog($"Login failed: {errorMsg}");
                onComplete?.Invoke(false, errorMsg);
            }
        }
    }    private IEnumerator FetchPlayerDataAfterLogin(Action<bool, string> onComplete)
    {
        DebugLog($"FetchPlayerDataAfterLogin: Starting with token: {AuthToken?.Substring(0, Math.Min(10, AuthToken?.Length ?? 0))}...");
        
        // Add a small delay to allow server to process the token
        yield return new WaitForSeconds(1f);
        
        // First, try to validate the token before fetching player data
        bool tokenValid = false;
        string tokenError = "";
        
        yield return StartCoroutine(ValidateCurrentToken((valid, error) => {
            tokenValid = valid;
            tokenError = error;
        }));
        
        if (!tokenValid)
        {
            DebugLog($"Token validation failed after login: {tokenError}");
            // Clear invalid token and report error
            AuthToken = null;
            PlayerPrefs.DeleteKey("AuthToken");
            PlayerPrefs.Save();
            onComplete?.Invoke(false, "Login failed: Invalid authentication token");
            yield break;
        }
        
        DebugLog("Token validated successfully, fetching player data...");
        
        yield return StartCoroutine(GetPlayerData((dataSuccess, dataError) => {
            if (dataSuccess && PlayerData != null && !string.IsNullOrEmpty(PlayerData.id))
            {
                DebugLog("Player data fetched successfully after login");
                OnPlayerDataLoaded?.Invoke(PlayerData);
                onComplete?.Invoke(true, "");
            }
            else
            {
                DebugLog($"Failed to fetch player data after login: {dataError}");
                
                // If token is invalid, try to clear it and report login failure
                if (dataError.Contains("Authorization expired") || dataError.Contains("Token is not valid"))
                {
                    DebugLog("Clearing invalid token received during login");
                    AuthToken = null;
                    PlayerPrefs.DeleteKey("AuthToken");
                    PlayerPrefs.Save();
                    onComplete?.Invoke(false, "Login failed: Invalid token received from server");
                }
                else
                {
                    onComplete?.Invoke(false, "Login successful but failed to load user data: " + dataError);
                }
            }
        }));
    }
      public IEnumerator LoginWithToken(string token, Action<bool, string> onComplete)
    {
        DebugLog($"Attempting token verification with: {token?.Substring(0, Math.Min(10, token?.Length ?? 0))}...");
          // Try primary endpoint first
        using (UnityWebRequest request = UnityWebRequest.Get($"{API_URL}/auth/verify"))
        {
            request.SetRequestHeader("x-auth-token", token);
            request.timeout = (int)requestTimeout;
              yield return request.SendWebRequest();
            
            // Log response code but don't spam for 404s since we have fallback
            if (request.responseCode == 404) {
                DebugLog("Auth verify endpoint not found (404), trying alternative method...");
            } else {
                DebugLog($"Token verification response code: {request.responseCode}");
            }
            
            if (request.result == UnityWebRequest.Result.Success)
            {
                try
                {
                    string responseText = request.downloadHandler.text;
                    if (string.IsNullOrEmpty(responseText))
                    {
                        DebugLog("Error: Token verification returned empty response");
                        onComplete?.Invoke(false, "Empty server response");
                        yield break;
                    }
                      try {
                        PlayerData = JsonConvert.DeserializeObject<PlayerDataModel>(responseText);
                        
                        if (PlayerData == null)
                        {
                            DebugLog("Error: Failed to parse user data from token verification");
                            onComplete?.Invoke(false, "Invalid user data format");
                            yield break;
                        }
                        
                        // Check for missing critical fields
                        if (string.IsNullOrEmpty(PlayerData.id) || string.IsNullOrEmpty(PlayerData.username))
                        {
                            DebugLog("Attempting to adapt data from token verification...");
                            
                            // Try to parse the raw response to look for alternative field names
                            var rawData = JsonConvert.DeserializeObject<Dictionary<string, object>>(responseText);
                            if (rawData != null)
                            {
                                // Check for _id instead of id
                                if (string.IsNullOrEmpty(PlayerData.id) && rawData.ContainsKey("_id"))
                                {
                                    PlayerData.id = rawData["_id"].ToString();
                                    DebugLog($"Found alternative ID field: '{PlayerData.id}'");
                                }
                                
                                // Check for name instead of username
                                if (string.IsNullOrEmpty(PlayerData.username) && rawData.ContainsKey("name"))
                                {
                                    PlayerData.username = rawData["name"].ToString();
                                    DebugLog($"Found alternative username field: '{PlayerData.username}'");
                                }
                                
                                // Still missing critical fields?
                                if (string.IsNullOrEmpty(PlayerData.id) || string.IsNullOrEmpty(PlayerData.username))
                                {
                                    DebugLog("Still missing critical fields after adaptation");
                                    onComplete?.Invoke(false, "Invalid user data structure");
                                    yield break;
                                }
                            }
                        }
                    }
                    catch (Exception parseEx) {
                        DebugLog($"Error parsing token verification response: {parseEx.Message}");
                        onComplete?.Invoke(false, "Failed to parse user data");
                        yield break;
                    }
                    
                    AuthToken = token;
                    PlayerPrefs.SetString("AuthToken", AuthToken);
                    PlayerPrefs.Save();
                    
                    DebugLog($"Token verification successful - ID: '{PlayerData.id}', Username: '{PlayerData.username}'");
                    onComplete?.Invoke(true, "");
                    yield break;
                }
                catch (Exception ex)
                {
                    DebugLog($"Token verification response parse error: {ex.Message}");
                    onComplete?.Invoke(false, "Invalid server response");
                    yield break;
                }
            }            // If primary endpoint fails with 404, try fallback approach
            if (request.responseCode == 404)
            {
                // 404 is expected when server doesn't have /auth/verify endpoint
                DebugLog("Auth verify endpoint not found (404) - using alternative method...");
                yield return StartCoroutine(LoginWithTokenFallback(token, onComplete));
            }
            else
            {
                string errorMsg = GetErrorMessage(request);
                // Only fail if it's definitely an authentication error
                if (request.responseCode == 401)
                {
                    DebugLog($"Token is invalid (401): {errorMsg}");
                    onComplete?.Invoke(false, "Token is invalid");
                }
                else
                {
                    DebugLog($"Server error or connectivity issue: {errorMsg} - trying fallback");
                    yield return StartCoroutine(LoginWithTokenFallback(token, onComplete));
                }
            }
        }
    }
      private IEnumerator LoginWithTokenFallback(string token, Action<bool, string> onComplete)
    {        // Fallback: Try to get player data directly via /player/data endpoint
        DebugLog("Trying alternative token validation via player data endpoint...");
        
        using (UnityWebRequest request = UnityWebRequest.Get($"{API_URL}/player/data"))
        {
            request.SetRequestHeader("x-auth-token", token);
            request.timeout = (int)requestTimeout;
            
            yield return request.SendWebRequest();
            
            DebugLog($"Fallback token verification response code: {request.responseCode}");
            
            if (request.result == UnityWebRequest.Result.Success)
            {
                try
                {
                    string responseText = request.downloadHandler.text;
                    if (string.IsNullOrEmpty(responseText))
                    {
                        DebugLog("Error: Fallback verification returned empty response");
                        onComplete?.Invoke(false, "Empty server response");
                        yield break;
                    }
                    
                    PlayerData = JsonConvert.DeserializeObject<PlayerDataModel>(responseText);
                    
                    // Validate the loaded data
                    if (PlayerData == null || string.IsNullOrEmpty(PlayerData.id))
                    {
                        DebugLog("Error: Invalid player data from fallback verification");
                        
                        // Try to restore from last saved state if available
                        string lastUserData = PlayerPrefs.GetString("LastUserData", "");
                        if (!string.IsNullOrEmpty(lastUserData))
                        {
                            DebugLog("Attempting to restore from last saved user data...");
                            try
                            {
                                var restoredData = JsonConvert.DeserializeObject<PlayerDataModel>(lastUserData);
                                if (restoredData != null && !string.IsNullOrEmpty(restoredData.id))
                                {
                                    PlayerData = restoredData;
                                    DebugLog($"Restored user data: {PlayerData.username} (ID: {PlayerData.id})");
                                }
                            }
                            catch (Exception ex)
                            {
                                DebugLog($"Failed to restore saved user data: {ex.Message}");
                            }
                        }
                        
                        if (PlayerData == null || string.IsNullOrEmpty(PlayerData.id))
                        {
                            onComplete?.Invoke(false, "Invalid user data");
                            yield break;
                        }
                    }
                    
                    AuthToken = token;
                    PlayerPrefs.SetString("AuthToken", AuthToken);
                    PlayerPrefs.Save();
                    
                    DebugLog($"Fallback token verification successful - ID: '{PlayerData.id}', Username: '{PlayerData.username}'");
                    OnPlayerDataLoaded?.Invoke(PlayerData);
                    onComplete?.Invoke(true, "");
                }
                catch (Exception ex)
                {
                    DebugLog($"Fallback verification parse error: {ex.Message}");
                    onComplete?.Invoke(false, "Invalid server response");
                }
            }
            else
            {
                string errorMsg = GetErrorMessage(request);
                DebugLog($"Fallback token verification also failed: {errorMsg}");
                onComplete?.Invoke(false, errorMsg);
            }
        }
    }
    
    public IEnumerator Register(string username, string email, string password, Action<bool, string> onComplete)
    {
        // Enhanced client-side validation with specific Vietnamese messages
        if (string.IsNullOrEmpty(username.Trim()) && string.IsNullOrEmpty(email.Trim()) && string.IsNullOrEmpty(password.Trim()))
        {
            onComplete?.Invoke(false, "Vui lòng điền đầy đủ tất cả các trường thông tin");
            yield break;
        }
        
        if (string.IsNullOrEmpty(username.Trim()))
        {
            onComplete?.Invoke(false, "Vui lòng nhập tên đăng nhập");
            yield break;
        }
        
        if (string.IsNullOrEmpty(email.Trim()))
        {
            onComplete?.Invoke(false, "Vui lòng nhập email");
            yield break;
        }
        
        if (string.IsNullOrEmpty(password.Trim()))
        {
            onComplete?.Invoke(false, "Vui lòng nhập mật khẩu");
            yield break;
        }
        
        if (username.Length < 3)
        {
            onComplete?.Invoke(false, "Tên đăng nhập phải có ít nhất 3 ký tự");
            yield break;
        }
        
        if (password.Length < 6)
        {
            onComplete?.Invoke(false, "Mật khẩu phải có ít nhất 6 ký tự");
            yield break;
        }
        
        if (!IsValidEmail(email))
        {
            onComplete?.Invoke(false, "Vui lòng nhập một địa chỉ email hợp lệ");
            yield break;
        }
        
        var registerData = new
        {
            username = username.Trim(),
            email = email.Trim().ToLower(),
            password = password
        };
        
        string jsonData = JsonConvert.SerializeObject(registerData);
        
        using (UnityWebRequest request = new UnityWebRequest($"{API_URL}/auth/register", "POST"))
        {
            byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(jsonData);
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");
            request.timeout = (int)requestTimeout;
            
            yield return request.SendWebRequest();
            
            if (request.result == UnityWebRequest.Result.Success)
            {
                try
                {                    var response = JsonConvert.DeserializeObject<LoginResponse>(request.downloadHandler.text);
                    AuthToken = response.token;
                    PlayerData = response.user;
                    
                    // Save token to PlayerPrefs for persistence
                    PlayerPrefs.SetString("AuthToken", AuthToken);
                    PlayerPrefs.Save();
                    
                    DebugLog($"Registration successful for user: {username}");
                    onComplete?.Invoke(true, "");
                }
                catch (Exception ex)
                {
                    DebugLog($"Registration response parse error: {ex.Message}");
                    onComplete?.Invoke(false, "Invalid server response");
                }
            }
            else
            {
                string errorMsg = GetErrorMessage(request);
                DebugLog($"Registration failed: {errorMsg}");
                onComplete?.Invoke(false, errorMsg);
            }
        }
    }    public IEnumerator GetPlayerData(Action<bool, string> onComplete)
    {
        if (string.IsNullOrEmpty(AuthToken))
        {
            DebugLog("Error: No auth token available for GetPlayerData");
            onComplete?.Invoke(false, "Not authenticated");
            yield break;
        }

        DebugLog($"Getting player data with token: {AuthToken.Substring(0, Math.Min(10, AuthToken.Length))}...");
        DebugLog($"Full API URL: {API_URL}/player/data");
        
        using (UnityWebRequest request = UnityWebRequest.Get($"{API_URL}/player/data"))
        {
            request.SetRequestHeader("x-auth-token", AuthToken);
            request.SetRequestHeader("Content-Type", "application/json");
            request.timeout = (int)requestTimeout;
            
            DebugLog($"Request headers set - x-auth-token: {AuthToken.Substring(0, Math.Min(10, AuthToken.Length))}...");
            DebugLog($"Request URL: {request.url}");
            
            yield return request.SendWebRequest();
            
            DebugLog($"GetPlayerData response code: {request.responseCode}");
            DebugLog($"GetPlayerData response: {request.downloadHandler.text}");
            
            if (request.result == UnityWebRequest.Result.Success)
            {
                try
                {
                    string responseText = request.downloadHandler.text;
                    PlayerData = JsonConvert.DeserializeObject<PlayerDataModel>(responseText);
                    
                    if (PlayerData != null && !string.IsNullOrEmpty(PlayerData.id))
                    {
                        DebugLog($"Player data loaded successfully - ID: {PlayerData.id}, Username: {PlayerData.username}");
                        OnPlayerDataLoaded?.Invoke(PlayerData);
                        onComplete?.Invoke(true, "");
                    }
                    else
                    {
                        DebugLog("Error: Received invalid player data");
                        DebugLog($"PlayerData is null: {PlayerData == null}");
                        if (PlayerData != null)
                        {
                            DebugLog($"PlayerData.id: '{PlayerData.id}', PlayerData.username: '{PlayerData.username}'");
                        }
                        onComplete?.Invoke(false, "Invalid player data received");
                    }
                }
                catch (Exception ex)
                {
                    DebugLog($"Error parsing player data: {ex.Message}");
                    DebugLog($"Raw response that failed to parse: {request.downloadHandler.text}");
                    onComplete?.Invoke(false, "Failed to parse player data");
                }
            }            else
            {
                string errorMsg = GetErrorMessage(request);
                DebugLog($"GetPlayerData failed: {errorMsg}");
                
                // Check if it's an authorization error
                if (request.responseCode == 401)
                {
                    DebugLog("Authorization error detected (401) - token is invalid, clearing");
                    AuthToken = null;
                    PlayerData = null;
                    PlayerPrefs.DeleteKey("AuthToken");
                    PlayerPrefs.Save();
                    onComplete?.Invoke(false, "Authorization expired - please login again");
                }
                else if (request.responseCode == 404)
                {
                    DebugLog("Player data endpoint not found (404) - server may be missing endpoint");
                    onComplete?.Invoke(false, "Server endpoint not available - please try again later");
                }
                else
                {
                    DebugLog($"Server error or connectivity issue (code: {request.responseCode}) - not clearing data");
                    onComplete?.Invoke(false, $"Failed to load data: {errorMsg}");
                }
            }
        }
    }
      public IEnumerator SavePlayerData(Action<bool, string> onComplete)
    {
        DebugLog("=== SavePlayerData Called ===");
        DebugLog($"IsLoggedIn: {IsLoggedIn}");
        DebugLog($"AuthToken exists: {!string.IsNullOrEmpty(AuthToken)}");
        DebugLog($"AuthToken value: {AuthToken?.Substring(0, Math.Min(10, AuthToken?.Length ?? 0))}...");
        DebugLog($"PlayerData exists: {PlayerData != null}");
        
        if (!IsLoggedIn || PlayerData == null)
        {
            string errorMsg = !IsLoggedIn ? "Not logged in" : "No player data";
            DebugLog($"Cannot save player data: {errorMsg}");
            if (PlayerData == null)
            {
                DebugLog("PlayerData is null - this indicates a data loading issue");
            }
            if (string.IsNullOrEmpty(AuthToken))
            {
                DebugLog("AuthToken is null or empty - checking PlayerPrefs...");
                string savedToken = PlayerPrefs.GetString("AuthToken", "");
                if (!string.IsNullOrEmpty(savedToken))
                {
                    DebugLog($"Found saved token in PlayerPrefs: {savedToken.Substring(0, Math.Min(10, savedToken.Length))}...");
                    AuthToken = savedToken;
                    DebugLog("Restored AuthToken from PlayerPrefs");
                }
                else
                {
                    DebugLog("No saved token found in PlayerPrefs");
                }
            }
            
            // Check again after token restoration attempt
            if (!IsLoggedIn)
            {
                onComplete?.Invoke(false, "No token, authorization denied");
                yield break;
            }
        }
        
        // Validate player data has essential fields
        if (string.IsNullOrEmpty(PlayerData.id) || string.IsNullOrEmpty(PlayerData.username))
        {
            DebugLog($"Invalid player data - ID: '{PlayerData.id}', Username: '{PlayerData.username}'");
            DebugLog("This indicates the player data was not properly loaded from the server");
            onComplete?.Invoke(false, "Invalid player data");
            yield break;
        }
        
        // Ensure we have the latest data before saving by collecting from game state
        DebugLog("Collecting latest game state before saving...");
        if (GameDataSynchronizer.Instance != null)
        {
            // Collect fresh data from the game before saving
            try
            {
                var collectDataMethod = typeof(GameDataSynchronizer).GetMethod("CollectDataFromGame", 
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                collectDataMethod?.Invoke(GameDataSynchronizer.Instance, null);
                DebugLog("Successfully collected latest game data before save");
            }
            catch (System.Exception ex)
            {
                DebugLog($"Error collecting game data before save: {ex.Message}");
            }
        }
        
        // Update timestamp before saving to ensure data freshness
        PlayerData.lastLoginDate = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
        
        // Create comprehensive save payload with user identification for server-side overwrite
        var savePayload = new {
            // Ensure user identification for proper overwrite
            userId = PlayerData.id,
            username = PlayerData.username,
            email = PlayerData.email,
            
            // Game progress data that should overwrite existing data
            level = PlayerData.level,
            experience = PlayerData.experience,
            money = PlayerData.money,
            health = PlayerData.health,
            kills = PlayerData.kills,
            lastLoginDate = PlayerData.lastLoginDate,
            
            // Checkpoint data for position saving
            checkpoint = PlayerData.checkpoint,
            
            // Weapons data
            weapons = PlayerData.weapons,
            
            // Additional field to force server-side overwrite
            forceOverwrite = true,
            saveTimestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff")
        };
        
        string jsonData = JsonConvert.SerializeObject(savePayload);
        DebugLog($"Saving player data for user: {PlayerData.username} (ID: {PlayerData.id})");
        DebugLog($"Using AuthToken: {AuthToken?.Substring(0, Math.Min(10, AuthToken?.Length ?? 0))}...");
        DebugLog($"Save payload size: {jsonData.Length} characters");
        
        // Try primary save endpoint first (/api/player/save) with overwrite semantics
        using (UnityWebRequest request = new UnityWebRequest($"{API_URL}/player/save", "PUT"))
        {
            byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(jsonData);
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");
            request.SetRequestHeader("x-auth-token", AuthToken);
            // Add header to explicitly request overwrite behavior
            request.SetRequestHeader("X-Force-Overwrite", "true");
            request.timeout = (int)requestTimeout;
            
            DebugLog($"Sending save request to: {API_URL}/player/save");
            yield return request.SendWebRequest();
            
            DebugLog($"Save response code: {request.responseCode}");
            DebugLog($"Save response: {request.downloadHandler.text}");
            
            if (request.result == UnityWebRequest.Result.Success)
            {
                DebugLog("Player data saved successfully with overwrite");
                
                // Verify the save was successful by checking response
                try
                {
                    var responseData = JsonConvert.DeserializeObject<PlayerDataModel>(request.downloadHandler.text);
                    if (responseData != null && responseData.id == PlayerData.id)
                    {
                        DebugLog("Save verification successful - data properly overwritten on server");
                        onComplete?.Invoke(true, "Data saved and verified");
                    }
                    else
                    {
                        DebugLog("Save response doesn't match sent data - possible server issue");
                        onComplete?.Invoke(true, "Data saved but verification failed");
                    }
                }
                catch (System.Exception ex)
                {
                    DebugLog($"Error parsing save response: {ex.Message}");
                    onComplete?.Invoke(true, "Data saved but response parsing failed");
                }
                yield break;
            }
            else
            {
                string errorMsg = GetErrorMessage(request);
                DebugLog($"Primary save endpoint failed: {errorMsg}");
                
                // If primary fails with 404, try alternative endpoint
                if (request.responseCode == 404)
                {
                    DebugLog("Trying alternative save endpoint (/api/player/data)...");
                    yield return StartCoroutine(SavePlayerDataAlternative(onComplete));
                }
                else
                {
                    onComplete?.Invoke(false, errorMsg);
                }
            }
        }
    }    private IEnumerator SavePlayerDataAlternative(Action<bool, string> onComplete)
    {
        if (!IsLoggedIn || PlayerData == null)
        {
            DebugLog("Alternative save failed: Not logged in or no player data");
            onComplete?.Invoke(false, "Not logged in or no player data");
            yield break;
        }
        
        // Create comprehensive save payload for alternative endpoint with overwrite semantics
        var savePayload = new {
            // Ensure user identification for proper overwrite
            userId = PlayerData.id,
            username = PlayerData.username,
            email = PlayerData.email,
            
            // Game progress data that should overwrite existing data
            level = PlayerData.level,
            experience = PlayerData.experience,
            money = PlayerData.money,
            health = PlayerData.health,
            kills = PlayerData.kills,
            lastLoginDate = PlayerData.lastLoginDate,
            
            // Checkpoint data for position saving
            checkpoint = PlayerData.checkpoint,
            
            // Weapons data
            weapons = PlayerData.weapons,
            
            // Additional field to force server-side overwrite
            forceOverwrite = true,
            saveTimestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff")
        };
        
        string jsonData = JsonConvert.SerializeObject(savePayload);
        DebugLog($"Attempting alternative save for user: {PlayerData.username} (ID: {PlayerData.id})");
        DebugLog($"Using AuthToken: {AuthToken?.Substring(0, Math.Min(10, AuthToken?.Length ?? 0))}...");
        DebugLog($"Alternative save payload size: {jsonData.Length} characters");
        
        using (UnityWebRequest request = new UnityWebRequest($"{API_URL}/player/data", "PUT"))
        {
            byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(jsonData);
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");
            request.SetRequestHeader("x-auth-token", AuthToken);
            // Add header to explicitly request overwrite behavior
            request.SetRequestHeader("X-Force-Overwrite", "true");
            request.SetRequestHeader("X-Save-Mode", "OVERWRITE");
            request.timeout = (int)requestTimeout;
            
            DebugLog($"Sending alternative save request to: {API_URL}/player/data");
            yield return request.SendWebRequest();
            
            DebugLog($"Alternative save response code: {request.responseCode}");
            DebugLog($"Alternative save response: {request.downloadHandler.text}");
            
            if (request.result == UnityWebRequest.Result.Success)
            {
                DebugLog("Player data saved successfully (alternative endpoint) with overwrite");
                
                // Verify the save was successful by checking response
                try
                {
                    var responseData = JsonConvert.DeserializeObject<PlayerDataModel>(request.downloadHandler.text);
                    if (responseData != null && responseData.id == PlayerData.id)
                    {
                        DebugLog("Alternative save verification successful - data properly overwritten on server");
                        onComplete?.Invoke(true, "Data saved and verified via alternative endpoint");
                    }
                    else
                    {
                        DebugLog("Alternative save response doesn't match sent data - possible server issue");
                        onComplete?.Invoke(true, "Data saved via alternative endpoint but verification failed");
                    }
                }
                catch (System.Exception ex)
                {
                    DebugLog($"Error parsing alternative save response: {ex.Message}");
                    onComplete?.Invoke(true, "Data saved via alternative endpoint but response parsing failed");
                }
            }
            else
            {
                string errorMsg = GetErrorMessage(request);
                DebugLog($"Alternative save endpoint also failed: {errorMsg}");
                onComplete?.Invoke(false, errorMsg);
            }
        }
    }
      // Test server connectivity without authentication
    public IEnumerator TestServerConnectivity(System.Action<bool, string> onComplete)
    {
        DebugLog($"Testing server connectivity to: {API_URL}");
        
        // Test the base URL first
        string testUrl = API_URL.Replace("/api", "");
        using (UnityWebRequest request = UnityWebRequest.Get(testUrl))
        {
            request.timeout = 10;
            yield return request.SendWebRequest();
            
            DebugLog($"Server test - URL: {testUrl}");
            DebugLog($"Server test - Response code: {request.responseCode}");
            DebugLog($"Server test - Error: {request.error}");
            DebugLog($"Server test - Response: {request.downloadHandler.text}");
            
            if (request.result == UnityWebRequest.Result.Success)
            {
                DebugLog("Server connectivity test successful");
                onComplete?.Invoke(true, "Server is reachable");
            }
            else
            {
                DebugLog($"Server connectivity test failed: {request.error}");
                onComplete?.Invoke(false, "Cannot reach server: " + request.error);
            }
        }
    }
    
    public IEnumerator TestAPIEndpoints(System.Action<bool, string> onComplete)
    {
        DebugLog("Testing API endpoints...");
        
        // Test health endpoint first
        using (UnityWebRequest request = UnityWebRequest.Get(API_URL.Replace("/api", "")))
        {
            request.timeout = 10;
            yield return request.SendWebRequest();
            
            if (request.result != UnityWebRequest.Result.Success)
            {
                DebugLog($"Server connectivity test failed: {request.error}");
                onComplete?.Invoke(false, "Cannot reach server: " + request.error);
                yield break;
            }
            
            DebugLog("Server is reachable");
        }
        
        // Test if user is logged in and can access player data
        if (!IsLoggedIn)
        {
            DebugLog("API test: Not logged in");
            onComplete?.Invoke(false, "Not logged in");
            yield break;
        }
        
        DebugLog($"Testing player data endpoint with token: {AuthToken?.Substring(0, Math.Min(10, AuthToken.Length))}...");
        
        // Test GET player data endpoint
        using (UnityWebRequest request = UnityWebRequest.Get($"{API_URL}/player/data"))
        {
            request.SetRequestHeader("x-auth-token", AuthToken);
            request.SetRequestHeader("Content-Type", "application/json");
            request.timeout = 10;
            
            yield return request.SendWebRequest();
            
            DebugLog($"Player data endpoint test - Response code: {request.responseCode}");
            DebugLog($"Player data endpoint test - Response: {request.downloadHandler.text}");
            
            if (request.result == UnityWebRequest.Result.Success)
            {
                DebugLog("Player data GET endpoint test successful");
                
                // Try to parse the response
                try
                {
                    var testData = JsonConvert.DeserializeObject<PlayerDataModel>(request.downloadHandler.text);
                    if (testData != null && !string.IsNullOrEmpty(testData.id) && !string.IsNullOrEmpty(testData.username))
                    {
                        DebugLog($"API test successful - Valid player data: {testData.username} (ID: {testData.id})");
                        onComplete?.Invoke(true, $"API working correctly. User: {testData.username}");
                    }
                    else
                    {
                        DebugLog($"API test warning - Invalid player data structure: ID='{testData?.id}', Username='{testData?.username}'");
                        onComplete?.Invoke(false, "Server returns invalid player data structure");
                    }
                }
                catch (Exception ex)
                {
                    DebugLog($"API test error - Failed to parse player data: {ex.Message}");
                    onComplete?.Invoke(false, "Server response parsing error: " + ex.Message);
                }
            }
            else
            {
                string errorMsg = $"Player data GET endpoint failed: {request.responseCode} - {request.error}";
                DebugLog(errorMsg);
                onComplete?.Invoke(false, errorMsg);
            }
        }
    }
    
    private IEnumerator TestSaveEndpoints(System.Action<bool, string> onComplete)
    {
        DebugLog("Testing save endpoints...");
        
        if (PlayerData == null)
        {
            onComplete?.Invoke(false, "No player data to test save with");
            yield break;
        }
        
        string testData = JsonConvert.SerializeObject(PlayerData);
        
        // Test primary save endpoint
        using (UnityWebRequest request = new UnityWebRequest($"{API_URL}/player/save", "PUT"))
        {
            byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(testData);
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");
            request.SetRequestHeader("x-auth-token", AuthToken);
            request.timeout = 10;
            
            yield return request.SendWebRequest();
            
            if (request.result == UnityWebRequest.Result.Success)
            {
                DebugLog("Primary save endpoint (/player/save) working");
                onComplete?.Invoke(true, "All endpoints working, primary save endpoint available");
                yield break;
            }
            else
            {
                DebugLog($"Primary save endpoint failed: {request.responseCode} - {request.error}");
            }
        }
        
        // Test alternative save endpoint
        using (UnityWebRequest request = new UnityWebRequest($"{API_URL}/player/data", "PUT"))
        {
            byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(testData);
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");
            request.SetRequestHeader("x-auth-token", AuthToken);
            request.timeout = 10;
            
            yield return request.SendWebRequest();
            
            if (request.result == UnityWebRequest.Result.Success)
            {
                DebugLog("Alternative save endpoint (/player/data) working");
                onComplete?.Invoke(true, "Endpoints working, using alternative save endpoint");
            }
            else
            {
                DebugLog($"Alternative save endpoint also failed: {request.responseCode} - {request.error}");
                onComplete?.Invoke(false, "Both save endpoints failed");
            }
        }
    }
      public void Logout()
    {
        DebugLog("Starting logout process...");
        // Đã xóa: Không lưu PlayerData vào PlayerPrefs khi logout để tránh restore lại khi new game
        // if (PlayerData != null && !string.IsNullOrEmpty(PlayerData.id))
        // {
        //     DebugLog($"Saving logout state for user: {PlayerData.username} (ID: {PlayerData.id})");
        //     PlayerPrefs.SetString("LastUserData", JsonConvert.SerializeObject(PlayerData));
        // }
        AuthToken = null;
        PlayerData = null;
        PlayerPrefs.DeleteKey("AuthToken");
        PlayerPrefs.DeleteKey("LastCheckpoint");
        PlayerPrefs.Save();
        // Clear data synchronizer
        if (GameDataSynchronizer.Instance != null)
        {
            GameDataSynchronizer.Instance.ClearData();
        }
    }
    
    // Method to restore AuthToken from PlayerPrefs if it gets lost
    public void RestoreAuthTokenFromPrefs()
    {
        if (string.IsNullOrEmpty(AuthToken))
        {
            string savedToken = PlayerPrefs.GetString("AuthToken", "");
            if (!string.IsNullOrEmpty(savedToken))
            {
                DebugLog($"Restoring AuthToken from PlayerPrefs: {savedToken.Substring(0, Math.Min(10, savedToken.Length))}...");
                AuthToken = savedToken;
                DebugLog("AuthToken restored successfully");
            }
            else
            {
                DebugLog("No saved AuthToken found in PlayerPrefs");
            }
        }
        else
        {
            DebugLog($"AuthToken already exists: {AuthToken.Substring(0, Math.Min(10, AuthToken.Length))}...");
        }
    }
    
    // Method to force reload player data if it becomes corrupted
    public void ForceReloadPlayerData(Action<bool, string> onComplete = null)
    {
        DebugLog("Force reloading player data...");
        
        if (!IsLoggedIn)
        {
            RestoreAuthTokenFromPrefs();
            if (!IsLoggedIn)
            {
                DebugLog("Cannot reload player data: Not logged in");
                onComplete?.Invoke(false, "Not logged in");
                return;
            }
        }
        
        StartCoroutine(GetPlayerData((success, error) => {
            if (success && PlayerData != null && !string.IsNullOrEmpty(PlayerData.id) && !string.IsNullOrEmpty(PlayerData.username))
            {
                DebugLog($"Player data force reload successful: {PlayerData.username} (ID: {PlayerData.id})");
                onComplete?.Invoke(true, "Player data reloaded successfully");
            }
            else
            {                DebugLog($"Player data force reload failed: {error}");
                onComplete?.Invoke(false, error ?? "Failed to reload player data");
            }
        }));
    }
    

    public IEnumerator ForceRefreshPlayerData(Action<bool, string> onComplete = null)
    {
        DebugLog("Force refreshing player data - clearing cache and loading fresh from server...");
        
        if (!IsLoggedIn)
        {
            RestoreAuthTokenFromPrefs();
            if (!IsLoggedIn)
            {
                DebugLog("Cannot refresh player data: Not logged in");
                onComplete?.Invoke(false, "Not logged in");
                yield break;
            }
        }
        
        // Clear any cached data first to ensure fresh load
        PlayerData = null;
        
        // Force reload fresh data from server
        yield return StartCoroutine(GetPlayerData((success, error) => {
            if (success && PlayerData != null && !string.IsNullOrEmpty(PlayerData.id) && !string.IsNullOrEmpty(PlayerData.username))
            {
                DebugLog($"Player data force refresh successful: {PlayerData.username} (ID: {PlayerData.id})");
                DebugLog("Fresh data confirmed loaded from server");
                onComplete?.Invoke(true, "Player data refreshed successfully");
            }
            else
            {
                DebugLog($"Player data force refresh failed: {error}");
                onComplete?.Invoke(false, error ?? "Failed to refresh player data");
            }
        }));
    }

    // Method to validate if the current token is working
    public IEnumerator ValidateCurrentToken(Action<bool, string> onComplete)
    {
        if (string.IsNullOrEmpty(AuthToken))
        {
            DebugLog("ValidateCurrentToken: No token to validate");
            onComplete?.Invoke(false, "No token available");
            yield break;
        }

        DebugLog($"Validating current token: {AuthToken.Substring(0, Math.Min(10, AuthToken.Length))}...");

        // Try the primary verification endpoint first
        using (UnityWebRequest request = UnityWebRequest.Get($"{API_URL}/auth/verify"))
        {
            request.SetRequestHeader("x-auth-token", AuthToken);
            request.timeout = (int)requestTimeout;

            yield return request.SendWebRequest();

            if (request.responseCode != 404) {
                DebugLog($"Token validation response code: {request.responseCode}");
            }
            
            if (request.result == UnityWebRequest.Result.Success)
            {
                DebugLog("Token validation successful via /auth/verify");
                
                // Try to update PlayerData if it's missing or incomplete
                if (PlayerData == null || string.IsNullOrEmpty(PlayerData.id))
                {
                    try
                    {
                        string responseText = request.downloadHandler.text;
                        if (!string.IsNullOrEmpty(responseText))
                        {
                            var updatedData = JsonConvert.DeserializeObject<PlayerDataModel>(responseText);
                            if (updatedData != null && !string.IsNullOrEmpty(updatedData.id))
                            {
                                PlayerData = updatedData;
                                DebugLog($"Updated PlayerData from verification: {PlayerData.username} (ID: {PlayerData.id})");
                                OnPlayerDataLoaded?.Invoke(PlayerData);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        DebugLog($"Could not parse player data from verification response: {ex.Message}");
                    }
                }
                
                onComplete?.Invoke(true, "Token is valid");
                yield break;
            }
            else if (request.responseCode == 404)
            {
                DebugLog("Auth verify endpoint not found (404), trying alternative method...");
                // Fallback to player data endpoint - DON'T CLEAR TOKEN YET
                yield return StartCoroutine(ValidateTokenAlternative(onComplete));
                yield break;
            }
            else
            {
                DebugLog($"Primary token validation failed: {request.responseCode} - {request.downloadHandler.text}");
                // Try alternative validation before giving up
                yield return StartCoroutine(ValidateTokenAlternative(onComplete));
            }
        }
    }private IEnumerator ValidateTokenAlternative(Action<bool, string> onComplete)
    {
        DebugLog("Trying alternative token validation via player data endpoint...");

        using (UnityWebRequest request = UnityWebRequest.Get($"{API_URL}/player/data"))
        {
            request.SetRequestHeader("x-auth-token", AuthToken);
            request.timeout = (int)requestTimeout;

            yield return request.SendWebRequest();

            DebugLog($"Alternative token validation response code: {request.responseCode}");

            if (request.result == UnityWebRequest.Result.Success)
            {
                DebugLog("Alternative token validation successful - token is still valid");
                
                // Try to load player data from this response if we don't have it
                if (PlayerData == null || string.IsNullOrEmpty(PlayerData.id))
                {
                    try
                    {
                        var playerDataFromResponse = JsonConvert.DeserializeObject<PlayerDataModel>(request.downloadHandler.text);
                        if (playerDataFromResponse != null && !string.IsNullOrEmpty(playerDataFromResponse.id))
                        {
                            PlayerData = playerDataFromResponse;
                            DebugLog($"Restored player data from validation: {PlayerData.username} (ID: {PlayerData.id})");
                            OnPlayerDataLoaded?.Invoke(PlayerData);
                        }
                    }
                    catch (Exception ex)
                    {
                        DebugLog($"Could not parse player data from validation response: {ex.Message}");
                    }
                }
                
                onComplete?.Invoke(true, "Token is valid (alternative check)");
            }
            else
            {
                string errorMsg = GetErrorMessage(request);
                DebugLog($"Alternative token validation also failed: {errorMsg}");
                  // Only clear token if it's definitely a 401 (Unauthorized) error
                // Don't clear for network errors, 404s, or server errors
                if (request.responseCode == 401)
                {
                    DebugLog("Token is definitely invalid (401 Unauthorized) - clearing");
                    AuthToken = null;
                    PlayerData = null;
                    PlayerPrefs.DeleteKey("AuthToken");
                    PlayerPrefs.Save();
                    onComplete?.Invoke(false, "Token is invalid");
                }
                else
                {
                    DebugLog($"Server error or connectivity issue (code: {request.responseCode}) - keeping token for retry");
                    onComplete?.Invoke(false, $"Server connectivity issue: {errorMsg}");
                }
            }
        }
    }

    private string GetErrorMessage(UnityWebRequest request)
    {
        try
        {
            if (!string.IsNullOrEmpty(request.downloadHandler.text))
            {
                var errorResponse = JsonConvert.DeserializeObject<ErrorResponse>(request.downloadHandler.text);
                return errorResponse.error ?? request.error;
            }
        }
        catch
        {
            // If JSON parsing fails, return the raw error
        }
        
        return request.error;
    }
    
    private void DebugLog(string message)
    {
        if (debugMode)
        {
            Debug.Log($"[GameAPI] {message}");
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
    
    // Thêm method debug vào GameAPI
    public void DebugTokenInfo()
    {
        if (!string.IsNullOrEmpty(AuthToken))
        {
            try
            {
                // Decode JWT token để xem user ID
                var parts = AuthToken.Split('.');
                if (parts.Length >= 2)
                {
                    var payload = parts[1];
                    // Add padding if needed
                    while (payload.Length % 4 != 0)
                        payload += "=";
                
                    var decodedBytes = System.Convert.FromBase64String(payload);
                    var decodedText = System.Text.Encoding.UTF8.GetString(decodedBytes);
                    DebugLog($"Token payload: {decodedText}");
                }
            }
            catch (Exception ex)
            {
                DebugLog($"Failed to decode token: {ex.Message}");
            }
        }
    }
}

// Data Models
[System.Serializable]
public class LoginResponse
{
    public string token;
    public PlayerDataModel user;
}

[System.Serializable]
public class ErrorResponse
{
    public string error;
}

[System.Serializable]
public class PlayerDataModel
{
    public string id;
    public string username;
    public string email;
    public int level = 1;
    public int experience = 0;
    public int money = 0;
    public float health = 100f;
    public int kills = 0;  // Add zombie kills tracking
    public CheckpointData checkpoint;
    public List<WeaponData> weapons = new List<WeaponData>();
    public string lastLoginDate;
    // Thêm trường để lưu trạng thái chìa khóa
    public bool hasKey = false;
}

[System.Serializable]
public class CheckpointData
{
    public string sceneId;
    public SerializableVector3 position; // Changed from Vector3 to SerializableVector3
    public string timestamp;
    public string additionalData;
}

[System.Serializable]
public class SerializableVector3
{
    public float x;
    public float y;
    public float z;
    
    public SerializableVector3() { }
    
    public SerializableVector3(Vector3 vector)
    {
        x = vector.x;
        y = vector.y;
        z = vector.z;
    }
    
    public Vector3 ToVector3()
    {
        return new Vector3(x, y, z);
    }
    
    public static implicit operator Vector3(SerializableVector3 serializable)
    {
        return serializable?.ToVector3() ?? Vector3.zero;
    }
    
    public static implicit operator SerializableVector3(Vector3 vector)
    {
        return new SerializableVector3(vector);
    }
}

[System.Serializable]
public class WeaponData
{
    public string id;
    public string name;
    public int damage;
    public int level;
    public bool isUnlocked;
    public int ammo;
}
