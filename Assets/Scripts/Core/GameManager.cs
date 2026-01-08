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

    [Header("UI")]
    [SerializeField] private Canvas StartCanvas;
    [SerializeField] private TMP_Text stateText;
    [SerializeField] private TMP_Text player1NameText;
    [SerializeField] private TMP_InputField PlayerNameField;
    [SerializeField] private Button ReadyButton;

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
        countdown.OnChange += OnCountdownChanged; // Timer-Update für alle Clients

        Player1.OnChange += (oldVal, newVal, asServer) =>
        {
            if (player1NameText != null)
                player1NameText.text = newVal;
        };
    }

    public override void OnStartServer()
    {
        base.OnStartServer();
        gameState.Value = GameState.WaitingForPlayers;
    }

    public override void OnStartClient()
    {
        base.OnStartClient();
        UpdateStateText(); // Initiale UI-Aktualisierung für Clients
    }

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

    public void SetPlayerReady()
    {
        foreach (var player in FindObjectsByType<PlayerMovement>(FindObjectsSortMode.None))
        {
            if (player.IsOwner)
            {
                if (!player.IsReady)
                    ReadyButton.image.color = Color.green;
                else
                    ReadyButton.image.color = Color.white;

                player.SetReadyStateServerRpc(PlayerNameField.text);
            }
        }
    }

    [TargetRpc]
    public void DisableNameField(NetworkConnection con, bool isOff)
    {
        PlayerNameField.gameObject.SetActive(!isOff);
    }

    #region State-Handling

    private void OnStateChanged(GameState oldState, GameState newState, bool asServer)
    {
        UpdateStateText();
    }

    private void OnCountdownChanged(float oldVal, float newVal, bool asServer)
    {
        UpdateStateText(); // UI aktualisieren wenn Countdown sich ändert
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
                stateText.text = "Finished";
                break;
            case GameState.StartingGame:
                if (countdown.Value > 0)
                {
                    stateText.text = Mathf.Ceil(countdown.Value).ToString(); // Ganze Zahlen anzeigen
                }
                else
                {
                    stateText.text = "GO!";
                }
                break;
        }
    }

    #endregion

    private IEnumerator SwitchToStartingGame()
    {
        yield return null; // 1 Frame warten

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

        HideStartCanvasClientRpc();
        EnablePlayerMovementClientRpc();
    }

    [ObserversRpc]
    private void HideStartCanvasClientRpc()
    {
        if (StartCanvas != null)
            StartCanvas.gameObject.SetActive(false);
    }

    [ObserversRpc] // Changed to ObserversRpc to reach all clients
    private void EnablePlayerMovementClientRpc()
    {
        foreach (var player in FindObjectsByType<PlayerMovement>(FindObjectsSortMode.None))
        {
            if (player.IsOwner) // Only enable for the owner
            {
                player.StartGame();
            }
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