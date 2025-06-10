// Pathfinder.cs
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

// A* pathfinding logic with obstacle avoidance and corner-cut prevention

public class Pathfinder
{
    private Tilemap tilemap;
    private Vector3 minBounds;
    private Vector3 maxBounds;
    private Vector2 spriteExtents;

    private static readonly Vector3Int[] directions = {
        new Vector3Int(1, 0, 0), new Vector3Int(-1, 0, 0),
        new Vector3Int(0, 1, 0), new Vector3Int(0, -1, 0),
        new Vector3Int(1, 1, 0), new Vector3Int(1, -1, 0),
        new Vector3Int(-1, 1, 0), new Vector3Int(-1,-1, 0)
    };

    public Pathfinder(Tilemap tilemap, Vector3 minBounds, Vector3 maxBounds, Vector2 spriteExtents)
    {
        this.tilemap = tilemap;
        this.minBounds = minBounds;
        this.maxBounds = maxBounds;
        this.spriteExtents = spriteExtents;
    }

    public List<Vector3Int> FindPath(Vector3Int start, Vector3Int goal)
    {
        var openSet = new List<Node> { new Node(start, 0, Heuristic(start, goal), null) };
        var closedSet = new HashSet<Vector3Int>();

        while (openSet.Count > 0)
        {
            openSet.Sort((a, b) => a.fCost.CompareTo(b.fCost));
            Node current = openSet[0];
            openSet.RemoveAt(0);
            if (current.position == goal) return ReconstructPath(current);

            closedSet.Add(current.position);
            foreach (var dir in directions)
            {
                Vector3Int neighbor = current.position + dir;
                if (closedSet.Contains(neighbor)) continue;

                Vector3 center = tilemap.CellToWorld(neighbor) + (Vector3)tilemap.cellSize * 0.5f;
                if (center.x < minBounds.x + spriteExtents.x ||
                    center.x > maxBounds.x - spriteExtents.x ||
                    center.y < minBounds.y + spriteExtents.y ||
                    center.y > maxBounds.y - spriteExtents.y)
                    continue;

                if (dir.x != 0 && dir.y != 0)
                {
                    var side1 = current.position + new Vector3Int(dir.x, 0, 0);
                    var side2 = current.position + new Vector3Int(0, dir.y, 0);
                    if (IsBlocked(side1) || IsBlocked(side2)) continue;
                }

                if (IsBlocked(neighbor)) continue;

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

    public bool IsBlocked(Vector3Int cell)
    {
        Vector3 center = tilemap.CellToWorld(cell) + (Vector3)tilemap.cellSize * 0.5f;
        Vector2 checkSize = spriteExtents * 2f * 0.95f;
        Collider2D[] hits = Physics2D.OverlapBoxAll(center, checkSize, 0f);
        foreach (var hit in hits)
            if (!hit.isTrigger) return true;
        return false;
    }

    private List<Vector3Int> ReconstructPath(Node node)
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

    private float Heuristic(Vector3Int a, Vector3Int b)
    {
        int dx = Mathf.Abs(a.x - b.x), dy = Mathf.Abs(a.y - b.y);
        return Mathf.Max(dx, dy);
    }

    private class Node
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
