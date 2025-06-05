using UnityEngine;

public static class UserDataBridge
{
    /// <summary>
    /// Convert PlayerDataModel to UserDataModel
    /// </summary>
    public static UserDataModel ToUserDataModel(PlayerDataModel playerData)
    {
        if (playerData == null) return null;
        
        return new UserDataModel
        {
            userId = playerData.id,
            username = playerData.username,
            email = playerData.email,
            level = playerData.level,
            experience = playerData.experience,
            money = playerData.money,
            health = playerData.health,
            lastLoginDate = System.DateTime.TryParse(playerData.lastLoginDate, out var date) ? date : System.DateTime.Now
        };
    }
    
    /// <summary>
    /// Convert UserDataModel to PlayerDataModel
    /// </summary>
    public static PlayerDataModel ToPlayerDataModel(UserDataModel userData)
    {
        if (userData == null) return null;
        
        return new PlayerDataModel
        {
            id = userData.userId,
            username = userData.username,
            email = userData.email,
            level = userData.level,
            experience = (int)userData.experience,
            money = userData.money,
            health = userData.health,
            lastLoginDate = userData.lastLoginDate.ToString("yyyy-MM-dd HH:mm:ss")
        };
    }
}