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
    private readonly SyncVar<int> remainingEnemies = new SyncVar<int>();
    private readonly SyncVar<int> totalEnemiesThisWave = new SyncVar<int>();

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        // Callbacks für UI Updates
        currentWave.OnChange += (oldVal, newVal, asServer) => UpdateWaveUI();
        remainingEnemies.OnChange += (oldVal, newVal, asServer) => UpdateProgressBar();
        totalEnemiesThisWave.OnChange += (oldVal, newVal, asServer) => UpdateProgressBar();
    }

    public override void OnStartServer()
    {
        base.OnStartServer();

        currentWave.Value = 0;
        remainingEnemies.Value = 0;
        totalEnemiesThisWave.Value = 0;
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

            // Berechne Anzahl Enemies für diese Wave
            totalEnemiesThisWave.Value = startEnemyCount + ((currentWave.Value - 1) * enemyIncreasePerWave);
            remainingEnemies.Value = totalEnemiesThisWave.Value;

            //Debug.Log($"=== Wave {currentWave.Value} gestartet! ===");
            //Debug.Log($"Enemies: {totalEnemiesThisWave.Value}");

            // Spawne alle Enemies dieser Wave nacheinander
            for (int i = 0; i < totalEnemiesThisWave.Value; i++)
            {
                enemySpawner.SpawnSingleEnemy();

                // Kurze Verzögerung zwischen Spawns
                yield return new WaitForSeconds(timeBetweenSpawns);
            }

           // Debug.Log($"Alle Enemies gespawned für Wave {currentWave.Value}!");

            // Warte bis alle Enemies tot sind (Bar leer)
            yield return new WaitUntil(() => remainingEnemies.Value <= 0);

            //Debug.Log($"=== Wave {currentWave.Value} abgeschlossen! ===");

            // Nächste Wave startet sofort (keine Pause)
        }
    }

    // Wird vom EnemyController aufgerufen wenn Enemy stirbt
    [Server]
    public void OnEnemyKilled()
    {
        remainingEnemies.Value--;
        remainingEnemies.Value = Mathf.Max(0, remainingEnemies.Value); // Nie unter 0

        //Debug.Log($"Enemy getötet! Verbleibend: {remainingEnemies.Value}/{totalEnemiesThisWave.Value}");
    }

    #region UI Updates

    private void UpdateWaveUI()
    {
        if (waveText != null)
        {
            waveText.text = $"Welle {currentWave.Value}";
        }
    }

    private void UpdateProgressBar()
    {
        if (waveProgressSlider != null && totalEnemiesThisWave.Value > 0)
        {
            // Slider ist VOLL am Anfang, geht runter wenn Enemies getötet werden
            float progress = (float)remainingEnemies.Value / totalEnemiesThisWave.Value;

            waveProgressSlider.maxValue = 1f;
            waveProgressSlider.value = progress;
        }
    }

    #endregion

    // Public Getter
    public int CurrentWave => currentWave.Value;
    public int RemainingEnemies => remainingEnemies.Value;
    public int TotalEnemiesThisWave => totalEnemiesThisWave.Value;
}