using FishNet.Object;
using System.Collections;
using FishNet.Managing.Timing;
using UnityEngine;
using UnityEngine.InputSystem;
using FishNet.Object.Synchronizing;

public class PlayerMovement : NetworkBehaviour
{
    [SerializeField]
    private float moveSpeed = 5f;

    private readonly SyncVar<bool> isReady = new SyncVar<bool>();
    public bool IsReady => isReady.Value;
    private Vector2 _input;

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

        CameraFollow cam = Camera.main.GetComponent<CameraFollow>();
        cam.SetTarget(transform);
    }
    private void OnDisable()
    {
        if (TimeManager != null)
            TimeManager.OnTick -= OnTick;
    }

    private void OnTick()
    {
        if (!IsOwner)
            return;

        if (!isReady.Value)
            return;
        ReadInput();

        if (_input != Vector2.zero)
            MoveServerRpc(_input);
    }

    private void ReadInput()
    {
        if (Keyboard.current == null)
            return;

        float x = 0f;
        float y = 0f;

        if (Keyboard.current.aKey.isPressed) x -= 1f;
        if (Keyboard.current.dKey.isPressed) x += 1f;
        if (Keyboard.current.sKey.isPressed) y -= 1f;
        if (Keyboard.current.wKey.isPressed) y += 1f;

        _input = new Vector2(x, y).normalized;
    }

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
    #region ReadyStateHandling
    [ServerRpc]
    public void SetReadyStateServerRpc(string name)
    {
        isReady.Value = !isReady.Value;

        if (transform.position.x < 0)
        {
            OwnNetworkGameManager.Instance.Player1.Value = name;
        }
        else
        {
            OwnNetworkGameManager.Instance.Player2.Value = name;
        }

        OwnNetworkGameManager.Instance.DisableNameField(Owner, isReady.Value);
        OwnNetworkGameManager.Instance.CheckAndStartGame();
    }

    #endregion
}
