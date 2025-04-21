using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class WeaponPickup : MonoBehaviour
{
    public int weaponIndex;     // Index in the weaponPrefabs array
    public string weaponName;   // Name of the weapon
    public int remainingAmmo;   // Store the remaining ammo when dropped
    public bool isPistol;       // Is this a pistol (primary weapon that can't be dropped)
    
    // Thêm các thuộc tính mới để lưu thông tin chi tiết về vũ khí
    public bool isAutomatic;    // Lưu loại súng (tự động/bán tự động)
    public float damage;        // Lưu sát thương
    public float recoilAmount;  // Lưu lượng giật
    public float baseSpread;    // Lưu độ chính xác cơ bản
    public GameObject impactEffect; // Lưu prefab hiệu ứng va chạm
    
    // Tham chiếu đến các components
    [HideInInspector] public RuntimeAnimatorController animatorController;
    [HideInInspector] public AudioClip gunshotClip;
    [HideInInspector] public float gunVolume;
    
    // Lưu thông tin UI references để khôi phục
    [HideInInspector] public string ammoTextPath;
    [HideInInspector] public string scoreTextPath; 
    
    
    // Thêm tham chiếu đến vũ khí gameplay prefab gốc
    [HideInInspector] public GameObject originalWeaponPrefab;
    [HideInInspector] public int originalWeaponIndex = -1;
    
    void Start()
    {
        // Add rotation effect to make the weapon more noticeable
        StartCoroutine(RotateWeapon());
    }
    
    // Simple rotation effect for dropped weapons
    IEnumerator RotateWeapon()
    {
        while (true)
        {
            transform.Rotate(new Vector3(0, 1, 0), 50 * Time.deltaTime);
            yield return null;
        }
    }
    
    // Phương thức mới để sao chép thuộc tính từ vũ khí gốc
    public void CopyPropertiesFromGun(Gun sourceGun)
    {
        if (sourceGun == null) return;
        
        this.remainingAmmo = sourceGun.currentAmmo;
        this.isPistol = sourceGun.isPistol;
        this.isAutomatic = sourceGun.isAutomatic;
        this.damage = sourceGun.damage;
        this.recoilAmount = sourceGun.recoilAmount;
        this.baseSpread = sourceGun.baseSpread;
        this.impactEffect = sourceGun.impactEffect;
        
        // Lưu thông tin animator nếu có
        if (sourceGun.animator != null && sourceGun.animator.runtimeAnimatorController != null)
        {
            this.animatorController = sourceGun.animator.runtimeAnimatorController;
        }
        
        // Sao chép cài đặt âm thanh
        if (sourceGun.gunshotSound != null)
        {
            this.gunshotClip = sourceGun.gunshotSound.clip;
            this.gunVolume = sourceGun.gunshotSound.volume;
        }
        
        // Lưu đường dẫn đến các text UI để có thể tìm lại sau
        if (sourceGun.ammoText != null)
        {
            this.ammoTextPath = GetGameObjectPath(sourceGun.ammoText.gameObject);
        }
        
        // Save the scoreText path from the ScoreManager instead
        Text scoreText = ScoreManager.GetScoreText();
        if (scoreText != null)
        {
            this.scoreTextPath = GetGameObjectPath(scoreText.gameObject);
        }
    }
    
    // Helper method to get the full path of a GameObject in the hierarchy
    private string GetGameObjectPath(GameObject obj)
    {
        if (obj == null) return "";
        
        string path = obj.name;
        Transform parent = obj.transform.parent;
        
        while (parent != null)
        {
            path = parent.name + "/" + path;
            parent = parent.parent;
        }
        
        return path;
    }
}