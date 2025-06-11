using UnityEngine;

/// <summary>
/// Generates a global flow field on right-click using a SpriteRenderer bounds grid.
/// </summary>
public class UnitOrderGiver : MonoBehaviour
{
    [Tooltip("Background SpriteRenderer defining play area")] public SpriteRenderer background;
    [Tooltip("Size of each grid cell in world units")] public float cellSize = 1f;

    private FlowField flowField = new FlowField();

    void Update()
    {
        if (Input.GetMouseButtonDown(1))
        {
            Vector3 wp = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            wp.z = 0f;

            Bounds b = background.bounds;
            flowField.Generate(b, cellSize, wp);

            foreach (var sel in UnitSelector.GetSelectedUnits())
                sel.GetComponent<UnitMover>().ApplyFlowField(flowField);
        }
    }
}
