using FishNet.Object;
using FishNet.Connection;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(CompositeCollider2D))]
public class BorderManager : NetworkBehaviour
{
    [SerializeField] private GameObject outOfBoundsWarning;
    [SerializeField] private float outOfBoundsTime = 5f;

    // player out of bounds status
    private readonly HashSet<NetworkConnection> playersOutOfBounds = new();

    // timer coroutines for each player
    private readonly Dictionary<NetworkConnection, Coroutine> outOfBoundsTimers = new();

    private void Awake()
    {
        CompositeCollider2D compositeCollider = GetComponent<CompositeCollider2D>();
        compositeCollider.isTrigger = true;
        compositeCollider.geometryType = CompositeCollider2D.GeometryType.Polygons;

        if (outOfBoundsWarning != null)
            outOfBoundsWarning.SetActive(false);
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (!IsServerStarted)
            return;

        PlayerMovement player = other.GetComponent<PlayerMovement>();
        if (player == null)
            return;

        NetworkConnection conn = player.Owner;

        if (playersOutOfBounds.Add(conn))
        {
            NotifyOutOfBoundsTargetRpc(conn);

            // Start 5s Timer
            Coroutine c = StartCoroutine(OutOfBoundsTimer(conn));
            outOfBoundsTimers[conn] = c;
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!IsServerStarted)
            return;

        PlayerMovement player = other.GetComponent<PlayerMovement>();
        if (player == null)
            return;

        NetworkConnection conn = player.Owner;

        if (playersOutOfBounds.Remove(conn))
        {
            NotifyBackInBoundsTargetRpc(conn);

            //break Timer
            if (outOfBoundsTimers.TryGetValue(conn, out Coroutine c))
            {
                StopCoroutine(c);
                outOfBoundsTimers.Remove(conn);
            }
        }
    }

    private IEnumerator OutOfBoundsTimer(NetworkConnection conn)
    {
        yield return new WaitForSeconds(outOfBoundsTime);

  
        if (playersOutOfBounds.Contains(conn))
        {
            HandleOutOfBoundsTimeout(conn);
        }
    }

    [Server]
    private void HandleOutOfBoundsTimeout(NetworkConnection conn)
    {
        Debug.Log($"Spieler {conn.ClientId} war 5 Sekunden außerhalb!");
        OwnNetworkGameManager.Instance.GameOver();
    }

    [TargetRpc]
    private void NotifyOutOfBoundsTargetRpc(NetworkConnection conn)
    {
        if (outOfBoundsWarning != null)
            outOfBoundsWarning.SetActive(true);
    }

    [TargetRpc]
    private void NotifyBackInBoundsTargetRpc(NetworkConnection conn)
    {
        if (outOfBoundsWarning != null)
            outOfBoundsWarning.SetActive(false);
    }
}
