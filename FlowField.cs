using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

/// <summary>
/// Generates a flow field (vector field) from any target cell on the tilemap.
/// </summary>
public class FlowField
{
    private Vector2[,] directions;
    private int width, height;
    private Vector3 origin;
    private float cellSize;

    public void Generate(Tilemap tilemap, Vector3Int targetCell)
    {
        var bounds = tilemap.cellBounds;
        width = bounds.size.x;
        height = bounds.size.y;
        origin = tilemap.CellToWorld(bounds.min);
        cellSize = tilemap.cellSize.x; // assume square cells

        // distance field
        float[,] dist = new float[width, height];
        for (int x = 0; x < width; x++)
            for (int y = 0; y < height; y++)
                dist[x, y] = float.MaxValue;

        var queue = new Queue<Vector3Int>();
        int tx = targetCell.x - bounds.min.x;
        int ty = targetCell.y - bounds.min.y;
        if (tx < 0 || ty < 0 || tx >= width || ty >= height) return;
        dist[tx, ty] = 0f;
        queue.Enqueue(targetCell);

        Vector3Int[] dirs = new Vector3Int[] {
            new Vector3Int(1,0,0), new Vector3Int(-1,0,0),
            new Vector3Int(0,1,0), new Vector3Int(0,-1,0),
            new Vector3Int(1,1,0), new Vector3Int(1,-1,0), new Vector3Int(-1,1,0), new Vector3Int(-1,-1,0)
        };

        while (queue.Count > 0)
        {
            var cell = queue.Dequeue();
            int cx = cell.x - bounds.min.x;
            int cy = cell.y - bounds.min.y;
            float cd = dist[cx, cy];
            foreach (var d in dirs)
            {
                var nc = cell + d;
                int nx = nc.x - bounds.min.x;
                int ny = nc.y - bounds.min.y;
                if (nx < 0 || ny < 0 || nx >= width || ny >= height) continue;
                if (tilemap.HasTile(nc) == false) continue; // skip empty
                float cost = (d.x != 0 && d.y != 0) ? 1.4142f : 1f;
                if (dist[nx, ny] > cd + cost)
                {
                    dist[nx, ny] = cd + cost;
                    queue.Enqueue(nc);
                }
            }
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
                Vector3Int cell = new Vector3Int(x + bounds.min.x, y + bounds.min.y, 0);
                float best = dist[x, y];
                Vector2 bestDir = Vector2.zero;

                foreach (var d in dirs)
                {
                    var nc = cell + d;
                    int nx = nc.x - bounds.min.x;
                    int ny = nc.y - bounds.min.y;
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
    /// Returns the direction vector at world position.
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