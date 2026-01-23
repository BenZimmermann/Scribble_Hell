using FishNet.Object;
using FishNet.Object.Synchronizing;
using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class Bullet : NetworkBehaviour
{
    private BulletData bulletData;
    private readonly SyncVar<Vector2> syncDirection = new SyncVar<Vector2>();
    private readonly SyncVar<float> syncSpeed = new SyncVar<float>();
    private readonly SyncVar<int> syncDamage = new SyncVar<int>();

    private Rigidbody2D rb;
    private SpriteRenderer spriteRenderer;
    private TrailRenderer trailRenderer;
    private int pierceCount = 0;
    //private bool isInitialized = false;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        trailRenderer = GetComponent<TrailRenderer>();

        // SyncVar Callbacks
        syncDirection.OnChange += OnDirectionChanged;
        syncSpeed.OnChange += OnSpeedChanged;
    }

    [Server]
    public void Initialize(BulletData data, Vector2 shootDirection)
    {
        bulletData = data;
        syncDirection.Value = shootDirection.normalized;
        syncSpeed.Value = bulletData.bulletSpeed;

        //Veolocety on server
        ApplyVelocity();

        // Rotation
        if (bulletData.rotateWithDirection)
        {
            float angle = Mathf.Atan2(syncDirection.Value.y, syncDirection.Value.x) * Mathf.Rad2Deg;
            transform.rotation = Quaternion.Euler(0, 0, angle);
        }

       
        InitializeVisualsClientRpc(data.bulletName);

        //isInitialized = true;

        // Auto-Destroy after LifeTime
        StartCoroutine(DestroyAfterTime());
    }

    public override void OnStartClient()
    {
        base.OnStartClient();


        if (!IsServerStarted && syncDirection.Value != Vector2.zero)
        {
            ApplyVelocity();

            if (bulletData != null && bulletData.rotateWithDirection)
            {
                float angle = Mathf.Atan2(syncDirection.Value.y, syncDirection.Value.x) * Mathf.Rad2Deg;
                transform.rotation = Quaternion.Euler(0, 0, angle);
            }
        }
    }
    
    private void OnDirectionChanged(Vector2 oldVal, Vector2 newVal, bool asServer)
    {
        if (!asServer && newVal != Vector2.zero)
        {
            ApplyVelocity();

            if (bulletData != null && bulletData.rotateWithDirection)
            {
                float angle = Mathf.Atan2(newVal.y, newVal.x) * Mathf.Rad2Deg;
                transform.rotation = Quaternion.Euler(0, 0, angle);
            }
        }
    }

    private void OnSpeedChanged(float oldVal, float newVal, bool asServer)
    {
        if (!asServer)
        {
            ApplyVelocity();
        }
    }

    private void ApplyVelocity()
    {
        if (rb != null && syncDirection.Value != Vector2.zero)
        {
            rb.linearVelocity = syncDirection.Value * syncSpeed.Value;
        }
    }

    [ObserversRpc]
    private void InitializeVisualsClientRpc(string dataName)
    {
        bulletData = Resources.Load<BulletData>($"BulletData/{dataName}");

        if (bulletData == null)
        {
            return;
        }

        ApplyVisuals();
    }

    private void ApplyVisuals()
    {
        if (bulletData == null) return;

        if (spriteRenderer != null)
        {
            if (bulletData.bulletSprite != null)
                spriteRenderer.sprite = bulletData.bulletSprite;

            spriteRenderer.color = bulletData.bulletColor;
            transform.localScale = new Vector3(bulletData.bulletSize.x, bulletData.bulletSize.y, 1);
        }

        if (trailRenderer != null)
        {
            trailRenderer.enabled = bulletData.hasTrail;
            if (bulletData.hasTrail)
            {
                trailRenderer.startColor = bulletData.trailColor;
                trailRenderer.endColor = new Color(bulletData.trailColor.r, bulletData.trailColor.g, bulletData.trailColor.b, 0);
                trailRenderer.time = bulletData.trailTime;
            }
        }
    }
    // Auto-destroy coroutine 
    private IEnumerator DestroyAfterTime()
    {
        yield return new WaitForSeconds(bulletData.lifeTime);

        if (IsServerStarted)
        {
            ServerManager.Despawn(gameObject);
        }
    }

    [Server]
    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (!IsServerStarted) return;

        EnemyController enemy = collision.GetComponent<EnemyController>();
        if (enemy != null)
        {
            enemy.TakeDamage(bulletData.damageToEnemies);

            // Piercing Logic (not in use)
            if (bulletData.piercing)
            {
                pierceCount++;
                if (pierceCount >= bulletData.maxPierceCount)
                {
                    DestroyBullet();
                }
            }
            else if (bulletData.destroyOnHit)
            {
                DestroyBullet();
            }
        }
    }

    [Server]
    private void DestroyBullet()
    {
        ServerManager.Despawn(gameObject);
    }
}