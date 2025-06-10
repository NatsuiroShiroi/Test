using UnityEngine;
using UnityEngine.Tilemaps;

[RequireComponent(typeof(Rigidbody2D), typeof(Collider2D))]
public class UnitMover : MonoBehaviour
{
    public float moveSpeed = 5f;
    public float separationWeight = 1.5f;
    public float separationRadius = 0.5f;

    private Rigidbody2D rb;
    private FlowField currentField;
    private Vector2 desiredDir;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.freezeRotation = true;
        rb.gravityScale = 0f;
    }

    public void ApplyFlowField(FlowField field)
    {
        currentField = field;
    }

    void FixedUpdate()
    {
        if (currentField == null) return;

        // 1) Global direction from flow field
        desiredDir = currentField.GetDirection(transform.position);

        // 2) Local separation
        Vector2 sep = Vector2.zero;
        var hits = Physics2D.OverlapCircleAll(transform.position, separationRadius);
        foreach (var hit in hits)
        {
            if (hit.gameObject == gameObject) continue;
            if (hit.GetComponent<UnitMover>() != null)
            {
                Vector2 away = (Vector2)(transform.position - hit.transform.position);
                sep += away.normalized / away.magnitude;
            }
        }

        Vector2 move = (desiredDir + sep * separationWeight).normalized * moveSpeed * Time.fixedDeltaTime;
        rb.MovePosition(rb.position + move);
    }
}
