using UnityEngine;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using UnityEngine.SceneManagement;

public class DataPersistenceManager : MonoBehaviour
{
    public static DataPersistenceManager instance { get; private set; }
    public GameData gameData;
    private string savePath;

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }
        instance = this;
        DontDestroyOnLoad(gameObject);

        savePath = Application.persistentDataPath + "/save.dat";
    }

    public void NewGame()
    {
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            Destroy(playerObj);
        }

        gameData = new GameData();

        gameData.playerPosition = new Vector3(509.47f, 21.142f, 365.85f);

        SaveGame();

        SceneManager.LoadScene("Cutscene");
    }

    public void LoadGame()
    {
        
        if (File.Exists(savePath))
        {
            BinaryFormatter formatter = new BinaryFormatter();
            using (FileStream stream = new FileStream(savePath, FileMode.Open))
            {
                gameData = formatter.Deserialize(stream) as GameData;
            }
        }

        SceneManager.sceneLoaded += OnSceneLoaded;

        SceneManager.LoadScene("Scene_A");
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null && gameData != null)
        {
            CharacterController controller = playerObj.GetComponent<CharacterController>();
            if (controller != null) controller.enabled = false;

            playerObj.transform.position = gameData.playerPosition;

            if (controller != null) controller.enabled = true;
        }

        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    public void SaveGame()
    {
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            UpdatePlayerTransform(playerObj.transform.position);

            HealthManager health = playerObj.GetComponent<HealthManager>();
            if (health != null && gameData != null)
            {
                gameData.playerHealth = health.currentHealth;
                gameData.playerStamina = health.currentStamina;
            }
        }
        

        if (ScoreManager.Instance != null && gameData != null)
        {
            gameData.playerScore = ScoreManager.Instance.currentScore;
        }

        BinaryFormatter formatter = new BinaryFormatter();
        using (FileStream stream = new FileStream(savePath, FileMode.Create))
        {
            formatter.Serialize(stream, gameData);
        }

        Debug.Log("Pooooooooo"+playerObj.transform.position);
    }


    public bool HasSavedGame()
    {
        return File.Exists(savePath);
    }

    private void OnApplicationQuit()
    {
        SaveGame();
    }

    public GameData GetData() => gameData;

    public void UpdatePlayerTransform(Vector3 pos)
    {
        if (gameData == null) return;
        gameData.playerPosition = pos;
    }
}
