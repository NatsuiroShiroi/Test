using System.Collections.Generic;
using UnityEngine;

public class UnitSelector : MonoBehaviour
{
    // Only UnitMover instances can be selected
    public static List<UnitMover> SelectedUnits = new List<UnitMover>();

    private Texture2D whiteTexture;
    private Vector3 dragStart;
    private bool isDragging;
    private bool dragCleared;

    void OnEnable()
    {
        whiteTexture = new Texture2D(1, 1);
        whiteTexture.SetPixel(0, 0, Color.white);
        whiteTexture.Apply();
    }

    void Update()
    {
        // Begin drag or click
        if (Input.GetMouseButtonDown(0))
        {
            dragStart   = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            dragStart.z = 0f;
            isDragging  = true;
            dragCleared = false;
        }

        // End drag or click
        if (Input.GetMouseButtonUp(0) && isDragging)
        {
            Vector3 dragEnd   = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            dragEnd.z         = 0f;
            bool dragSelect   = Vector3.Distance(dragStart, dragEnd) >= 0.1f;
            bool shiftHeld    = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);

            // Clear old selection on marquee-drag if not Shift-held
            if (dragSelect && !dragCleared && !shiftHeld)
            {
                foreach (var u in SelectedUnits)
                    u.IsSelected = false;
                SelectedUnits.Clear();
                dragCleared = true;
            }

            if (!dragSelect)
            {
                // Single click
                var hit = Physics2D.OverlapPoint(dragEnd);
                if (hit != null)
                    TrySelect(hit.GetComponent<UnitMover>(), shiftHeld);
            }
            else
            {
                // Marquee-select all UnitMovers in the rectangle
                var min = new Vector3(
                    Mathf.Min(dragStart.x, dragEnd.x),
                    Mathf.Min(dragStart.y, dragEnd.y));
                var max = new Vector3(
                    Mathf.Max(dragStart.x, dragEnd.x),
                    Mathf.Max(dragStart.y, dragEnd.y));

                foreach (var u in FindObjectsOfType<UnitMover>())
                {
                    var p = u.transform.position;
                    if (p.x >= min.x && p.x <= max.x &&
                        p.y >= min.y && p.y <= max.y)
                    {
                        TrySelect(u, shiftHeld: true);
                    }
                }
            }

            isDragging = false;
        }
    }

    void OnGUI()
    {
        float H = Screen.height, T = 2f;

        // Draw drag rectangle border
        if (isDragging)
        {
            var cam = Camera.main;
            var s = cam.WorldToScreenPoint(dragStart);
            var e = cam.WorldToScreenPoint(cam.ScreenToWorldPoint(Input.mousePosition));
            e.z = 0f;

            float x = Mathf.Min(s.x, e.x);
            float y = H - Mathf.Max(s.y, e.y);
            float w = Mathf.Abs(e.x - s.x);
            float h = Mathf.Abs(e.y - s.y);

            GUI.DrawTexture(new Rect(x,     y,     w, T), whiteTexture);
            GUI.DrawTexture(new Rect(x,     y + h - T, w, T), whiteTexture);
            GUI.DrawTexture(new Rect(x,     y,     T, h), whiteTexture);
            GUI.DrawTexture(new Rect(x + w - T, y, T, h), whiteTexture);
        }

        // Draw white border around selected sprites
        foreach (var u in SelectedUnits)
        {
            var sr = u.GetComponent<SpriteRenderer>();
            var b  = sr.bounds;
            var cam = Camera.main;

            var bl = cam.WorldToScreenPoint(new Vector3(b.min.x, b.min.y));
            var tr = cam.WorldToScreenPoint(new Vector3(b.max.x, b.max.y));

            float x = bl.x;
            float y = H - tr.y;
            float w = tr.x - bl.x;
            float h = tr.y - bl.y;

            GUI.DrawTexture(new Rect(x,     y,     w, T), whiteTexture);
            GUI.DrawTexture(new Rect(x,     y + h - T, w, T), whiteTexture);
            GUI.DrawTexture(new Rect(x,     y,     T, h), whiteTexture);
            GUI.DrawTexture(new Rect(x + w - T, y, T, h), whiteTexture);
        }
    }

    private void TrySelect(UnitMover u, bool shiftHeld)
    {
        if (u == null) return;

        if (!u.IsSelected)
        {
            if (!shiftHeld)
            {
                // clear previous selection
                foreach (var other in SelectedUnits)
                    other.IsSelected = false;
                SelectedUnits.Clear();
            }
            u.IsSelected = true;
            SelectedUnits.Add(u);
        }
        else if (!shiftHeld)
        {
            // deselect on click if not Shift-held
            u.IsSelected = false;
            SelectedUnits.Remove(u);
        }
    }
}
