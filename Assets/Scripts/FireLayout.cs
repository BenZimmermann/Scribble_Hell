using System.Collections.Generic;
using UnityEngine;

[ExecuteAlways]
public class CircleBorderSpawner_TopDown : MonoBehaviour
{
    [Header("Border Settings")]
    public GameObject prefab;

    [Range(0, 100)]
    public int amount = 12;

    [Tooltip("Wenn 0, wird der Radius automatisch vom Objekt berechnet")]
    public float radius = 0f;

    public float zOffset = 0f;

    private readonly List<GameObject> spawned = new List<GameObject>();

    private void OnValidate()
    {
        if (!enabled || prefab == null)
            return;

        UpdateRadius();
        UpdatePrefabs();
    }

    private void UpdateRadius()
    {
        if (radius > 0f)
            return;

        Renderer r = GetComponent<Renderer>();
        if (r != null)
        {
            // XY-Ebene für Top-Down
            radius = Mathf.Max(r.bounds.extents.x, r.bounds.extents.y);
        }
    }

    private void UpdatePrefabs()
    {
        // Zu viele Objekte entfernen
        while (spawned.Count > amount)
        {
            GameObject obj = spawned[^1];
            spawned.RemoveAt(spawned.Count - 1);

#if UNITY_EDITOR
            if (obj != null) DestroyImmediate(obj);
#else
            if (obj != null) Destroy(obj);
#endif
        }

        // Fehlende Objekte hinzufügen
        while (spawned.Count < amount)
        {
#if UNITY_EDITOR
            GameObject obj = (GameObject)UnityEditor.PrefabUtility.InstantiatePrefab(prefab, transform);
#else
            GameObject obj = Instantiate(prefab, transform);
#endif
            spawned.Add(obj);
        }

        // Positionierung NUR auf dem Rand (kein Zentrum!)
        for (int i = 0; i < spawned.Count; i++)
        {
            float angle = (float)i / spawned.Count * Mathf.PI * 2f;

            Vector3 localPos = new Vector3(
                Mathf.Cos(angle) * radius,
                Mathf.Sin(angle) * radius,
                zOffset
            );

            spawned[i].transform.localPosition = localPos;
            // ⚠️ Keine Rotation setzen → Prefab-Rotation bleibt erhalten
        }
    }
}
