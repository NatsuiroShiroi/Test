using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public static class Pathfinder
{
    static readonly Vector3Int[] _dirs = {
        new(1,0,0),new(-1,0,0),new(0,1,0),new(0,-1,0),
        new(1,1,0),new(1,-1,0),new(-1,1,0),new(-1,-1,0)
    };

    public static List<Vector3Int> FindPath(
        Tilemap map,
        Vector3Int start,
        Vector3Int goal,
        Vector2 spriteExtents,
        Vector3 minBounds,
        Vector3 maxBounds
    )
    {
        var open = new List<Node> { new(start, 0, Heur(start, goal), null) };
        var closed = new HashSet<Vector3Int>();

        while (open.Count > 0)
        {
            open.Sort((a, b) => a.fCost.CompareTo(b.fCost));
            var cur = open[0]; open.RemoveAt(0);
            if (cur.position == goal) return Reconstruct(cur);
            closed.Add(cur.position);

            foreach (var d in _dirs)
            {
                var nxt = cur.position + d;
                if (closed.Contains(nxt)) continue;

                // world‐bounds
                var wc = map.CellToWorld(nxt) + (Vector3)map.cellSize * 0.5f;
                if (wc.x < minBounds.x + spriteExtents.x || wc.x > maxBounds.x - spriteExtents.x ||
                   wc.y < minBounds.y + spriteExtents.y || wc.y > maxBounds.y - spriteExtents.y)
                    continue;

                // corner‐cut prevention
                if (d.x != 0 && d.y != 0)
                {
                    if (IsBlocked(map, cur.position + new Vector3Int(d.x, 0, 0), spriteExtents) &&
                       IsBlocked(map, cur.position + new Vector3Int(0, d.y, 0), spriteExtents))
                        continue;
                }

                if (IsBlocked(map, nxt, spriteExtents)) continue;

                float cost = (Mathf.Abs(d.x) + Mathf.Abs(d.y) == 2) ? 1.4142f : 1f;
                float gNew = cur.gCost + cost;
                var existing = open.Find(n => n.position == nxt);
                if (existing == null)
                    open.Add(new Node(nxt, gNew, Heur(nxt, goal), cur));
                else if (gNew < existing.gCost)
                {
                    existing.gCost = gNew;
                    existing.parent = cur;
                }
            }
        }

        return new List<Vector3Int>();
    }

    public static bool IsBlocked(Tilemap map, Vector3Int cell, Vector2 spriteExtents)
    {
        var ctr = map.CellToWorld(cell) + (Vector3)map.cellSize * 0.5f;
        var size = spriteExtents * 2f * 0.95f;
        foreach (var hit in Physics2D.OverlapBoxAll(ctr, size, 0f))
            if (!hit.isTrigger)
                return true;
        return false;
    }

    static List<Vector3Int> Reconstruct(Node n)
    {
        var r = new List<Vector3Int>();
        while (n != null) { r.Add(n.position); n = n.parent; }
        r.Reverse();
        return r;
    }

    static float Heur(Vector3Int a, Vector3Int b)
    {
        int dx = Mathf.Abs(a.x - b.x), dy = Mathf.Abs(a.y - b.y);
        return Mathf.Max(dx, dy);
    }

    class Node
    {
        public Vector3Int position;
        public float gCost, hCost;
        public float fCost => gCost + hCost;
        public Node parent;
        public Node(Vector3Int p, float g, float h, Node pr)
        {
            position = p; gCost = g; hCost = h; parent = pr;
        }
    }
}