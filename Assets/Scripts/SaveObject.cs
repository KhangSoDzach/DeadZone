using UnityEngine;
using UnityEngine.SceneManagement;

public class SaveObject : MonoBehaviour
{
    private static SaveObject instance;

    void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
        }
        }

}
