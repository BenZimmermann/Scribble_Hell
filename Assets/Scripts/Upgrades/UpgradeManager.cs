using FishNet.Object;
using FishNet.Object.Synchronizing;
using FishNet.Connection;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class UpgradeManager : NetworkBehaviour
{
    public static UpgradeManager Instance { get; private set; }

    [Header("Upgrade Settings")]
    [SerializeField] private List<UpgradeData> allUpgrades = new List<UpgradeData>();
    [SerializeField] private int upgradeEveryXWaves = 2;

    [Header("UI")]
    [SerializeField] private GameObject upgradeUICanvas;

    // Sync Variables
    private readonly SyncVar<bool> isUpgradePhase = new SyncVar<bool>();
    private readonly SyncVar<string> player1UpgradeChoice = new SyncVar<string>();
    private readonly SyncVar<string> player2UpgradeChoice = new SyncVar<string>();

    // Speichert die 3 zufälligen Upgrades dieser Runde (Server)
    private List<UpgradeData> currentUpgradeOptions = new List<UpgradeData>();

    // Speichert welche Spieler bereits gewählt haben
    private HashSet<NetworkConnection> playersWhoChose = new HashSet<NetworkConnection>();

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        isUpgradePhase.OnChange += OnUpgradePhaseChanged;
    }

    public override void OnStartServer()
    {
        base.OnStartServer();
        isUpgradePhase.Value = false;
    }

    public override void OnStartClient()
    {
        base.OnStartClient();

        if (upgradeUICanvas != null)
            upgradeUICanvas.SetActive(false);
    }

    // Wird vom WaveManager aufgerufen
    [Server]
    public void CheckForUpgradePhase(int currentWave)
    {
        // Alle X Wellen → Upgrade Phase
        if (currentWave > 0 && currentWave == upgradeEveryXWaves)
        {
            StartUpgradePhase();
        }
    }

    [Server]
    private void StartUpgradePhase()
    {
        Debug.Log(" Upgrade Phase gestartet!");

        isUpgradePhase.Value = true;
        playersWhoChose.Clear();
        player1UpgradeChoice.Value = "";
        player2UpgradeChoice.Value = "";

        // Wähle 3 zufällige Upgrades
        currentUpgradeOptions = GetRandomUpgrades(3);

        // Sende Upgrade Optionen an alle Clients
        string[] upgradeNames = currentUpgradeOptions.Select(u => u.upgradeName).ToArray();
        ShowUpgradeUIClientRpc(upgradeNames);
    }

    [Server]
    private List<UpgradeData> GetRandomUpgrades(int count)
    {
        if (allUpgrades == null || allUpgrades.Count == 0)
        {
            Debug.LogError("Keine Upgrades verfügbar!");
            return new List<UpgradeData>();
        }

        // Kopiere Liste und shuffle
        List<UpgradeData> shuffled = new List<UpgradeData>(allUpgrades);

        for (int i = 0; i < shuffled.Count; i++)
        {
            UpgradeData temp = shuffled[i];
            int randomIndex = Random.Range(i, shuffled.Count);
            shuffled[i] = shuffled[randomIndex];
            shuffled[randomIndex] = temp;
        }

        // Nimm die ersten X
        return shuffled.Take(count).ToList();
    }

    [ObserversRpc]
    private void ShowUpgradeUIClientRpc(string[] upgradeNames)
    {
        Debug.Log($" Upgrade Optionen erhalten: {string.Join(", ", upgradeNames)}");

        if (upgradeUICanvas != null)
        {
            upgradeUICanvas.SetActive(true);

            // Finde UpgradeUI Component und setze Optionen
            UpgradeUI upgradeUI = upgradeUICanvas.GetComponent<UpgradeUI>();
            if (upgradeUI != null)
            {
                upgradeUI.ShowUpgrades(upgradeNames);
            }
        }
    }

    // Wird von UpgradeUI Button aufgerufen
    public void SelectUpgrade(int upgradeIndex)
    {
        if (upgradeIndex < 0 || upgradeIndex >= 3)
        {
            Debug.LogError($"Ungültiger Upgrade Index: {upgradeIndex}");
            return;
        }

        // Finde lokalen Spieler
        var localPlayer = FindObjectsByType<PlayerMovement>(FindObjectsSortMode.None)
            .FirstOrDefault(p => p.IsOwner);

        if (localPlayer != null)
        {
            // Sende Auswahl zum Server
            SelectUpgradeServerRpc(upgradeIndex);
        }
    }

    [ServerRpc(RequireOwnership = false)]
    private void SelectUpgradeServerRpc(int upgradeIndex, NetworkConnection sender = null)
    {
        if (sender == null || !isUpgradePhase.Value)
            return;

        // Verhindere doppelte Auswahl
        if (playersWhoChose.Contains(sender))
        {
            Debug.LogWarning($"Player {sender.ClientId} hat bereits gewählt!");
            return;
        }

        if (upgradeIndex < 0 || upgradeIndex >= currentUpgradeOptions.Count)
        {
            Debug.LogError($"Ungültiger Index: {upgradeIndex}");
            return;
        }

        UpgradeData selectedUpgrade = currentUpgradeOptions[upgradeIndex];
        playersWhoChose.Add(sender);

        Debug.Log($" Player {sender.ClientId} wählte: {selectedUpgrade.upgradeName}");

        // Speichere Auswahl
        var gameManager = OwnNetworkGameManager.Instance;
        bool isPlayer1 = sender == GetPlayer1Connection();

        if (isPlayer1)
        {
            player1UpgradeChoice.Value = selectedUpgrade.upgradeName;
        }
        else
        {
            player2UpgradeChoice.Value = selectedUpgrade.upgradeName;
        }

        // Wende Upgrade an
        ApplyUpgrade(sender, selectedUpgrade);

        // Verstecke UI für diesen Spieler
        HideUpgradeUITargetRpc(sender);

        // Prüfe ob beide gewählt haben
        CheckIfAllPlayersChose();
    }

    [Server]
    private void CheckIfAllPlayersChose()
    {
        var players = FindObjectsByType<PlayerMovement>(FindObjectsSortMode.None);
        int requiredPlayers = Mathf.Min(2, players.Length);

        if (playersWhoChose.Count >= requiredPlayers)
        {
            Debug.Log(" Alle Spieler haben gewählt!");
            EndUpgradePhase();
        }
    }

    [Server]
    private void EndUpgradePhase()
    {
        isUpgradePhase.Value = false;

        // Verstecke UI für alle (falls noch nicht)
        HideUpgradeUIClientRpc();

        Debug.Log(" Spiel wird fortgesetzt!");
    }

    [Server]
    private void ApplyUpgrade(NetworkConnection conn, UpgradeData upgrade)
    {
        // Finde Spieler mit dieser Connection
        var players = FindObjectsByType<PlayerMovement>(FindObjectsSortMode.None);
        PlayerMovement targetPlayer = null;

        foreach (var player in players)
        {
            if (player.Owner == conn)
            {
                targetPlayer = player;
                break;
            }
        }

        if (targetPlayer == null)
        {
            Debug.LogError("Spieler nicht gefunden!");
            return;
        }

        // Wende Upgrade an basierend auf Typ
        ApplyUpgradeToPlayerTargetRpc(conn, upgrade.upgradeName);
    }

    [TargetRpc]
    private void ApplyUpgradeToPlayerTargetRpc(NetworkConnection conn, string upgradeName)
    {
        // Lade Upgrade Data aus Resources
        UpgradeData upgrade = Resources.Load<UpgradeData>($"Upgrades/{upgradeName}");

        if (upgrade == null)
        {
            Debug.LogError($"Upgrade '{upgradeName}' nicht in Resources/Upgrades/ gefunden!");
            return;
        }

        // Finde lokalen Spieler
        var localPlayer = FindObjectsByType<PlayerMovement>(FindObjectsSortMode.None)
            .FirstOrDefault(p => p.IsOwner);

        if (localPlayer == null)
            return;

        Debug.Log($" Applying Upgrade: {upgrade.upgradeName}");

        switch (upgrade.upgradeType)
        {
            case UpgradeType.MoveSpeed:
                ApplyMoveSpeedUpgrade(localPlayer, upgrade);
                break;

            case UpgradeType.FireRate:
                ApplyFireRateUpgrade(localPlayer, upgrade);
                break;

            case UpgradeType.WeaponChange:
                ApplyWeaponChangeUpgrade(localPlayer, upgrade);
                break;

            case UpgradeType.DamageDouble:
                ApplyDamageUpgrade(localPlayer, upgrade);
                break;
        }
    }

    private void ApplyMoveSpeedUpgrade(PlayerMovement player, UpgradeData upgrade)
    {
        player.ApplyMoveSpeedMultiplier(upgrade.moveSpeedMultiplier);
        Debug.Log($" Move Speed erhöht! ({upgrade.moveSpeedMultiplier}x)");
    }

    private void ApplyFireRateUpgrade(PlayerMovement player, UpgradeData upgrade)
    {
        BulletSpawner spawner = player.GetComponent<BulletSpawner>();
        if (spawner != null)
        {
            spawner.ApplyFireRateMultiplier(upgrade.fireRateMultiplier);
            Debug.Log($" Fire Rate erhöht! ({upgrade.fireRateMultiplier}x)");
        }
    }

    private void ApplyWeaponChangeUpgrade(PlayerMovement player, UpgradeData upgrade)
    {
        BulletSpawner spawner = player.GetComponent<BulletSpawner>();
        if (spawner != null && upgrade.weaponBulletData != null)
        {
            spawner.ChangeBulletData(upgrade.weaponBulletData);
            Debug.Log($" Waffe geändert zu: {upgrade.weaponBulletData.bulletName}");
        }
    }

    private void ApplyDamageUpgrade(PlayerMovement player, UpgradeData upgrade)
    {
        BulletSpawner spawner = player.GetComponent<BulletSpawner>();
        if (spawner != null)
        {
            spawner.ApplyDamageMultiplier(upgrade.damageMultiplier);
            Debug.Log($" Damage erhöht! ({upgrade.damageMultiplier}x)");
        }
    }

    [TargetRpc]
    private void HideUpgradeUITargetRpc(NetworkConnection conn)
    {
        if (upgradeUICanvas != null)
            upgradeUICanvas.SetActive(false);
    }

    [ObserversRpc]
    private void HideUpgradeUIClientRpc()
    {
        if (upgradeUICanvas != null)
            upgradeUICanvas.SetActive(false);
    }

    private void OnUpgradePhaseChanged(bool oldVal, bool newVal, bool asServer)
    {
        // Optional: Reagiere auf Upgrade Phase Status
    }

    // Helper: Finde Player 1 Connection
    private NetworkConnection GetPlayer1Connection()
    {
        var gameManager = OwnNetworkGameManager.Instance;
        var players = FindObjectsByType<PlayerMovement>(FindObjectsSortMode.None);

        foreach (var player in players)
        {
            if (!string.IsNullOrEmpty(gameManager.Player1.Value))
            {
                // Erster Spieler der ready war = Player 1
                return players.FirstOrDefault()?.Owner;
            }
        }

        return null;
    }

    public bool IsUpgradePhase => isUpgradePhase.Value;
}