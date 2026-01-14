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

    [Header("Auto Shoot Settings")]
    [SerializeField] private bool autoShootEnabled = true;

    [Header("Audio")]
    [SerializeField] private AudioSource audioSource;

    private PlayerMovement playerMovement;
    private Vector2 lastMoveDirection = Vector2.up; // Standard: nach oben
    private float lastShootTime = -999f;
    private bool isBurstActive = false;
    private Coroutine autoShootCoroutine;

    private void Awake()
    {
        playerMovement = GetComponent<PlayerMovement>();

        if (audioSource == null)
            audioSource = GetComponent<AudioSource>();
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

        // Tracke die Bewegungsrichtung
        UpdateMoveDirection();
    }

    private void UpdateMoveDirection()
    {
        Vector2 moveInput = playerMovement.moveAction.ReadValue<Vector2>();

        if (moveInput.magnitude > 0.1f)
        {
            lastMoveDirection = moveInput.normalized;
        }
    }

    // Wird vom PlayerMovement.StartGame() aufgerufen
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
                        ShootBulletServerRpc(lastMoveDirection);
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
                        ShootBulletServerRpc(lastMoveDirection);
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
            ShootBulletServerRpc(lastMoveDirection);

            if (i < bulletData.burstCount - 1)
                yield return new WaitForSeconds(bulletData.burstDelay);
        }

        isBurstActive = false;
    }

    private void ShootSpread()
    {
        // Berechne Spread Winkel
        float startAngle = -(bulletData.spreadAngle * (bulletData.spreadCount - 1)) / 2f;

        for (int i = 0; i < bulletData.spreadCount; i++)
        {
            float angle = startAngle + (bulletData.spreadAngle * i);
            Vector2 direction = RotateVector(lastMoveDirection, angle);
            ShootBulletServerRpc(direction);
        }
    }

    #endregion

    [ServerRpc]
    private void ShootBulletServerRpc(Vector2 direction)
    {
        if (bulletPrefab == null || bulletData == null) return;

        // Spawn Position
        Vector3 spawnPos = firePoint != null ? firePoint.position : transform.position;

        // Erstelle Bullet
        GameObject bullet = Instantiate(bulletPrefab, spawnPos, Quaternion.identity);

        // Spawne im Netzwerk
        ServerManager.Spawn(bullet);

        // Initialisiere Bullet
        Bullet bulletScript = bullet.GetComponent<Bullet>();
        if (bulletScript != null)
        {
            bulletScript.Initialize(bulletData, direction);
        }

        // Sound abspielen
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

    // Hilfsfunktion zum Rotieren von Vektoren
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

        Gizmos.color = Color.cyan;
        Gizmos.DrawLine(pos, pos + (Vector3)lastMoveDirection * 2f);

        // Spread Visualisierung
        if (bulletData != null && bulletData.shootPattern == ShootPattern.Spread)
        {
            float startAngle = -(bulletData.spreadAngle * (bulletData.spreadCount - 1)) / 2f;

            for (int i = 0; i < bulletData.spreadCount; i++)
            {
                float angle = startAngle + (bulletData.spreadAngle * i);
                Vector2 direction = RotateVector(lastMoveDirection, angle);
                Gizmos.DrawLine(pos, pos + (Vector3)direction * 1.5f);
            }
        }
    }
}