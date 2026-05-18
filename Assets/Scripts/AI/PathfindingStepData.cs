using System.Collections.Generic;
using UnityEngine;

namespace DungeonCrawler.AI
{
    public class PathfindingStepData
    {
        public Vector3Int currentCell;
        public List<Vector3Int> visitedCells = new List<Vector3Int>();
        public List<Vector3Int> frontierCells = new List<Vector3Int>();
        public List<Vector3Int> finalPathCells = new List<Vector3Int>();
        public bool isFinished;
        public bool pathFound;
    }
}
