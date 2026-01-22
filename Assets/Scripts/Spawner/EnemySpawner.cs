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
            Debug.LogWarning("Keine Enemy Prefabs zugewiesen!");
            return;
        }

        // Wähle gewichtetes zufälliges Enemy Prefab
        EnemyPrefabEntry selectedEntry = GetWeightedRandomEnemy();

        if (selectedEntry == null || selectedEntry.enemyPrefab == null)
        {
            Debug.LogWarning("Kein gültiges Enemy Prefab gefunden!");
            return;
        }

        // Spawn Position
        Vector3 spawnPosition = GetSpawnPositionNearPlayers();

        // Spawne Enemy
        GameObject enemy = Instantiate(selectedEntry.enemyPrefab, spawnPosition, Quaternion.identity);

        // Spawne im Netzwerk
        ServerManager.Spawn(enemy);

        // Füge zur Liste hinzu
        activeEnemies.Add(enemy);

        Debug.Log($"Enemy gespawned: {selectedEntry.enemyPrefab.name} (Rarity: {selectedEntry.rarity})");
    }

    [Server]
    private EnemyPrefabEntry GetWeightedRandomEnemy()
    {
        float totalWeight = 0f;

        // Berechne Gesamtgewicht
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

        // Zufallsauswahl basierend auf Gewicht
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

        // Fallback: Erstes gültiges Prefab
        return enemyPrefabs.Find(e => e.enemyPrefab != null);
    }

    [Server]
    private Vector3 GetSpawnPositionNearPlayers()
    {
        var players = FindObjectsByType<PlayerMovement>(FindObjectsSortMode.None);

        if (players.Length == 0)
            return Vector3.zero;

        // Wähle zufälligen Spieler
        PlayerMovement target = players[UnityEngine.Random.Range(0, players.Length)];

        // Zufällige Richtung und Distanz
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
        Debug.Log("Alle Enemies entfernt!");
    }
}

// Serializable Class für Enemy Prefab Einträge
[System.Serializable]
public class EnemyPrefabEntry
{
    [Tooltip("Enemy Prefab (muss NetworkObject haben!)")]
    public GameObject enemyPrefab;

    [Tooltip("Rarity bestimmt Spawn-Wahrscheinlichkeit")]
    public EnemyRarityType rarity = EnemyRarityType.Common;
}