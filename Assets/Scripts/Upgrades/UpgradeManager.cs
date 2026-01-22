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
    public PlayerMovement playerMovement;

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

    [Server]
    public void CheckForUpgradePhase(int currentWave)
    {
        if (currentWave > 0 && currentWave % upgradeEveryXWaves == 0)
        {
            StartUpgradePhase();
        }
    }

    [Server]
    private void StartUpgradePhase()
    {
        Debug.Log("⬆️ Upgrade Phase gestartet!");

        isUpgradePhase.Value = true;
        playersWhoChose.Clear();
        player1UpgradeChoice.Value = "";
        player2UpgradeChoice.Value = "";

        currentUpgradeOptions = GetRandomUpgrades(3);

        string[] upgradeNames = currentUpgradeOptions.Select(u => u.upgradeName).ToArray();
        ShowUpgradeUIClientRpc(upgradeNames);

        // Pausiere das Spiel für alle Clients
        PauseGameClientRpc();
    }

    [Server]
    private List<UpgradeData> GetRandomUpgrades(int count)
    {
        if (allUpgrades == null || allUpgrades.Count == 0)
        {
            Debug.LogError("Keine Upgrades verfügbar!");
            return new List<UpgradeData>();
        }

        List<UpgradeData> shuffled = new List<UpgradeData>(allUpgrades);

        for (int i = 0; i < shuffled.Count; i++)
        {
            UpgradeData temp = shuffled[i];
            int randomIndex = Random.Range(i, shuffled.Count);
            shuffled[i] = shuffled[randomIndex];
            shuffled[randomIndex] = temp;
        }

        return shuffled.Take(count).ToList();
    }

    [ObserversRpc]
    private void ShowUpgradeUIClientRpc(string[] upgradeNames)
    {
        Debug.Log($" Upgrade Optionen erhalten: {string.Join(", ", upgradeNames)}");

        if (upgradeUICanvas != null)
        {
            upgradeUICanvas.SetActive(true);

            UpgradeUI upgradeUI = upgradeUICanvas.GetComponent<UpgradeUI>();
            if (upgradeUI != null)
            {
                upgradeUI.ShowUpgrades(upgradeNames);
            }
        }
    }

    [ObserversRpc]
    private void PauseGameClientRpc()
    {
        Time.timeScale = 0;
        Debug.Log(" Spiel pausiert");
    }

    [ObserversRpc]
    private void ResumeGameClientRpc()
    {
        Time.timeScale = 1;
        Debug.Log(" Spiel fortgesetzt");
    }

    public void SelectUpgrade(int upgradeIndex)
    {
        if (upgradeIndex < 0 || upgradeIndex >= 3)
        {
            Debug.LogError($"Ungültiger Upgrade Index: {upgradeIndex}");
            return;
        }

        var localPlayer = FindObjectsByType<PlayerMovement>(FindObjectsSortMode.None)
            .FirstOrDefault(p => p.IsOwner);

        if (localPlayer != null)
        {
            SelectUpgradeServerRpc(upgradeIndex);
        }
    }

    [ServerRpc(RequireOwnership = false)]
    private void SelectUpgradeServerRpc(int upgradeIndex, NetworkConnection sender = null)
    {
        if (sender == null || !isUpgradePhase.Value)
            return;

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

        // Bestimme ob Player 1 oder 2
        var players = FindObjectsByType<PlayerMovement>(FindObjectsSortMode.None)
            .OrderBy(p => p.Owner.ClientId)
            .ToList();

        bool isPlayer1 = players.Count > 0 && players[0].Owner == sender;

        if (isPlayer1)
        {
            player1UpgradeChoice.Value = selectedUpgrade.upgradeName;
        }
        else
        {
            player2UpgradeChoice.Value = selectedUpgrade.upgradeName;
        }

        ApplyUpgrade(sender, selectedUpgrade);
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
        HideUpgradeUIClientRpc();
        ResumeGameClientRpc();
        Debug.Log(" Spiel wird fortgesetzt!");
    }

    [Server]
    private void ApplyUpgrade(NetworkConnection conn, UpgradeData upgrade)
    {
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


        Debug.Log($" Applying Upgrade on Server: {upgrade.upgradeName} to Player {conn.ClientId}");

        switch (upgrade.upgradeType)
        {
            case UpgradeType.MoveSpeed:
                targetPlayer.ApplyMoveSpeedMultiplier(upgrade.moveSpeedMultiplier);
                Debug.Log($" Move Speed erhöht! ({upgrade.moveSpeedMultiplier}x)");
                break;

            case UpgradeType.FireRate:
                BulletSpawner spawnerFR = targetPlayer.GetComponent<BulletSpawner>();
                if (spawnerFR != null)
                {
                    spawnerFR.ApplyFireRateMultiplier(upgrade.fireRateMultiplier);
                    Debug.Log($" Fire Rate erhöht! ({upgrade.fireRateMultiplier}x)");
                }
                break;

            case UpgradeType.WeaponChange:
                BulletSpawner spawnerWC = targetPlayer.GetComponent<BulletSpawner>();
                if (spawnerWC != null && upgrade.weaponBulletData != null)
                {
                    spawnerWC.ChangeBulletData(upgrade.weaponBulletData);
                    Debug.Log($" Waffe geändert zu: {upgrade.weaponBulletData.bulletName}");
                }
                break;

            case UpgradeType.DamageDouble:
                BulletSpawner spawnerDM = targetPlayer.GetComponent<BulletSpawner>();
                if (spawnerDM != null)
                {
                    spawnerDM.ApplyDamageMultiplier(upgrade.damageMultiplier);
                    Debug.Log($" Damage erhöht! ({upgrade.damageMultiplier}x)");
                }
                break;
        }

        // Sende Notification an den Client (optional, für UI-Feedback)
        NotifyUpgradeAppliedTargetRpc(conn, upgrade.upgradeName);
    }
    [TargetRpc]

    private void NotifyUpgradeAppliedTargetRpc(NetworkConnection conn, string upgradeName)
    {
        Debug.Log($" Upgrade erhalten: {upgradeName}");
        // Hier kannst du optional UI-Feedback zeigen (z.B. "Upgrade erhalten!" Nachricht)
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

    }

    public bool IsUpgradePhase => isUpgradePhase.Value;
}