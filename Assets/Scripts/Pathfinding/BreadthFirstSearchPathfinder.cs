using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;
using DungeonCrawler.AI;

namespace DungeonCrawler.Pathfinding
{
    /// <summary>
    /// Grid Breadth First Search pathfinder using 4-direction movement.
    /// BFS explores all neighbors layer by layer, finding the shortest path in unweighted grids.
    /// </summary>
    public static class BreadthFirstSearchPathfinder
    {
        static BreadthFirstSearchPathfinder()
        {
            UnityEngine.Debug.Log("Breadth First Search pathfinder initialized");
        }

        public static List<PathfindingStepData> GenerateSearchSteps(Vector3 startWorldPosition, Vector3 targetWorldPosition)
        {
            return GenerateSearchSteps(PathfindingGrid.Instance, startWorldPosition, targetWorldPosition);
        }

        public static List<PathfindingStepData> GenerateSearchSteps(PathfindingGrid grid, Vector3 startWorldPosition, Vector3 targetWorldPosition)
        {
            List<PathfindingStepData> steps = new List<PathfindingStepData>();
            if (grid == null)
            {
                return steps;
            }

            if (!grid.TryGetNodeFromWorld(startWorldPosition, out GridNode startNode) ||
                !grid.TryGetNodeFromWorld(targetWorldPosition, out GridNode targetNode))
            {
                return steps;
            }

            if (startNode == null || targetNode == null || !startNode.IsWalkable || !targetNode.IsWalkable)
            {
                return steps;
            }

            Queue<GridNode> queue = new Queue<GridNode>();
            HashSet<GridNode> visited = new HashSet<GridNode>();
            Dictionary<GridNode, GridNode> parentMap = new Dictionary<GridNode, GridNode>();

            queue.Enqueue(startNode);
            visited.Add(startNode);

            while (queue.Count > 0)
            {
                GridNode currentNode = queue.Dequeue();

                if (currentNode == targetNode)
                {
                    List<Vector3Int> finalPath = ReconstructPathCells(startNode, targetNode, parentMap);
                    steps.Add(CreateStep(currentNode, visited, queue, finalPath, true, true));
                    return steps;
                }

                foreach (GridNode neighbour in grid.GetNeighbours(currentNode))
                {
                    if (neighbour.IsWalkable && !visited.Contains(neighbour))
                    {
                        visited.Add(neighbour);
                        parentMap[neighbour] = currentNode;
                        queue.Enqueue(neighbour);
                    }
                }

                steps.Add(CreateStep(currentNode, visited, queue, null, false, false));
            }

            if (steps.Count > 0)
            {
                steps[steps.Count - 1].isFinished = true;
                steps[steps.Count - 1].pathFound = false;
            }

            return steps;
        }

        public static List<Vector3> FindPath(PathfindingGrid grid, Vector3 startWorldPosition, Vector3 targetWorldPosition, PathfindingStats stats = null)
        {
            Stopwatch sw = Stopwatch.StartNew();
            int nodesVisited = 0;

            if (stats != null)
            {
                stats.recalculationCount++;
                stats.visitedPositions.Clear();
            }

            List<Vector3> path = new List<Vector3>();
            if (grid == null)
            {
                if (stats != null) stats.lastPathFailed = true;
                return path;
            }

            if (!grid.TryGetNodeFromWorld(startWorldPosition, out GridNode startNode) ||
                !grid.TryGetNodeFromWorld(targetWorldPosition, out GridNode targetNode))
            {
                if (stats != null) stats.lastPathFailed = true;
                return path;
            }

            if (startNode == null || targetNode == null || !startNode.IsWalkable || !targetNode.IsWalkable)
            {
                if (stats != null) stats.lastPathFailed = true;
                return path;
            }

            if (startNode == targetNode)
            {
                if (stats != null)
                {
                    stats.lastPathTimeMs = (float)sw.Elapsed.TotalMilliseconds;
                    stats.visitedNodes = 0;
                    stats.pathLength = 0;
                    stats.lastPathFailed = false;
                }
                return path;
            }

            Queue<GridNode> queue = new Queue<GridNode>();
            HashSet<GridNode> visited = new HashSet<GridNode>();
            Dictionary<GridNode, GridNode> parentMap = new Dictionary<GridNode, GridNode>();

            queue.Enqueue(startNode);
            visited.Add(startNode);

            bool found = false;
            while (queue.Count > 0)
            {
                nodesVisited++;
                GridNode currentNode = queue.Dequeue();
                
                if (stats != null) stats.visitedPositions.Add(currentNode.WorldPosition);

                if (currentNode == targetNode)
{
                    found = true;
                    break;
                }

                foreach (GridNode neighbour in grid.GetNeighbours(currentNode))
                {
                    if (neighbour.IsWalkable && !visited.Contains(neighbour))
                    {
                        visited.Add(neighbour);
                        parentMap[neighbour] = currentNode;
                        queue.Enqueue(neighbour);
                    }
                }
            }

            sw.Stop();
            if (found)
            {
                path = ReconstructPath(startNode, targetNode, parentMap);
                if (stats != null)
                {
                    stats.lastPathTimeMs = (float)sw.Elapsed.TotalMilliseconds;
                    stats.visitedNodes = nodesVisited;
                    stats.pathLength = path.Count;
                    stats.lastPathFailed = false;
                }
            }
            else
            {
                if (stats != null)
                {
                    stats.lastPathTimeMs = (float)sw.Elapsed.TotalMilliseconds;
                    stats.visitedNodes = nodesVisited;
                    stats.pathLength = 0;
                    stats.lastPathFailed = true;
                }
            }

            return path;
        }

        private static List<Vector3> ReconstructPath(GridNode startNode, GridNode targetNode, Dictionary<GridNode, GridNode> parentMap)
        {
            List<Vector3> path = new List<Vector3>();
            GridNode currentNode = targetNode;

            while (currentNode != startNode)
            {
                path.Add(currentNode.WorldPosition);
                if (parentMap.TryGetValue(currentNode, out GridNode parent))
                {
                    currentNode = parent;
                }
                else
                {
                    break;
                }
            }

            path.Reverse();
            return path;
        }

        private static List<Vector3Int> ReconstructPathCells(GridNode startNode, GridNode targetNode, Dictionary<GridNode, GridNode> parentMap)
        {
            List<Vector3Int> path = new List<Vector3Int>();
            GridNode currentNode = targetNode;

            while (currentNode != startNode)
            {
                path.Add(currentNode.CellPosition);
                if (parentMap.TryGetValue(currentNode, out GridNode parent))
                {
                    currentNode = parent;
                }
                else
                {
                    break;
                }
            }

            path.Reverse();
            return path;
        }

        private static PathfindingStepData CreateStep(
            GridNode currentNode,
            HashSet<GridNode> visited,
            Queue<GridNode> frontier,
            List<Vector3Int> finalPath,
            bool isFinished,
            bool pathFound)
        {
            PathfindingStepData step = new PathfindingStepData
            {
                currentCell = currentNode.CellPosition,
                isFinished = isFinished,
                pathFound = pathFound
            };

            foreach (GridNode node in visited)
            {
                step.visitedCells.Add(node.CellPosition);
            }

            foreach (GridNode node in frontier)
            {
                step.frontierCells.Add(node.CellPosition);
            }

            if (finalPath != null)
            {
                step.finalPathCells.AddRange(finalPath);
            }

            return step;
        }
    }
}
