using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

[RequireComponent(typeof(Rigidbody2D), typeof(Collider2D))]
public class Movement : MonoBehaviour
{
    [Tooltip("Units per second for your character")]
    public float moveSpeed = 5f;

    [Tooltip("Units per second for the camera")]
    public float cameraSpeed = 5f;

    [Tooltip("Your background SpriteRenderer (optional if only one in scene)")]
    public SpriteRenderer background;

    [Tooltip("The Tilemap to use for pathfinding")]
    public Tilemap tilemap;

    private Rigidbody2D rb;
    private Vector3 minBounds;
    private Vector3 maxBounds;
    private Vector2 spriteExtents;

    private Vector3 destination;
    private bool hasDodged = false;
    private static List<Movement> selectedUnitsGlobal = new List<Movement>();
    private static bool hasIssuedMoveCommand = false;
    private static bool dragCleared = false;
    private List<Vector3> path;
    private int targetIndex;

    // Selection state
    private bool isSelected = false;
    private Vector3 dragStart;
    private bool isDragging = false;

    // For drawing the selection rectangle and sprite outline
    private Texture2D whiteTexture;

    void OnEnable()
    {
        // Create a 1x1 white texture for drawing
        whiteTexture = new Texture2D(1, 1);
        whiteTexture.SetPixel(0, 0, Color.white);
        whiteTexture.Apply();
    }

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.freezeRotation = true;
        rb.gravityScale = 0f;

        var col2d = GetComponent<Collider2D>();
        col2d.sharedMaterial = new PhysicsMaterial2D { friction = 0f, bounciness = 0f };

        background = background ?? Object.FindFirstObjectByType<SpriteRenderer>();
        tilemap = tilemap ?? Object.FindFirstObjectByType<Tilemap>();

        if (background == null || tilemap == null)
        {
            Debug.LogError("No background SpriteRenderer or Tilemap found. Assign in Inspector.");
            enabled = false;
            return;
        }

        var bg = background.bounds;
        minBounds = new Vector3(bg.min.x, bg.min.y, 0f);
        maxBounds = new Vector3(bg.max.x, bg.max.y, 0f);

        spriteExtents = GetComponent<SpriteRenderer>().bounds.extents;

        path = new List<Vector3>();
        targetIndex = 0;
        destination = transform.position;   // ← initialize destination
    }

    void Update()
    {
        HandleSelection();
        HandleClickToMove();
        HandleCameraMove();
    }

    void FixedUpdate()
    {
        HandlePathMovement();
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

            // Top
            GUI.DrawTexture(new Rect(x, y, width, thickness), whiteTexture);
            // Bottom
            GUI.DrawTexture(new Rect(x, y + height - thickness, width, thickness), whiteTexture);
            // Left
            GUI.DrawTexture(new Rect(x, y, thickness, height), whiteTexture);
            // Right
            GUI.DrawTexture(new Rect(x + width - thickness, y, thickness, height), whiteTexture);

            // Also underline sprite borders for units within the drag area
            // Determine drag world rectangle
            Vector3 dragEndWorld = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            dragEndWorld.z = 0f;
            Vector3 boxMin = new Vector3(Mathf.Min(dragStart.x, dragEndWorld.x), Mathf.Min(dragStart.y, dragEndWorld.y), 0f);
            Vector3 boxMax = new Vector3(Mathf.Max(dragStart.x, dragEndWorld.x), Mathf.Max(dragStart.y, dragEndWorld.y), 0f);
            // If this unit's position is inside the drag box
            Vector3 pos = transform.position;
            if (pos.x >= boxMin.x && pos.x <= boxMax.x && pos.y >= boxMin.y && pos.y <= boxMax.y)
            {
                // Draw border around this sprite
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

        // Draw white border around selected sprite
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
            dragCleared = false;  // reset clear-flag each new drag
        }

        if (Input.GetMouseButtonUp(0) && isDragging)
        {
            Vector3 dragEnd = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            dragEnd.z = 0f;
            isDragging = false;

            bool dragSelect = Vector3.Distance(dragStart, dragEnd) >= 0.1f;
            bool shift = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);

            // only clear on marquee‐drag if Shift is NOT held
            if (dragSelect && !dragCleared && !shift)
            {
                foreach (var u in selectedUnitsGlobal)
                    u.isSelected = false;
                selectedUnitsGlobal.Clear();
                dragCleared = true;
            }

            // figure out if this unit falls under the click or box
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
                    Mathf.Min(dragStart.y, dragEnd.y)
                );
                var max = new Vector3(
                    Mathf.Max(dragStart.x, dragEnd.x),
                    Mathf.Max(dragStart.y, dragEnd.y)
                );
                var p = transform.position;
                newlySelected = p.x >= min.x && p.x <= max.x
                             && p.y >= min.y && p.y <= max.y;
            }

            // update selection
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

    private void HandleClickToMove()
    {
        if (!Input.GetMouseButtonDown(1))
            hasIssuedMoveCommand = false;

        if (!hasIssuedMoveCommand && Input.GetMouseButtonDown(1))
        {
            hasIssuedMoveCommand = true;
            int count = selectedUnitsGlobal.Count;
            if (count == 0) return;

            var wp = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            wp.z = 0f;
            wp.x = Mathf.Clamp(wp.x, minBounds.x + spriteExtents.x, maxBounds.x - spriteExtents.x);
            wp.y = Mathf.Clamp(wp.y, minBounds.y + spriteExtents.y, maxBounds.y - spriteExtents.y);

            float spread = Mathf.Max(tilemap.cellSize.x, tilemap.cellSize.y) * 0.6f;

            for (int i = 0; i < count; i++)
            {
                var unit = selectedUnitsGlobal[i];

                float angle = 2 * Mathf.PI * i / count;
                Vector3 offset = new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0) * spread;
                Vector3 targetWorld = wp + offset;
                targetWorld.x = Mathf.Clamp(targetWorld.x,
                    minBounds.x + unit.spriteExtents.x,
                    maxBounds.x - unit.spriteExtents.x);
                targetWorld.y = Mathf.Clamp(targetWorld.y,
                    minBounds.y + unit.spriteExtents.y,
                    maxBounds.y - unit.spriteExtents.y);

                var startCell = tilemap.WorldToCell(unit.transform.position);
                var goalCell = tilemap.WorldToCell(targetWorld);

                var cellPath = FindPath(startCell, goalCell);
                unit.path.Clear();
                foreach (var cell in cellPath)
                {
                    var p = tilemap.GetCellCenterWorld(cell);
                    p.z = 0f;
                    unit.path.Add(p);
                }

                unit.targetIndex = 0;
                unit.hasDodged = false;       // reset dodge
                unit.destination = targetWorld; // ← remember final destination
            }
        }
    }

    private void HandlePathMovement()
    {
        // If we’ve run out of waypoints (or never had any), try to rebuild
        if (path == null || targetIndex >= path.Count)
        {
            // If we’re not yet at our destination, rebuild the path…
            if (Vector3.Distance(transform.position, destination) > 0.01f)
            {
                var start = tilemap.WorldToCell(transform.position);
                var goal = tilemap.WorldToCell(destination);
                var cellPath = FindPath(start, goal);   // returns List<Vector3Int>

                path.Clear();                           // our List<Vector3>
                foreach (var cell in cellPath)
                {
                    var world = tilemap.GetCellCenterWorld(cell);
                    world.z = 0f;
                    path.Add(world);
                }

                targetIndex = 0;
                hasDodged = false;  // allow dodge again
            }
            else
            {
                // We’re effectively at the destination
                return;
            }
        }

        // *** NEW GUARD: if there still are no waypoints, just wait. ***
        if (path.Count == 0)
            return;

        // Now safe to access path[targetIndex]
        Vector3 targetPos = path[targetIndex];
        Vector3Int tCell = tilemap.WorldToCell(targetPos);

        if (IsBlocked(tCell))
        {
            var blocker = FindBlockingMover(tCell);
            if (blocker != null)
            {
                Movement mover = (Random.value < .5f) ? this : blocker;
                Movement other = (mover == this) ? blocker : this;
                StepAside(mover, other);
            }
            return;
        }

        // Normal movement toward the next waypoint
        Vector2 curr = rb.position;
        Vector2 toT = (Vector2)targetPos - curr;
        float step = moveSpeed * Time.fixedDeltaTime;
        if (toT.magnitude <= step)
        {
            rb.MovePosition(targetPos);
            targetIndex++;
        }
        else
        {
            rb.MovePosition(curr + toT.normalized * step);
        }
    }

    private Movement FindBlockingMover(Vector3Int cell)
    {
        Vector3 worldC = tilemap.CellToWorld(cell) + (Vector3)tilemap.cellSize * 0.5f;
        foreach (var hit in Physics2D.OverlapBoxAll(worldC, spriteExtents * 2f, 0f))
        {
            if (!hit.isTrigger && hit.gameObject != gameObject)
            {
                var m = hit.GetComponent<Movement>();
                if (m != null && m.path != null && m.targetIndex < m.path.Count)
                    return m;
            }
        }
        return null;
    }

    private void StepAside(Movement m, Movement other)
    {
        if (m.hasDodged) return;

        var tc = tilemap.WorldToCell(m.path[m.targetIndex]);
        var cc = tilemap.WorldToCell(m.transform.position);
        var dir = new Vector3Int(tc.x - cc.x, tc.y - cc.y, 0);
        var perp = new Vector3Int(-dir.y, dir.x, 0);
        var oc = tilemap.WorldToCell(other.transform.position);
        var toB = new Vector3Int(oc.x - cc.x, oc.y - cc.y, 0);

        if (perp.x * toB.x + perp.y * toB.y > 0)
            perp = new Vector3Int(-perp.x, -perp.y, 0);

        var diag = new Vector3Int(dir.x + perp.x, dir.y + perp.y, 0);
        var nc = cc + diag;
        Vector3 worldNC = tilemap.CellToWorld(nc) + (Vector3)tilemap.cellSize * 0.5f;

        if (worldNC.x >= minBounds.x + spriteExtents.x &&
            worldNC.x <= maxBounds.x - spriteExtents.x &&
            worldNC.y >= minBounds.y + spriteExtents.y &&
            worldNC.y <= maxBounds.y - spriteExtents.y &&
            !IsBlocked(nc))
        {
            worldNC.z = 0f;
            m.path.Insert(m.targetIndex, worldNC);
            m.hasDodged = true;
        }
    }

    private void HandleCameraMove()
    {
        float camMx = 0f, camMy = 0f;
        if (Input.GetKey(KeyCode.W)) camMy += 1f;
        if (Input.GetKey(KeyCode.S)) camMy -= 1f;
        if (Input.GetKey(KeyCode.A)) camMx -= 1f;
        if (Input.GetKey(KeyCode.D)) camMx += 1f;

        var camMove = new Vector3(camMx, camMy, 0f);
        if (camMove.sqrMagnitude > 1f) camMove.Normalize();
        Camera.main.transform.Translate(camMove * cameraSpeed * Time.deltaTime, Space.World);
    }

    // A* pathfinding with obstacle avoidance and strict corner-cut prevention
    List<Vector3Int> FindPath(Vector3Int start, Vector3Int goal)
    {
        var openSet = new List<Node> { new Node(start, 0, Heuristic(start, goal), null) };
        var closedSet = new HashSet<Vector3Int>();

        while (openSet.Count > 0)
        {
            openSet.Sort((a, b) => a.fCost.CompareTo(b.fCost));
            Node current = openSet[0];
            openSet.RemoveAt(0);

            if (current.position == goal)
                return ReconstructPath(current);

            closedSet.Add(current.position);

            foreach (var dir in directions)
            {
                Vector3Int neighbor = current.position + dir;
                if (closedSet.Contains(neighbor))
                    continue;

                // 1) world-bounds check
                Vector3 center = tilemap.CellToWorld(neighbor) + (Vector3)tilemap.cellSize * 0.5f;
                if (center.x < minBounds.x + spriteExtents.x || center.x > maxBounds.x - spriteExtents.x ||
                    center.y < minBounds.y + spriteExtents.y || center.y > maxBounds.y - spriteExtents.y)
                    continue;

                // 2) corner-cut prevention
                if (dir.x != 0 && dir.y != 0)
                {
                    var side1 = current.position + new Vector3Int(dir.x, 0, 0);
                    var side2 = current.position + new Vector3Int(0, dir.y, 0);
                    if (IsBlocked(side1) || IsBlocked(side2))
                        continue;
                }

                // 3) block any cell your sprite overlaps
                if (IsBlocked(neighbor))
                    continue;

                float cost = (Mathf.Abs(dir.x) + Mathf.Abs(dir.y) == 2) ? 1.4142f : 1f;
                float gNew = current.gCost + cost;

                Node existing = openSet.Find(n => n.position == neighbor);
                if (existing == null)
                    openSet.Add(new Node(neighbor, gNew, Heuristic(neighbor, goal), current));
                else if (gNew < existing.gCost)
                {
                    existing.gCost = gNew;
                    existing.parent = current;
                }
            }
        }

        return new List<Vector3Int>();
    }

    // Now uses your character's extents to decide if a tile is truly occupiable
    bool IsBlocked(Vector3Int cell)
    {
        Vector3 center = tilemap.CellToWorld(cell) + (Vector3)tilemap.cellSize * 0.5f;
        // use the sprite's full width/height (slightly shrunk to avoid float issues)
        Vector2 checkSize = spriteExtents * 2f * 0.95f;
        Collider2D[] hits = Physics2D.OverlapBoxAll(center, checkSize, 0f);
        foreach (var hit in hits)
            if (!hit.isTrigger && hit.gameObject != gameObject)
                return true;
        return false;
    }

    List<Vector3Int> ReconstructPath(Node node)
    {
        var result = new List<Vector3Int>();
        while (node != null)
        {
            result.Add(node.position);
            node = node.parent;
        }
        result.Reverse();
        return result;
    }

    float Heuristic(Vector3Int a, Vector3Int b)
    {
        int dx = Mathf.Abs(a.x - b.x), dy = Mathf.Abs(a.y - b.y);
        return Mathf.Max(dx, dy);
    }

    static readonly Vector3Int[] directions = {
        new Vector3Int(1,  0, 0),  new Vector3Int(-1,  0, 0),
        new Vector3Int(0,  1, 0),  new Vector3Int( 0, -1, 0),
        new Vector3Int(1,  1, 0),  new Vector3Int( 1, -1, 0),
        new Vector3Int(-1, 1, 0),  new Vector3Int(-1,-1, 0)
    };

    class Node
    {
        public Vector3Int position;
        public float gCost, hCost;
        public float fCost => gCost + hCost;
        public Node parent;
        public Node(Vector3Int pos, float g, float h, Node p)
        {
            position = pos;
            gCost = g;
            hCost = h;
            parent = p;
        }
    }
}
