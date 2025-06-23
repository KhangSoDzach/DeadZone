using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class Zombie_4 : MonoBehaviour
{
    void Start()
    {
        ApplyDifficultyScaling();
    }
    private void ApplyDifficultyScaling()
    {
        float multiplier = 1f;

        if (DataPersistenceManager.instance != null && DataPersistenceManager.instance.GetData() != null)
        {
            multiplier = DataPersistenceManager.instance.GetData().difficultyMode;
        }

        attackDamage *= multiplier + 2;
        zombieHealth *= multiplier ;
        remainHeath = zombieHealth;


    }

    [Header("Zombie Things")]
    public LayerMask PlayerLayer;
    public NavMeshAgent zombieAgent;
    public Transform LookPoint;
    public Transform playerBody;
    public Camera AttackingRaycastArea;

    [Header("Zombie Standing")]
    public float zombieSpeed;

    [Header("Zombie Attacking Zone")]
    public float attackSpeed;
    public bool prevAttack;

    [Header("Zombie Animations")]
    public Animator aniZombie;

    [Header("Zombie States")]
    public float observationRadius;
    public float attackingRadius;
    public bool playerExistenceRadius;
    public bool playerInAttackingRadius;

    [Header("Zombie Health and Damage")]
    public float attackDamage = 3f;
    private float zombieHealth = 50f;
    private float remainHeath;

    [Header("Zombie Sounds")]
    public AudioSource audioSource;
    public AudioClip idleGroanSound;

    private float nextSoundTime = 0f;

    [Header("Money Drop Settings")]
    public GameObject moneyPrefab;          // Prefab đồng tiền
    public float dropChance = 0.8f;         // Tỷ lệ rơi tiền (0-1)
    public int minCoinsDropped = 3;         // Số lượng đồng tiền tối thiểu
    public int maxCoinsDropped = 5;         // Số lượng đồng tiền tối đa

    // Update is called once per frame
    void Update()
    {
        if (playerBody == null || LookPoint == null)
        {
            TryAssignPlayerReferences();
            return;
        }
        playerExistenceRadius = Physics.CheckSphere(transform.position, observationRadius, PlayerLayer);
        playerInAttackingRadius = Physics.CheckSphere(transform.position, attackingRadius, PlayerLayer);
        if (playerExistenceRadius && !playerInAttackingRadius) ChasingPlayer();
        if (playerExistenceRadius && playerInAttackingRadius) AttackPlayer();

        if (playerExistenceRadius && !playerInAttackingRadius)
        {
            if (Time.time >= nextSoundTime)
            {
                if (!audioSource.isPlaying)
                {
                    audioSource.PlayOneShot(idleGroanSound);
                }

                nextSoundTime = Time.time + 25f;
            }
        }
        if (!playerExistenceRadius && !playerInAttackingRadius)
        {
            Idle();
        }
    }
    private void Awake()
    {
        remainHeath = zombieHealth;
        zombieAgent = GetComponent<NavMeshAgent>();

    }
    private void Idle()
    {
        zombieAgent.SetDestination(transform.position);
        aniZombie.SetBool("isIdle", true);
        aniZombie.SetBool("isRunning", false);
    }
    private void ChasingPlayer()
    {
        if (zombieAgent.SetDestination(playerBody.position))
        {
            if (DataPersistenceManager.instance != null && DataPersistenceManager.instance.GetData() != null)
            {
                float multiplier = DataPersistenceManager.instance.GetData().difficultyMode;
                zombieAgent.speed = multiplier * 6;

            }
            aniZombie.SetBool("isIdle", false);
            aniZombie.SetBool("isRunning", true);
            aniZombie.SetBool("isAttacking", false);
        }

    }
    private void TryAssignPlayerReferences()
    {
        if (playerBody == null)
        {
            GameObject playerObj = GameObject.FindWithTag("Player");
            if (playerObj != null)
            {
                playerBody = playerObj.transform;
            }
        }

        if (LookPoint == null && playerBody != null)
        {
            Transform lookTarget = playerBody.Find("LookPoint");
            if (lookTarget != null)
            {
                LookPoint = lookTarget;
            }
            else
            {
                LookPoint = playerBody;
            }
        }
    }
    private void AttackPlayer()
    {
        zombieAgent.SetDestination(transform.position);
        transform.LookAt(LookPoint);
        if (!prevAttack)
        {
            ApplyZombieDamage();
            aniZombie.SetBool("isRunning", false);
            aniZombie.SetBool("isAttacking", true);


            Invoke(nameof(ApplyZombieDamage), 0.3f);
            prevAttack = true;
            Invoke(nameof(ActiveAttacking), attackSpeed);
        }
    }
    private void ApplyZombieDamage()
    {
        RaycastHit hit;
        if (Physics.Raycast(AttackingRaycastArea.transform.position, AttackingRaycastArea.transform.forward, out hit, attackingRadius))
        {
            // Check if we hit the player
            HealthManager playerHealth = hit.transform.GetComponent<HealthManager>();
            if (playerHealth != null)
            {
                // Apply damage to the player
                playerHealth.TakeDamage(attackDamage);
                Debug.Log("Player hit for " + attackDamage + " damage");
            }
        }
    }
    private void ActiveAttacking()
    {
        prevAttack = false;
    }
    public void zombieGotHit(float takeDamge)
    {
        observationRadius = 30f;
        remainHeath -= takeDamge;
        if (remainHeath <= 0)
        {
            zombieDie();

            aniZombie.SetBool("isDead", true);
        }
    }

    private void zombieDie()
    {
        transform.LookAt(LookPoint);
        zombieAgent.SetDestination(transform.position);
        zombieSpeed = 0;
        attackingRadius = 0;
        observationRadius = 0;
        playerInAttackingRadius = false;
        playerExistenceRadius = false;

        aniZombie.SetBool("isDead", true);
        Object.Destroy(gameObject, 5.0f);
        
        // Kiểm tra tỷ lệ rơi tiền
        if (Random.value <= dropChance)
        {
            // Xác định số lượng đồng tiền rơi ra
            int coinCount = Random.Range(minCoinsDropped, maxCoinsDropped + 1);
            
            for (int i = 0; i < coinCount; i++)
            {
                // Tạo vị trí rơi ngẫu nhiên xung quanh zombie
                Vector3 randomOffset = new Vector3(
                    Random.Range(-0.5f, 0.5f),
                    0.1f,  // Đặt cao hơn một chút so với mặt đất
                    Random.Range(-0.5f, 0.5f)
                );
                
                // Tạo đồng tiền
                if (moneyPrefab != null)
                {
                    Instantiate(moneyPrefab, transform.position + randomOffset, Quaternion.Euler(0, Random.Range(0, 360), 0));
                }
                else
                {
                    Debug.LogWarning("Money prefab not assigned to zombie!");
                }
            }
        }
    }
}
