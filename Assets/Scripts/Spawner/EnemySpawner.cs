using FishNet.Object;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemySpawner : NetworkBehaviour
{
    [Header("Enemy Prefabs")]
    [SerializeField] private List<EnemyPrefabEntry> enemyPrefabs = new();

    [Header("Spawn Settings")]
    [SerializeField] private float minSpawnDistance = 8f;
    [SerializeField] private float maxSpawnDistance = 12f;

    private readonly List<GameObject> activeEnemies = new();

    // value of rarity weights
    private static readonly Dictionary<EnemyRarityType, float> RARITY_WEIGHTS = new()
    {
        { EnemyRarityType.Common, 60f },
        { EnemyRarityType.Uncommon, 25f },
        { EnemyRarityType.Rare, 10f },
        { EnemyRarityType.Epic, 4f },
        { EnemyRarityType.Legendary, 1f }
    };

    [Server]
    public void SpawnSingleEnemy()
    {
        if (enemyPrefabs == null || enemyPrefabs.Count == 0)
        {
            return;
        }

        // Randomly select an enemy prefab based on rarity weights
        EnemyPrefabEntry selectedEntry = GetWeightedRandomEnemy();

        if (selectedEntry == null || selectedEntry.enemyPrefab == null)
        {
            Debug.LogWarning("Kein gültiges Enemy Prefab gefunden!");
            return;
        }

        // Spawn Position
        Vector3 spawnPosition = GetSpawnPositionNearPlayers();

        // Spawn Enemy
        GameObject enemy = Instantiate(selectedEntry.enemyPrefab, spawnPosition, Quaternion.identity);

        ServerManager.Spawn(enemy);

        activeEnemies.Add(enemy);

        Debug.Log($"Enemy gespawned: {selectedEntry.enemyPrefab.name} (Rarity: {selectedEntry.rarity})");
    }

    [Server]
    private EnemyPrefabEntry GetWeightedRandomEnemy()
    {
        float totalWeight = 0f;

        //calculate total weight
        foreach (EnemyPrefabEntry entry in enemyPrefabs)
        {
            if (entry.enemyPrefab != null)
            {
                totalWeight += RARITY_WEIGHTS[entry.rarity];
            }
        }

        if (totalWeight == 0f)
        {
            Debug.LogWarning("Gesamtgewicht ist 0!");
            return null;
        }

        //random roll
        float roll = UnityEngine.Random.Range(0f, totalWeight);
        float current = 0f;

        foreach (EnemyPrefabEntry entry in enemyPrefabs)
        {
            if (entry.enemyPrefab == null)
                continue;

            current += RARITY_WEIGHTS[entry.rarity];

            if (roll <= current)
            {
                return entry;
            }
        }

        return enemyPrefabs.Find(e => e.enemyPrefab != null);
    }

    [Server]
    private Vector3 GetSpawnPositionNearPlayers()
    {
        var players = FindObjectsByType<PlayerMovement>(FindObjectsSortMode.None);

        if (players.Length == 0)
            return Vector3.zero;

        // randomly select a player
        PlayerMovement target = players[UnityEngine.Random.Range(0, players.Length)];

        // random direction and distance
        Vector2 direction = UnityEngine.Random.insideUnitCircle.normalized;
        float distance = UnityEngine.Random.Range(minSpawnDistance, maxSpawnDistance);

        return target.transform.position + (Vector3)(direction * distance);
    }

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
}

// Serializable Class für Enemy Prefab Einträge
[System.Serializable]
public class EnemyPrefabEntry
{
    public GameObject enemyPrefab;
    public EnemyRarityType rarity = EnemyRarityType.Common;
}