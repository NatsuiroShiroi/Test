// MoveOrderGiver.cs
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class MoveOrderGiver : MonoBehaviour
{
    [Tooltip("Your background SpriteRenderer (optional if only one in scene)")]
    public SpriteRenderer background;
    [Tooltip("The Tilemap to use for pathfinding")]
    public Tilemap tilemap;
    [Tooltip("Angle-spread radius for multi-unit fan-out")]
    public float spreadFactor = 0.6f;

    void Update()
    {
        if (Input.GetMouseButtonDown(1))
        {
            var sel = UnitSelector.SelectedUnits;
            Debug.Log($"MoveOrderGiver: right-click detected, issuing orders to {sel.Count} units");

            if (sel.Count == 0)
                return;

            // Raw click → world
            Vector3 wp = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            wp.z = 0f;

            // clamp
            var b = background.bounds;
            wp.x = Mathf.Clamp(wp.x, b.min.x, b.max.x);
            wp.y = Mathf.Clamp(wp.y, b.min.y, b.max.y);

            float spread = Mathf.Max(tilemap.cellSize.x, tilemap.cellSize.y) * spreadFactor;

            for (int i = 0; i < sel.Count; i++)
            {
                var u = sel[i];
                Vector3 target = wp;
                if (sel.Count > 1)
                {
                    float angle = 2 * Mathf.PI * i / sel.Count;
                    target += new Vector3(Mathf.Cos(angle), Mathf.Sin(angle)) * spread;
                }

                Debug.Log($"MoveOrderGiver: About to call SetDestination on {u.name}");
                u.SetDestination(target);
                Debug.Log($"MoveOrderGiver: Called SetDestination on {u.name}");
            }
        }
    }
}
