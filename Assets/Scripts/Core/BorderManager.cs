using FishNet.Object;
using FishNet.Connection;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(CompositeCollider2D))]
public class BorderManager : NetworkBehaviour
{
    [SerializeField] private GameObject outOfBoundsWarning;

    // Merkt sich, welche Clients aktuell außerhalb sind
    private readonly HashSet<NetworkConnection> playersOutOfBounds = new();

    private void Awake()
    {
        // Composite Collider Setup
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
        }
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