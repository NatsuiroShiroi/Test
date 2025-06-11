using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Generates a flow field (vector field) from any target world position over a rectangular area.
/// </summary>
public class FlowField
{
    private Vector2[,] directions;
    private int width, height;
    private Vector3 origin;
    private float cellSize;

    /// <summary>
    /// World-space origin of the grid (bottom-left corner).
    /// </summary>
    public Vector3 Origin => origin;
    /// <summary>
    /// Size of each square cell.
    /// </summary>
    public float CellSize => cellSize;

    /// <summary>
    /// Generate the flow field over the given bounds with a uniform grid cellSize, pointing toward targetWorld.
    /// </summary>
    public void Generate(Bounds bounds, float cellSize, Vector3 targetWorld)
    {
        origin = bounds.min;
        this.cellSize = cellSize;
        width = Mathf.CeilToInt(bounds.size.x / cellSize);
        height = Mathf.CeilToInt(bounds.size.y / cellSize);

        float[,] dist = new float[width, height];
        for (int x = 0; x < width; x++)
            for (int y = 0; y < height; y++)
                dist[x, y] = float.MaxValue;

        int tx = Mathf.FloorToInt((targetWorld.x - origin.x) / cellSize);
        int ty = Mathf.FloorToInt((targetWorld.y - origin.y) / cellSize);
        if (tx < 0 || ty < 0 || tx >= width || ty >= height) return;

        var dirs4 = new Vector2Int[] { new Vector2Int(1, 0), new Vector2Int(-1, 0), new Vector2Int(0, 1), new Vector2Int(0, -1) };
        var dirs8 = new Vector2Int[] { new Vector2Int(1, 1), new Vector2Int(1, -1), new Vector2Int(-1, 1), new Vector2Int(-1, -1) };

        var queue = new Queue<Vector2Int>();
        dist[tx, ty] = 0f;
        queue.Enqueue(new Vector2Int(tx, ty));

        while (queue.Count > 0)
        {
            var cell = queue.Dequeue();
            float cd = dist[cell.x, cell.y];
            // 4-way neighbors
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
            // 8-way neighbors
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

        directions = new Vector2[width, height];
        for (int x = 0; x < width; x++)
            for (int y = 0; y < height; y++)
            {
                if (dist[x, y] == float.MaxValue)
                {
                    directions[x, y] = Vector2.zero;
                    continue;
                }
                Vector2 bestDir = Vector2.zero;
                float best = dist[x, y];
                foreach (var d in dirs4)
                {
                    int nx = x + d.x, ny = y + d.y;
                    if (nx < 0 || ny < 0 || nx >= width || ny >= height) continue;
                    if (dist[nx, ny] < best)
                    {
                        best = dist[nx, ny];
                        bestDir = new Vector2(d.x, d.y);
                    }
                }
                foreach (var d in dirs8)
                {
                    int nx = x + d.x, ny = y + d.y;
                    if (nx < 0 || ny < 0 || nx >= width || ny >= height) continue;
                    if (dist[nx, ny] < best)
                    {
                        best = dist[nx, ny];
                        bestDir = new Vector2(d.x, d.y);
                    }
                }
                directions[x, y] = bestDir.normalized;
            }
    }

    /// <summary>
    /// Returns the direction at a given world position (or zero if at goal or out of bounds).
    /// </summary>
    public Vector2 GetDirection(Vector3 worldPos)
    {
        if (directions == null) return Vector2.zero;
        int x = Mathf.FloorToInt((worldPos.x - origin.x) / cellSize);
        int y = Mathf.FloorToInt((worldPos.y - origin.y) / cellSize);
        if (x < 0 || y < 0 || x >= width || y >= height) return Vector2.zero;
        return directions[x, y];
    }
}
