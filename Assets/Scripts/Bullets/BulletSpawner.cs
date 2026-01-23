using FishNet.Object;
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public class BulletSpawner : NetworkBehaviour
{
    [Header("Bullet Configuration")]
    [SerializeField] private BulletData bulletData;
    [SerializeField] private GameObject bulletPrefab;
    [SerializeField] private Transform firePoint;

    [Header("Aim Settings")]
    [SerializeField] private bool aimWithMouse = true;
    [SerializeField] private Camera mainCamera;
    private BulletData runtimeBulletData;

    [Header("Auto Shoot Settings")]
    [SerializeField] private bool autoShootEnabled = true;

    [Header("Audio")]
    [SerializeField] private AudioSource audioSource;


    private PlayerMovement playerMovement;
    private Vector2 aimDirection = Vector2.up;
    private float lastShootTime = -999f;
    private bool isBurstActive = false;
    private Coroutine autoShootCoroutine;

    private void Awake()
    {
        playerMovement = GetComponent<PlayerMovement>();

        if (audioSource == null)
            audioSource = GetComponent<AudioSource>();

        if (mainCamera == null)
            mainCamera = Camera.main;

    }

    public override void OnStartClient()
    {
        base.OnStartClient();

        if (!IsOwner)
        {
            enabled = false;
            return;
        }
    }

    private void Update()
    {
        if (!IsOwner) return;
        if (OwnNetworkGameManager.Instance.CurrentState != GameState.Playing) return;

        //update aim direction each frame
        UpdateAimDirection();
    }
    // refresh aim direction to mouse position
    private void UpdateAimDirection()
    {
        if (!aimWithMouse || mainCamera == null) return;

        // mouse position in world space
        Vector3 mouseWorldPos = mainCamera.ScreenToWorldPoint(Mouse.current.position.ReadValue());
        mouseWorldPos.z = 0;

        
        Vector2 directionToMouse = (mouseWorldPos - transform.position).normalized;

        if (directionToMouse.magnitude > 0.1f)
        {
            aimDirection = directionToMouse;
        }
    }

    //called to start auto shooting

    public void StartShooting()
    {
        if (!autoShootEnabled || bulletData == null) return;

        if (autoShootCoroutine != null)
        {
            StopCoroutine(autoShootCoroutine);
        }

        autoShootCoroutine = StartCoroutine(AutoShootCoroutine());
    }

    public void StopShooting()
    {
        if (autoShootCoroutine != null)
        {
            StopCoroutine(autoShootCoroutine);
            autoShootCoroutine = null;
        }
        isBurstActive = false;
    }
    // Auto shooting coroutines based on shoot pattern
    private IEnumerator AutoShootCoroutine()
    {
        while (true)
        {
            if (OwnNetworkGameManager.Instance.CurrentState != GameState.Playing)
            {
                yield return null;
                continue;
            }

            switch (bulletData.shootPattern)
            {
                case ShootPattern.Single:
                    if (Time.time - lastShootTime >= bulletData.fireRate)
                    {
                        ShootBulletServerRpc(aimDirection);
                        lastShootTime = Time.time;
                    }
                    break;

                case ShootPattern.Burst:
                    if (Time.time - lastShootTime >= bulletData.burstCooldown && !isBurstActive)
                    {
                        StartCoroutine(BurstCoroutine());
                    }
                    break;

                case ShootPattern.Spread:
                    if (Time.time - lastShootTime >= bulletData.spreadFireRate)
                    {
                        ShootSpread();
                        lastShootTime = Time.time;
                    }
                    break;

                case ShootPattern.RapidFire:
                    if (Time.time - lastShootTime >= bulletData.rapidFireRate)
                    {
                        ShootBulletServerRpc(aimDirection);
                        lastShootTime = Time.time;
                    }
                    break;
            }

            yield return null;
        }
    }

    #region Shoot Patterns

    private IEnumerator BurstCoroutine()
    {
        isBurstActive = true;
        lastShootTime = Time.time;

        for (int i = 0; i < bulletData.burstCount; i++)
        {
            ShootBulletServerRpc(aimDirection);

            if (i < bulletData.burstCount - 1)
                yield return new WaitForSeconds(bulletData.burstDelay);
        }

        isBurstActive = false;
    }

    private void ShootSpread()
    {
        float startAngle = -(bulletData.spreadAngle * (bulletData.spreadCount - 1)) / 2f;

        for (int i = 0; i < bulletData.spreadCount; i++)
        {
            float angle = startAngle + (bulletData.spreadAngle * i);
            Vector2 direction = RotateVector(aimDirection, angle);
            ShootBulletServerRpc(direction);
        }
    }

    #endregion
    public void ApplyFireRateMultiplier(float multiplier)
    {
        if (runtimeBulletData == null) return;
        //works theoretically -_-
        //modify Fire Rate in Runtime Kopie
        switch (runtimeBulletData.shootPattern)
        {
            case ShootPattern.Single:
                runtimeBulletData.fireRate /= multiplier;
                break;
            case ShootPattern.Burst:
                runtimeBulletData.burstCooldown /= multiplier;
                break;
            case ShootPattern.Spread:
                runtimeBulletData.spreadFireRate /= multiplier;
                break;
            case ShootPattern.RapidFire:
                runtimeBulletData.rapidFireRate /= multiplier;
                break;
        }

        Debug.Log($" Fire Rate erhöht! Neue Rate: {runtimeBulletData.fireRate}");
    }
    public void ApplyDamageMultiplier(int multiplier)
    {
        if (runtimeBulletData == null) return;

        // modify Damage in Runtime Kopie
        runtimeBulletData.damageToEnemies *= multiplier;

        Debug.Log($" Damage erhöht! Neuer Damage: {runtimeBulletData.damageToEnemies}");
    }

    public void ChangeBulletData(BulletData newBulletData)
    {
        if (newBulletData == null) return;

        // create runtime copy of new bullet data
        runtimeBulletData = Instantiate(newBulletData);

        Debug.Log($" Waffe geändert zu: {runtimeBulletData.bulletName}");
    }


    [ServerRpc]
    private void ShootBulletServerRpc(Vector2 direction)
    {
        if (bulletPrefab == null || bulletData == null) return;

        // Spawn Position
        Vector3 spawnPos = firePoint != null ? firePoint.position : transform.position;

  
        GameObject bullet = Instantiate(bulletPrefab, spawnPos, Quaternion.identity);


        ServerManager.Spawn(bullet);

        // Initialise Bullet
        Bullet bulletScript = bullet.GetComponent<Bullet>();
        if (bulletScript != null)
        {
            bulletScript.Initialize(bulletData, direction);
        }


        PlayShootSoundClientRpc();
    }

    [ObserversRpc]
    private void PlayShootSoundClientRpc()
    {
        if (bulletData.shootSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(bulletData.shootSound, bulletData.soundVolume);
        }
    }


    private Vector2 RotateVector(Vector2 vector, float degrees)
    {
        float radians = degrees * Mathf.Deg2Rad;
        float cos = Mathf.Cos(radians);
        float sin = Mathf.Sin(radians);

        return new Vector2(
            vector.x * cos - vector.y * sin,
            vector.x * sin + vector.y * cos
        );
    }

    private void OnDisable()
    {
        StopShooting();
    }

    // Debug Visualisierung
    private void OnDrawGizmos()
    {
        if (!Application.isPlaying) return;
        if (!IsOwner) return;

        Vector3 pos = firePoint != null ? firePoint.position : transform.position;

        // Zeige Richtung zur Maus
        Gizmos.color = Color.cyan;
        Gizmos.DrawLine(pos, pos + (Vector3)aimDirection * 2f);

        // Spread Visualisierung
        if (bulletData != null && bulletData.shootPattern == ShootPattern.Spread)
        {
            float startAngle = -(bulletData.spreadAngle * (bulletData.spreadCount - 1)) / 2f;

            Gizmos.color = Color.yellow;
            for (int i = 0; i < bulletData.spreadCount; i++)
            {
                float angle = startAngle + (bulletData.spreadAngle * i);
                Vector2 direction = RotateVector(aimDirection, angle);
                Gizmos.DrawLine(pos, pos + (Vector3)direction * 1.5f);
            }
        }
    }
}