using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using DungeonCrawler.Enemy;

namespace DungeonCrawler.AI
{
    public class PathfindingStats
    {
        public float lastPathTimeMs;
        public int visitedNodes;
        public int pathLength;
        public int recalculationCount;
        public bool lastPathFailed;
        public List<Vector3> visitedPositions = new List<Vector3>();
    }

    public class PathfindingBenchmarkUI : MonoBehaviour
    {
        [Header("Settings")]
        [SerializeField] private float updateInterval = 0.25f;

        [Header("UI References")]
        [SerializeField] private Text astarStatsText;
        [SerializeField] private Text bfsStatsText;

        private float nextUpdateTime;

        private static PathfindingStats astarStats = new PathfindingStats();
        private static PathfindingStats bfsStats = new PathfindingStats();

        public static PathfindingStats AStarStats => astarStats;
        public static PathfindingStats BFSStats => bfsStats;

        [Header("Visualization")]
        [SerializeField] private GameObject visitMarkerPrefab;
        [SerializeField] private Transform bfsVisContainer;
        [SerializeField] private Transform astarVisContainer;

        private List<GameObject> markers = new List<GameObject>();

        private void Start()
        {
            if (astarStatsText != null) astarStatsText.color = Color.green;
            if (bfsStatsText != null) bfsStatsText.color = new Color(0.6f, 0.4f, 1f);
        }

        private void Update()
        {
            if (Time.time >= nextUpdateTime)
            {
                UpdateUI();
                nextUpdateTime = Time.time + updateInterval;
            }
        }

        private void UpdateUI()
        {
            if (astarStatsText != null) astarStatsText.text = GetStatsString("A*", astarStats);
            if (bfsStatsText != null) bfsStatsText.text = GetStatsString("BFS", bfsStats);
        }

        public void ClearMarkers()
        {
            foreach (var m in markers) if (m != null) Destroy(m);
            markers.Clear();
            astarStats.visitedPositions.Clear();
            bfsStats.visitedPositions.Clear();
        }

        public void DrawSearchArea()
        {
            ClearMarkers();
            if (visitMarkerPrefab == null) return;

            // Draw BFS Area (Cyan) - Loang
            if (bfsVisContainer != null)
            {
                foreach (var pos in bfsStats.visitedPositions)
                {
                    GameObject m = Instantiate(visitMarkerPrefab, pos, Quaternion.identity, bfsVisContainer);
                    m.transform.localScale = Vector3.one * 0.95f;
                    var sr = m.GetComponent<SpriteRenderer>();
                    if (sr != null)
                    {
                        sr.color = new Color(0, 0.9f, 1f, 0.45f);
                        sr.sortingOrder = 6;
                    }
                    markers.Add(m);
                }
            }

            // Draw A* Area (Yellow) - Phóng
            if (astarVisContainer != null)
            {
                foreach (var pos in astarStats.visitedPositions)
                {
                    GameObject m = Instantiate(visitMarkerPrefab, pos, Quaternion.identity, astarVisContainer);
                    m.transform.localScale = Vector3.one * 0.95f;
                    var sr = m.GetComponent<SpriteRenderer>();
                    if (sr != null)
                    {
                        sr.color = new Color(1f, 0.8f, 0, 0.45f);
                        sr.sortingOrder = 6;
                    }
                    markers.Add(m);
                }
            }
        }

        private string GetStatsString(string label, PathfindingStats stats)
        {
            if (stats.lastPathFailed && stats.pathLength == 0 && stats.recalculationCount > 0)
            {
                return $"<b>{label} Benchmark</b>\n" +
                       $"<color=red>PATH FAILED</color>\n" +
                       $"Time: {stats.lastPathTimeMs:F2} ms\n" +
                       $"Nodes: {stats.visitedNodes}\n" +
                       $"Recalc: {stats.recalculationCount}";
            }

            return $"<b>{label} Benchmark</b>\n" +
                   $"Time: {stats.lastPathTimeMs:F2} ms\n" +
                   $"Nodes: {stats.visitedNodes}\n" +
                   $"Path Len: {stats.pathLength}\n" +
                   $"Recalc: {stats.recalculationCount}";
        }
    }
}