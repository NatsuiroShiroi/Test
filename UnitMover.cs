using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D), typeof(Collider2D))]
public class UnitMover : MonoBehaviour
{
    public float moveSpeed = 5f;
    public float separationWeight = 1.5f;
    // separationRadius no longer needed manually, we use occupancy avoidance

    private Rigidbody2D rb;
    private FlowField currentField;

    // Grid info from flow field
    private Vector3 gridOrigin;
    private float gridCellSize;

    // Static list for occupancy checks
    private static List<UnitMover> allMovers = new List<UnitMover>();

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Kinematic;
        rb.freezeRotation = true;
        rb.gravityScale = 0f;

        var col2d = GetComponent<Collider2D>();
        col2d.isTrigger = true;

        // register for occupancy
        allMovers.Add(this);
    }

    void OnDestroy()
    {
        allMovers.Remove(this);
    }

    /// <summary>
    /// Receive the flow field and cache grid parameters
    /// </summary>
    public void ApplyFlowField(FlowField field)
    {
        currentField = field;
        gridOrigin = field.Origin;
        gridCellSize = field.CellSize;
    }

    void FixedUpdate()
    {
        if (currentField == null)
            return;

        Vector2 desire = currentField.GetDirection(transform.position);

        // If at goal cell, center then clear
        if (desire == Vector2.zero)
        {
            // smooth center approach omitted for brevity
            currentField = null;
            return;
        }

        float step = moveSpeed * Time.fixedDeltaTime;
        Vector2 moveVector = desire * step;
        Vector2 currentPos = rb.position;
        Vector2 nextPos = currentPos + moveVector;

        // occupancy check: will nextPos enter a cell already occupied?
        int tx = Mathf.FloorToInt((nextPos.x - gridOrigin.x) / gridCellSize);
        int ty = Mathf.FloorToInt((nextPos.y - gridOrigin.y) / gridCellSize);
        bool cellOccupied = false;
        foreach (var m in allMovers)
        {
            if (m == this) continue;
            Vector3 mp = m.transform.position;
            int mx = Mathf.FloorToInt((mp.x - gridOrigin.x) / gridCellSize);
            int my = Mathf.FloorToInt((mp.y - gridOrigin.y) / gridCellSize);
            if (mx == tx && my == ty)
            {
                cellOccupied = true;
                break;
            }
        }

        if (cellOccupied)
        {
            // avoid by moving perpendicular to desire
            Vector2 perp = new Vector2(-desire.y, desire.x).normalized;
            rb.MovePosition(currentPos + perp * step);
            return;
        }

        // no occupancy, proceed with separation steering
        Vector2 sep = Vector2.zero;
        var hits = Physics2D.OverlapCircleAll(transform.position, gridCellSize * 0.5f);
        foreach (var h in hits)
        {
            if (h.gameObject == gameObject) continue;
            var other = h.GetComponent<UnitMover>();
            if (other != null)
            {
                Vector2 away = (Vector2)(transform.position - other.transform.position);
                sep += away.normalized / away.sqrMagnitude;
            }
        }
        // final move
        Vector2 finalMove = (desire + sep * separationWeight).normalized * step;
        rb.MovePosition(currentPos + finalMove);
    }
}
