using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

[RequireComponent(typeof(Rigidbody2D), typeof(Collider2D))]
public class UnitMover : MonoBehaviour
{
    [Tooltip("Units per second")]
    public float moveSpeed = 5f;

    private const float SnapThreshold = 0.05f;

    private Rigidbody2D rb;
    private FlowField currentField;

    [Tooltip("Tilemap used for cell↔world conversions")]
    public Tilemap tilemap;

    // Movement state
    private bool isMoving;
    private Vector3Int currentCell;
    private Vector3 targetCenter;

    // --- Reservation system, reset and populated each FixedUpdate ---
    private static HashSet<Vector3Int> reserved = new HashSet<Vector3Int>();
    private static int lastFrame = -1;
    // Track all active movers
    private static List<UnitMover> allMovers = new List<UnitMover>();

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Kinematic;
        rb.freezeRotation = true;
        GetComponent<Collider2D>().isTrigger = true;

        allMovers.Add(this);
    }

    void OnDestroy()
    {
        allMovers.Remove(this);
    }

    void Start()
    {
        tilemap = tilemap ?? Object.FindFirstObjectByType<Tilemap>();
        if (tilemap == null)
            Debug.LogError($"UnitMover '{name}': no Tilemap assigned or found!");
    }

    /// <summary>
    /// Called by UnitOrderGiver when a new FlowField is ready.
    /// </summary>
    public void ApplyFlowField(FlowField field)
    {
        currentField = field;
        isMoving = false;
        currentCell = tilemap.WorldToCell(transform.position);
    }

    void FixedUpdate()
    {
        // 1) Once per physics tick, reset & fill reservations with each mover's currentCell
        if (lastFrame != Time.frameCount)
        {
            lastFrame = Time.frameCount;
            reserved.Clear();
            foreach (var m in allMovers)
                reserved.Add(m.currentCell);
        }

        if (currentField == null || tilemap == null)
            return;

        // 2) If already heading to a center, keep going
        if (isMoving)
        {
            StepTowardCenter();
            return;
        }

        // 3) Sample the flow‐field
        Vector2 dir = currentField.GetDirection(transform.position);
        if (dir == Vector2.zero)
        {
            // Arrived or unreachable
            currentField = null;
            return;
        }

        // 4) Quantize to one of 8 grid directions
        Vector2Int q = new Vector2Int(
            Mathf.RoundToInt(dir.x),
            Mathf.RoundToInt(dir.y)
        );
        if (q == Vector2Int.zero) return;

        // 5) Compute desired next cell
        currentCell = tilemap.WorldToCell(transform.position);
        var desired = new Vector3Int(
            currentCell.x + q.x,
            currentCell.y + q.y,
            currentCell.z
        );

        // 6) If that cell is already in reserved, skip movement
        if (reserved.Contains(desired))
            return;

        // 7) Otherwise reserve & begin moving there
        reserved.Add(desired);
        targetCenter = tilemap.GetCellCenterWorld(desired);
        targetCenter.z = transform.position.z;
        isMoving = true;
    }

    /// <summary>
    /// Smoothly moves toward targetCenter, snapping when close enough.
    /// </summary>
    private void StepTowardCenter()
    {
        Vector2 pos = rb.position;
        Vector2 toC = (Vector2)targetCenter - pos;
        float step = moveSpeed * Time.fixedDeltaTime;

        if (toC.magnitude <= SnapThreshold)
        {
            // Snap exactly, update currentCell, stop
            rb.MovePosition(targetCenter);
            isMoving = false;
            currentCell = tilemap.WorldToCell(transform.position);
        }
        else
        {
            // Move partway
            rb.MovePosition(pos + toC.normalized * step);
        }
    }
}
