using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

// Issues move commands to selected units on right-click, distributing them around the target point

public class UnitOrderGiver : MonoBehaviour
{
    [Tooltip("Your background SpriteRenderer (optional if only one in scene)")] public SpriteRenderer background;
    [Tooltip("The Tilemap to use for pathfinding")] public Tilemap tilemap;

    private Vector3 minBounds;
    private Vector3 maxBounds;
    private bool hasIssuedMoveCommand = false;

    void Start()
    {
        background = background ?? Object.FindFirstObjectByType<SpriteRenderer>();
        tilemap = tilemap ?? Object.FindFirstObjectByType<Tilemap>();
        if (background == null || tilemap == null)
        {
            //Debug.LogError("No background SpriteRenderer or Tilemap found. Assign in Inspector.");
            enabled = false;
            return;
        }

        var bg = background.bounds;
        minBounds = new Vector3(bg.min.x, bg.min.y, 0f);
        maxBounds = new Vector3(bg.max.x, bg.max.y, 0f);
    }

    void Update()
    {
        HandleClickToMove();
    }

    private void HandleClickToMove()
    {
        if (!Input.GetMouseButtonDown(1))
            hasIssuedMoveCommand = false;

        if (!hasIssuedMoveCommand && Input.GetMouseButtonDown(1))
        {
            hasIssuedMoveCommand = true;

            var selected = UnitSelector.GetSelectedUnits();
            int count = selected.Count;
            if (count == 0) return;

            // World‐point under mouse
            Vector3 wp = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            wp.z = 0f;
            wp.x = Mathf.Clamp(wp.x, minBounds.x, maxBounds.x);
            wp.y = Mathf.Clamp(wp.y, minBounds.y, maxBounds.y);

            float spread = Mathf.Max(tilemap.cellSize.x, tilemap.cellSize.y) * 0.6f;
            for (int i = 0; i < count; i++)
            {
                var selector = selected[i];
                var mover = selector.GetComponent<UnitMover>();

                // Only fan out if more than one unit is selected
                Vector3 offset = Vector3.zero;
                if (count > 1)
                {
                    float angle = 2 * Mathf.PI * i / count;
                    offset = new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0f) * spread;
                }

                Vector3 targetWorld = wp + offset;
                // Clamp per‐unit to keep sprite inside bounds
                var extents = selector.GetComponent<SpriteRenderer>().bounds.extents;
                targetWorld.x = Mathf.Clamp(
                    targetWorld.x,
                    minBounds.x + extents.x,
                    maxBounds.x - extents.x
                );
                targetWorld.y = Mathf.Clamp(
                    targetWorld.y,
                    minBounds.y + extents.y,
                    maxBounds.y - extents.y
                );

                // Compute grid path
                Vector3Int startCell = tilemap.WorldToCell(mover.transform.position);
                Vector3Int goalCell = tilemap.WorldToCell(targetWorld);
                var cellPath = mover.Pathfinder.FindPath(startCell, goalCell);

                // Build waypoints & initiate movement
                mover.ClearPath();
                foreach (var cell in cellPath)
                {
                    Vector3 p = tilemap.GetCellCenterWorld(cell);
                    p.z = 0f;
                    mover.AppendWaypoint(p);
                }
                mover.ResetMovement(targetWorld);
            }
        }
    }
}
