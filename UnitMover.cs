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

    [Tooltip("Tilemap used to convert cells ↔ world positions")]
    public Tilemap tilemap;

    private bool isMoving;
    private Vector3Int currentCell;
    private Vector3 targetCenter;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Kinematic;
        rb.freezeRotation = true;
        GetComponent<Collider2D>().isTrigger = true;
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
        if (currentField == null || tilemap == null) return;

        // 1) Continue any in-progress move
        if (isMoving)
        {
            StepTowardCenter();
            return;
        }

        // 2) Sample flow-field
        Vector2 dir = currentField.GetDirection(transform.position);
        if (dir == Vector2.zero)
        {
            currentField = null; // done or unreachable
            return;
        }

        // 3) Quantize direction
        Vector2Int q = new Vector2Int(
            Mathf.RoundToInt(dir.x),
            Mathf.RoundToInt(dir.y)
        );
        if (q == Vector2Int.zero) return;

        // 4) Compute next cell
        currentCell = tilemap.WorldToCell(transform.position);
        Vector3Int desiredCell = new Vector3Int(
            currentCell.x + q.x,
            currentCell.y + q.y,
            currentCell.z
        );

        // 5) Try to step into desiredCell
        if (TryMoveToCell(desiredCell))
            return;

        // 6) If blocked, attempt a local detour
        Vector3Int detourCell;
        if (ComputeDetourCell(currentCell, q, out detourCell))
        {
            // detour found
            BeginMoveTo(detourCell);
        }
        // else no detour → wait here
    }

    private bool TryMoveToCell(Vector3Int cell)
    {
        Vector3 center = tilemap.GetCellCenterWorld(cell);
        center.z = transform.position.z;
        if (Physics2D.OverlapPoint(center) != null)
            return false;

        BeginMoveTo(cell);
        return true;
    }

    private void BeginMoveTo(Vector3Int cell)
    {
        targetCenter = tilemap.GetCellCenterWorld(cell);
        targetCenter.z = transform.position.z;
        isMoving = true;
    }

    private bool ComputeDetourCell(Vector3Int origin, Vector2Int q, out Vector3Int detour)
    {
        // diagonal move? try cardinals first
        if (q.x != 0 && q.y != 0)
        {
            var c1 = new Vector3Int(origin.x + q.x, origin.y, origin.z);
            if (TryCellFree(c1)) { detour = c1; return true; }
            var c2 = new Vector3Int(origin.x, origin.y + q.y, origin.z);
            if (TryCellFree(c2)) { detour = c2; return true; }
        }
        else
        {
            // cardinal move? try both diagonals
            if (q.x != 0)
            {
                var d1 = new Vector3Int(origin.x + q.x, origin.y + 1, origin.z);
                if (TryCellFree(d1)) { detour = d1; return true; }
                var d2 = new Vector3Int(origin.x + q.x, origin.y - 1, origin.z);
                if (TryCellFree(d2)) { detour = d2; return true; }
            }
            else // q.y != 0
            {
                var d1 = new Vector3Int(origin.x + 1, origin.y + q.y, origin.z);
                if (TryCellFree(d1)) { detour = d1; return true; }
                var d2 = new Vector3Int(origin.x - 1, origin.y + q.y, origin.z);
                if (TryCellFree(d2)) { detour = d2; return true; }
            }
        }

        detour = origin;
        return false;
    }

    private bool TryCellFree(Vector3Int cell)
    {
        Vector3 center = tilemap.GetCellCenterWorld(cell);
        center.z = transform.position.z;
        return Physics2D.OverlapPoint(center) == null;
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
        }
        else
        {
            rb.MovePosition(pos + toC.normalized * step);
        }
    }
}
