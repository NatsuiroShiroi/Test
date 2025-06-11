// UnitMover.cs
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D), typeof(Collider2D))]
public class UnitMover : MonoBehaviour
{
    [Tooltip("Units per second")]
    public float moveSpeed = 5f;

    private Rigidbody2D rb;
    private FlowField currentField;

    // for debugging: show what each mover sees
    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Kinematic;
        rb.freezeRotation = true;
        GetComponent<Collider2D>().isTrigger = true;
    }

    /// <summary>
    /// Receive the flow field and start moving.
    /// </summary>
    public void ApplyFlowField(FlowField field)
    {
        currentField = field;
        Debug.Log($"[UnitMover:{name}] Received new FlowField");
    }

    void FixedUpdate()
    {
        if (currentField == null) return;

        // Sample the flow field
        Vector2 desire = currentField.GetDirection(transform.position);
        Debug.Log($"[UnitMover:{name}] pos={transform.position}, desire={desire}");

        if (desire == Vector2.zero)
        {
            Debug.Log($"[UnitMover:{name}] zero desire → stopping");
            currentField = null;
            return;
        }

        // Move along desire
        float step = moveSpeed * Time.fixedDeltaTime;
        Vector2 next = rb.position + desire * step;
        rb.MovePosition(next);
    }
}
