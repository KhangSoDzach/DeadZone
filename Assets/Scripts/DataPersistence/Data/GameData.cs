using UnityEngine;
using System;
using Scripts.API; 

[Serializable]
public class GameData
{
    public float difficultyMode;
    public float playerHealth;
    public float playerStamina;
    public int ammo;
    public int money;
    public int totalAmmo ;
    public int playerScore;

    public SerializableVector3 playerPosition;
    public bool hasKey = false;
    public string currentObjectiveText = "Find key in Clinic Villa";
    public GameData()
    {
        playerHealth = 100f;
        playerStamina = 100f; 
        ammo = 30;
        totalAmmo = 30;
        money = 0;
        playerScore = 0;
        difficultyMode = 1.2f;
        currentObjectiveText = "Find key in Clinic Villa";

    playerPosition = new Vector3(509.47f, 25.142f, 365.85f);
    }
}
