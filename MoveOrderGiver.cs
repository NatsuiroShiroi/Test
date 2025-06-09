using UnityEngine;
using UnityEngine.Tilemaps;

public class MoveOrderGiver : MonoBehaviour
{
    public Tilemap tilemap;
    public SpriteRenderer background;

    void Update()
    {
        if (Input.GetMouseButtonDown(1))
        {
            var sel = UnitSelector.SelectedUnits;
            if (sel.Count == 0) return;

            // click‐point in world
            Vector3 wp = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            wp.z = 0;

            // dispatch to each unit
            foreach (var u in sel)
                u.SetDestination(wp);
        }
    }
}