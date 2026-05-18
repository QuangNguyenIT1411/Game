using UnityEngine;

namespace DungeonCrawler.Pathfinding
{
    /// <summary>
    /// Runtime node used by grid-based pathfinding.
    /// </summary>
    public class GridNode
    {
        public GridNode(Vector3Int cellPosition, Vector3 worldPosition, bool isWalkable)
        {
            CellPosition = cellPosition;
            WorldPosition = worldPosition;
            IsWalkable = isWalkable;
        }

        public Vector3Int CellPosition { get; }
        public Vector3 WorldPosition { get; }
        public bool IsWalkable { get; }
        public int GCost { get; set; }
        public int HCost { get; set; }
        public int FCost => GCost + HCost;
        public GridNode Parent { get; set; }

        public const int InfiniteCost = 1000000;

        public void ResetPathData()
        {
            GCost = InfiniteCost;
            HCost = 0;
            Parent = null;
        }
}
}
