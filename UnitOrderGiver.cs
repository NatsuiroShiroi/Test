// UnitOrderGiver.cs
using UnityEngine;
using UnityEngine.Tilemaps;

public class UnitOrderGiver : MonoBehaviour
{
    [Tooltip("The Tilemap to use for pathfinding")]
    public Tilemap tilemap;

    private FlowField flowField = new FlowField();

    void Update()
    {
        if (!Input.GetMouseButtonDown(1)) return;

        Vector3 wp = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        wp.z = 0f;
        Vector3Int targetCell = tilemap.WorldToCell(wp);

        flowField.Generate(tilemap, targetCell);

        foreach (var sel in UnitSelector.GetSelectedUnits())
        {
            var mover = sel.GetComponent<UnitMover>();
            if (mover != null)
                mover.ApplyFlowField(flowField);
        }
    }
}
