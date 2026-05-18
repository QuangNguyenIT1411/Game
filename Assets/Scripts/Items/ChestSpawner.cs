using DungeonCrawler.Dungeon;
using DungeonCrawler.Pathfinding;
using UnityEngine;

namespace DungeonCrawler.Items
{
    public class ChestSpawner : MonoBehaviour
    {
        public static void SpawnChestsForFloor(int floor)
        {
            if (FloorManager.Instance != null && FloorManager.Instance.IsVillageMode)
            {
                return;
            }

            int chestCount = floor > 0 && floor % 10 == 0 ? 2 : 1;
            for (int i = 0; i < chestCount; i++)
            {
                SpawnChest(FindChestPosition(i));
            }
        }

        public static GameObject SpawnChest(Vector3 position)
        {
            GameObject chestObject = new GameObject("Chest");
            chestObject.transform.position = position;
            chestObject.transform.rotation = Quaternion.identity;
            chestObject.transform.localScale = Vector3.one;
            chestObject.AddComponent<Chest>();
            return chestObject;
        }

        private static Vector3 FindChestPosition(int index)
        {
            Vector3[] preferredPositions =
            {
                new Vector3(7.5f, 8.5f, 0f),
                new Vector3(14.5f, 8.5f, 0f),
                new Vector3(6.5f, 3.5f, 0f)
            };

            PathfindingGrid grid = PathfindingGrid.Instance;
            if (grid == null)
            {
                return preferredPositions[Mathf.Clamp(index, 0, preferredPositions.Length - 1)];
            }

            foreach (Vector3 preferred in preferredPositions)
            {
                GridNode nearest = FindNearestWalkableNode(grid, preferred);
                if (nearest != null && nearest.IsWalkable)
                {
                    return nearest.WorldPosition;
                }
            }

            foreach (GridNode node in grid.GetAllNodes())
            {
                if (node != null && node.IsWalkable)
                {
                    return node.WorldPosition;
                }
            }

            return preferredPositions[0];
        }

        private static GridNode FindNearestWalkableNode(PathfindingGrid grid, Vector3 target)
        {
            GridNode bestNode = null;
            float bestDistance = float.MaxValue;
            foreach (GridNode node in grid.GetAllNodes())
            {
                if (node == null || !node.IsWalkable)
                {
                    continue;
                }

                float distance = Vector2.Distance(target, node.WorldPosition);
                if (distance < bestDistance)
                {
                    bestDistance = distance;
                    bestNode = node;
                }
            }

            return bestNode;
        }
    }
}
