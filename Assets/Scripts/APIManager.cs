using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;
using Newtonsoft.Json;
using UnityEngine.UI;
using TMPro;
using System;

public class APIManager : MonoBehaviour
{
    private static APIManager _instance;
    public static APIManager Instance
    {
        get
        {
            if (_instance == null)
            {
                GameObject go = new GameObject("APIManager");
                _instance = go.AddComponent<APIManager>();
                DontDestroyOnLoad(_instance.gameObject);
            }
            return _instance;
        }
    }

    // API Server URL
    private string baseUrl = "http://localhost:5000/api";
    
    // Token for authentication
    private string authToken;
    
    // Player data
    private PlayerData playerData;
    
    // Auth status
    private bool isLoggedIn = false;

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
        }
        
        // Try to load saved token
        authToken = PlayerPrefs.GetString("AuthToken", "");
        if (!string.IsNullOrEmpty(authToken))
        {
            StartCoroutine(ValidateToken());
        }
    }

    // Register a new user
    public void Register(string username, string email, string password, System.Action<bool, string> callback)
    {
        StartCoroutine(RegisterCoroutine(username, email, password, callback));
    }

    private IEnumerator RegisterCoroutine(string username, string email, string password, System.Action<bool, string> callback)
    {
        // Create the request data
        Dictionary<string, string> formData = new Dictionary<string, string>
        {
            { "username", username },
            { "email", email },
            { "password", password }
        };

        string jsonData = JsonConvert.SerializeObject(formData);
        byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonData);

        // Create request
        using (UnityWebRequest request = new UnityWebRequest(baseUrl + "/auth/register", "POST"))
        {
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");

            // Send the request
            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                // Parse response
                var response = JsonConvert.DeserializeObject<Dictionary<string, string>>(request.downloadHandler.text);
                
                if (response != null && response.ContainsKey("token"))
                {
                    authToken = response["token"];
                    PlayerPrefs.SetString("AuthToken", authToken);
                    isLoggedIn = true;
                      // Get player data
                    StartCoroutine(FetchPlayerData());
                    
                    callback(true, "Registration successful!");
                }
                else
                {
                    callback(false, "Registration failed: Invalid response");
                }
            }
            else
            {
                string errorMsg = "Registration failed: " + request.error;
                
                // Try to get more detailed error from response
                if (!string.IsNullOrEmpty(request.downloadHandler.text))
                {
                    try
                    {
                        var errorResponse = JsonConvert.DeserializeObject<Dictionary<string, string>>(request.downloadHandler.text);
                        if (errorResponse != null && errorResponse.ContainsKey("msg"))
                        {
                            errorMsg = errorResponse["msg"];
                        }
                    }
                    catch { /* Use default error message */ }
                }
                
                callback(false, errorMsg);
            }
        }
    }

    // Login with username and password
    public void Login(string username, string password, System.Action<bool, string> callback)
    {
        StartCoroutine(LoginCoroutine(username, password, callback));
    }

    private IEnumerator LoginCoroutine(string username, string password, System.Action<bool, string> callback)
    {
        // Create the request data
        Dictionary<string, string> formData = new Dictionary<string, string>
        {
            { "username", username },
            { "password", password }
        };

        string jsonData = JsonConvert.SerializeObject(formData);
        byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonData);

        // Create request
        using (UnityWebRequest request = new UnityWebRequest(baseUrl + "/auth/login", "POST"))
        {
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");

            // Send the request
            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                // Parse response
                var response = JsonConvert.DeserializeObject<Dictionary<string, string>>(request.downloadHandler.text);
                
                if (response != null && response.ContainsKey("token"))
                {
                    authToken = response["token"];
                    PlayerPrefs.SetString("AuthToken", authToken);
                    isLoggedIn = true;
                    
                    // Get player data                    StartCoroutine(FetchPlayerData());
                    
                    callback(true, "Login successful!");
                }
                else
                {
                    callback(false, "Login failed: Invalid response");
                }
            }
            else
            {
                string errorMsg = "Login failed: " + request.error;
                
                // Try to get more detailed error from response
                if (!string.IsNullOrEmpty(request.downloadHandler.text))
                {
                    try
                    {
                        var errorResponse = JsonConvert.DeserializeObject<Dictionary<string, string>>(request.downloadHandler.text);
                        if (errorResponse != null && errorResponse.ContainsKey("msg"))
                        {
                            errorMsg = errorResponse["msg"];
                        }
                    }
                    catch { /* Use default error message */ }
                }
                
                callback(false, errorMsg);
            }
        }
    }

    // Validate saved token
    private IEnumerator ValidateToken()
    {
        using (UnityWebRequest request = UnityWebRequest.Get(baseUrl + "/auth/user"))
        {
            request.SetRequestHeader("x-auth-token", authToken);
            
            yield return request.SendWebRequest();
            
            if (request.result == UnityWebRequest.Result.Success)
            {
                isLoggedIn = true;
                Debug.Log("Token is valid, user is authenticated");
                  // Get player data
                StartCoroutine(FetchPlayerData());
            }
            else
            {
                isLoggedIn = false;
                authToken = "";
                PlayerPrefs.DeleteKey("AuthToken");
                Debug.Log("Token validation failed: " + request.error);
            }
        }
    }

    // Logout current user
    public void Logout()
    {
        authToken = "";
        isLoggedIn = false;
        playerData = null;
        PlayerPrefs.DeleteKey("AuthToken");
        
        Debug.Log("User logged out");
    }

    // Check if user is logged in
    public bool IsLoggedIn()
    {
        return isLoggedIn;
    }    // Get player data from server
    private IEnumerator FetchPlayerData()
    {
        if (!isLoggedIn || string.IsNullOrEmpty(authToken))
        {
            Debug.LogError("Cannot get player data: Not authenticated");
            yield break;
        }

        using (UnityWebRequest request = UnityWebRequest.Get(baseUrl + "/player/data"))
        {
            request.SetRequestHeader("x-auth-token", authToken);
            
            yield return request.SendWebRequest();
            
            if (request.result == UnityWebRequest.Result.Success)
            {
                // Parse player data
                playerData = JsonConvert.DeserializeObject<PlayerData>(request.downloadHandler.text);
                
                Debug.Log("Player data loaded successfully");
                
                // Notify any listeners that player data has been updated
                OnPlayerDataLoaded?.Invoke(playerData);
            }
            else
            {
                Debug.LogError("Failed to get player data: " + request.error);
            }
        }
    }

    // Save player data to server
    public void SavePlayerData(PlayerData data, System.Action<bool, string> callback = null)
    {
        if (!isLoggedIn || string.IsNullOrEmpty(authToken))
        {
            callback?.Invoke(false, "Not authenticated");
            return;
        }

        StartCoroutine(SavePlayerDataCoroutine(data, callback));
    }

    private IEnumerator SavePlayerDataCoroutine(PlayerData data, System.Action<bool, string> callback)
    {
        string jsonData = JsonConvert.SerializeObject(data);
        byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonData);

        using (UnityWebRequest request = new UnityWebRequest(baseUrl + "/player/save", "PUT"))
        {
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");
            request.SetRequestHeader("x-auth-token", authToken);
            
            yield return request.SendWebRequest();
            
            if (request.result == UnityWebRequest.Result.Success)
            {
                // Update local player data
                playerData = JsonConvert.DeserializeObject<PlayerData>(request.downloadHandler.text);
                Debug.Log("Player data saved successfully");
                
                // Notify any listeners that player data has been updated
                OnPlayerDataLoaded?.Invoke(playerData);
                
                callback?.Invoke(true, "Data saved successfully");
            }
            else
            {
                string errorMsg = "Failed to save player data: " + request.error;
                Debug.LogError(errorMsg);
                callback?.Invoke(false, errorMsg);
            }
        }
    }

    // Save player checkpoint
    public void SaveCheckpoint(string sceneId, Vector3 position, System.Action<bool, string> callback = null)
    {
        if (!isLoggedIn || string.IsNullOrEmpty(authToken))
        {
            callback?.Invoke(false, "Not authenticated");
            return;
        }

        if (playerData == null)
        {
            callback?.Invoke(false, "Player data not loaded");
            return;
        }

        // Create checkpoint data
        var checkpoint = new Checkpoint
        {
            sceneId = sceneId,
            position = new Position
            {
                x = position.x,
                y = position.y,
                z = position.z
            }
        };

        StartCoroutine(SaveCheckpointCoroutine(checkpoint, callback));
    }

    private IEnumerator SaveCheckpointCoroutine(Checkpoint checkpoint, System.Action<bool, string> callback)
    {
        Dictionary<string, Checkpoint> data = new Dictionary<string, Checkpoint>
        {
            { "checkpoint", checkpoint }
        };

        string jsonData = JsonConvert.SerializeObject(data);
        byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonData);

        using (UnityWebRequest request = new UnityWebRequest(baseUrl + "/player/checkpoint", "PUT"))
        {
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");
            request.SetRequestHeader("x-auth-token", authToken);
            
            yield return request.SendWebRequest();
            
            if (request.result == UnityWebRequest.Result.Success)
            {
                // Update local player data
                playerData = JsonConvert.DeserializeObject<PlayerData>(request.downloadHandler.text);
                Debug.Log("Checkpoint saved successfully");
                
                callback?.Invoke(true, "Checkpoint saved successfully");
            }
            else
            {
                string errorMsg = "Failed to save checkpoint: " + request.error;
                Debug.LogError(errorMsg);
                callback?.Invoke(false, errorMsg);
            }
        }
    }

    // Update player money
    public void UpdateMoney(int money, System.Action<bool, string> callback = null)
    {
        if (!isLoggedIn || string.IsNullOrEmpty(authToken))
        {
            callback?.Invoke(false, "Not authenticated");
            return;
        }

        Dictionary<string, int> data = new Dictionary<string, int>
        {
            { "money", money }
        };

        StartCoroutine(UpdateMoneyCoroutine(data, callback));
    }

    private IEnumerator UpdateMoneyCoroutine(Dictionary<string, int> data, System.Action<bool, string> callback)
    {
        string jsonData = JsonConvert.SerializeObject(data);
        byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonData);

        using (UnityWebRequest request = new UnityWebRequest(baseUrl + "/player/money", "PUT"))
        {
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");
            request.SetRequestHeader("x-auth-token", authToken);
            
            yield return request.SendWebRequest();
            
            if (request.result == UnityWebRequest.Result.Success)
            {
                // Update local player data
                playerData = JsonConvert.DeserializeObject<PlayerData>(request.downloadHandler.text);
                Debug.Log("Money updated successfully");
                
                callback?.Invoke(true, "Money updated successfully");
            }
            else
            {
                string errorMsg = "Failed to update money: " + request.error;
                Debug.LogError(errorMsg);
                callback?.Invoke(false, errorMsg);
            }
        }
    }

    // Update weapons
    public void UpdateWeapons(List<Weapon> weapons, string currentWeapon, System.Action<bool, string> callback = null)
    {
        if (!isLoggedIn || string.IsNullOrEmpty(authToken))
        {
            callback?.Invoke(false, "Not authenticated");
            return;
        }

        Dictionary<string, object> data = new Dictionary<string, object>
        {
            { "weapons", weapons },
            { "currentWeapon", currentWeapon }
        };

        StartCoroutine(UpdateWeaponsCoroutine(data, callback));
    }

    private IEnumerator UpdateWeaponsCoroutine(Dictionary<string, object> data, System.Action<bool, string> callback)
    {
        string jsonData = JsonConvert.SerializeObject(data);
        byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonData);

        using (UnityWebRequest request = new UnityWebRequest(baseUrl + "/player/weapons", "PUT"))
        {
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");
            request.SetRequestHeader("x-auth-token", authToken);
            
            yield return request.SendWebRequest();
            
            if (request.result == UnityWebRequest.Result.Success)
            {
                // Update local player data
                playerData = JsonConvert.DeserializeObject<PlayerData>(request.downloadHandler.text);
                Debug.Log("Weapons updated successfully");
                
                callback?.Invoke(true, "Weapons updated successfully");
            }
            else
            {
                string errorMsg = "Failed to update weapons: " + request.error;
                Debug.LogError(errorMsg);
                callback?.Invoke(false, errorMsg);
            }
        }
    }    // Get current player data
    public PlayerData GetLocalPlayerData()
    {
        return playerData;
    }
    
    // Reload player data from server
    public void ReloadPlayerData(System.Action<bool> callback = null)
    {
        StartCoroutine(ReloadPlayerDataCoroutine(callback));
    }
    
    private IEnumerator ReloadPlayerDataCoroutine(System.Action<bool> callback)
    {
        if (!isLoggedIn || string.IsNullOrEmpty(authToken))
        {
            Debug.LogError("Cannot reload player data: Not authenticated");
            callback?.Invoke(false);
            yield break;
        }
        
        using (UnityWebRequest request = UnityWebRequest.Get(baseUrl + "/player/data"))
        {
            request.SetRequestHeader("x-auth-token", authToken);
            
            yield return request.SendWebRequest();
            
            if (request.result == UnityWebRequest.Result.Success)
            {
                // Parse player data
                playerData = JsonConvert.DeserializeObject<PlayerData>(request.downloadHandler.text);
                Debug.Log("Player data reloaded successfully");
                
                // Notify any listeners that player data has been updated
                OnPlayerDataLoaded?.Invoke(playerData);
                
                callback?.Invoke(true);
            }
            else
            {
                Debug.LogError("Failed to reload player data: " + request.error);
                callback?.Invoke(false);
            }
        }
    }
    
    // Event for player data loaded
    public delegate void PlayerDataLoadedHandler(PlayerData data);
    public event PlayerDataLoadedHandler OnPlayerDataLoaded;
}

// Class to match the server's PlayerData model
[System.Serializable]
public class PlayerData
{
    public string userId;
    public int money;
    public int health;
    public Ammunition ammunition;
    public List<Weapon> weapons;
    public string currentWeapon;
    public Checkpoint checkpoint;
    public int kills;
    public int level;
    public string lastSaved;
}

[System.Serializable]
public class Ammunition
{
    public int pistol;
    public int rifle;
}

[System.Serializable]
public class Weapon
{
    public string id;
    public string name;
    public int damage;
    public int ammo;
    public int level;
    public bool isUnlocked;
}

[System.Serializable]
public class Checkpoint
{
    public string sceneId;
    public Position position;
    public string timestamp;
}

[System.Serializable]
public class Position
{
    public float x;
    public float y;
    public float z;
}
