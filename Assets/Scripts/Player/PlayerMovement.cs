using FishNet.Object;
using FishNet.Managing.Timing;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerMovement : NetworkBehaviour
{
    [SerializeField]
    private float moveSpeed = 5f;

    private Vector2 _input;

    public override void OnStartNetwork()
    {
        base.OnStartNetwork();

        if (TimeManager != null)
            TimeManager.OnTick += OnTick;
    }

    private void OnDisable()
    {
        if (TimeManager != null)
            TimeManager.OnTick -= OnTick;
    }

    /* ==============================
     * Client: read input (Owner only)
     * ============================== */
    private void OnTick()
    {
        if (!IsOwner)
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

    /* ==============================
     * Server: apply movement
     * ============================== */
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
}
