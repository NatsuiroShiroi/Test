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
    [Tooltip("Angle‐spread radius for multi‐unit fan‐out")]
    public float spreadFactor = 0.6f;

    private bool hasIssuedMoveCommand = false;

    void Update()
    {
        // reset one‐shot guard when button is up
        if (!Input.GetMouseButton(1))
            hasIssuedMoveCommand = false;

        if (!hasIssuedMoveCommand && Input.GetMouseButtonDown(1))
        {
            hasIssuedMoveCommand = true;

            var sel = UnitSelector.SelectedUnits;
            int count = sel.Count;
            if (count == 0) return;

            // raw click → world
            Vector3 wp = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            wp.z = 0f;

            // clamp to background bounds
            var b = background.bounds;
            float minX = b.min.x, maxX = b.max.x;
            float minY = b.min.y, maxY = b.max.y;
            wp.x = Mathf.Clamp(wp.x, minX, maxX);
            wp.y = Mathf.Clamp(wp.y, minY, maxY);

            // spread radius in world units
            float spread = Mathf.Max(tilemap.cellSize.x, tilemap.cellSize.y) * spreadFactor;

            // dispatch each selected unit
            for (int i = 0; i < count; i++)
            {
                var u = sel[i];
                Vector3 target = wp;

                if (count > 1)
                {
                    float angle = 2 * Mathf.PI * i / count;
                    target += new Vector3(Mathf.Cos(angle), Mathf.Sin(angle)) * spread;
                }

                // clamp each target as well
                target.x = Mathf.Clamp(target.x, minX, maxX);
                target.y = Mathf.Clamp(target.y, minY, maxY);

                u.SetDestination(target);
            }
        }
    }
}
