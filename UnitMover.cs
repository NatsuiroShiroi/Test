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

    private Rigidbody2D rb;
    private Vector2 extents;
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

        extents = GetComponent<SpriteRenderer>().bounds.extents;

        // compute world‐bounds from background
        var bg = background.bounds;
        minBounds = new Vector3(bg.min.x, bg.min.y, 0);
        maxBounds = new Vector3(bg.max.x, bg.max.y, 0);

        path = new List<Vector3>();
        destination = transform.position;
        targetIndex = 0;
        hasDodged = false;
    }

    public void SetDestination(Vector3 worldDest)
    {
        // clamp to bg minus extents
        destination = worldDest;
        destination.x = Mathf.Clamp(destination.x, minBounds.x + extents.x, maxBounds.x - extents.x);
        destination.y = Mathf.Clamp(destination.y, minBounds.y + extents.y, maxBounds.y - extents.y);

        BuildPath();
        targetIndex = 0;
        hasDodged = false;
    }

    void BuildPath()
    {
        var start = tilemap.WorldToCell(transform.position);
        var goal = tilemap.WorldToCell(destination);

        var cells = Pathfinder.FindPath(
            tilemap, start, goal, extents, minBounds, maxBounds
        );

        path.Clear();
        foreach (var c in cells)
        {
            var w = tilemap.GetCellCenterWorld(c);
            w.z = 0; path.Add(w);
        }
    }

    void FixedUpdate()
    {
        if (targetIndex >= path.Count) return;

        var targetPos = path[targetIndex];
        var tCell = tilemap.WorldToCell(targetPos);

        if (Pathfinder.IsBlocked(tilemap, tCell, extents))
        {
            if (!hasDodged)
            {
                StepAside();
                hasDodged = true;
            }
            return;
        }

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

    void StepAside()
    {
        // identical to your previous diag‐dodge code,
        // but inserting into this.path at targetIndex.
        // …
    }
}