using System;
using System.Collections.Generic;
using UnityEngine;

public class GridManager : MonoBehaviour {
    [Header("Grid Size")]
    [SerializeField] private int _width = 16;
    [SerializeField] private int _height = 9;

    [Header("References")]
    [SerializeField] private Tile _tilePrefab;
    [SerializeField] private Transform _cam;

    // Singleton
    public static GridManager Instance { get; private set; }

    // Grid data
    private Dictionary<Vector2Int, Tile> _tiles;

    // Events
    public event Action<Tile> OnTileClickedEvent;
    public event Action<Tile> OnTileHoverStartEvent;
    public event Action<Tile> OnTileHoverEndEvent;

    // Direction vectors for neighbor lookups
    private static readonly Vector2Int[] _directions = {
        Vector2Int.up,
        Vector2Int.down,
        Vector2Int.left,
        Vector2Int.right
    };

    public int Width => _width;
    public int Height => _height;
    public IReadOnlyDictionary<Vector2Int, Tile> Tiles => _tiles;

    void Awake() {
        if (Instance != null && Instance != this) {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    void Start() {
        GenerateGrid();
    }

    void GenerateGrid() {
        _tiles = new Dictionary<Vector2Int, Tile>();

        int tileCount = _width * _height;
        Texture2D whiteTex = GenerateTileTexture();
        Sprite tileSprite = Sprite.Create(whiteTex, new Rect(0, 0, 1, 1), Vector2.one * 0.5f, 1);

        for (int x = 0; x < _width; x++) {
            for (int y = 0; y < _height; y++) {
                Tile tile;

                if (_tilePrefab != null) {
                    tile = Instantiate(_tilePrefab, new Vector3(x + 0.5f, y + 0.5f, 0), Quaternion.identity);
                    tile.name = $"Tile {x} {y}";
                } else {
                    var go = new GameObject($"Tile {x} {y}");
                    go.transform.position = new Vector3(x + 0.5f, y + 0.5f, 0);
                    go.transform.localScale = Vector3.one;

                    var sr = go.AddComponent<SpriteRenderer>();
                    sr.sprite = tileSprite;
                    sr.sortingOrder = 0;

                    tile = go.AddComponent<Tile>();
                }

                var pos = new Vector2Int(x, y);
                var isOffset = (x % 2 == 0 && y % 2 != 0) || (x % 2 != 0 && y % 2 == 0);
                tile.Init(pos, isOffset);

                _tiles[pos] = tile;
            }
        }

        // Posisikan kamera di tengah grid
        Vector3 center = new Vector3(_width / 2f, _height / 2f, -10f);

        if (_cam != null) {
            _cam.position = center;
        } else {
            var mainCam = Camera.main;
            if (mainCam != null) {
                mainCam.transform.position = center;
                if (mainCam.orthographic) {
                    // Set size agar grid terlihat pas: setengah tinggi + padding
                    mainCam.orthographicSize = Mathf.Max(_width / (2f * mainCam.aspect), _height / 2f) + 1f;
                }
            }
        }
    }

    Texture2D GenerateTileTexture() {
        var tex = new Texture2D(1, 1);
        tex.SetPixel(0, 0, Color.white);
        tex.filterMode = FilterMode.Point;
        tex.Apply();
        return tex;
    }

    // ===== PUBLIC API =====

    public Tile GetTileAtPosition(Vector2Int pos) {
        _tiles.TryGetValue(pos, out var tile);
        return tile;
    }

    /// <summary>
    /// Konversi world position ke grid position (floor)
    /// </summary>
    public Vector2Int WorldToGridPos(Vector3 worldPos) {
        int x = Mathf.FloorToInt(worldPos.x);
        int y = Mathf.FloorToInt(worldPos.y);
        return new Vector2Int(x, y);
    }

    /// <summary>
    /// Dapatkan tile dari world position (via klik mouse)
    /// </summary>
    public Tile GetTileAtWorldPos(Vector3 worldPos) {
        var gridPos = WorldToGridPos(worldPos);
        return GetTileAtPosition(gridPos);
    }

    /// <summary>
    /// 4-directional neighbors
    /// </summary>
    public List<Tile> GetNeighbors(Vector2Int pos) {
        var result = new List<Tile>(4);
        foreach (var dir in _directions) {
            var neighborPos = pos + dir;
            if (_tiles.TryGetValue(neighborPos, out var neighbor)) {
                result.Add(neighbor);
            }
        }
        return result;
    }

    /// <summary>
    /// BFS — cari semua tile dalam range langkah tertentu. occupied tiles = blocked.
    /// </summary>
    public List<Tile> GetTilesInRange(Vector2Int origin, int range, bool includeOccupied = false) {
        var visited = new HashSet<Vector2Int>();
        var result = new List<Tile>();
        var queue = new Queue<(Vector2Int pos, int dist)>();
        queue.Enqueue((origin, 0));
        visited.Add(origin);

        while (queue.Count > 0) {
            var (current, dist) = queue.Dequeue();

            if (dist > 0 && dist <= range) {
                if (_tiles.TryGetValue(current, out var tile)) {
                    result.Add(tile);
                }
            }

            if (dist >= range) continue;

            foreach (var dir in _directions) {
                var next = current + dir;
                if (visited.Contains(next)) continue;
                visited.Add(next);

                if (_tiles.TryGetValue(next, out var nextTile)) {
                    // Cuma bisa lewat tile yang walkable (kecuali includeOccupied=true)
                    if (includeOccupied || nextTile.IsWalkable || next == origin) {
                        queue.Enqueue((next, dist + 1));
                    }
                }
            }
        }

        return result;
    }

    /// <summary>
    /// BFS — cari jarak terpendek dari origin ke target (-1 jika unreachable)
    /// </summary>
    public int GetDistance(Vector2Int origin, Vector2Int target) {
        if (origin == target) return 0;

        var visited = new HashSet<Vector2Int>();
        var queue = new Queue<(Vector2Int pos, int dist)>();
        queue.Enqueue((origin, 0));
        visited.Add(origin);

        while (queue.Count > 0) {
            var (current, dist) = queue.Dequeue();

            foreach (var dir in _directions) {
                var next = current + dir;
                if (next == target) return dist + 1;
                if (visited.Contains(next)) continue;
                visited.Add(next);

                if (_tiles.TryGetValue(next, out var nextTile) && nextTile.IsWalkable) {
                    queue.Enqueue((next, dist + 1));
                }
            }
        }

        return -1; // unreachable
    }

    // ===== UNIT PLACEMENT =====

    public bool PlaceUnit(Unit unit, Vector2Int pos) {
        if (!_tiles.TryGetValue(pos, out var tile)) return false;
        if (!tile.IsWalkable) return false;

        tile.SetOccupant(unit);
        unit.transform.position = new Vector3(pos.x + 0.5f, pos.y + 0.5f, 0);
        unit.SetGridPos(pos);
        return true;
    }

    public void RemoveUnit(Unit unit) {
        var pos = unit.GridPos;
        if (_tiles.TryGetValue(pos, out var tile)) {
            tile.ClearOccupant();
        }
    }

    // ===== EVENTS (dipanggil dari Tile) =====

    public void OnTileClicked(Tile tile) {
        OnTileClickedEvent?.Invoke(tile);
    }

    public void OnTileHoverStart(Tile tile) {
        OnTileHoverStartEvent?.Invoke(tile);
    }

    public void OnTileHoverEnd(Tile tile) {
        OnTileHoverEndEvent?.Invoke(tile);
    }
}
