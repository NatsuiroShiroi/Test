using UnityEngine;
using UnityEngine.Tilemaps;

public class UnitOrderGiver : MonoBehaviour
{
    [Tooltip("The Tilemap to use for pathfinding (primary)")]
    public Tilemap tilemap;

    [Tooltip("Fallback background SpriteRenderer if your Tilemap has no tiles")]
    public SpriteRenderer background;
    [Tooltip("Cell size (world units) when using the sprite fallback")]
    public float cellSize = 1f;

    private FlowField flowField = new FlowField();

    void Start()
    {
        if (tilemap == null)
            tilemap = Object.FindFirstObjectByType<Tilemap>();
        if (background == null)
            background = Object.FindFirstObjectByType<SpriteRenderer>();
    }

    void Update()
    {
        if (!Input.GetMouseButtonDown(1)) return;

        Vector3 wp = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        wp.z = 0f;

        // --- DEBUG: which source are we using? ---
        bool useTilemap = tilemap != null
                       && tilemap.cellBounds.size.x > 0
                       && tilemap.cellBounds.size.y > 0;

        if (useTilemap)
        {
            Debug.Log($"[OrderGiver] Using Tilemap (cellBounds={tilemap.cellBounds})");
            Vector3Int targetCell = tilemap.WorldToCell(wp);
            flowField.Generate(tilemap, targetCell);
        }
        else if (background != null)
        {
            Debug.Log($"[OrderGiver] Using Sprite fallback (bounds={background.bounds}, cellSize={cellSize})");
            flowField.Generate(background.bounds, cellSize, wp);
        }
        else
        {
            Debug.LogWarning("[OrderGiver] No valid grid source! Cannot generate FlowField.");
            return;
        }

        foreach (var sel in UnitSelector.GetSelectedUnits())
        {
            var mover = sel.GetComponent<UnitMover>();
            if (mover != null)
                mover.ApplyFlowField(flowField);
        }
    }
}
