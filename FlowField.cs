// FlowField.cs
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class FlowField
{
    private Vector2[,] directions;
    private int width, height;
    private Vector3 origin;
    private float cellSize;
    private Tilemap tilemap;

    /// <summary>
    /// Builds the field over your Tilemap.cellBounds, pointing every cell 
    /// toward the center of targetCell, but never through non-trigger colliders.
    /// </summary>
    public void Generate(Tilemap tilemap, Vector3Int targetCell)
    {
        this.tilemap = tilemap;
        var cb = tilemap.cellBounds;
        width = cb.size.x;
        height = cb.size.y;
        origin = tilemap.CellToWorld(cb.min);
        cellSize = tilemap.cellSize.x;

        // init distances
        var dist = new float[width, height];
        for (int x = 0; x < width; x++)
            for (int y = 0; y < height; y++)
                dist[x, y] = float.MaxValue;

        // convert targetCell → local coords
        int tx = targetCell.x - cb.min.x;
        int ty = targetCell.y - cb.min.y;
        if (tx < 0 || ty < 0 || tx >= width || ty >= height)
        {
            directions = null;
            return;
        }

        // BFS / Dijkstra
        var queue = new Queue<Vector3Int>();
        dist[tx, ty] = 0f;
        queue.Enqueue(targetCell);

        var dirs4 = new[] {
            new Vector3Int(1, 0, 0),  new Vector3Int(-1, 0, 0),
            new Vector3Int(0, 1, 0),  new Vector3Int(0, -1, 0)
        };
        var dirs8 = new[] {
            new Vector3Int(1, 1, 0),  new Vector3Int(1, -1, 0),
            new Vector3Int(-1, 1, 0), new Vector3Int(-1, -1, 0)
        };

        while (queue.Count > 0)
        {
            var cell = queue.Dequeue();
            int cx = cell.x - cb.min.x, cy = cell.y - cb.min.y;
            float cd = dist[cx, cy];

            // check 4 cardinals
            foreach (var d in dirs4)
                TryRelax(cell, d, cb, cd, dist, queue, 1f);

            // check diagonals
            foreach (var d in dirs8)
                TryRelax(cell, d, cb, cd, dist, queue, 1.4142f);
        }

        // build vector field
        directions = new Vector2[width, height];
        for (int x = 0; x < width; x++)
            for (int y = 0; y < height; y++)
            {
                if (dist[x, y] == float.MaxValue)
                {
                    directions[x, y] = Vector2.zero;
                    continue;
                }

                float best = dist[x, y];
                Vector2 bestDir = Vector2.zero;

                // among 8 neighbors, find one with strictly smaller dist
                foreach (var d in dirs4)
                    PickBest(d, x, y, cb, dist, ref best, ref bestDir);
                foreach (var d in dirs8)
                    PickBest(d, x, y, cb, dist, ref best, ref bestDir);

                directions[x, y] = bestDir.normalized;
            }
    }

    private void TryRelax(
        Vector3Int cell,
        Vector3Int dir,
        BoundsInt cb,
        float cd,
        float[,] dist,
        Queue<Vector3Int> queue,
        float cost
    )
    {
        var nc = cell + dir;
        int nx = nc.x - cb.min.x, ny = nc.y - cb.min.y;
        if (nx < 0 || ny < 0 || nx >= width || ny >= height) return;

        // ** static obstacle check **
        Vector3 worldC = tilemap.CellToWorld(nc)
                       + (Vector3)tilemap.cellSize * 0.5f;
        var hits = Physics2D.OverlapBoxAll(
            worldC,
            tilemap.cellSize * 0.9f,
            0f
        );
        foreach (var h in hits)
            if (!h.isTrigger)  // blocked by a wall/obstacle
                return;

        if (dist[nx, ny] > cd + cost)
        {
            dist[nx, ny] = cd + cost;
            queue.Enqueue(nc);
        }
    }

    private void PickBest(
        Vector3Int d,
        int x, int y,
        BoundsInt cb,
        float[,] dist,
        ref float best,
        ref Vector2 bestDir
    )
    {
        int nx = x + d.x, ny = y + d.y;
        if (nx < 0 || ny < 0 || nx >= width || ny >= height) return;
        if (dist[nx, ny] < best)
        {
            best = dist[nx, ny];
            bestDir = new Vector2(d.x, d.y);
        }
    }

    /// <summary>
    /// Returns the unit‐vector pointing from worldPos *toward*
    /// the goal, or zero if at goal/unreachable/out-of-bounds.
    /// </summary>
    public Vector2 GetDirection(Vector3 worldPos)
    {
        if (directions == null) return Vector2.zero;
        int x = Mathf.FloorToInt((worldPos.x - origin.x) / cellSize);
        int y = Mathf.FloorToInt((worldPos.y - origin.y) / cellSize);
        if (x < 0 || y < 0 || x >= width || y >= height)
            return Vector2.zero;
        return directions[x, y];
    }
}
