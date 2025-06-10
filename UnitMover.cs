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

    [Tooltip("Shrink factor for obstacle overlap checks")]
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

        var bg = background.bounds;
        minBounds = new Vector3(bg.min.x, bg.min.y, 0f);
        maxBounds = new Vector3(bg.max.x, bg.max.y, 0f);

        path = new List<Vector3>();
        targetIndex = 0;
        destination = transform.position;
        hasDodged = false;
    }

    public void SetDestination(Vector3 worldTarget)
    {
        // Clamp to background minus extents
        worldTarget.x = Mathf.Clamp(worldTarget.x, minBounds.x + spriteExtents.x, maxBounds.x - spriteExtents.x);
        worldTarget.y = Mathf.Clamp(worldTarget.y, minBounds.y + spriteExtents.y, maxBounds.y - spriteExtents.y);
        destination = worldTarget;

        BuildPath();
        targetIndex = 0;
        hasDodged = false;
    }

    void BuildPath()
    {
        var start = tilemap.WorldToCell(transform.position);
        var goal = tilemap.WorldToCell(destination);

        var cells = Pathfinder.FindPath(
            tilemap, start, goal,
            spriteExtents, minBounds, maxBounds
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
        // Rebuild if path exhausted but not at destination
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

        // Blocked by another mover?
        if (Pathfinder.IsBlocked(tilemap, tCell, spriteExtents * overlapShrink))
        {
            var blocker = FindBlockingMover(tCell);
            if (blocker != null)
            {
                UnitMover mover = (Random.value < 0.5f) ? this : blocker;
                UnitMover other = (mover == this) ? blocker : this;

                if (!mover.hasDodged)
                {
                    StepAside(mover, other);
                    mover.hasDodged = true;
                }
                else
                {
                    // still blocked after dodge: re-plan
                    mover.BuildPath();
                    mover.targetIndex = 0;
                    mover.hasDodged = false;
                }
            }
            return;
        }

        // Move toward next waypoint
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

    private UnitMover FindBlockingMover(Vector3Int cell)
    {
        Vector3 worldC = tilemap.CellToWorld(cell) + (Vector3)tilemap.cellSize * 0.5f;
        var hits = Physics2D.OverlapBoxAll(worldC, spriteExtents * 2f * overlapShrink, 0f);
        foreach (var h in hits)
        {
            if (h.isTrigger || h.gameObject == gameObject) continue;
            var m = h.GetComponent<UnitMover>();
            if (m != null && m.path != null && m.targetIndex < m.path.Count)
                return m;
        }
        return null;
    }

    private void StepAside(UnitMover mover, UnitMover blocker)
    {
        var tc = tilemap.WorldToCell(mover.path[mover.targetIndex]);
        var cc = tilemap.WorldToCell(mover.transform.position);
        var dir = new Vector3Int(tc.x - cc.x, tc.y - cc.y, 0);
        var perp = new Vector3Int(-dir.y, dir.x, 0);

        var oc = tilemap.WorldToCell(blocker.transform.position);
        var toB = new Vector3Int(oc.x - cc.x, oc.y - cc.y, 0);

        if (perp.x * toB.x + perp.y * toB.y > 0)
            perp = new Vector3Int(-perp.x, -perp.y, 0);

        var diag = new Vector3Int(dir.x + perp.x, dir.y + perp.y, 0);
        var nc = cc + diag;
        Vector3 wnc = tilemap.GetCellCenterWorld(nc);
        wnc.z = 0f;

        if (wnc.x >= minBounds.x + spriteExtents.x &&
            wnc.x <= maxBounds.x - spriteExtents.x &&
            wnc.y >= minBounds.y + spriteExtents.y &&
            wnc.y <= maxBounds.y - spriteExtents.y &&
            !Pathfinder.IsBlocked(tilemap, nc, spriteExtents * overlapShrink))
        {
            mover.path.Insert(mover.targetIndex, wnc);
        }
    }
}
