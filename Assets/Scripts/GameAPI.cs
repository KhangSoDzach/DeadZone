using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;

namespace DevionGames
{
    [Serializable]
    public class AuthData
    {
        public string token;
    }

    [Serializable]
    public class LoginRequest
    {
        public string username;
        public string password;
    }

    [Serializable]
    public class RegisterRequest
    {
        public string username;
        public string email;
        public string password;
    }

    [Serializable]
    public class Position
    {
        public float x;
        public float y;
        public float z;
    }

    [Serializable]
    public class Checkpoint
    {
        public string sceneId;
        public Position position;
        public string timestamp;
    }

    [Serializable]
    public class Weapon
    {
        public string id;
        public string name;
        public float damage;
        public int ammo;
        public int level;
        public bool isUnlocked;
    }

    [Serializable]
    public class Ammunition
    {
        public int pistol;
        public int rifle;
    }

    [Serializable]
    public class PlayerDataModel
    {
        public string userId;
        public int money;
        public float health;
        public Ammunition ammunition;
        public List<Weapon> weapons;
        public string currentWeapon;
        public Checkpoint checkpoint;
        public int kills;
        public int level;
        public string lastSaved;
    }

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

        private const string API_URL = "http://localhost:5000/api";
        private string _token;
        private PlayerDataModel _playerData;

        public bool IsLoggedIn => !string.IsNullOrEmpty(_token);
        public PlayerDataModel PlayerData => _playerData;

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

            // Try to load token from PlayerPrefs
            _token = PlayerPrefs.GetString("AuthToken", null);
        }

        public IEnumerator Login(string username, string password, Action<bool, string> callback)
        {
            LoginRequest request = new LoginRequest
            {
                username = username,
                password = password
            };

            string json = JsonUtility.ToJson(request);
            byte[] bodyRaw = Encoding.UTF8.GetBytes(json);

            using (UnityWebRequest www = new UnityWebRequest($"{API_URL}/auth/login", "POST"))
            {
                www.uploadHandler = new UploadHandlerRaw(bodyRaw);
                www.downloadHandler = new DownloadHandlerBuffer();
                www.SetRequestHeader("Content-Type", "application/json");

                yield return www.SendWebRequest();

                if (www.result != UnityWebRequest.Result.Success)
                {
                    Debug.LogError($"Login Error: {www.error}");
                    callback(false, www.error);
                    yield break;
                }

                AuthData authData = JsonUtility.FromJson<AuthData>(www.downloadHandler.text);
                _token = authData.token;

                // Save token
                PlayerPrefs.SetString("AuthToken", _token);
                PlayerPrefs.Save();

                // After successful login, get player data
                yield return StartCoroutine(GetPlayerData((success, errorMsg) => {
                    callback(success, errorMsg);
                }));
            }
        }

        public IEnumerator Register(string username, string email, string password, Action<bool, string> callback)
        {
            RegisterRequest request = new RegisterRequest
            {
                username = username,
                email = email,
                password = password
            };

            string json = JsonUtility.ToJson(request);
            byte[] bodyRaw = Encoding.UTF8.GetBytes(json);

            using (UnityWebRequest www = new UnityWebRequest($"{API_URL}/auth/register", "POST"))
            {
                www.uploadHandler = new UploadHandlerRaw(bodyRaw);
                www.downloadHandler = new DownloadHandlerBuffer();
                www.SetRequestHeader("Content-Type", "application/json");

                yield return www.SendWebRequest();

                if (www.result != UnityWebRequest.Result.Success)
                {
                    Debug.LogError($"Register Error: {www.error}");
                    callback(false, www.error);
                    yield break;
                }

                AuthData authData = JsonUtility.FromJson<AuthData>(www.downloadHandler.text);
                _token = authData.token;

                // Save token
                PlayerPrefs.SetString("AuthToken", _token);
                PlayerPrefs.Save();

                // After successful registration, get player data
                yield return StartCoroutine(GetPlayerData((success, errorMsg) => {
                    callback(success, errorMsg);
                }));
            }
        }

        public void Logout()
        {
            _token = null;
            _playerData = null;
            PlayerPrefs.DeleteKey("AuthToken");
            PlayerPrefs.Save();
            
            // Load login scene or main menu
            SceneManager.LoadScene("MainMenu");
        }

        public IEnumerator GetPlayerData(Action<bool, string> callback)
        {
            if (string.IsNullOrEmpty(_token))
            {
                callback(false, "Not logged in");
                yield break;
            }

            using (UnityWebRequest www = UnityWebRequest.Get($"{API_URL}/player/data"))
            {
                www.SetRequestHeader("x-auth-token", _token);

                yield return www.SendWebRequest();

                if (www.result != UnityWebRequest.Result.Success)
                {
                    Debug.LogError($"Get Player Data Error: {www.error}");
                    
                    // If unauthorized, clear token
                    if (www.responseCode == 401)
                    {
                        _token = null;
                        PlayerPrefs.DeleteKey("AuthToken");
                        PlayerPrefs.Save();
                    }
                    
                    callback(false, www.error);
                    yield break;
                }

                _playerData = JsonUtility.FromJson<PlayerDataModel>(www.downloadHandler.text);
                callback(true, null);
            }
        }        // Update player data model before saving
        public void UpdatePlayerDataModel(PlayerDataModel newData)
        {
            if (newData != null)
            {
                _playerData = newData;
            }
        }
        
        public IEnumerator SavePlayerData(Action<bool, string> callback)
        {
            if (string.IsNullOrEmpty(_token) || _playerData == null)
            {
                callback(false, "Not logged in or player data not loaded");
                yield break;
            }

            string json = JsonUtility.ToJson(_playerData);
            byte[] bodyRaw = Encoding.UTF8.GetBytes(json);

            using (UnityWebRequest www = UnityWebRequest.Put($"{API_URL}/player/save", bodyRaw))
            {
                www.SetRequestHeader("x-auth-token", _token);
                www.SetRequestHeader("Content-Type", "application/json");

                yield return www.SendWebRequest();

                if (www.result != UnityWebRequest.Result.Success)
                {
                    Debug.LogError($"Save Player Data Error: {www.error}");
                    callback(false, www.error);
                    yield break;
                }

                _playerData = JsonUtility.FromJson<PlayerDataModel>(www.downloadHandler.text);
                callback(true, null);
            }
        }

        public IEnumerator UpdateMoney(int money, Action<bool, string> callback)
        {
            if (string.IsNullOrEmpty(_token) || _playerData == null)
            {
                callback(false, "Not logged in or player data not loaded");
                yield break;
            }

            string json = $"{{\"money\": {money}}}";
            byte[] bodyRaw = Encoding.UTF8.GetBytes(json);

            using (UnityWebRequest www = UnityWebRequest.Put($"{API_URL}/player/money", bodyRaw))
            {
                www.SetRequestHeader("x-auth-token", _token);
                www.SetRequestHeader("Content-Type", "application/json");

                yield return www.SendWebRequest();

                if (www.result != UnityWebRequest.Result.Success)
                {
                    Debug.LogError($"Update Money Error: {www.error}");
                    callback(false, www.error);
                    yield break;
                }

                _playerData = JsonUtility.FromJson<PlayerDataModel>(www.downloadHandler.text);
                callback(true, null);
            }
        }

        public IEnumerator UpdateCheckpoint(string sceneId, Vector3 position, Action<bool, string> callback)
        {
            if (string.IsNullOrEmpty(_token) || _playerData == null)
            {
                callback(false, "Not logged in or player data not loaded");
                yield break;
            }

            Checkpoint checkpoint = new Checkpoint
            {
                sceneId = sceneId,
                position = new Position
                {
                    x = position.x,
                    y = position.y,
                    z = position.z
                }
            };

            string json = JsonUtility.ToJson(new { checkpoint });
            byte[] bodyRaw = Encoding.UTF8.GetBytes(json);

            using (UnityWebRequest www = UnityWebRequest.Put($"{API_URL}/player/checkpoint", bodyRaw))
            {
                www.SetRequestHeader("x-auth-token", _token);
                www.SetRequestHeader("Content-Type", "application/json");

                yield return www.SendWebRequest();

                if (www.result != UnityWebRequest.Result.Success)
                {
                    Debug.LogError($"Update Checkpoint Error: {www.error}");
                    callback(false, www.error);
                    yield break;
                }

                _playerData = JsonUtility.FromJson<PlayerDataModel>(www.downloadHandler.text);
                callback(true, null);
            }
        }

        public IEnumerator UpdateWeapons(List<Weapon> weapons, string currentWeapon, Action<bool, string> callback)
        {
            if (string.IsNullOrEmpty(_token) || _playerData == null)
            {
                callback(false, "Not logged in or player data not loaded");
                yield break;
            }

            string json = JsonUtility.ToJson(new { weapons, currentWeapon });
            byte[] bodyRaw = Encoding.UTF8.GetBytes(json);

            using (UnityWebRequest www = UnityWebRequest.Put($"{API_URL}/player/weapons", bodyRaw))
            {
                www.SetRequestHeader("x-auth-token", _token);
                www.SetRequestHeader("Content-Type", "application/json");

                yield return www.SendWebRequest();

                if (www.result != UnityWebRequest.Result.Success)
                {
                    Debug.LogError($"Update Weapons Error: {www.error}");
                    callback(false, www.error);
                    yield break;
                }

                _playerData = JsonUtility.FromJson<PlayerDataModel>(www.downloadHandler.text);
                callback(true, null);
            }
        }

        public IEnumerator UpdateAmmunition(int pistolAmmo, int rifleAmmo, Action<bool, string> callback)
        {
            if (string.IsNullOrEmpty(_token) || _playerData == null)
            {
                callback(false, "Not logged in or player data not loaded");
                yield break;
            }

            Ammunition ammunition = new Ammunition
            {
                pistol = pistolAmmo,
                rifle = rifleAmmo
            };

            string json = JsonUtility.ToJson(new { ammunition });
            byte[] bodyRaw = Encoding.UTF8.GetBytes(json);

            using (UnityWebRequest www = UnityWebRequest.Put($"{API_URL}/player/ammunition", bodyRaw))
            {
                www.SetRequestHeader("x-auth-token", _token);
                www.SetRequestHeader("Content-Type", "application/json");

                yield return www.SendWebRequest();

                if (www.result != UnityWebRequest.Result.Success)
                {
                    Debug.LogError($"Update Ammunition Error: {www.error}");
                    callback(false, www.error);
                    yield break;
                }

                _playerData = JsonUtility.FromJson<PlayerDataModel>(www.downloadHandler.text);
                callback(true, null);
            }
        }
    }
}
