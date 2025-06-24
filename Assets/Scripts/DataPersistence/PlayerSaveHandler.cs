using System.Collections;
using UnityEngine;

public class PlayerSaveHandler : MonoBehaviour
{
    public GameObject playerPrefab;

    void Start()
    {
        StartCoroutine(SpawnPlayerWithDelay());
    }

    IEnumerator SpawnPlayerWithDelay()
    {
        yield return null;

        if (GameObject.FindGameObjectWithTag("Player") != null)
            yield break;

        Vector3 spawnPos = new Vector3(509.47f, 25.142f, 365.85f);

        if (DataPersistenceManager.instance != null)
        {
            GameData data = DataPersistenceManager.instance.GetData();
            if (data != null)
            {
                spawnPos = data.playerPosition;
            }
        }

        GameObject player = Instantiate(playerPrefab, spawnPos, Quaternion.identity);
        player.name = "Player";
        player.tag = "Player";

        CharacterController controller = player.GetComponent<CharacterController>();
        if (controller != null)
        {
            controller.enabled = false;
            player.transform.position = spawnPos;
            controller.enabled = true;
        }
        else
        {
            player.transform.position = spawnPos;
        }

        DontDestroyOnLoad(player); 
    }

}
