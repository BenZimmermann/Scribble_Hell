using FishNet.Connection;
using FishNet.Managing.Scened;
using FishNet.Managing;
using FishNet.Managing.Timing;
using FishNet.Object;
using FishNet.Object.Synchronizing;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using FishNet;
using UnityEngine.SceneManagement;

public class OwnNetworkGameManager : NetworkBehaviour
{
    public static OwnNetworkGameManager Instance { get; private set; }

    [Header("Lobby UI")]
    [SerializeField] private Canvas LobbyCanvas;
    [SerializeField] private TMP_Text stateText;
    [SerializeField] private TMP_Text playerNameReady;
    [SerializeField] private TMP_InputField PlayerNameField;
    [SerializeField] private Button ReadyButton;

    [Header("Ingame UI")]
    [SerializeField] private TMP_Text ingamePlayer1NameText;
    [SerializeField] private TMP_Text ingamePlayer2NameText;
    [SerializeField] private Slider ingamePlayer1LivesSlider;
    [SerializeField] private Slider ingamePlayer2LivesSlider;
    [SerializeField] private TMP_Text scoreText; // NEU: Score Anzeige
    [SerializeField] private TMP_Text gameOverScoreText; // NEU: Score im GameOver
    [SerializeField] private int maxLives = 3;

    public readonly SyncVar<string> Player1 = new SyncVar<string>();
    public readonly SyncVar<string> Player2 = new SyncVar<string>();
    public readonly SyncVar<float> countdown = new SyncVar<float>();

    public readonly SyncVar<int> currentScore = new SyncVar<int>();

    [Header("Lives")]
    public readonly SyncVar<int> Player1Lives = new SyncVar<int>();
    public readonly SyncVar<int> Player2Lives = new SyncVar<int>();

    private NetworkConnection player1Connection;
    private NetworkConnection player2Connection;

    [Header("Game")]
    private readonly SyncVar<GameState> gameState = new SyncVar<GameState>();

    [Header("GameOver")]
    [SerializeField] private Image GameOverCanvas;
    public GameState CurrentState => gameState.Value;
    private NetworkManager networkManager;


    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
        networkManager = InstanceFinder.NetworkManager;
        gameState.OnChange += OnStateChanged;
        countdown.OnChange += OnCountdownChanged;

        Player1.OnChange += (oldVal, newVal, asServer) =>
        {
            UpdateIngameNames();
        };

        Player2.OnChange += (oldVal, newVal, asServer) =>
        {
            UpdateIngameNames();
        };

        Player1Lives.OnChange += (oldVal, newVal, asServer) =>
        {
            UpdateIngameLives();
        };

        Player2Lives.OnChange += (oldVal, newVal, asServer) =>
        {
            UpdateIngameLives();
        };
    }

    public override void OnStartServer()
    {
        base.OnStartServer();
        gameState.Value = GameState.WaitingForPlayers;
        Player1.Value = "";
        Player2.Value = "";
        currentScore.Value = 0;
        Player1Lives.Value = maxLives;
        Player2Lives.Value = maxLives;
        player1Connection = null;
        player2Connection = null;
        GameOverCanvas.gameObject.SetActive(false);
    }

    public override void OnStartClient()
    {
        base.OnStartClient();
        UpdateStateText();
        UpdateIngameNames();
        UpdateIngameLives();
        UpdateScoreUI();
    }

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
    public void UpdateScoreUI()
    {
        if (scoreText != null)
        {
            scoreText.text = $"{currentScore.Value}";
        }
    }
    [Server]
    public void AddScore()
    {
            currentScore.Value += 15;
        
    }
    private void UpdateIngameLives()
    {
        if (ingamePlayer1LivesSlider != null)
        {
            ingamePlayer1LivesSlider.maxValue = maxLives;
            ingamePlayer1LivesSlider.value = Player1Lives.Value;
        }

        if (ingamePlayer2LivesSlider != null)
        {
            ingamePlayer2LivesSlider.maxValue = maxLives;
            ingamePlayer2LivesSlider.value = Player2Lives.Value;
        }
    }
    [Server]
    public void ResetScore()
    {
        currentScore.Value = 0;
        Debug.Log(" Score zurückgesetzt");
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

    public void SetPlayerReady()
    {
        var localPlayer = FindObjectsByType<PlayerMovement>(FindObjectsSortMode.None)
            .FirstOrDefault(p => p.IsOwner);

        if (localPlayer != null)
        {
            string playerName = PlayerNameField.text.Trim();

            if (string.IsNullOrEmpty(playerName))
            {
                return;
            }

            if (!localPlayer.IsReady)
                ReadyButton.image.color = Color.green;
            else
                ReadyButton.image.color = Color.white;

            localPlayer.SetReadyStateServerRpc(playerName);
        }
    }

    [Server]
    public void AssignPlayerName(string name, NetworkConnection conn)
    {
        if (string.IsNullOrEmpty(Player1.Value))
        {
            Player1.Value = name;
            player1Connection = conn;
            Debug.Log($"Player 1 zugewiesen: {name}");
        }
        else if (string.IsNullOrEmpty(Player2.Value))
        {
            Player2.Value = name;
            player2Connection = conn;
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

    [TargetRpc]
    public void ShowPlayerNameReady(NetworkConnection con, string name)
    {
        if (playerNameReady != null)
        {
            if (string.IsNullOrEmpty(name))
            {
                playerNameReady.gameObject.SetActive(false);
            }
            else
            {
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

    #region Game Flow

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
        ResetScore();
        HideLobbyCanvasClientRpc();
        EnablePlayerMovementClientRpc();
        if (WaveManager.Instance != null)
        {
            WaveManager.Instance.StartWaveSystem();
        }
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

    #endregion

    #region Lives Management

    [Server]
    public void LoseLife(NetworkConnection conn)
    {
        if (player1Connection == null || player2Connection == null)
        {
            Debug.LogError($"FEHLER: Connections sind NULL! P1={player1Connection?.ClientId}, P2={player2Connection?.ClientId}");
            return;
        }
        var player = conn.FirstObject.GetComponent<PlayerMovement>();
        if (player == null)
        {
            return;
        }
        Debug.Log($"ICH BIN CONNECTION {conn.ClientId} IN GameManger.LoseLife()");

        int playerIndex = -1;

        if (!string.IsNullOrEmpty(Player1.Value) && conn == player1Connection)
        {
            Debug.LogError("player" + conn);
            playerIndex = 0;
        }
        else if (!string.IsNullOrEmpty(Player2.Value) && conn == player2Connection)
        {
            Debug.LogError("player" + conn);
            playerIndex = 1;
        }

        if (playerIndex == 0)
        {
            Debug.LogError("damage player1");
            Player1Lives.Value = Mathf.Max(0, Player1Lives.Value - 1);
        }
        else if (playerIndex == 1)
        {
            Debug.LogError("damage player2");
            Player2Lives.Value = Mathf.Max(0, Player2Lives.Value - 1);
        }
        if (Player2Lives.Value <= 0 || Player1Lives.Value <= 0)
        {
            Debug.Log("SPIEL VORBEI");
            GameOver();
        }
    }
    [Server]
    public void ResetLives()
    {
        Player1Lives.Value = maxLives;
        Player2Lives.Value = maxLives;
        player1Connection = null;
        player2Connection = null;
    }
    [ObserversRpc]
    public void GameOver()
    {
        GameOverCanvas.gameObject.SetActive(true);
        if (gameOverScoreText != null)
        {
            gameOverScoreText.text = $"Highscore: {currentScore.Value}";
        }
        Time.timeScale = 0;
    }
    #endregion
    public void OnQuit()
    {
        Application.Quit();
    }
    public void OnRestart()
    {
        // Setze Time.timeScale zurück
        Time.timeScale = 1f;

        // Lade Scene einfach neu - Unity macht den Rest
        UnityEngine.SceneManagement.SceneManager.LoadScene(
            UnityEngine.SceneManagement.SceneManager.GetActiveScene().buildIndex
        );
    }
    public void QuitToMenu()
    {
        Time.timeScale = 1f;

        // Trenne Netzwerk-Verbindung
        NetworkManager nm = InstanceFinder.NetworkManager;
        if (nm != null)
        {
            if (nm.IsServerStarted)
                nm.ServerManager.StopConnection(true);

            if (nm.IsClientStarted)
                nm.ClientManager.StopConnection();
        }

        Debug.Log(" Zurück zum Menü");
        UnityEngine.SceneManagement.SceneManager.LoadScene(0);
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