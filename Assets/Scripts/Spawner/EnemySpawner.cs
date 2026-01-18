using FishNet.Object;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemySpawner : NetworkBehaviour
{
    [Header("Enemy Settings")]
    [SerializeField] private GameObject enemyPrefab;
    [SerializeField] private List<EnemyData> enemyTypes = new();

    [Header("Spawn Settings")]
    [SerializeField] private float minSpawnDistance = 8f;
    [SerializeField] private float maxSpawnDistance = 12f;
    [SerializeField] private float spawnInterval = 3f;
    [SerializeField] private int maxEnemies = 10;

    private readonly List<GameObject> activeEnemies = new();
    private Coroutine spawnCoroutine;

    private static readonly Dictionary<EnemyRarityType, float> RARITY_WEIGHTS =
        new()
        {
            { EnemyRarityType.Common, 60f },
            { EnemyRarityType.Uncommon, 25f },
            { EnemyRarityType.Rare, 10f },
            { EnemyRarityType.Epic, 4f },
            { EnemyRarityType.Legendary, 1f }
        };

    public override void OnStartServer()
    {
        base.OnStartServer();
        //StartCoroutine(WaitForGameStart());
    }

    //private IEnumerator WaitForGameStart()
    //{
    //    while (OwnNetworkGameManager.Instance == null ||
    //           OwnNetworkGameManager.Instance.CurrentState != GameState.Playing)
    //    {
    //        yield return new WaitForSeconds(0.5f);
    //    }

    //    StartSpawning();
    //}

    //[Server]
    //private void StartSpawning()
    //{
    //    if (spawnCoroutine != null)
    //        StopCoroutine(spawnCoroutine);

    //    spawnCoroutine = StartCoroutine(SpawnLoop());
    //}

    //private IEnumerator SpawnLoop()
    //{
    //    while (true)
    //    {
    //        activeEnemies.RemoveAll(e => e == null);

    //        if (activeEnemies.Count < maxEnemies)
    //            SpawnSingleEnemy();

    //        yield return new WaitForSeconds(spawnInterval);
    //    }
    //}


    [Server]
    public void SpawnSingleEnemy()
    {
        if (enemyPrefab == null || enemyTypes.Count == 0)
            return;

        Vector3 spawnPosition = GetSpawnPositionNearPlayers();
        EnemyData enemyData = GetWeightedRandomEnemy();

        if (enemyData == null)
            return;

        GameObject enemy = Instantiate(enemyPrefab, spawnPosition, Quaternion.identity);

        EnemyController controller = enemy.GetComponent<EnemyController>();
        if (controller != null)
            controller.SetEnemyData(enemyData);

        ServerManager.Spawn(enemy);
        activeEnemies.Add(enemy);
    }


    [Server]
    private EnemyData GetWeightedRandomEnemy()
    {
        float totalWeight = 0f;

        foreach (EnemyData data in enemyTypes)
        {
            totalWeight += RARITY_WEIGHTS[data.rarity];
        }

        float roll = Random.Range(0f, totalWeight);
        float current = 0f;

        foreach (EnemyData data in enemyTypes)
        {
            current += RARITY_WEIGHTS[data.rarity];
            if (roll <= current)
                return data;
        }

        return enemyTypes[0]; // Fallback
    }


    [Server]
    private Vector3 GetSpawnPositionNearPlayers()
    {
        var players = FindObjectsByType<PlayerMovement>(FindObjectsSortMode.None);

        if (players.Length == 0)
            return Vector3.zero;

        PlayerMovement target = players[Random.Range(0, players.Length)];
        Vector2 direction = Random.insideUnitCircle.normalized;
        float distance = Random.Range(minSpawnDistance, maxSpawnDistance);

        return target.transform.position + (Vector3)(direction * distance);
    }

    // ---------------- CLEANUP ----------------

    [Server]
    public void ClearAllEnemies()
    {
        foreach (var enemy in activeEnemies)
        {
            if (enemy != null)
                ServerManager.Despawn(enemy);
        }

        activeEnemies.Clear();
    }

    private void OnDisable()
    {
        if (spawnCoroutine != null)
            StopCoroutine(spawnCoroutine);
    }
}
