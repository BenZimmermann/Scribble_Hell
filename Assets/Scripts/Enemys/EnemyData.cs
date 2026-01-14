using UnityEngine;

[CreateAssetMenu(fileName = "New Enemy Data", menuName = "Game/Enemy Data")]
public class EnemyData : ScriptableObject
{
    [Header("Basic Info")]
    public string enemyName = "Enemy";
    public Sprite enemySprite;
    public Color enemyColor = Color.white;

    [Header("Movement")]
    public EnemyMovementType movementType = EnemyMovementType.Chase;
    public float moveSpeed = 3f;
    public float rotationSpeed = 5f;

    [Header("Chase Behavior")]
    public float chaseRange = 10f;
    public bool stopAtDistance = false;
    public float stopDistance = 2f;

    //[Header("Patrol Behavior")]
    //[Tooltip("Nur für Patrol Movement")]
    //public float patrolRadius = 5f;
    //public float waitTimeAtPoint = 2f;

    //[Header("Wander Behavior")]
    //[Tooltip("Nur für Wander Movement")]
    //public float wanderRadius = 8f;
    //public float wanderChangeInterval = 3f;

    [Header("Damage")]
    public int damageAmount = 1;
    public float damageCooldown = 1f;

    [Header("Health")]
    public bool hasHealth = false;
    public int maxHealth = 3;

    [Header("Visual Effects")]
    public bool flashOnDamage = true;
    public Color damageFlashColor = Color.red;
    public float flashDuration = 0.2f;

    [Header("Audio Clips")]
    public AudioClip spawnSound;
    public AudioClip damageSound;
    public AudioClip deathSound;
}

public enum EnemyMovementType
{
    Stationary,  // Bewegt sich nicht
    Chase,       
}