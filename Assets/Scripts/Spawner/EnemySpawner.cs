using FishNet.Object;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemySpawner : NetworkBehaviour
{
    [Header("Spawn Settings")]
    [SerializeField] private GameObject enemyPrefab;
    [SerializeField] private float spawnInterval = 3f;
    [SerializeField] private Transform[] spawnPoints;
    [SerializeField] private int maxEnemies = 10;

    [Header("Spawn Control")]
    [SerializeField] private bool spawnOnGameStart = true;

    private List<GameObject> activeEnemies = new List<GameObject>();
    private Coroutine spawnCoroutine;

    public override void OnStartServer()
    {
        base.OnStartServer();

        if (spawnOnGameStart)
        {
            // Warte bis das Spiel startet
            StartCoroutine(WaitForGameStart());
        }
    }

    private IEnumerator WaitForGameStart()
    {
        // Warte bis GameState auf Playing ist
        while (OwnNetworkGameManager.Instance == null ||
               OwnNetworkGameManager.Instance.CurrentState != GameState.Playing)
        {
            yield return new WaitForSeconds(0.5f);
        }

        // Starte Spawning
        StartSpawning();
    }

    [Server]
    public void StartSpawning()
    {
        if (spawnCoroutine != null)
        {
            StopCoroutine(spawnCoroutine);
        }

        spawnCoroutine = StartCoroutine(SpawnEnemies());
        Debug.Log("Enemy Spawning gestartet!");
    }

    [Server]
    public void StopSpawning()
    {
        if (spawnCoroutine != null)
        {
            StopCoroutine(spawnCoroutine);
            spawnCoroutine = null;
        }

        Debug.Log("Enemy Spawning gestoppt!");
    }

    private IEnumerator SpawnEnemies()
    {
        while (true)
        {
            // Entferne zerstörte Enemies aus der Liste
            activeEnemies.RemoveAll(enemy => enemy == null);

            // Spawne nur wenn unter dem Maximum
            if (activeEnemies.Count < maxEnemies)
            {
                SpawnEnemy();
            }

            yield return new WaitForSeconds(spawnInterval);
        }
    }

    [Server]
    private void SpawnEnemy()
    {
        // Wähle einen zufälligen Spawn Point
        Transform spawnPoint = spawnPoints[Random.Range(0, spawnPoints.Length)];

        // Spawne den Enemy
        GameObject enemy = Instantiate(enemyPrefab, spawnPoint.position, spawnPoint.rotation);

        // Spawne das Objekt im Netzwerk (wichtig für FishNet!)
        ServerManager.Spawn(enemy);

        // Füge zur Liste hinzu
        activeEnemies.Add(enemy);
    }

    [Server]
    public void ClearAllEnemies()
    {
        foreach (GameObject enemy in activeEnemies)
        {
            if (enemy != null)
            {
                ServerManager.Despawn(enemy);
            }
        }

        activeEnemies.Clear();
    }

    private void OnDisable()
    {
        StopSpawning();
    }
}