using FishNet.Object;
using FishNet.Connection;
using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class BorderManager : NetworkBehaviour
{
    [SerializeField] GameObject outOfBounce;
    private void Awake()
    {
        // Sicherheit
        GetComponent<Collider2D>().isTrigger = true;
        outOfBounce.SetActive(false);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!IsServerStarted)
            return;

        PlayerMovement player = other.GetComponent<PlayerMovement>();
        if (player == null)
            return;

        // Nur dem Owner dieses Spielers Bescheid sagen
        NotifyOutOfBoundsTargetRpc(player.Owner);
    }
    private void OnTriggerStay2D(Collider2D other)
    {
        if (!IsServerStarted)
            return;

        PlayerMovement player = other.GetComponent<PlayerMovement>();
        if (player == null)
            return;


        NotifyOutOfBoundsTargetRpc(player.Owner);
    }
    private void OnTriggerExit2D(Collider2D other)
    {
        if (!IsServerStarted)
            return;

        PlayerMovement player = other.GetComponent<PlayerMovement>();
        if (player == null)
            return;

        NotifyBackInBoundsTargetRpc(player.Owner);
    }

    [TargetRpc]
    private void NotifyOutOfBoundsTargetRpc(NetworkConnection conn)
    {
        Debug.Log(" Du bist auﬂerhalb der Spielgrenze!");
        outOfBounce.SetActive(true);
    }

    [TargetRpc]
    private void NotifyBackInBoundsTargetRpc(NetworkConnection conn)
    {
        outOfBounce.SetActive(false);
        Debug.Log("Du bist wieder innerhalb der Spielgrenze.");
    }
}

