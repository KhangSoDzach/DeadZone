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

        LoadObjectiveFromSave();
    }
    public void LoadObjectiveFromSave()
    {
        if (DataPersistenceManager.instance != null)
        {
            string savedText = DataPersistenceManager.instance.GetData().currentObjectiveText;
            if (!string.IsNullOrEmpty(savedText))
            {
                UpdateObjectiveFromSave(savedText);
            }
        }
    }


    public void UpdateObjectiveFromSave(string message)
    {
        if (!string.IsNullOrEmpty(message))
        {
            objectiveText.text ="Objective: "+ message;
        }
    }

}
