using UnityEngine;

public static class PlayerPrefsUtility
{
    public static void SavePlayerPosition(Vector3 pos)
    {
        PlayerPrefs.SetFloat("PlayerX", pos.x);
        PlayerPrefs.SetFloat("PlayerY", pos.y);
        PlayerPrefs.SetFloat("PlayerZ", pos.z);
        PlayerPrefs.Save();
    }

    public static Vector3 LoadPlayerPosition(Vector3 defaultPosition)
    {
        if (PlayerPrefs.HasKey("PlayerX") && PlayerPrefs.HasKey("PlayerY") && PlayerPrefs.HasKey("PlayerZ"))
        {
            float x = PlayerPrefs.GetFloat("PlayerX");
            float y = PlayerPrefs.GetFloat("PlayerY");
            float z = PlayerPrefs.GetFloat("PlayerZ");
            return new Vector3(x, y, z);
        }
        else
        {
            return defaultPosition;
        }
    }

    public static void ClearPlayerPosition()
    {
        PlayerPrefs.DeleteKey("PlayerX");
        PlayerPrefs.DeleteKey("PlayerY");
        PlayerPrefs.DeleteKey("PlayerZ");
    }
}
