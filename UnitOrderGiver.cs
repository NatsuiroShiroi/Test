using UnityEngine;
using UnityEngine.Tilemaps;

/// <summary>
/// Generates a global flow field on right-click and broadcasts it to all units.
/// </summary>
public class UnitOrderGiver : MonoBehaviour
{
    [Tooltip("Tilemap used for pathfinding")] public Tilemap tilemap;
    private FlowField flowField = new FlowField();

    void Update()
    {
        if (Input.GetMouseButtonDown(1))
        {
            Vector3 wp = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            wp.z = 0f;
            Vector3Int targetCell = tilemap.WorldToCell(wp);
            flowField.Generate(tilemap, targetCell);

            // Broadcast to all movers
            foreach (var mover in UnitSelector.GetSelectedUnits())
            {
                var m = mover.GetComponent<UnitMover>();
                m.ApplyFlowField(flowField);
            }
        }
    }
}
