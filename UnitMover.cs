// UnitMover.cs
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

    [Tooltip("Tilemap used for grid conversions")]
    public Tilemap tilemap;

    private bool isMoving;
    private Vector3Int currentCell;
    private Vector3 targetCenter;

    // Per‐tick occupancy map
    private static HashSet<Vector3Int> occupied = new HashSet<Vector3Int>();
    private static int lastFrame = -1;
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

    public void ApplyFlowField(FlowField field)
    {
        currentField = field;
        isMoving = false;
        currentCell = tilemap.WorldToCell(transform.position);
    }

    void FixedUpdate()
    {
        // rebuild occupied once per FixedUpdate
        if (lastFrame != Time.frameCount)
        {
            lastFrame = Time.frameCount;
            occupied.Clear();
            foreach (var m in allMovers)
                occupied.Add(m.currentCell);
        }

        if (currentField == null || tilemap == null) return;

        // 1) If mid‐move, continue
        if (isMoving)
        {
            StepTowardCenter();
            return;
        }

        // 2) Sample flow‐field
        Vector2 dir = currentField.GetDirection(transform.position);
        if (dir == Vector2.zero)
        {
            currentField = null; // at goal or unreachable
            return;
        }

        // 3) Quantize to grid step
        Vector2Int q = new Vector2Int(
            Mathf.RoundToInt(dir.x),
            Mathf.RoundToInt(dir.y)
        );
        if (q == Vector2Int.zero) return;

        // 4) Compute next cell
        currentCell = tilemap.WorldToCell(transform.position);
        var nextCell = new Vector3Int(
            currentCell.x + q.x,
            currentCell.y + q.y,
            currentCell.z
        );

        // 5) If occupied, wait
        if (occupied.Contains(nextCell))
            return;

        // 6) Otherwise reserve & move
        occupied.Add(nextCell);
        targetCenter = tilemap.GetCellCenterWorld(nextCell);
        targetCenter.z = transform.position.z;
        isMoving = true;
    }

    private void StepTowardCenter()
    {
        Vector2 pos = rb.position;
        Vector2 toC = (Vector2)targetCenter - pos;
        float step = moveSpeed * Time.fixedDeltaTime;

        if (toC.magnitude <= SnapThreshold)
        {
            rb.MovePosition(targetCenter);
            isMoving = false;
            currentCell = tilemap.WorldToCell(transform.position);
        }
        else
        {
            rb.MovePosition(pos + toC.normalized * step);
        }
    }
}
