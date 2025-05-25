using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class Zombie_1 : MonoBehaviour
{   

    // Start is called before the first frame update
    void Start()
    {
        
    }

    [Header("Zombie Things")]
    public LayerMask PlayerLayer;
    public NavMeshAgent zombieAgent;
    public Transform LookPoint;
    public Transform playerBody;
    public Camera AttackingRaycastArea;

    [Header("Zombie Behavior")]
    public GameObject[] guardingPoints;
    public int currentPosition = 0;
    public float zombieSpeed;
    public float walkingRadius = 2;

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
    public float attackDamage = 20f;
    private float zombieHealth = 75f;
    private float remainHeath;

    [Header("Zombie Sounds")]
    public AudioSource audioSource;
    public AudioClip idleGroanSound;

    [Header("Money Drop Settings")]
    public GameObject moneyPrefab;          // Prefab đồng tiền
    public float dropChance = 0.7f;         // Tỷ lệ rơi tiền (0-1)
    public int minCoinsDropped = 1;         // Số lượng đồng tiền tối thiểu
    public int maxCoinsDropped = 3;         // Số lượng đồng tiền tối đa

    private float nextSoundTime = 0f;

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
        if (!playerExistenceRadius && !playerInAttackingRadius) Guard();
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

                nextSoundTime = Time.time + 30f;
            }
        }

    }
    private void Awake()
    {
        remainHeath = zombieHealth;
        zombieAgent = GetComponent<NavMeshAgent>();

    }
    private void Guard()
    {
        if(guardingPoints.Length > 0)
        {
            aniZombie.SetBool("isWalking", true);
            aniZombie.SetBool("isIdle", false);
            if (Vector3.Distance(guardingPoints[currentPosition].transform.position, transform.position) < walkingRadius)
            {
                currentPosition = Random.Range(0, guardingPoints.Length);
                if (currentPosition >= guardingPoints.Length)
                {
                    currentPosition = 0;
                }
            }
            transform.position = Vector3.MoveTowards(transform.position, guardingPoints[currentPosition].transform.position, Time.deltaTime * zombieSpeed);

            //change zombie facing
            transform.LookAt(guardingPoints[currentPosition].transform.position);
        }
        else
        {
            aniZombie.SetBool("isWalking", false);
            aniZombie.SetBool("isIdle", true);
        }
    }
    private void ChasingPlayer()
    {
       observationRadius = 30f;
       if (zombieAgent.SetDestination(playerBody.position))
        {
            zombieAgent.speed = 4;
            aniZombie.SetBool("isIdle", false);
            aniZombie.SetBool("isWalking", false);
            aniZombie.SetBool("isRunning", true);
            aniZombie.SetBool("isAttacking", false);
            aniZombie.SetBool("isDead", false);

        }
        else
        {
            aniZombie.SetBool("isIdle", false);
            aniZombie.SetBool("isWalking", false);
            aniZombie.SetBool("isRunning", false);
            aniZombie.SetBool("isAttacking", false);
            aniZombie.SetBool("isDead", true);
        }
    }
    private void AttackPlayer()
    {
        zombieAgent.SetDestination(transform.position);
        transform.LookAt(LookPoint);

        if(!prevAttack)
        {
            //Code the player take damage from player

            aniZombie.SetBool("isAttacking", true);
            aniZombie.SetBool("isWalking", false);
            aniZombie.SetBool("isRunning", false);
            aniZombie.SetBool("isDead", false);
            aniZombie.SetTrigger("isAttack");
            zombieAgent.isStopped = true;

            Invoke(nameof(EndReaction), 2.4f);

            Invoke(nameof(ApplyZombieDamage), 1.5f);


            prevAttack = true;
            Invoke(nameof(ActiveAttacking), attackSpeed);
        }
    }
    public void ApplyZombieDamage()
    {
        RaycastHit hit;
        if (Physics.Raycast(AttackingRaycastArea.transform.position, AttackingRaycastArea.transform.forward, out hit, attackingRadius))
        {
            Debug.Log("attac k");
            HealthManager playerHealth = hit.transform.GetComponent<HealthManager>();
            if (playerHealth != null)
            {
                // Apply damage to the player
                playerHealth.TakeDamage(attackDamage);
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

            aniZombie.SetBool("isWalking", false);
            aniZombie.SetBool("isRunning", false);
            aniZombie.SetBool("isAttacking", false);
            aniZombie.SetBool("isDead", true);
        }
        if (remainHeath == 40|| remainHeath == 60 || remainHeath == 80)
        {
            //Reaction hit
            aniZombie.SetTrigger("isHit");
            zombieAgent.isStopped = true;

            Invoke(nameof(EndReaction), 1f);
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
    private void EndReaction()
    {
        zombieAgent.isStopped = false;
    }
    private void zombieDie()
    {
        transform.LookAt(LookPoint);
        zombieAgent.SetDestination(transform.position);
        zombieSpeed = 0;
        attackingRadius = 0;
        observationRadius = 0;
        playerInAttackingRadius=false;
        playerExistenceRadius=false;

        aniZombie.SetBool("isWalking", false);
        aniZombie.SetBool("isRunning", false);
        aniZombie.SetBool("isAttacking", false);
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
