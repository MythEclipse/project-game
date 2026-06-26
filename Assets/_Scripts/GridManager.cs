using System.Collections.Generic;
using UnityEngine;

public class GridManager : MonoBehaviour {
    [SerializeField] private int _width = 16;
    [SerializeField] private int _height = 9;
    [SerializeField] private Transform _cam;
    [SerializeField] private Tile _tilePrefab;

    private Dictionary<Vector2, Tile> _tiles;

    void Start() {
        GenerateGrid();
    }

    void GenerateGrid() {
        _tiles = new Dictionary<Vector2, Tile>();

        if (_cam == null) {
            var go = GameObject.Find("Main Camera");
            if (go != null) _cam = go.transform;
        }

        for (int x = 0; x < _width; x++) {
            for (int y = 0; y < _height; y++) {
                Tile tile;

                if (_tilePrefab != null) {
                    tile = Instantiate(_tilePrefab, new Vector3(x, y), Quaternion.identity);
                    tile.name = $"Tile {x} {y}";
                } else {
                    var go = new GameObject($"Tile {x} {y}");
                    go.transform.position = new Vector3(x, y);
                    go.transform.localScale = Vector3.one;
                    tile = go.AddComponent<Tile>();
                }

                var isOffset = (x % 2 == 0 && y % 2 != 0) || (x % 2 != 0 && y % 2 == 0);
                tile.Init(isOffset);

                _tiles[new Vector2(x, y)] = tile;
            }
        }

        if (_cam != null) {
            _cam.transform.position = new Vector3(_width / 2f - 0.5f, _height / 2f - 0.5f, -10);
        }
    }

    public Tile GetTileAtPosition(Vector2 pos) {
        _tiles.TryGetValue(pos, out var tile);
        return tile;
    }
}
