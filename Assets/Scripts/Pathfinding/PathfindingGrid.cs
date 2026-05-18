using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace DungeonCrawler.Pathfinding
{
    /// <summary>
    /// Builds a walkable grid from FloorTilemap and WallTilemap.
    /// </summary>
    public class PathfindingGrid : MonoBehaviour
    {
        [SerializeField] private Tilemap floorTilemap;
        [SerializeField] private Tilemap wallTilemap;

        private readonly Dictionary<Vector3Int, GridNode> nodes = new Dictionary<Vector3Int, GridNode>();

        public static PathfindingGrid Instance { get; private set; }

        private void Awake()
        {
            Instance = this;
            FindTilemapsIfMissing();
            RebuildGrid();
        }

        private void OnValidate()
        {
            if (!Application.isPlaying)
            {
                FindTilemapsIfMissing();
            }
        }

        public void Configure(Tilemap floor, Tilemap wall)
        {
            floorTilemap = floor;
            wallTilemap = wall;
            RebuildGrid();
        }

        public void RebuildGrid()
        {
            nodes.Clear();

            if (floorTilemap == null)
            {
                Debug.LogWarning($"PathfindingGrid {name} cannot build: FloorTilemap is missing.", this);
                return;
            }

            // Sync tilemap bounds to ensure we capture all tiles
            floorTilemap.CompressBounds();
            if (wallTilemap != null) wallTilemap.CompressBounds();

            BoundsInt bounds = floorTilemap.cellBounds;
            foreach (Vector3Int cellPosition in bounds.allPositionsWithin)
            {
                if (!floorTilemap.HasTile(cellPosition))
                {
                    continue;
                }

                bool hasWall = wallTilemap != null && wallTilemap.HasTile(cellPosition);
                bool isWalkable = !hasWall;
                Vector3 worldPosition = floorTilemap.GetCellCenterWorld(cellPosition);
                nodes[cellPosition] = new GridNode(cellPosition, worldPosition, isWalkable);
            }
            
            if (nodes.Count == 0)
            {
                Debug.LogWarning($"PathfindingGrid {name} rebuilt but has 0 nodes! Check tilemaps.", this);
            }
        }

        public bool TryGetNodeFromWorld(Vector3 worldPosition, out GridNode node)
        {
            node = null;

            if (floorTilemap == null)
            {
                return false;
            }

            Vector3Int cellPosition = floorTilemap.WorldToCell(worldPosition);
            return nodes.TryGetValue(cellPosition, out node);
        }

        public List<GridNode> GetNeighbours(GridNode node)
        {
            List<GridNode> neighbours = new List<GridNode>(4);
            if (node == null)
            {
                return neighbours;
            }

            AddNeighbour(node.CellPosition + Vector3Int.up, neighbours);
            AddNeighbour(node.CellPosition + Vector3Int.down, neighbours);
            AddNeighbour(node.CellPosition + Vector3Int.left, neighbours);
            AddNeighbour(node.CellPosition + Vector3Int.right, neighbours);

            return neighbours;
        }

        public IEnumerable<GridNode> GetAllNodes()
        {
            return nodes.Values;
        }

        private void AddNeighbour(Vector3Int cellPosition, List<GridNode> neighbours)
        {
            if (nodes.TryGetValue(cellPosition, out GridNode node) && node.IsWalkable)
            {
                neighbours.Add(node);
            }
        }

        private void FindTilemapsIfMissing()
        {
            if (floorTilemap == null)
            {
                GameObject floorObject = GameObject.Find("FloorTilemap");
                if (floorObject != null)
                {
                    floorTilemap = floorObject.GetComponent<Tilemap>();
                }
            }

            if (wallTilemap == null)
            {
                GameObject wallObject = GameObject.Find("WallTilemap");
                if (wallObject != null)
                {
                    wallTilemap = wallObject.GetComponent<Tilemap>();
                }
            }
        }
    }
}
