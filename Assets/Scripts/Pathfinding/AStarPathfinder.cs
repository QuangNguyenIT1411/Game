using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;
using DungeonCrawler.AI;

namespace DungeonCrawler.Pathfinding
{
    /// <summary>
    /// Grid A* pathfinder using 4-direction movement.
    /// </summary>
    public static class AStarPathfinder
    {
        private const int StraightCost = 10;

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

            foreach (GridNode node in grid.GetAllNodes())
            {
                node.ResetPathData();
            }

            List<GridNode> openSet = new List<GridNode> { startNode };
            HashSet<GridNode> closedSet = new HashSet<GridNode>();

            startNode.GCost = 0;
            startNode.HCost = GetDistance(startNode, targetNode);

            while (openSet.Count > 0)
            {
                GridNode currentNode = GetLowestCostNode(openSet);
                openSet.Remove(currentNode);
                closedSet.Add(currentNode);

                if (currentNode == targetNode)
                {
                    List<Vector3Int> finalPath = RetracePathCells(startNode, targetNode);
                    steps.Add(CreateStep(currentNode, closedSet, openSet, finalPath, true, true));
                    return steps;
                }

                foreach (GridNode neighbour in grid.GetNeighbours(currentNode))
                {
                    if (closedSet.Contains(neighbour))
                    {
                        continue;
                    }

                    int tentativeGCost = currentNode.GCost + GetDistance(currentNode, neighbour);
                    if (tentativeGCost >= neighbour.GCost)
                    {
                        continue;
                    }

                    neighbour.Parent = currentNode;
                    neighbour.GCost = tentativeGCost;
                    neighbour.HCost = GetDistance(neighbour, targetNode);

                    if (!openSet.Contains(neighbour))
                    {
                        openSet.Add(neighbour);
                    }
                }

                steps.Add(CreateStep(currentNode, closedSet, openSet, null, false, false));
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

            foreach (GridNode node in grid.GetAllNodes())
            {
                node.ResetPathData();
            }

            List<GridNode> openSet = new List<GridNode> { startNode };
            HashSet<GridNode> closedSet = new HashSet<GridNode>();

            startNode.GCost = 0;
            startNode.HCost = GetDistance(startNode, targetNode);

            while (openSet.Count > 0)
            {
                nodesVisited++;
                GridNode currentNode = GetLowestCostNode(openSet);
                
                if (stats != null) stats.visitedPositions.Add(currentNode.WorldPosition);

                if (currentNode == targetNode)
{
                    path = RetracePath(startNode, targetNode);
                    sw.Stop();
                    if (stats != null)
                    {
                        stats.lastPathTimeMs = (float)sw.Elapsed.TotalMilliseconds;
                        stats.visitedNodes = nodesVisited;
                        stats.pathLength = path.Count;
                        stats.lastPathFailed = false;
                    }
                    return path;
                }

                openSet.Remove(currentNode);
                closedSet.Add(currentNode);

                foreach (GridNode neighbour in grid.GetNeighbours(currentNode))
                {
                    if (closedSet.Contains(neighbour))
                    {
                        continue;
                    }

                    int tentativeGCost = currentNode.GCost + GetDistance(currentNode, neighbour);
                    if (tentativeGCost >= neighbour.GCost)
                    {
                        continue;
                    }

                    neighbour.Parent = currentNode;
                    neighbour.GCost = tentativeGCost;
                    neighbour.HCost = GetDistance(neighbour, targetNode);

                    if (!openSet.Contains(neighbour))
                    {
                        openSet.Add(neighbour);
                    }
                }
            }

            sw.Stop();
            if (stats != null)
            {
                stats.lastPathTimeMs = (float)sw.Elapsed.TotalMilliseconds;
                stats.visitedNodes = nodesVisited;
                stats.pathLength = 0;
                stats.lastPathFailed = true;
                UnityEngine.Debug.Log($"[A*] Path calculation failed in {stats.lastPathTimeMs:F3} ms");
            }

            return path;
        }

        private static List<Vector3> RetracePath(GridNode startNode, GridNode targetNode)
        {
            List<Vector3> path = new List<Vector3>();
            GridNode currentNode = targetNode;

            while (currentNode != null && currentNode != startNode)
            {
                path.Add(currentNode.WorldPosition);
                currentNode = currentNode.Parent;
            }

            path.Reverse();
            return path;
        }

        private static List<Vector3Int> RetracePathCells(GridNode startNode, GridNode targetNode)
        {
            List<Vector3Int> path = new List<Vector3Int>();
            GridNode currentNode = targetNode;

            while (currentNode != null && currentNode != startNode)
            {
                path.Add(currentNode.CellPosition);
                currentNode = currentNode.Parent;
            }

            path.Reverse();
            return path;
        }

        private static PathfindingStepData CreateStep(
            GridNode currentNode,
            HashSet<GridNode> closedSet,
            List<GridNode> openSet,
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

            foreach (GridNode node in closedSet)
            {
                step.visitedCells.Add(node.CellPosition);
            }

            foreach (GridNode node in openSet)
            {
                step.frontierCells.Add(node.CellPosition);
            }

            if (finalPath != null)
            {
                step.finalPathCells.AddRange(finalPath);
            }

            return step;
        }

        private static GridNode GetLowestCostNode(List<GridNode> nodes)
        {
            GridNode bestNode = nodes[0];
            for (int i = 1; i < nodes.Count; i++)
            {
                GridNode candidate = nodes[i];
                if (candidate.FCost < bestNode.FCost ||
                    candidate.FCost == bestNode.FCost && candidate.HCost < bestNode.HCost)
                {
                    bestNode = candidate;
                }
            }

            return bestNode;
        }

        private static int GetDistance(GridNode from, GridNode to)
        {
            int distanceX = Mathf.Abs(from.CellPosition.x - to.CellPosition.x);
            int distanceY = Mathf.Abs(from.CellPosition.y - to.CellPosition.y);
            return (distanceX + distanceY) * StraightCost;
        }
    }
}
