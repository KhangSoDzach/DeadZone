using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ObjectiveManager : MonoBehaviour
{
    public TextMeshProUGUI objectiveText;
    public static ObjectiveManager Instance;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
    }

    void Start()
    {
        UpdateObjective("Find key in villa");
    }

    public void UpdateObjective(string message)
    {
        if (objectiveText != null)
        {
            objectiveText.text = "Objective: " + message;
        }
    }
}
