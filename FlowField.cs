// FlowField.cs
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

/// <summary>
/// Generates a flow‐field that points every cell toward the target.
/// </summary>
public class FlowField
{
    private Vector2[,] directions;
    private int width, height;
    private Vector3 origin;
    private float cellSize;

    /// <summary>Lower‐left corner of the field in world space.</summary>
    public Vector3 Origin => origin;
    /// <summary>Size of each cell in world units.</summary>
    public float CellSize => cellSize;

    /// <summary>
    /// Primary generator: builds a field over 'bounds' with square cells of side 'cellSize',
    /// all directing toward 'targetWorld'.
    /// </summary>
    public void Generate(Bounds bounds, float cellSize, Vector3 targetWorld)
    {
        origin = bounds.min;
        this.cellSize = cellSize;
        width = Mathf.CeilToInt(bounds.size.x / cellSize);
        height = Mathf.CeilToInt(bounds.size.y / cellSize);

        if (width <= 0 || height <= 0)
        {
            directions = null;
            return;
        }

        // 1) Build distance map (reverse Dijkstra)
        float[,] dist = new float[width, height];
        for (int x = 0; x < width; x++)
            for (int y = 0; y < height; y++)
                dist[x, y] = float.MaxValue;

        int tx = Mathf.FloorToInt((targetWorld.x - origin.x) / cellSize);
        int ty = Mathf.FloorToInt((targetWorld.y - origin.y) / cellSize);
        if (tx < 0 || ty < 0 || tx >= width || ty >= height)
        {
            directions = null;
            return;
        }

        var queue = new Queue<Vector2Int>();
        dist[tx, ty] = 0f;
        queue.Enqueue(new Vector2Int(tx, ty));

        var dirs4 = new[] {
            new Vector2Int(1, 0),  new Vector2Int(-1, 0),
            new Vector2Int(0, 1),  new Vector2Int(0, -1)
        };
        var dirs8 = new[] {
            new Vector2Int(1, 1),  new Vector2Int(1, -1),
            new Vector2Int(-1, 1), new Vector2Int(-1, -1)
        };

        while (queue.Count > 0)
        {
            var cell = queue.Dequeue();
            float cd = dist[cell.x, cell.y];

            // Cardinal moves
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

            // Diagonals
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

        // 2) Convert distances to direction vectors
        directions = new Vector2[width, height];
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                if (dist[x, y] == float.MaxValue)
                {
                    directions[x, y] = Vector2.zero;
                    continue;
                }

                float best = dist[x, y];
                Vector2 bestDir = Vector2.zero;

                // Find neighbor with smallest dist
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
    }

    /// <summary>
    /// Tilemap‐based overload: builds the same field over your Tilemap.cellBounds,
    /// pointing toward the center of 'targetCell'.
    /// </summary>
    public void Generate(Tilemap tilemap, Vector3Int targetCell)
    {
        // Compute world‐space bounds from cellBounds
        var cb = tilemap.cellBounds;
        float cs = tilemap.cellSize.x;
        Vector3 originWs = tilemap.CellToWorld(cb.min);
        Bounds worldBounds = new Bounds
        {
            min = originWs,
            size = new Vector3(cb.size.x * cs, cb.size.y * cs, 0f)
        };

        // Use actual cell center as the world‐space goal
        Vector3 targetWorld = tilemap.GetCellCenterWorld(targetCell);

        // Delegate
        Generate(worldBounds, cs, targetWorld);
    }

    /// <summary>
    /// After generating, sample this to get the unit‐vector
    /// pointing from worldPos toward the goal. Zero means “at goal or unreachable.”
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
