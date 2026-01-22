using FishNet.Object;
using FishNet.Object.Synchronizing;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class WaveManager : NetworkBehaviour
{
    public static WaveManager Instance { get; private set; }

    [Header("Wave Settings")]
    [SerializeField] private int startEnemyCount = 10;
    [SerializeField] private int enemyIncreasePerWave = 5;
    [SerializeField] private float timeBetweenSpawns = 0.5f;

    [Header("UI")]
    [SerializeField] private TMP_Text waveText;
    [SerializeField] private Slider waveProgressSlider;

    [Header("Spawning")]
    [SerializeField] private EnemySpawner enemySpawner;

    // Sync Variables
    private readonly SyncVar<int> currentWave = new SyncVar<int>();
    private readonly SyncVar<int> enemiesKilled = new SyncVar<int>();
    private readonly SyncVar<int> totalEnemiesThisWave = new SyncVar<int>();
    private readonly SyncVar<bool> isWaveActive = new SyncVar<bool>();

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        // Callbacks für UI Updates
        currentWave.OnChange += (oldVal, newVal, asServer) => UpdateWaveUI();
        enemiesKilled.OnChange += (oldVal, newVal, asServer) => UpdateProgressBar();
        totalEnemiesThisWave.OnChange += (oldVal, newVal, asServer) => UpdateProgressBar();
        isWaveActive.OnChange += (oldVal, newVal, asServer) => UpdateWaveUI();
    }

    public override void OnStartServer()
    {
        base.OnStartServer();

        currentWave.Value = 0;
        enemiesKilled.Value = 0;
        totalEnemiesThisWave.Value = 0;
        isWaveActive.Value = false;
    }

    public override void OnStartClient()
    {
        base.OnStartClient();
        UpdateWaveUI();
        UpdateProgressBar();
    }

    // Wird vom GameManager aufgerufen wenn Spiel startet
    [Server]
    public void StartWaveSystem()
    {
        if (enemySpawner == null)
        {
            Debug.LogError("EnemySpawner nicht zugewiesen!");
            return;
        }

        StartCoroutine(WaveSystemCoroutine());
    }

    [Server]
    private IEnumerator WaveSystemCoroutine()
    {
        while (true)
        {
            // Starte neue Wave
            currentWave.Value++;
            enemiesKilled.Value = 0;

            // Berechne Anzahl Enemies für diese Wave
            totalEnemiesThisWave.Value = startEnemyCount + ((currentWave.Value - 1) * enemyIncreasePerWave);

            Debug.Log($"=== Wave {currentWave.Value} gestartet! ===");
            Debug.Log($"Enemies: {totalEnemiesThisWave.Value}");

            // Wave ist jetzt AKTIV - Enemies können spawnen
            isWaveActive.Value = true;

            // Spawne alle Enemies dieser Wave nacheinander
            for (int i = 0; i < totalEnemiesThisWave.Value; i++)
            {
                enemySpawner.SpawnSingleEnemy();

                // Kurze Verzögerung zwischen Spawns
                yield return new WaitForSeconds(timeBetweenSpawns);
            }

            Debug.Log($"Alle {totalEnemiesThisWave.Value} Enemies gespawned für Wave {currentWave.Value}!");

            // Warte bis alle Enemies getötet wurden
            yield return new WaitUntil(() => enemiesKilled.Value >= totalEnemiesThisWave.Value);

            // Wave BEENDET - Keine Enemies mehr spawnen
            isWaveActive.Value = false;

            Debug.Log($"=== Wave {currentWave.Value} abgeschlossen! ===");
            Debug.Log($"Alle {totalEnemiesThisWave.Value} Enemies getötet!");
            UpgradeManager.Instance.CheckForUpgradePhase(currentWave.Value);
            // Kurze Pause, dann nächste Wave
            yield return new WaitForSeconds(1f);
        }
    }

    // Wird vom EnemyController aufgerufen wenn Enemy stirbt
    [Server]
    public void OnEnemyKilled()
    {
        // Nur zählen wenn Wave aktiv ist
        if (!isWaveActive.Value)
        {
            Debug.LogWarning("Enemy getötet, aber keine Wave aktiv!");
            return;
        }

        enemiesKilled.Value++;

        // Verhindere Überschreitung
        if (enemiesKilled.Value > totalEnemiesThisWave.Value)
        {
            enemiesKilled.Value = totalEnemiesThisWave.Value;
        }

        int remaining = totalEnemiesThisWave.Value - enemiesKilled.Value;
        Debug.Log($"Enemy getötet! Fortschritt: {enemiesKilled.Value}/{totalEnemiesThisWave.Value} (noch {remaining} übrig)");
    }

    #region UI Updates

    private void UpdateWaveUI()
    {
        if (waveText != null)
        {
            if (isWaveActive.Value)
            {
                waveText.text = $"Wave {currentWave.Value}";
            }
            else
            {
                waveText.text = $"Starting...";
            }
        }
    }

    private void UpdateProgressBar()
    {
        if (waveProgressSlider != null && totalEnemiesThisWave.Value > 0)
        {
            // Slider startet VOLL (100%) und geht RUNTER wenn Enemies getötet werden
            int remainingEnemies = totalEnemiesThisWave.Value - enemiesKilled.Value;
            float progress = (float)remainingEnemies / totalEnemiesThisWave.Value;

            waveProgressSlider.maxValue = 1f;
            waveProgressSlider.value = progress;
        }
    }

    #endregion

    // Public Getter
    public int CurrentWave => currentWave.Value;
    public int EnemiesKilled => enemiesKilled.Value;
    public int TotalEnemiesThisWave => totalEnemiesThisWave.Value;
    public bool IsWaveActive => isWaveActive.Value;
}