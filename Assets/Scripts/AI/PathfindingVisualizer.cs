using System.Collections;
using System.Collections.Generic;
using DungeonCrawler.Pathfinding;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace DungeonCrawler.AI
{
    public class PathfindingVisualizer : MonoBehaviour
    {
        private enum VisualizationMode
        {
            AStar,
            BFS
        }

        public static PathfindingVisualizer Instance { get; private set; }

        [SerializeField] private float visualizationStepDelay = 0.08f;
        [SerializeField] private int overlaySortingOrder = 8;
        [SerializeField] private bool clearBeforeStart = true;

        private readonly List<GameObject> overlayObjects = new List<GameObject>();
        private readonly List<GameObject> astarOverlayObjects = new List<GameObject>();
        private readonly List<GameObject> bfsOverlayObjects = new List<GameObject>();
        private readonly Dictionary<string, Sprite> spriteCache = new Dictionary<string, Sprite>();
        private Transform overlayRoot;
        private Tilemap floorTilemap;
        private Tilemap wallTilemap;
        private Coroutine autoPlayCoroutine;

        private readonly Color astarVisitedColor = new Color(0.45f, 1f, 0.45f, 0.4f);
        private readonly Color astarFrontierColor = new Color(0.8f, 1f, 0.55f, 0.35f);
        private readonly Color astarPathColor = new Color(0.1f, 0.9f, 0.1f, 0.75f);
        private readonly Color bfsVisitedColor = new Color(0.55f, 0.45f, 1f, 0.42f);
        private readonly Color bfsFrontierColor = new Color(0.35f, 0.85f, 1f, 0.35f);
        private readonly Color bfsPathColor = new Color(0f, 0.9f, 1f, 0.75f);
        private readonly Color currentNodeColor = new Color(1f, 0.9f, 0.05f, 0.85f);

        public float VisualizationStepDelay
        {
            get => visualizationStepDelay;
            set => visualizationStepDelay = Mathf.Clamp(value, 0.02f, 0.5f);
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            EnsureOverlayRoot();
            FindTilemaps();
        }

        public static PathfindingVisualizer GetOrCreate()
        {
            if (Instance != null)
            {
                return Instance;
            }

            GameObject visualizerObject = new GameObject("PathfindingVisualizer");
            return visualizerObject.AddComponent<PathfindingVisualizer>();
        }

        public void ShowAStarSearch()
        {
            StartVisualization(VisualizationMode.AStar, "Enemy_AStar_Test", true);
            Debug.Log("A* visualization started");
        }

        public void ShowBFSSearch()
        {
            StartVisualization(VisualizationMode.BFS, "Enemy_BFS_Test", true);
            Debug.Log("BFS visualization started");
        }

        public void CompareSearches()
        {
            if (clearBeforeStart)
            {
                ClearVisualization();
            }

            StopAutoPlay();
            List<PathfindingStepData> astarSteps = BuildSteps(VisualizationMode.AStar, "Enemy_AStar_Test");
            List<PathfindingStepData> bfsSteps = BuildSteps(VisualizationMode.BFS, "Enemy_BFS_Test");

            if (astarSteps.Count == 0 && bfsSteps.Count == 0)
            {
                return;
            }

            autoPlayCoroutine = StartCoroutine(PlayCompareSteps(astarSteps, bfsSteps));
            Debug.Log("Compare visualization started");
        }

        public void StopAutoPlay()
        {
            if (autoPlayCoroutine != null)
            {
                StopCoroutine(autoPlayCoroutine);
                autoPlayCoroutine = null;
            }
        }

        public void ClearVisualization()
        {
            StopAutoPlay();
            foreach (GameObject overlayObject in overlayObjects)
            {
                if (overlayObject != null)
                {
                    Destroy(overlayObject);
                }
            }

            overlayObjects.Clear();
            astarOverlayObjects.Clear();
            bfsOverlayObjects.Clear();
            Debug.Log("Visualization cleared");
        }

        private void StartVisualization(VisualizationMode mode, string enemyName, bool clearFirst)
        {
            if (clearFirst && clearBeforeStart)
            {
                ClearVisualization();
            }
            else
            {
                StopAutoPlay();
            }

            List<PathfindingStepData> steps = BuildSteps(mode, enemyName);
            if (steps.Count == 0)
            {
                return;
            }

            autoPlayCoroutine = StartCoroutine(PlaySteps(steps, mode, Vector3.zero));
        }

        private List<PathfindingStepData> BuildSteps(VisualizationMode mode, string enemyName)
        {
            GameObject enemyObject = GameObject.Find(enemyName);
            if (enemyObject == null)
            {
                Debug.LogWarning($"{enemyName} not found. Cannot start pathfinding visualization.");
                return new List<PathfindingStepData>();
            }

            GameObject playerObject = GameObject.Find("Player");
            if (playerObject == null)
            {
                Debug.LogWarning("Player not found. Cannot start pathfinding visualization.");
                return new List<PathfindingStepData>();
            }

            PathfindingGrid grid = PathfindingGrid.Instance;
            if (grid == null)
            {
                Debug.LogWarning("PathfindingGrid not found. Cannot start pathfinding visualization.");
                return new List<PathfindingStepData>();
            }

            if (mode == VisualizationMode.BFS)
            {
                return BreadthFirstSearchPathfinder.GenerateSearchSteps(grid, enemyObject.transform.position, playerObject.transform.position);
            }

            return AStarPathfinder.GenerateSearchSteps(grid, enemyObject.transform.position, playerObject.transform.position);
        }

        private IEnumerator PlaySteps(List<PathfindingStepData> steps, VisualizationMode mode, Vector3 offset)
        {
            for (int i = 0; i < steps.Count; i++)
            {
                DrawStep(steps[i], mode, offset);
                Debug.Log($"Visualization step: {i + 1}/{steps.Count}");
                yield return new WaitForSecondsRealtime(visualizationStepDelay);
            }

            autoPlayCoroutine = null;
        }

        private IEnumerator PlayCompareSteps(List<PathfindingStepData> astarSteps, List<PathfindingStepData> bfsSteps)
        {
            int maxStepCount = Mathf.Max(astarSteps.Count, bfsSteps.Count);
            Vector3 astarOffset = new Vector3(-0.08f, 0.08f, 0f);
            Vector3 bfsOffset = new Vector3(0.08f, -0.08f, 0f);

            for (int i = 0; i < maxStepCount; i++)
            {
                if (i < astarSteps.Count)
                {
                    DrawStep(astarSteps[i], VisualizationMode.AStar, astarOffset);
                }

                if (i < bfsSteps.Count)
                {
                    DrawStep(bfsSteps[i], VisualizationMode.BFS, bfsOffset);
                }

                Debug.Log($"Visualization step: {i + 1}/{maxStepCount}");
                yield return new WaitForSecondsRealtime(visualizationStepDelay);
            }

            autoPlayCoroutine = null;
        }

        private void DrawStep(PathfindingStepData step, VisualizationMode mode, Vector3 offset)
        {
            EnsureOverlayRoot();
            FindTilemaps();
            ClearModeVisualization(mode);

            Color visitedColor = mode == VisualizationMode.AStar ? astarVisitedColor : bfsVisitedColor;
            Color frontierColor = mode == VisualizationMode.AStar ? astarFrontierColor : bfsFrontierColor;
            Color pathColor = mode == VisualizationMode.AStar ? astarPathColor : bfsPathColor;

            foreach (Vector3Int cell in step.visitedCells)
            {
                DrawCell(cell, visitedColor, 0.78f, offset, "Visited", mode);
            }

            foreach (Vector3Int cell in step.frontierCells)
            {
                DrawCell(cell, frontierColor, 0.55f, offset, "Frontier", mode);
            }

            DrawCell(step.currentCell, currentNodeColor, 0.88f, offset, "Current", mode);

            if (step.finalPathCells.Count > 0)
            {
                foreach (Vector3Int cell in step.finalPathCells)
                {
                    DrawCell(cell, pathColor, 0.62f, offset, "Path", mode);
                }
            }
        }

        private void ClearModeVisualization(VisualizationMode mode)
        {
            List<GameObject> modeObjects = mode == VisualizationMode.AStar ? astarOverlayObjects : bfsOverlayObjects;
            foreach (GameObject overlayObject in modeObjects)
            {
                if (overlayObject != null)
                {
                    overlayObjects.Remove(overlayObject);
                    Destroy(overlayObject);
                }
            }

            modeObjects.Clear();
        }

        private void DrawCell(Vector3Int cell, Color color, float scale, Vector3 offset, string label, VisualizationMode mode)
        {
            if (IsWallCell(cell))
            {
                return;
            }

            Vector3 worldPosition = GetCellWorldPosition(cell) + offset;
            string modeName = mode == VisualizationMode.AStar ? "AStar" : "BFS";
            GameObject marker = new GameObject($"Pathfinding_{modeName}_{label}_{cell.x}_{cell.y}");
            marker.transform.SetParent(overlayRoot);
            marker.transform.position = new Vector3(worldPosition.x, worldPosition.y, -0.2f);
            marker.transform.localScale = new Vector3(scale, scale, 1f);

            SpriteRenderer spriteRenderer = marker.AddComponent<SpriteRenderer>();
            spriteRenderer.sprite = GetSquareSprite(color);
            spriteRenderer.color = color;
            spriteRenderer.sortingOrder = overlaySortingOrder;

            overlayObjects.Add(marker);
            if (mode == VisualizationMode.AStar)
            {
                astarOverlayObjects.Add(marker);
            }
            else
            {
                bfsOverlayObjects.Add(marker);
            }
        }

        private Vector3 GetCellWorldPosition(Vector3Int cell)
        {
            if (floorTilemap != null)
            {
                return floorTilemap.GetCellCenterWorld(cell);
            }

            return new Vector3(cell.x + 0.5f, cell.y + 0.5f, 0f);
        }

        private bool IsWallCell(Vector3Int cell)
        {
            return wallTilemap != null && wallTilemap.HasTile(cell);
        }

        private void EnsureOverlayRoot()
        {
            if (overlayRoot != null)
            {
                return;
            }

            GameObject existing = GameObject.Find("PathfindingVisualizationOverlay");
            if (existing != null)
            {
                overlayRoot = existing.transform;
                return;
            }

            GameObject overlayObject = new GameObject("PathfindingVisualizationOverlay");
            overlayRoot = overlayObject.transform;
        }

        private void FindTilemaps()
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

        private Sprite GetSquareSprite(Color color)
        {
            string key = ColorUtility.ToHtmlStringRGBA(color);
            if (spriteCache.TryGetValue(key, out Sprite sprite))
            {
                return sprite;
            }

            Texture2D texture = new Texture2D(1, 1)
            {
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp
            };
            texture.SetPixel(0, 0, Color.white);
            texture.Apply();

            Sprite createdSprite = Sprite.Create(texture, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f), 1f);
            spriteCache[key] = createdSprite;
            return createdSprite;
        }
    }
}
