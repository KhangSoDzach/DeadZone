using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Boss : MonoBehaviour
{
    void Start()
    {
        BossHealthUI.Instance.ShowUI(bossName);
        BossHealthUI.Instance.UpdateHealth(remainHeath, zombieHealth);

        remainHeath = zombieHealth;
        zombieAgent = GetComponent<UnityEngine.AI.NavMeshAgent>();

        TryAssignPlayerReferences();
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
        zombieHealth *= multiplier + 10;
        remainHeath = zombieHealth;
        jumpAttackDamage*= multiplier;

    }

    [Header("Zombie Things")]
    public LayerMask PlayerLayer;
    public UnityEngine.AI.NavMeshAgent zombieAgent;
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
    public float attackDamage = 15f;
    private float zombieHealth = 1000f;
    private float remainHeath;

    [Header("Zombie Sounds")]
    public AudioSource audioSource;
    public AudioClip idleGroanSound;

    [Header("Heavy Attack Settings")]
    private float heavyAttackTimer = 0;
    public float heavyAttackDamage = 20f;

    [Header("Jump Attack Settings")]
    private float jumpAttackTimer = 0;
    public float jumpAttackDamage = 30f;

    private float nextSoundTime = 0f;


    public string bossName = "The Abyss"; void Update()
    {
        if (playerBody == null || LookPoint == null)
        {
            TryAssignPlayerReferences();
            return;
        }
        playerExistenceRadius = Physics.CheckSphere(transform.position, observationRadius, PlayerLayer);
        playerInAttackingRadius = Physics.CheckSphere(transform.position, attackingRadius, PlayerLayer);
        if (!playerExistenceRadius && !playerInAttackingRadius) Guard();
        if (playerExistenceRadius && !playerInAttackingRadius)
        {
            ChasingPlayer();
        } 

        if (playerExistenceRadius && !playerInAttackingRadius)
        {
            if (Time.time >= nextSoundTime)
            {
                if (!audioSource.isPlaying)
                {
                    audioSource.PlayOneShot(idleGroanSound);
                }

                nextSoundTime = Time.time + 20f;
            }
        }
        if (playerExistenceRadius && !playerInAttackingRadius)
        {
            jumpAttackTimer += Time.deltaTime;

            if (jumpAttackTimer >= 15f)
            {
                JumpAttacking();
            }
        }
        if (playerExistenceRadius && playerInAttackingRadius)
        {
            jumpAttackTimer += Time.deltaTime;
            heavyAttackTimer += Time.deltaTime;
            if (heavyAttackTimer >= 5f)
            {
                HeavyAttacking();
            }
            
            else
            {
                AttackPlayer();

            }
        }

    }
    private void Awake()
    {
        remainHeath = zombieHealth;
        zombieAgent = GetComponent<UnityEngine.AI.NavMeshAgent>();

    }
    private void Guard()
    {
        if (guardingPoints.Length > 0)
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
            zombieAgent.speed = 2;
            aniZombie.SetBool("isIdle", false);
            aniZombie.SetBool("isWalking", false);
            aniZombie.SetBool("isRunning", true);
            aniZombie.SetBool("isAttacking", false);
            aniZombie.SetBool("isDead", false);
            aniZombie.SetBool("isHeavyAttack", false);
            aniZombie.SetBool("isJumpAttack", false);



        }
        else
        {
            aniZombie.SetBool("isIdle", false);
            aniZombie.SetBool("isWalking", false);
            aniZombie.SetBool("isRunning", false);
            aniZombie.SetBool("isJumpAttack", false);
            aniZombie.SetBool("isAttacking", false);
            aniZombie.SetBool("isDead", true);
        }
    }
    private void AttackPlayer()
    {
        zombieAgent.SetDestination(transform.position);
        transform.LookAt(LookPoint);

        if (!prevAttack)
        {

            aniZombie.SetBool("isAttacking", true);
            aniZombie.SetBool("isWalking", false);
            aniZombie.SetBool("isRunning", false);
            aniZombie.SetBool("isDead", false);
            zombieAgent.isStopped = true;

            Invoke(nameof(EndReaction), 1f);

            Invoke(nameof(ApplyZombieDamage), 0.5f);


            prevAttack = true;
            Invoke(nameof(ActiveAttacking), attackSpeed);
        }
    }
    public void ApplyZombieDamage()
    {
        RaycastHit hit;
        if (Physics.Raycast(AttackingRaycastArea.transform.position, AttackingRaycastArea.transform.forward, out hit, attackingRadius))
        {
            HealthManager playerHealth = hit.transform.GetComponent<HealthManager>();
            if (playerHealth != null)
            {
                playerHealth.TakeDamage(attackDamage);
            }
        }

    }

    private void HeavyAttacking()
    {
        zombieAgent.SetDestination(transform.position);
        transform.LookAt(LookPoint);

        if (!prevAttack)
        {

            aniZombie.SetBool("isHeavyAttack", true);
            aniZombie.SetBool("isWalking", false);
            aniZombie.SetBool("isAttacking", false);
            aniZombie.SetBool("isRunning", false);
            aniZombie.SetBool("isDead", false);
            zombieAgent.isStopped = true;

            Invoke(nameof(EndReaction), 1.5f);

            Invoke(nameof(ApplyHeavyDamage), 1.8f);


            prevAttack = true;
            Invoke(nameof(ActiveAttacking), attackSpeed);
        }




    }
    private Vector3 jumpTargetPos;

    private void JumpAttacking()
    {
        zombieAgent.SetDestination(transform.position);
        transform.LookAt(LookPoint);

        aniZombie.SetBool("isHeavyAttack", false);
        aniZombie.SetBool("isJumpAttack", true);
        aniZombie.SetBool("isWalking", false);
        aniZombie.SetBool("isAttacking", false);
        aniZombie.SetBool("isRunning", false);
        aniZombie.SetBool("isDead", false);

        zombieAgent.isStopped = true;

        Invoke(nameof(EndReaction), 4f);

        jumpTargetPos = playerBody.position;

        StartCoroutine(JumpToTarget(jumpTargetPos, 1.5f));

       

    }
    private IEnumerator JumpToTarget(Vector3 targetPosition, float duration)
    {
        float elapsed = 0f;
        Vector3 startPos = transform.position;

        while (elapsed < duration)
        {
            float t = elapsed / duration;
            float height = 2f * Mathf.Sin(Mathf.PI * t); 
            Vector3 midPos = Vector3.Lerp(startPos, targetPosition, t);
            transform.position = new Vector3(midPos.x, midPos.y + height, midPos.z);
            elapsed += Time.deltaTime;
            yield return null;
        }

        transform.position = targetPosition;

        ApplyJumpAttackDamage();

    }

    void ApplyHeavyDamage()
    {
        heavyAttackTimer = 0;
        RaycastHit hit;
        if (Physics.Raycast(AttackingRaycastArea.transform.position, AttackingRaycastArea.transform.forward, out hit, attackingRadius))
        {
            HealthManager playerHealth = hit.transform.GetComponent<HealthManager>();
            if (playerHealth != null)
            {
                playerHealth.TakeDamage(heavyAttackDamage);
                Debug.Log("Heavy attack hit player!");
            }
        }
    }
    public void ApplyJumpAttackDamage()
    {
        jumpAttackTimer = 0;

        Collider[] hits = Physics.OverlapSphere(transform.position, 2f, PlayerLayer); 
        foreach (Collider col in hits)
        {
            HealthManager playerHealth = col.GetComponent<HealthManager>();
            if (playerHealth != null)
            {
                playerHealth.TakeDamage(jumpAttackDamage);
                Debug.Log("Jump attack hit player!");
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
        BossHealthUI.Instance.UpdateHealth(remainHeath, zombieHealth);

        if (remainHeath <= 0)
        {
            zombieDie();

            aniZombie.SetBool("isWalking", false);
            aniZombie.SetBool("isRunning", false);
            aniZombie.SetBool("isAttacking", false);
            aniZombie.SetBool("isDead", true);
        }
        if (remainHeath == 100 || remainHeath == 200 || remainHeath == 400 || remainHeath == 600 || remainHeath == 800)
        {
            aniZombie.SetTrigger("isHit");
            zombieAgent.isStopped = true;

            Invoke(nameof(EndReaction), 1.4f);
        }



    }

    private void EndReaction()
    {
        zombieAgent.isStopped = false;
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

    private void zombieDie()
    {
        transform.LookAt(LookPoint);
        zombieAgent.SetDestination(transform.position);
        zombieSpeed = 0;
        attackingRadius = 0;
        observationRadius = 0;
        playerInAttackingRadius = false;
        playerExistenceRadius = false;

        aniZombie.SetBool("isWalking", false);
        aniZombie.SetBool("isRunning", false);
        aniZombie.SetBool("isAttacking", false);
        aniZombie.SetBool("isDead", true);
        Object.Destroy(gameObject, 5.0f);
        BossHealthUI.Instance.HideUI();
        audioSource.PlayOneShot(idleGroanSound);
    }
}
