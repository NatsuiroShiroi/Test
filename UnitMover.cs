// UnitMover.cs
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

[RequireComponent(typeof(Rigidbody2D), typeof(Collider2D))]
public class UnitMover : MonoBehaviour
{
    [HideInInspector] public bool IsSelected;

    [Tooltip("Units per second for your character")]
    public float moveSpeed = 5f;
    [Tooltip("Your background SpriteRenderer (optional if only one in scene)")]
    public SpriteRenderer background;
    [Tooltip("The Tilemap to use for pathfinding")]
    public Tilemap tilemap;
    [Tooltip("How sharply to shrink the OverlapBox when checking obstacles")]
    public float overlapShrink = 0.95f;

    private Rigidbody2D rb;
    private Vector2 spriteExtents;
    private Vector3 minBounds, maxBounds;

    private Vector3 destination;
    private bool hasDodged;
    private List<Vector3> path;
    private int targetIndex;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.freezeRotation = true;
        rb.gravityScale = 0f;

        spriteExtents = GetComponent<SpriteRenderer>().bounds.extents;

        // compute world-space background bounds
        var bg = background.bounds;
        minBounds = new Vector3(bg.min.x, bg.min.y, 0f);
        maxBounds = new Vector3(bg.max.x, bg.max.y, 0f);

        path = new List<Vector3>();
        targetIndex = 0;
        destination = transform.position;
        hasDodged = false;
    }

    /// <summary>
    /// Assign a new destination; resets path, dodge, and index.
    /// </summary>
    public void SetDestination(Vector3 worldTarget)
    {
        // clamp to background
        worldTarget.x = Mathf.Clamp(worldTarget.x, minBounds.x + spriteExtents.x, maxBounds.x - spriteExtents.x);
        worldTarget.y = Mathf.Clamp(worldTarget.y, minBounds.y + spriteExtents.y, maxBounds.y - spriteExtents.y);
        destination = worldTarget;

        BuildPath();
        targetIndex = 0;
        hasDodged = false;
    }

    /// <summary>
    /// Run the pathfinder and fill the world-space path list.
    /// </summary>
    void BuildPath()
    {
        var start = tilemap.WorldToCell(transform.position);
        var goal = tilemap.WorldToCell(destination);

        var cells = Pathfinder.FindPath(
            tilemap,
            start, goal,
            spriteExtents,
            minBounds, maxBounds
        );

        path.Clear();
        foreach (var c in cells)
        {
            var w = tilemap.GetCellCenterWorld(c);
            w.z = 0f;
            path.Add(w);
        }
    }

    void FixedUpdate()
    {
        // if path exhausted but not at destination, re-plan
        if (targetIndex >= path.Count)
        {
            if (Vector3.Distance(transform.position, destination) > 0.01f)
            {
                BuildPath();
                targetIndex = 0;
                hasDodged = false;
            }
            else return;
        }

        if (path.Count == 0) return;

        Vector3 targetPos = path[targetIndex];
        Vector3Int tCell = tilemap.WorldToCell(targetPos);

        // if blocked by another mover, attempt one dodge
        if (Pathfinder.IsBlocked(tilemap, tCell, spriteExtents))
        {
            var blocker = FindBlockingMover(tCell);
            if (blocker != null && !hasDodged)
            {
                StepAside(blocker);
                hasDodged = true;
            }
            return;
        }

        // normal movement
        Vector2 cur = rb.position;
        Vector2 dir = (Vector2)targetPos - cur;
        float step = moveSpeed * Time.fixedDeltaTime;

        if (dir.magnitude <= step)
        {
            rb.MovePosition(targetPos);
            targetIndex++;
        }
        else
        {
            rb.MovePosition(cur + dir.normalized * step);
        }
    }

    /// <summary>
    /// Finds any other UnitMover actively following a path that occupies the given cell.
    /// </summary>
    private UnitMover FindBlockingMover(Vector3Int cell)
    {
        Vector3 worldC = tilemap.CellToWorld(cell) + (Vector3)tilemap.cellSize * 0.5f;
        var hits = Physics2D.OverlapBoxAll(worldC, spriteExtents * 2f * overlapShrink, 0f);
        foreach (var h in hits)
        {
            if (h.isTrigger || h.gameObject == gameObject)
                continue;

            var m = h.GetComponent<UnitMover>();
            if (m != null && m.path != null && m.targetIndex < m.path.Count)
                return m;
        }
        return null;
    }

    /// <summary>
    /// Inserts a one‐time diagonal dodge step for the given mover around 'blocker'.
    /// </summary>
    private void StepAside(UnitMover mover, UnitMover blocker)
    {
        if (mover.hasDodged) return;

        var tc = tilemap.WorldToCell(mover.path[mover.targetIndex]);
        var cc = tilemap.WorldToCell(mover.transform.position);
        var dir = new Vector3Int(tc.x - cc.x, tc.y - cc.y, 0);
        var perp = new Vector3Int(-dir.y, dir.x, 0);

        var oc = tilemap.WorldToCell(blocker.transform.position);
        var toB = new Vector3Int(oc.x - cc.x, oc.y - cc.y, 0);

        // flip perp if it points toward the blocker
        if (perp.x * toB.x + perp.y * toB.y > 0)
            perp = new Vector3Int(-perp.x, -perp.y, 0);

        var diag = new Vector3Int(dir.x + perp.x, dir.y + perp.y, 0);
        var nc = cc + diag;
        var worldNC = tilemap.GetCellCenterWorld(nc);
        worldNC.z = 0f;

        if (worldNC.x >= minBounds.x + spriteExtents.x &&
            worldNC.x <= maxBounds.x - spriteExtents.x &&
            worldNC.y >= minBounds.y + spriteExtents.y &&
            worldNC.y <= maxBounds.y - spriteExtents.y &&
            !Pathfinder.IsBlocked(tilemap, nc, spriteExtents))
        {
            mover.path.Insert(mover.targetIndex, worldNC);
            mover.hasDodged = true;
        }
    }
}
