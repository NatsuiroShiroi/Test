// FlowField.cs
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

/// <summary>
/// Generates a flow-field (vector field) from any target cell over a rectangular area.
/// </summary>
public class FlowField
{
    private Vector2[,] directions;
    private int width, height;
    private Vector3 origin;
    private float cellSize;

    /// <summary>World-space origin (bounds.min) of the last generated field.</summary>
    public Vector3 Origin => origin;
    /// <summary>Size of each grid cell in world units.</summary>
    public float CellSize => cellSize;

    /// <summary>
    /// Builds the flow-field over the given world-space bounds (min at bounds.min,
    /// size = bounds.size), using uniform square cells of side length cellSize,
    /// all pointing toward targetWorld.
    /// </summary>
    public void Generate(Bounds bounds, float cellSize, Vector3 targetWorld)
    {
        origin = bounds.min;
        this.cellSize = cellSize;
        width = Mathf.CeilToInt(bounds.size.x / cellSize);
        height = Mathf.CeilToInt(bounds.size.y / cellSize);

        // (… your existing distance-map + vector-map build code goes here …)
        // You don’t have to change anything below; it just uses origin, cellSize, width, height, and targetWorld.
        float[,] dist = new float[width, height];
        for (int x = 0; x < width; x++)
            for (int y = 0; y < height; y++)
                dist[x, y] = float.MaxValue;

        int tx = Mathf.FloorToInt((targetWorld.x - origin.x) / cellSize);
        int ty = Mathf.FloorToInt((targetWorld.y - origin.y) / cellSize);
        if (tx < 0 || ty < 0 || tx >= width || ty >= height)
            return;

        var queue = new Queue<Vector2Int>();
        dist[tx, ty] = 0f;
        queue.Enqueue(new Vector2Int(tx, ty));

        var dirs4 = new[] {
            new Vector2Int(1, 0), new Vector2Int(-1, 0),
            new Vector2Int(0, 1), new Vector2Int(0, -1)
        };
        var dirs8 = new[] {
            new Vector2Int(1, 1), new Vector2Int(1, -1),
            new Vector2Int(-1, 1),new Vector2Int(-1, -1)
        };

        while (queue.Count > 0)
        {
            var cell = queue.Dequeue();
            float cd = dist[cell.x, cell.y];
            // cardinal neighbours
            foreach (var d in dirs4)
            {
                int nx = cell.x + d.x, ny = cell.y + d.y;
                if (nx < 0 || ny < 0 || nx >= width || ny >= height) continue;
                float cost = 1f;
                if (dist[nx, ny] > cd + cost)
                {
                    dist[nx, ny] = cd + cost;
                    queue.Enqueue(new Vector2Int(nx, ny));
                }
            }
            // diagonal neighbours
            foreach (var d in dirs8)
            {
                int nx = cell.x + d.x, ny = cell.y + d.y;
                if (nx < 0 || ny < 0 || nx >= width || ny >= height) continue;
                float cost = 1.4142f;
                if (dist[nx, ny] > cd + cost)
                {
                    dist[nx, ny] = cd + cost;
                    queue.Enqueue(new Vector2Int(nx, ny));
                }
            }
        }

        // build the vector field
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

                // pick neighbour with smallest distance
                foreach (var d in dirs4)
                {
                    int nx = x + d.x, ny = y + d.y;
                    if (nx < 0 || ny < 0 || nx >= width || ny >= height) continue;
                    if (dist[nx, ny] < best)
                    {
                        best = dist[nx, ny];
                        bestDir = d;
                    }
                }
                foreach (var d in dirs8)
                {
                    int nx = x + d.x, ny = y + d.y;
                    if (nx < 0 || ny < 0 || nx >= width || ny >= height) continue;
                    if (dist[nx, ny] < best)
                    {
                        best = dist[nx, ny];
                        bestDir = d;
                    }
                }
                directions[x, y] = bestDir.normalized;
            }
    }

    /// <summary>
    /// NEW OVERLOAD: Build the flow‐field over the entire Tilemap’s cellBounds,
    /// pointing toward the given targetCell.
    /// </summary>
    public void Generate(Tilemap tilemap, Vector3Int targetCell)
    {
        var cb = tilemap.cellBounds;                       // integer cell bounds
        float cs = tilemap.cellSize.x;                     // assume square cells
        Vector3 originWs = tilemap.CellToWorld(cb.min);    // bottom-left corner in world
        // world-space size = number of cells * cellSize
        Bounds worldBounds = new Bounds
        {
            min = originWs,
            size = new Vector3(cb.size.x * cs, cb.size.y * cs, 0f)
        };
        // Use the cell‐center as the world “target”
        Vector3 targetWorld = tilemap.GetCellCenterWorld(targetCell);

        // Delegate to the original implementation
        Generate(worldBounds, cs, targetWorld);
    }

    /// <summary>
    /// Returns the unit-vector toward the goal from worldPos, or zero if at goal/out of bounds.
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
