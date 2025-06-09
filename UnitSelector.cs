using System.Collections.Generic;
using UnityEngine;

public class UnitSelector : MonoBehaviour
{
    public static List<UnitMover> SelectedUnits = new List<UnitMover>();

    private Vector3 dragStart;
    private bool isDragging, dragCleared;
    private Texture2D whiteTexture;

    void OnEnable()
    {
        whiteTexture = new Texture2D(1, 1);
        whiteTexture.SetPixel(0, 0, Color.white);
        whiteTexture.Apply();
    }

    void Update()
    {
        // Begin drag
        if (Input.GetMouseButtonDown(0))
        {
            dragStart = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            dragStart.z = 0;
            isDragging = true;
            dragCleared = false;
        }

        // End drag / click
        if (Input.GetMouseButtonUp(0) && isDragging)
        {
            Vector3 dragEnd = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            dragEnd.z = 0;

            bool dragSelect = Vector3.Distance(dragStart, dragEnd) >= 0.1f;
            bool shift = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);

            // clear old selection on marquee if Shift not held
            if (dragSelect && !dragCleared && !shift)
            {
                foreach (var u in SelectedUnits) u.IsSelected = false;
                SelectedUnits.Clear();
                dragCleared = true;
            }

            if (!dragSelect)
            {
                // single‐click
                var hit = Physics2D.OverlapPoint(dragEnd);
                if (hit != null)
                    TrySelect(hit.GetComponent<UnitMover>(), shift);
            }
            else
            {
                // marquee‐select
                var min = new Vector3(Mathf.Min(dragStart.x, dragEnd.x),
                                      Mathf.Min(dragStart.y, dragEnd.y));
                var max = new Vector3(Mathf.Max(dragStart.x, dragEnd.x),
                                      Mathf.Max(dragStart.y, dragEnd.y));

                foreach (var u in FindObjectsOfType<UnitMover>())
                {
                    var p = u.transform.position;
                    if (p.x >= min.x && p.x <= max.x &&
                        p.y >= min.y && p.y <= max.y)
                    {
                        TrySelect(u, shift);
                    }
                }
            }

            isDragging = false;
        }
    }

    void OnGUI()
    {
        if (isDragging)
        {
            // draw rubber‐band rectangle...
        }

        // draw white outline around all SelectedUnits...
    }

    private void TrySelect(UnitMover u, bool additive)
    {
        if (u == null) return;
        if (!u.IsSelected)
        {
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