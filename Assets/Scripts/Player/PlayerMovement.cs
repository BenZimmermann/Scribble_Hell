using FishNet.Object;
using FishNet.Managing.Timing;
using FishNet.Object.Synchronizing;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerMovement : NetworkBehaviour
{
    [Header("Movement")]
    [SerializeField] private float moveSpeed = 5f;

    [Header("Input System")]
    [SerializeField] public InputAction moveAction;

    private readonly SyncVar<bool> isReady = new SyncVar<bool>();
    public bool IsReady => isReady.Value;

    private Vector2 input;

    #region Lifecycle

    public override void OnStartNetwork()
    {
        base.OnStartNetwork();

        if (TimeManager != null)
            TimeManager.OnTick += OnTick;
    }

    public override void OnStartClient()
    {
        base.OnStartClient();

        if (!IsOwner)
            return;

        // Kamera nur lokal setzen
        CameraFollow cam = Camera.main.GetComponent<CameraFollow>();
        if (cam != null)
            cam.SetTarget(transform);

        //moveAction.Enable();
 
    }

    private void OnDisable()
    {
        moveAction.Disable();

        if (TimeManager != null)
            TimeManager.OnTick -= OnTick;
    }

    #endregion

    #region Tick / Input

    private void OnTick()
    {
        if (!IsOwner)
            return;

        if (!isReady.Value)
            return;

        ReadInput();

        if (input != Vector2.zero)
            MoveServerRpc(input);
    }

    private void ReadInput()
    {
        input = moveAction.ReadValue<Vector2>();
        if (input.sqrMagnitude > 1f)
            input.Normalize();
    }

    #endregion

    #region Movement (Server authoritative)

    [ServerRpc]
    private void MoveServerRpc(Vector2 input)
    {
        float delta = (float)TimeManager.TickDelta;

        Vector3 movement = new Vector3(
            input.x,
            input.y,
            0f
        ) * moveSpeed * delta;

        transform.position += movement;
    }

    #endregion

    #region Ready State Handling

    [ServerRpc]
    public void SetReadyStateServerRpc(string name)
    {
        isReady.Value = !isReady.Value;

        if (transform.position.x < 0)
            OwnNetworkGameManager.Instance.Player1.Value = name;
        else
            OwnNetworkGameManager.Instance.Player2.Value = name;

        OwnNetworkGameManager.Instance.DisableNameField(Owner, isReady.Value);
        OwnNetworkGameManager.Instance.CheckAndStartGame();
    }
    public void StartGame()
    {
        moveAction.Enable();
    }
    #endregion
}
