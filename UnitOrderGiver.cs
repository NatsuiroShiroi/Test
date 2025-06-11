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
        // 1) Only on right-click
        if (!Input.GetMouseButtonDown(1)) return;

        // 2) Mouse → world → cell
        Vector3 wp = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        wp.z = 0f;
        Vector3Int targetCell = tilemap.WorldToCell(wp);

        // 3) Generate over the *entire* tilemap
        flowField.Generate(tilemap, targetCell);

        // 4) Tell every selected unit to use it
        foreach (var sel in UnitSelector.GetSelectedUnits())
        {
            var mover = sel.GetComponent<UnitMover>();
            if (mover != null)
                mover.ApplyFlowField(flowField);
        }
    }
}
