using System.Collections.Generic;
using UnityEngine;

public class UnitSelector : MonoBehaviour
{
    public static List<UnitMover> SelectedUnits = new List<UnitMover>();

    Texture2D whiteTexture;
    Vector3 dragStart;
    bool isDragging, dragCleared;

    void OnEnable()
    {
        whiteTexture = new Texture2D(1, 1);
        whiteTexture.SetPixel(0, 0, Color.white);
        whiteTexture.Apply();
    }

    void Update()
    {
        var cam = Camera.main;

        // start drag/click
        if (Input.GetMouseButtonDown(0))
        {
            dragStart = cam.ScreenToWorldPoint(Input.mousePosition);
            dragStart.z = 0f;
            isDragging = true;
            dragCleared = false;
        }

        // live‐marquee: sync selection to units under rectangle
        if (isDragging)
        {
            Vector3 cur = cam.ScreenToWorldPoint(Input.mousePosition);
            cur.z = 0f;
            bool shiftHeld = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);

            // only clear existing selection once if NOT holding Shift
            if (!dragCleared && !shiftHeld)
            {
                foreach (var u in SelectedUnits) u.IsSelected = false;
                SelectedUnits.Clear();
                dragCleared = true;
            }

            var min = new Vector3(Mathf.Min(dragStart.x, cur.x),
                                  Mathf.Min(dragStart.y, cur.y));
            var max = new Vector3(Mathf.Max(dragStart.x, cur.x),
                                  Mathf.Max(dragStart.y, cur.y));

            var allUnits = Object.FindObjectsByType<UnitMover>(FindObjectsSortMode.None);
            foreach (var u in allUnits)
            {
                var p = u.transform.position;
                bool inside = p.x >= min.x && p.x <= max.x && p.y >= min.y && p.y <= max.y;
                if (inside && !u.IsSelected)
                {
                    u.IsSelected = true;
                    SelectedUnits.Add(u);
                }
                else if (!inside && u.IsSelected && !shiftHeld)
                {
                    u.IsSelected = false;
                    SelectedUnits.Remove(u);
                }
            }
        }

        // end drag/click
        if (Input.GetMouseButtonUp(0) && isDragging)
        {
            Vector3 dragEnd = cam.ScreenToWorldPoint(Input.mousePosition);
            dragEnd.z = 0f;
            bool dragSel = Vector3.Distance(dragStart, dragEnd) >= 0.1f;
            bool shiftHeld = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);

            if (!dragSel)
            {
                var hit = Physics2D.OverlapPoint(dragEnd);
                if (hit != null)
                    TrySelect(hit.GetComponent<UnitMover>(), shiftHeld);
                else if (!shiftHeld)
                {
                    foreach (var u in SelectedUnits) u.IsSelected = false;
                    SelectedUnits.Clear();
                }
            }

            isDragging = false;
        }
    }

    void OnGUI()
    {
        float H = Screen.height, T = 2f;
        var cam = Camera.main;

        if (isDragging)
        {
            var s = cam.WorldToScreenPoint(dragStart);
            var e = cam.WorldToScreenPoint(cam.ScreenToWorldPoint(Input.mousePosition));
            e.z = 0f;

            float x = Mathf.Min(s.x, e.x);
            float y = H - Mathf.Max(s.y, e.y);
            float w = Mathf.Abs(e.x - s.x);
            float h = Mathf.Abs(e.y - s.y);

            GUI.DrawTexture(new Rect(x, y, w, T), whiteTexture);
            GUI.DrawTexture(new Rect(x, y + h - T, w, T), whiteTexture);
            GUI.DrawTexture(new Rect(x, y, T, h), whiteTexture);
            GUI.DrawTexture(new Rect(x + w - T, y, T, h), whiteTexture);
        }

        foreach (var u in SelectedUnits)
        {
            var b = u.GetComponent<SpriteRenderer>().bounds;
            var bl = cam.WorldToScreenPoint(b.min);
            var tr = cam.WorldToScreenPoint(b.max);

            float x = bl.x;
            float y = H - tr.y;
            float w = tr.x - bl.x;
            float h = tr.y - bl.y;

            GUI.DrawTexture(new Rect(x, y, w, T), whiteTexture);
            GUI.DrawTexture(new Rect(x, y + h - T, w, T), whiteTexture);
            GUI.DrawTexture(new Rect(x, y, T, h), whiteTexture);
            GUI.DrawTexture(new Rect(x + w - T, y, T, h), whiteTexture);
        }
    }

    void TrySelect(UnitMover u, bool additive)
    {
        if (u == null) return;
        if (!u.IsSelected)
        {
            if (!additive)
            {
                foreach (var o in SelectedUnits) o.IsSelected = false;
                SelectedUnits.Clear();
            }
            u.IsSelected = true;
            SelectedUnits.Add(u);
        }
        else if (!additive)
        {
            u.IsSelected = false;
            SelectedUnits.Remove(u);
        }
    }
}
