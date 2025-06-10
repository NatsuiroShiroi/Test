using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

// Moves a unit along waypoints, handling collisions and dodge behavior

[RequireComponent(typeof(Rigidbody2D), typeof(Collider2D))]
public class UnitMover : MonoBehaviour
{
    [Tooltip("Units per second for your character")] public float moveSpeed = 5f;
    [Tooltip("Your background SpriteRenderer (optional if only one in scene)")] public SpriteRenderer background;
    [Tooltip("The Tilemap to use for pathfinding")] public Tilemap tilemap;

    private Rigidbody2D rb;
    private Vector3 minBounds;
    private Vector3 maxBounds;
    private Vector2 spriteExtents;
    private List<Vector3> path;
    private int targetIndex;
    private bool hasDodged;
    private Vector3 destination;
    public Pathfinder Pathfinder { get; private set; }

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.freezeRotation = true;
        rb.gravityScale = 0f;
        var col2d = GetComponent<Collider2D>();
        col2d.sharedMaterial = new PhysicsMaterial2D { friction = 0f, bounciness = 0f };
    }

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

        Bounds bg = background.bounds;
        minBounds = new Vector3(bg.min.x, bg.min.y, 0f);
        maxBounds = new Vector3(bg.max.x, bg.max.y, 0f);
        spriteExtents = GetComponent<SpriteRenderer>().bounds.extents;

        path = new List<Vector3>();
        targetIndex = 0;
        destination = transform.position;
        hasDodged = false;

        Pathfinder = new Pathfinder(tilemap, minBounds, maxBounds, spriteExtents, gameObject);
    }

    void FixedUpdate()
    {
        //Debug.Log($"[Mover:{name}] FixedUpdate. pos={transform.position:F2}, dest={destination:F2}, path.Count={path.Count}, targetIdx={targetIndex}");
        HandlePathMovement();
    }

    private void HandlePathMovement()
    {
        // Log current state each FixedUpdate before any movement
        //Debug.Log($"[Mover:{name}] HandlePathMovement - pos={transform.position:F2}, dest={destination:F2}, path.Count={path.Count}, targetIndex={targetIndex}");

        // If we’ve run out of waypoints, rebuild the path
        if (path == null || targetIndex >= path.Count)
        {
            //Debug.Log($"[Mover:{name}] No more waypoints — checking distance to destination: {Vector3.Distance(transform.position, destination):F2}");
            if (Vector3.Distance(transform.position, destination) > 0.01f)
            {
                // Recompute path
                Vector3Int start = tilemap.WorldToCell(transform.position);
                Vector3Int goal = tilemap.WorldToCell(destination);
                //Debug.Log($"[Mover:{name}] Rebuilding path: start={start}, goal={goal}");
                var cellPath = Pathfinder.FindPath(start, goal);
                //Debug.Log($"[Mover:{name}] Pathfinder returned {cellPath.Count} cells");

                path.Clear();
                foreach (var cell in cellPath)
                {
                    Vector3 world = tilemap.GetCellCenterWorld(cell);
                    world.z = 0f;
                    path.Add(world);
                }
                targetIndex = 0;
                hasDodged = false;
            }
            else
            {
                // We're at the destination
                return;
            }
        }

        // If we still have a waypoint, attempt to move toward it
        if (path.Count > 0 && targetIndex < path.Count)
        {
            Vector3 targetPos = path[targetIndex];
            //Debug.Log($"[Mover:{name}] Moving toward waypoint[{targetIndex}] = {targetPos:F2}");

            Vector3Int tCell = tilemap.WorldToCell(targetPos);
            if (Pathfinder.IsBlocked(tCell))
            {
                // Collision detected—try dodge
                //Debug.Log($"[Mover:{name}] Waypoint cell {tCell} is blocked, attempting StepAside");
                UnitMover blocker = FindBlockingMover(tCell);
                if (blocker != null)
                {
                    UnitMover mover = (Random.value < .5f) ? this : blocker;
                    UnitMover other = (mover == this) ? blocker : this;
                    StepAside(mover, other);
                }
                return;
            }

            // Normal movement toward waypoint
            Vector2 current = rb.position;
            Vector2 toTarget = (Vector2)targetPos - current;
            float step = moveSpeed * Time.fixedDeltaTime;

            if (toTarget.magnitude <= step)
            {
                rb.MovePosition(targetPos);
                //Debug.Log($"[Mover:{name}] Reached waypoint[{targetIndex}]");
                targetIndex++;
            }
            else
            {
                rb.MovePosition(current + toTarget.normalized * step);
            }
        }
    }

    private UnitMover FindBlockingMover(Vector3Int cell)
    {
        Vector3 worldC = tilemap.CellToWorld(cell) + (Vector3)tilemap.cellSize * 0.5f;
        foreach (var hit in Physics2D.OverlapBoxAll(worldC, spriteExtents * 2f, 0f))
        {
            if (!hit.isTrigger && hit.gameObject != gameObject)
            {
                var m = hit.GetComponent<UnitMover>();
                if (m != null && m.targetIndex < m.path.Count) return m;
            }
        }
        return null;
    }

    private void StepAside(UnitMover m, UnitMover other)
    {
        if (m.hasDodged) return;
        Vector3Int tc = tilemap.WorldToCell(m.path[m.targetIndex]);
        Vector3Int cc = tilemap.WorldToCell(m.transform.position);
        Vector3Int dir = new Vector3Int(tc.x - cc.x, tc.y - cc.y, 0);
        Vector3Int perp = new Vector3Int(-dir.y, dir.x, 0);
        Vector3Int oc = tilemap.WorldToCell(other.transform.position);
        Vector3Int toB = new Vector3Int(oc.x - cc.x, oc.y - cc.y, 0);
        if (perp.x * toB.x + perp.y * toB.y > 0) perp = -perp;
        Vector3Int diag = new Vector3Int(dir.x + perp.x, dir.y + perp.y, 0);
        Vector3 worldNC = tilemap.CellToWorld(cc + diag) + (Vector3)tilemap.cellSize * 0.5f;
        if (worldNC.x >= minBounds.x + spriteExtents.x &&
            worldNC.x <= maxBounds.x - spriteExtents.x &&
            worldNC.y >= minBounds.y + spriteExtents.y &&
            worldNC.y <= maxBounds.y - spriteExtents.y &&
            !Pathfinder.IsBlocked(cc + diag))
        {
            worldNC.z = 0f;
            m.path.Insert(m.targetIndex, worldNC);
            m.hasDodged = true;
        }
    }

    public void ClearPath()
    {
        path.Clear();
    }

    public void AppendWaypoint(Vector3 waypoint)
    {
        path.Add(waypoint);
    }

    public void ResetMovement(Vector3 finalDestination)
    {
        destination = finalDestination;
        targetIndex = 0;
        hasDodged = false;
    }
}
