using System;
using System.Collections.Generic;
using UnityEngine;


    [Serializable]
    public class UserDataModel
    {
        // Thông tin cơ bản
        public string userId;
        public string username;
        public string email;
        public string displayName;
        public string avatarUrl;
        public string role; // Loại tài khoản: normal, admin, vip...
        
        // Trạng thái tài khoản
        public bool isActive = true;
        public DateTime registrationDate;
        public DateTime lastLoginDate;
        
        // Thông tin trò chơi
        public int level = 1;
        public float experience = 0;
        public int money = 0;
        public int gems = 0;
        public int score = 0;
        
        // Tiến độ chơi
        public int highestLevelUnlocked = 1;
        public List<string> completedLevels = new List<string>();
        public string currentCheckpoint;
        
        // Thông tin vũ khí
        public List<string> unlockedWeapons = new List<string>();
        public string equippedWeapon;
        public Dictionary<string, int> weaponLevels = new Dictionary<string, int>();
        
        // Thông tin nhân vật
        public float health = 100;
        public int maxHealth = 100;
        public float moveSpeed = 5.0f;
        public float jumpHeight = 1.0f;
        
        // Thời gian chơi
        public float totalPlayTime = 0;
        public float lastSessionTime = 0;
        
        // Thành tựu và nhiệm vụ
        public List<string> achievements = new List<string>();
        public List<string> completedQuests = new List<string>();
        public List<string> activeQuests = new List<string>();
        
        // Thông tin kết nối và đồng bộ
        public DateTime lastSync;
        public string clientVersion;
        
        /// <summary>
        /// Sao chép dữ liệu từ UserDataModel khác
        /// </summary>
        public void CopyFrom(UserDataModel other)
        {
            if (other == null) return;
            
            // Sao chép các thuộc tính cơ bản
            this.userId = other.userId;
            this.username = other.username;
            this.email = other.email;
            this.displayName = other.displayName;
            this.avatarUrl = other.avatarUrl;
            this.role = other.role;
            
            // Sao chép trạng thái tài khoản
            this.isActive = other.isActive;
            this.registrationDate = other.registrationDate;
            this.lastLoginDate = other.lastLoginDate;
            
            // Sao chép thông tin trò chơi
            this.level = other.level;
            this.experience = other.experience;
            this.money = other.money;
            this.gems = other.gems;
            this.score = other.score;
            
            // Sao chép tiến độ chơi
            this.highestLevelUnlocked = other.highestLevelUnlocked;
            this.completedLevels = new List<string>(other.completedLevels);
            this.currentCheckpoint = other.currentCheckpoint;
            
            // Sao chép thông tin vũ khí
            this.unlockedWeapons = new List<string>(other.unlockedWeapons);
            this.equippedWeapon = other.equippedWeapon;
            
            // Sao chép thông tin nhân vật
            this.health = other.health;
            this.maxHealth = other.maxHealth;
            this.moveSpeed = other.moveSpeed;
            this.jumpHeight = other.jumpHeight;
            
            // Sao chép thời gian chơi
            this.totalPlayTime = other.totalPlayTime;
            this.lastSessionTime = other.lastSessionTime;
            
            // Sao chép thành tựu và nhiệm vụ
            this.achievements = new List<string>(other.achievements);
            this.completedQuests = new List<string>(other.completedQuests);
            this.activeQuests = new List<string>(other.activeQuests);
            
            // Sao chép thông tin đồng bộ
            this.lastSync = other.lastSync;
            this.clientVersion = other.clientVersion;
        }
        
        /// <summary>
        /// Chuyển đổi thành chuỗi JSON
        /// </summary>
        public string ToJson()
        {
            return JsonUtility.ToJson(this);
        }
        
        /// <summary>
        /// Tạo từ chuỗi JSON
        /// </summary>
        public static UserDataModel FromJson(string json)
        {
            return JsonUtility.FromJson<UserDataModel>(json);
        }
    }

