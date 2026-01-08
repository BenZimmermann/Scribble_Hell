using FishNet.Connection;
using FishNet.Managing.Timing;
using FishNet.Object;
using FishNet.Object.Synchronizing;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;


public class OwnNetworkGameManager : NetworkBehaviour
{
    public static OwnNetworkGameManager Instance { get; private set; }

    [Header("Lobby UI")]
    [SerializeField] private Canvas LobbyCanvas;
    [SerializeField] private TMP_Text stateText;
    [SerializeField] private TMP_Text playerNameReady; // Zeigt dem Spieler seinen Namen nach Ready
    [SerializeField] private TMP_InputField PlayerNameField;
    [SerializeField] private Button ReadyButton;

    [Header("Ingame UI")]
    [SerializeField] private TMP_Text ingamePlayer1NameText;
    [SerializeField] private TMP_Text ingamePlayer2NameText;

    public readonly SyncVar<string> Player1 = new SyncVar<string>();
    public readonly SyncVar<string> Player2 = new SyncVar<string>();
    public readonly SyncVar<float> countdown = new SyncVar<float>();

    [Header("Game")]
    private readonly SyncVar<GameState> gameState = new SyncVar<GameState>();
    public GameState CurrentState => gameState.Value;


    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        gameState.OnChange += OnStateChanged;
        countdown.OnChange += OnCountdownChanged;

        // Namen für Ingame UI aktualisieren
        Player1.OnChange += (oldVal, newVal, asServer) =>
        {
            UpdateIngameNames();
        };

        Player2.OnChange += (oldVal, newVal, asServer) =>
        {
            UpdateIngameNames();
        };
    }

    public override void OnStartServer()
    {
        base.OnStartServer();
        gameState.Value = GameState.WaitingForPlayers;
        Player1.Value = "";
        Player2.Value = "";
    }

    public override void OnStartClient()
    {
        base.OnStartClient();
        UpdateStateText();
        UpdateIngameNames();
    }

    // Aktualisiert die Namen im Spiel (für beide Spieler sichtbar)
    private void UpdateIngameNames()
    {
        if (ingamePlayer1NameText != null)
        {
            ingamePlayer1NameText.text = string.IsNullOrEmpty(Player1.Value)
                ? "Player 1"
                : Player1.Value;
        }

        if (ingamePlayer2NameText != null)
        {
            ingamePlayer2NameText.text = string.IsNullOrEmpty(Player2.Value)
                ? "Player 2"
                : Player2.Value;
        }
    }

    #region State-Handling

    [Server]
    public void CheckAndStartGame()
    {
        if (CurrentState != GameState.WaitingForPlayers) return;

        var players = FindObjectsByType<PlayerMovement>(FindObjectsSortMode.None);
        if (players.Length >= 2 && players.All(p => p.IsReady))
        {
            gameState.Value = GameState.FoundPlayers;
            StartCoroutine(SwitchToStartingGame());
        }
    }

    // Vom UI Button aufgerufen
    public void SetPlayerReady()
    {
        var localPlayer = FindObjectsByType<PlayerMovement>(FindObjectsSortMode.None)
            .FirstOrDefault(p => p.IsOwner);

        if (localPlayer != null)
        {
            string playerName = PlayerNameField.text.Trim();

            if (string.IsNullOrEmpty(playerName))
            {
                Debug.LogWarning("Bitte gib einen Namen ein!");
                return;
            }

            // Button Farbe ändern
            if (!localPlayer.IsReady)
                ReadyButton.image.color = Color.green;
            else
                ReadyButton.image.color = Color.white;

            // Namen zum Server senden
            localPlayer.SetReadyStateServerRpc(playerName);
        }
    }

    // Server weist den Namen zu (1. Client = Player1, 2. Client = Player2)
    [Server]
    public void AssignPlayerName(string name)
    {
        // Wenn Player1 leer ist, fülle Player1
        if (string.IsNullOrEmpty(Player1.Value))
        {
            Player1.Value = name;
            Debug.Log($"Player 1 zugewiesen: {name}");
        }
        // Sonst fülle Player2
        else if (string.IsNullOrEmpty(Player2.Value))
        {
            Player2.Value = name;
            Debug.Log($"Player 2 zugewiesen: {name}");
        }
        else
        {
            Debug.LogWarning("Beide Spieler-Slots bereits belegt!");
        }
    }

    [TargetRpc]
    public void DisableNameField(NetworkConnection con, bool isOff)
    {
        PlayerNameField.gameObject.SetActive(!isOff);

        if (ReadyButton != null)
        {
            var buttonText = ReadyButton.GetComponentInChildren<TMP_Text>();
            if (buttonText != null)
                buttonText.text = isOff ? "Cancel" : "Ready";
        }
    }

    // Zeigt dem Spieler seinen eigenen Namen in der Lobby
    [TargetRpc]
    public void ShowPlayerNameReady(NetworkConnection con, string name)
    {
        if (playerNameReady != null)
        {
            if (string.IsNullOrEmpty(name))
            {
                // Name verstecken wenn Cancel gedrückt
                playerNameReady.gameObject.SetActive(false);
            }
            else
            {
                // Name anzeigen wenn Ready gedrückt
                playerNameReady.text = $"Your Name: {name}";
                playerNameReady.gameObject.SetActive(true);
            }
        }
    }

    private void OnStateChanged(GameState oldState, GameState newState, bool asServer)
    {
        UpdateStateText();
    }

    private void OnCountdownChanged(float oldVal, float newVal, bool asServer)
    {
        UpdateStateText();
    }

    private void UpdateStateText()
    {
        if (stateText == null) return;

        switch (gameState.Value)
        {
            case GameState.WaitingForPlayers:
                stateText.text = "Waiting for players...";
                break;
            case GameState.FoundPlayers:
                stateText.text = "All players ready!";
                break;
            case GameState.StartingGame:
                if (countdown.Value > 0)
                {
                    stateText.text = Mathf.Ceil(countdown.Value).ToString();
                }
                else
                {
                    stateText.text = "GO!";
                }
                break;
            case GameState.Playing:
                stateText.text = "Playing";
                break;
        }
    }

    #endregion

    private IEnumerator SwitchToStartingGame()
    {
        yield return null;

        gameState.Value = GameState.StartingGame;
        StartCoroutine(StartGameCountdown());
    }

    [Server]
    public IEnumerator StartGameCountdown()
    {
        countdown.Value = 3f;

        while (countdown.Value > 0)
        {
            yield return new WaitForSeconds(1f);
            countdown.Value = Mathf.Max(0, countdown.Value - 1f);
        }

        gameState.Value = GameState.Playing;

        HideLobbyCanvasClientRpc();
        EnablePlayerMovementClientRpc();
    }

    [ObserversRpc]
    private void HideLobbyCanvasClientRpc()
    {
        if (LobbyCanvas != null)
            LobbyCanvas.gameObject.SetActive(false);
    }

    [ObserversRpc]
    private void EnablePlayerMovementClientRpc()
    {
        var localPlayer = FindObjectsByType<PlayerMovement>(FindObjectsSortMode.None)
            .FirstOrDefault(p => p.IsOwner);

        if (localPlayer != null)
        {
            localPlayer.StartGame();
        }
    }
}


// GameState Enum
public enum GameState
{
    WaitingForPlayers,
    StartingGame,
    FoundPlayers,
    Playing
}