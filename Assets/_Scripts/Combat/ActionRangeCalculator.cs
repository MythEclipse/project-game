using System.Collections.Generic;
using UnityEngine;

public static class ActionRangeCalculator {

    private static readonly Vector2Int[] _directions = {
        Vector2Int.up, Vector2Int.down,
        Vector2Int.left, Vector2Int.right
    };

    /// <summary>
    /// BFS — cari tile yang bisa dijangkau unit untuk movement.
    /// Occupied tile gabisa dilewati si unit.
    /// </summary>
    public static List<Tile> GetWalkableTiles(Vector2Int origin, int range, GridManager grid) {
        var visited = new HashSet<Vector2Int>();
        var result = new List<Tile>();
        var queue = new Queue<(Vector2Int pos, int dist)>();
        queue.Enqueue((origin, 0));
        visited.Add(origin);

        while (queue.Count > 0) {
            var (current, dist) = queue.Dequeue();

            if (dist > 0 && dist <= range) {
                if (grid.Tiles.TryGetValue(current, out var tile)) {
                    result.Add(tile);
                }
            }

            if (dist >= range) continue;

            foreach (var dir in _directions) {
                var next = current + dir;
                if (visited.Contains(next)) continue;
                visited.Add(next);

                if (grid.Tiles.TryGetValue(next, out var nextTile) && nextTile.IsWalkable) {
                    queue.Enqueue((next, dist + 1));
                }
            }
        }

        return result;
    }

    /// <summary>
    /// Dapatkan tile dalam attack range (ring pattern).
    /// Tidak terhalang occupant (beda dengan movement).
    /// </summary>
    public static List<Tile> GetAttackableTiles(Vector2Int origin, int range, GridManager grid) {
        var result = new List<Tile>();

        for (int dx = -range; dx <= range; dx++) {
            for (int dy = -range; dy <= range; dy++) {
                // Manhattan distance ≤ range tapi > 0 (bukan tile sendiri)
                if (Mathf.Abs(dx) + Mathf.Abs(dy) > range) continue;
                if (dx == 0 && dy == 0) continue;

                var pos = new Vector2Int(origin.x + dx, origin.y + dy);
                if (grid.Tiles.TryGetValue(pos, out var tile)) {
                    result.Add(tile);
                }
            }
        }

        return result;
    }

    /// <summary>
    /// Dapatkan tile dalam radius tertentu (untuk spell/area).
    /// </summary>
    public static List<Tile> GetTilesInRadius(Vector2Int origin, int radius, GridManager grid) {
        var result = new List<Tile>();

        for (int dx = -radius; dx <= radius; dx++) {
            for (int dy = -radius; dy <= radius; dy++) {
                if (dx * dx + dy * dy > radius * radius) continue;

                var pos = new Vector2Int(origin.x + dx, origin.y + dy);
                if (grid.Tiles.TryGetValue(pos, out var tile)) {
                    result.Add(tile);
                }
            }
        }

        return result;
    }
}
