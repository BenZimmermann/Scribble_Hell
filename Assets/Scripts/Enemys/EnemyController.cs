using FishNet.Object;
using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class EnemyController : NetworkBehaviour
{
    [Header("Enemy Configuration")]
    [SerializeField] private EnemyData enemyData;

    [Header("Components")]
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private Rigidbody2D rb;

    private float lastDamageTime = -999f;
    private int currentHealth;
    private Vector2 currentDirection;
    //private Vector3 patrolCenter;
    //private Vector3 targetPatrolPoint;
    //private float nextWanderTime;
    private bool isWaiting;

    public void SetEnemyData(EnemyData data)
    {
        enemyData = data;

        if (IsServerStarted)
        {
            InitializeEnemy();
        }
    }
    private void Awake()
    {
        if (rb == null) rb = GetComponent<Rigidbody2D>();
        if (spriteRenderer == null) spriteRenderer = GetComponent<SpriteRenderer>();
    }

    public override void OnStartServer()
    {
        base.OnStartServer();
        InitializeEnemy();
    }

    public override void OnStartClient()
    {
        base.OnStartClient();
        ApplyVisuals();
    }

    private void InitializeEnemy()
    {
        currentHealth = enemyData.maxHealth;
        //patrolCenter = transform.position;
        //SetNewPatrolPoint();
        //nextWanderTime = Time.time + enemyData.wanderChangeInterval;
        currentDirection = Random.insideUnitCircle.normalized;
    }

    private void ApplyVisuals()
    {
        if (enemyData == null || spriteRenderer == null) return;

        if (enemyData.enemySprite != null)
            spriteRenderer.sprite = enemyData.enemySprite;

        spriteRenderer.color = enemyData.enemyColor;
    }

    private void FixedUpdate()
    {
        if (!IsServerStarted) return;
        if (enemyData == null) return;

        HandleMovement();
    }

    private void HandleMovement()
    {
        switch (enemyData.movementType)
        {
            case EnemyMovementType.Stationary:
                rb.linearVelocity = Vector2.zero;
                break;

            case EnemyMovementType.Chase:
                ChaseNearestPlayer();
                break;

            //case EnemyMovementType.Patrol:
            //    PatrolMovement();
            //    break;

            //case EnemyMovementType.Wander:
            //    WanderMovement();
            //    break;
        }
    }

    #region Movement Types

    private void ChaseNearestPlayer()
    {
        var players = FindObjectsByType<PlayerMovement>(FindObjectsSortMode.None);
        Transform nearestPlayer = null;
        float nearestDistance = float.MaxValue;

        foreach (var player in players)
        {
            float distance = Vector2.Distance(transform.position, player.transform.position);
            if (distance < nearestDistance && distance <= enemyData.chaseRange)
            {
                nearestDistance = distance;
                nearestPlayer = player.transform;
            }
        }

        if (nearestPlayer != null)
        {
            // Stoppe bei bestimmter Distanz wenn aktiviert
            if (enemyData.stopAtDistance && nearestDistance <= enemyData.stopDistance)
            {
                rb.linearVelocity = Vector2.zero;
                return;
            }

            Vector2 direction = (nearestPlayer.position - transform.position).normalized;
            rb.linearVelocity = direction * enemyData.moveSpeed;

            // Rotation zum Spieler
            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            transform.rotation = Quaternion.Lerp(
                transform.rotation,
                Quaternion.Euler(0, 0, angle - 90),
                enemyData.rotationSpeed * Time.fixedDeltaTime
            );
        }
        else
        {
            rb.linearVelocity = Vector2.zero;
        }
    }

    //private void PatrolMovement()
    //{
    //    if (isWaiting) return;

    //    Vector2 direction = (targetPatrolPoint - transform.position).normalized;
    //    float distance = Vector2.Distance(transform.position, targetPatrolPoint);

    //    if (distance < 0.5f)
    //    {
    //        StartCoroutine(WaitAtPatrolPoint());
    //    }
    //    else
    //    {
    //        rb.linearVelocity = direction * enemyData.moveSpeed;
    //    }
    //}

    //private IEnumerator WaitAtPatrolPoint()
    //{
    //    isWaiting = true;
    //    rb.linearVelocity = Vector2.zero;

    //    yield return new WaitForSeconds(enemyData.waitTimeAtPoint);

    //    SetNewPatrolPoint();
    //    isWaiting = false;
    //}

    //private void SetNewPatrolPoint()
    //{
    //    Vector2 randomPoint = Random.insideUnitCircle * enemyData.patrolRadius;
    //    targetPatrolPoint = patrolCenter + new Vector3(randomPoint.x, randomPoint.y, 0);
    //}

    //private void WanderMovement()
    //{
    //    if (Time.time >= nextWanderTime)
    //    {
    //        currentDirection = Random.insideUnitCircle.normalized;
    //        nextWanderTime = Time.time + enemyData.wanderChangeInterval;
    //    }

    //    // Bleibe im Wander Radius
    //    float distanceFromCenter = Vector2.Distance(transform.position, patrolCenter);
    //    if (distanceFromCenter > enemyData.wanderRadius)
    //    {
    //        currentDirection = (patrolCenter - transform.position).normalized;
    //    }

    //    rb.linearVelocity = currentDirection * enemyData.moveSpeed;
    //}

    #endregion

    #region Damage System

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (Time.time - lastDamageTime < enemyData.damageCooldown)
            return;

        PlayerMovement player = collision.gameObject.GetComponent<PlayerMovement>();

        if (player != null && player.IsOwner)
        {
            lastDamageTime = Time.time;
            player.TakeDamageServerRpc();

            if (IsServerStarted && enemyData.flashOnDamage)
            {
                FlashDamageClientRpc();
            }
        }
        if (collision.gameObject.GetComponent<PlayerMovement>())
            { 
            Die();
            }
    }

    [ObserversRpc]
    public void FlashDamageClientRpc()
    {
        if (spriteRenderer != null)
        {
            StartCoroutine(DamageFlashCoroutine());
        }
    }

    private IEnumerator DamageFlashCoroutine()
    {
        Color originalColor = spriteRenderer.color;
        spriteRenderer.color = enemyData.damageFlashColor;
        Debug.Log("Enemy flash color applied.");
        yield return new WaitForSeconds(enemyData.flashDuration);

        spriteRenderer.color = originalColor;
    }

    #endregion

    #region Health System

    [Server]
    public void TakeDamage(int damage)
    {
        if (!enemyData.hasHealth) return;

        currentHealth -= damage;

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    [Server]
    private void Die()
    {
        if (WaveManager.Instance != null)
        {
            WaveManager.Instance.OnEnemyKilled();
        }
        ServerManager.Despawn(gameObject);
    }

    #endregion

    // Debug Visualisierung
    private void OnDrawGizmosSelected()
    {
        if (enemyData == null) return;

        Gizmos.color = Color.yellow;

        switch (enemyData.movementType)
        {
            case EnemyMovementType.Chase:
                Gizmos.DrawWireSphere(transform.position, enemyData.chaseRange);
                if (enemyData.stopAtDistance)
                {
                    Gizmos.color = Color.red;
                    Gizmos.DrawWireSphere(transform.position, enemyData.stopDistance);
                }
                break;

            //case EnemyMovementType.Patrol:
            //    Vector3 center = Application.isPlaying ? patrolCenter : transform.position;
            //    Gizmos.DrawWireSphere(center, enemyData.patrolRadius);
            //    break;

            //case EnemyMovementType.Wander:
            //    Vector3 wanderCenter = Application.isPlaying ? patrolCenter : transform.position;
            //    Gizmos.DrawWireSphere(wanderCenter, enemyData.wanderRadius);
            //    break;
        }
    }
}