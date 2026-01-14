using UnityEngine;

[CreateAssetMenu(fileName = "New Bullet Data", menuName = "Game/Bullet Data")]
public class BulletData : ScriptableObject
{
    [Header("Basic Info")]
    public string bulletName = "Bullet";
    public Sprite bulletSprite;
    public Color bulletColor = Color.white;
    public Vector2 bulletSize = new Vector2(0.2f, 0.2f);

    [Header("Movement")]
    public float bulletSpeed = 10f;
    public float lifeTime = 5f;

    [Header("Damage")]
    public int damageToEnemies = 1;
    public bool destroyOnHit = true;
    public bool piercing = false;
    [Tooltip("Wie viele Enemies kann die Bullet durchdringen? (nur wenn piercing aktiv)")]
    public int maxPierceCount = 1;

    [Header("Shoot Pattern")]
    public ShootPattern shootPattern = ShootPattern.Single;

    [Header("Single Shot Settings")]
    public float fireRate = 0.5f; // Sekunden zwischen Schüssen

    [Header("Burst Shot Settings")]
    [Tooltip("Anzahl Bullets pro Burst")]
    public int burstCount = 3;
    [Tooltip("Verzögerung zwischen Bullets im Burst")]
    public float burstDelay = 0.1f;
    [Tooltip("Verzögerung zwischen Bursts")]
    public float burstCooldown = 1f;

    [Header("Spread Shot Settings")]
    [Tooltip("Anzahl Bullets gleichzeitig")]
    public int spreadCount = 3;
    [Tooltip("Winkel zwischen Bullets in Grad")]
    public float spreadAngle = 15f;
    public float spreadFireRate = 0.7f;

    [Header("Rapid Fire Settings")]
    public float rapidFireRate = 0.1f;

    [Header("Visual Effects")]
    public bool rotateWithDirection = true;
    public bool hasTrail = false;
    public Color trailColor = Color.white;
    public float trailTime = 0.5f;

    [Header("Audio (Optional)")]
    public AudioClip shootSound;
    public float soundVolume = 1f;
}

public enum ShootPattern
{
    Single,      // Ein Schuss in Bewegungsrichtung
    Burst,       // Mehrere Schüsse kurz hintereinander
    Spread,      // Mehrere Schüsse gleichzeitig in Fächer
    RapidFire    // Sehr schnelles Dauerschießen
}