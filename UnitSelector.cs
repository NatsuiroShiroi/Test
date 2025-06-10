using System.Collections.Generic;
using UnityEngine;

// Allows click-and-drag selection of units, drawing selection rectangles and borders

[RequireComponent(typeof(Collider2D))]
public class UnitSelector : MonoBehaviour
{
    private static List<UnitSelector> selectedUnitsGlobal = new List<UnitSelector>();
    private static bool dragCleared = false;

    private bool isSelected = false;
    private Vector3 dragStart;
    private bool isDragging = false;
    private Texture2D whiteTexture;

    void OnEnable()
    {
        whiteTexture = new Texture2D(1, 1);
        whiteTexture.SetPixel(0, 0, Color.white);
        whiteTexture.Apply();
    }

    void Update()
    {
        HandleSelection();
    }

    void OnGUI()
    {
        // Draw selection rectangle border when dragging
        if (isDragging)
        {
            Vector3 startScreen = Camera.main.WorldToScreenPoint(dragStart);
            Vector3 currentWorld = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            currentWorld.z = 0f;
            Vector3 currentScreen = Camera.main.WorldToScreenPoint(currentWorld);

            float x = Mathf.Min(startScreen.x, currentScreen.x);
            float y = Screen.height - Mathf.Max(startScreen.y, currentScreen.y);
            float width = Mathf.Abs(startScreen.x - currentScreen.x);
            float height = Mathf.Abs(startScreen.y - currentScreen.y);
            float thickness = 2f;

            // Draw marquee
            GUI.DrawTexture(new Rect(x, y, width, thickness), whiteTexture);
            GUI.DrawTexture(new Rect(x, y + height - thickness, width, thickness), whiteTexture);
            GUI.DrawTexture(new Rect(x, y, thickness, height), whiteTexture);
            GUI.DrawTexture(new Rect(x + width - thickness, y, thickness, height), whiteTexture);

            // Underline sprite borders of units in drag area
            Vector3 dragEndWorld = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            dragEndWorld.z = 0f;
            Vector3 boxMin = new Vector3(
                Mathf.Min(dragStart.x, dragEndWorld.x),
                Mathf.Min(dragStart.y, dragEndWorld.y), 0f);
            Vector3 boxMax = new Vector3(
                Mathf.Max(dragStart.x, dragEndWorld.x),
                Mathf.Max(dragStart.y, dragEndWorld.y), 0f);

            Vector3 pos = transform.position;
            if (pos.x >= boxMin.x && pos.x <= boxMax.x && pos.y >= boxMin.y && pos.y <= boxMax.y)
            {
                var sr = GetComponent<SpriteRenderer>();
                Bounds b = sr.bounds;
                Vector3 worldBL = new Vector3(b.min.x, b.min.y, 0f);
                Vector3 worldTR = new Vector3(b.max.x, b.max.y, 0f);
                Vector3 screenBL = Camera.main.WorldToScreenPoint(worldBL);
                Vector3 screenTR = Camera.main.WorldToScreenPoint(worldTR);
                float sx = screenBL.x;
                float sy = Screen.height - screenTR.y;
                float sw = screenTR.x - screenBL.x;
                float sh = screenTR.y - screenBL.y;
                float t = 2f;

                GUI.DrawTexture(new Rect(sx, sy, sw, t), whiteTexture);
                GUI.DrawTexture(new Rect(sx, sy + sh - t, sw, t), whiteTexture);
                GUI.DrawTexture(new Rect(sx, sy, t, sh), whiteTexture);
                GUI.DrawTexture(new Rect(sx + sw - t, sy, t, sh), whiteTexture);
            }
        }

        // Draw border around individually selected unit
        if (isSelected)
        {
            var sr = GetComponent<SpriteRenderer>();
            Bounds b = sr.bounds;
            Vector3 worldBL = new Vector3(b.min.x, b.min.y, 0f);
            Vector3 worldTR = new Vector3(b.max.x, b.max.y, 0f);
            Vector3 screenBL = Camera.main.WorldToScreenPoint(worldBL);
            Vector3 screenTR = Camera.main.WorldToScreenPoint(worldTR);

            float x = screenBL.x;
            float y = Screen.height - screenTR.y;
            float width = screenTR.x - screenBL.x;
            float height = screenTR.y - screenBL.y;
            float thickness = 2f;

            GUI.DrawTexture(new Rect(x, y, width, thickness), whiteTexture);
            GUI.DrawTexture(new Rect(x, y + height - thickness, width, thickness), whiteTexture);
            GUI.DrawTexture(new Rect(x, y, thickness, height), whiteTexture);
            GUI.DrawTexture(new Rect(x + width - thickness, y, thickness, height), whiteTexture);
        }
    }

    private void HandleSelection()
    {
        if (Input.GetMouseButtonDown(0))
        {
            dragStart = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            dragStart.z = 0f;
            isDragging = true;
            dragCleared = false;
        }

        if (Input.GetMouseButtonUp(0) && isDragging)
        {
            Vector3 dragEnd = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            dragEnd.z = 0f;
            isDragging = false;

            bool dragSelect = Vector3.Distance(dragStart, dragEnd) >= 0.1f;
            bool shift = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);

            if (dragSelect && !dragCleared && !shift)
            {
                foreach (var u in selectedUnitsGlobal) u.isSelected = false;
                selectedUnitsGlobal.Clear();
                dragCleared = true;
            }

            bool newlySelected;
            if (!dragSelect)
            {
                var hit = Physics2D.OverlapPoint(dragEnd);
                newlySelected = hit != null && hit.gameObject == gameObject;
            }
            else
            {
                var min = new Vector3(
                    Mathf.Min(dragStart.x, dragEnd.x),
                    Mathf.Min(dragStart.y, dragEnd.y));
                var max = new Vector3(
                    Mathf.Max(dragStart.x, dragEnd.x),
                    Mathf.Max(dragStart.y, dragEnd.y));
                var p = transform.position;
                newlySelected = p.x >= min.x && p.x <= max.x && p.y >= min.y && p.y <= max.y;
            }

            if (newlySelected && !isSelected)
            {
                selectedUnitsGlobal.Add(this);
                isSelected = true;
            }
            else if (!newlySelected && isSelected && !shift)
            {
                selectedUnitsGlobal.Remove(this);
                isSelected = false;
            }
        }
    }

    public static List<UnitSelector> GetSelectedUnits() => selectedUnitsGlobal;
}
