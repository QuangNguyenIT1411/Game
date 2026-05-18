using System.Collections;
using System.Collections.Generic;
using DungeonCrawler.Pathfinding;
using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEngine.UI;

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
        private GameObject statsRoot;
        private Text astarStatsText;
        private Text bfsStatsText;
        private Font statsFont;

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
            StartVisualization(VisualizationMode.AStar, "Enemy_AStar_Test", true, true);
            ShowStatsPanel(true, false);
            Debug.Log("Visualization stats shown: A*");
            Debug.Log("A* visualization started");
        }

        public void ShowBFSSearch()
        {
            StartVisualization(VisualizationMode.BFS, "Enemy_BFS_Test", true, true);
            ShowStatsPanel(false, true);
            Debug.Log("Visualization stats shown: BFS");
            Debug.Log("BFS visualization started");
        }

        public void CompareSearches()
        {
            if (clearBeforeStart)
            {
                ClearVisualization();
            }

            StopAutoPlay();
            VisualizationRunData astarRun = BuildRunData(VisualizationMode.AStar, "Enemy_AStar_Test");
            VisualizationRunData bfsRun = BuildRunData(VisualizationMode.BFS, "Enemy_BFS_Test");

            if (astarRun.steps.Count == 0 && bfsRun.steps.Count == 0)
            {
                return;
            }

            ShowStatsPanel(true, true);
            Debug.Log("Visualization stats shown: Compare");
            autoPlayCoroutine = StartCoroutine(PlayCompareSteps(astarRun, bfsRun));
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

        public void HideStatsPanel()
        {
            if (statsRoot != null)
            {
                statsRoot.SetActive(false);
            }

            Debug.Log("Visualization stats hidden");
        }

        private void StartVisualization(VisualizationMode mode, string enemyName, bool clearFirst, bool showSingleStats)
        {
            if (clearFirst && clearBeforeStart)
            {
                ClearVisualization();
            }
            else
            {
                StopAutoPlay();
            }

            VisualizationRunData runData = BuildRunData(mode, enemyName);
            if (runData.steps.Count == 0)
            {
                return;
            }

            if (showSingleStats)
            {
                UpdateStatsText(mode, null, runData, 0, runData.steps.Count);
            }

            autoPlayCoroutine = StartCoroutine(PlaySteps(runData, mode, Vector3.zero));
        }

        private VisualizationRunData BuildRunData(VisualizationMode mode, string enemyName)
        {
            VisualizationRunData runData = new VisualizationRunData();
            GameObject enemyObject = GameObject.Find(enemyName);
            if (enemyObject == null)
            {
                Debug.LogWarning($"{enemyName} not found. Cannot start pathfinding visualization.");
                return runData;
            }

            GameObject playerObject = GameObject.Find("Player");
            if (playerObject == null)
            {
                Debug.LogWarning("Player not found. Cannot start pathfinding visualization.");
                return runData;
            }

            PathfindingGrid grid = PathfindingGrid.Instance;
            if (grid == null)
            {
                Debug.LogWarning("PathfindingGrid not found. Cannot start pathfinding visualization.");
                return runData;
            }

            if (mode == VisualizationMode.BFS)
            {
                PathfindingStats stats = PathfindingBenchmarkUI.BFSStats;
                BreadthFirstSearchPathfinder.FindPath(grid, enemyObject.transform.position, playerObject.transform.position, stats);
                runData.stats = CopyStats(stats);
                runData.steps = BreadthFirstSearchPathfinder.GenerateSearchSteps(grid, enemyObject.transform.position, playerObject.transform.position);
                return runData;
            }

            PathfindingStats astarStats = PathfindingBenchmarkUI.AStarStats;
            AStarPathfinder.FindPath(grid, enemyObject.transform.position, playerObject.transform.position, astarStats);
            runData.stats = CopyStats(astarStats);
            runData.steps = AStarPathfinder.GenerateSearchSteps(grid, enemyObject.transform.position, playerObject.transform.position);
            return runData;
        }

        private IEnumerator PlaySteps(VisualizationRunData runData, VisualizationMode mode, Vector3 offset)
        {
            for (int i = 0; i < runData.steps.Count; i++)
            {
                DrawStep(runData.steps[i], mode, offset);
                UpdateStatsText(mode, runData.steps[i], runData, i + 1, runData.steps.Count);
                Debug.Log($"Visualization step: {i + 1}/{runData.steps.Count}");
                yield return new WaitForSecondsRealtime(visualizationStepDelay);
            }

            autoPlayCoroutine = null;
        }

        private IEnumerator PlayCompareSteps(VisualizationRunData astarRun, VisualizationRunData bfsRun)
        {
            int maxStepCount = Mathf.Max(astarRun.steps.Count, bfsRun.steps.Count);
            Vector3 astarOffset = new Vector3(-0.08f, 0.08f, 0f);
            Vector3 bfsOffset = new Vector3(0.08f, -0.08f, 0f);

            for (int i = 0; i < maxStepCount; i++)
            {
                if (i < astarRun.steps.Count)
                {
                    DrawStep(astarRun.steps[i], VisualizationMode.AStar, astarOffset);
                    UpdateStatsText(VisualizationMode.AStar, astarRun.steps[i], astarRun, i + 1, astarRun.steps.Count);
                }

                if (i < bfsRun.steps.Count)
                {
                    DrawStep(bfsRun.steps[i], VisualizationMode.BFS, bfsOffset);
                    UpdateStatsText(VisualizationMode.BFS, bfsRun.steps[i], bfsRun, i + 1, bfsRun.steps.Count);
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

        private void ShowStatsPanel(bool showAStar, bool showBFS)
        {
            EnsureStatsUi();
            statsRoot.SetActive(true);
            statsRoot.transform.SetAsLastSibling();
            CanvasGroup canvasGroup = statsRoot.GetComponent<CanvasGroup>();
            if (canvasGroup == null)
            {
                canvasGroup = statsRoot.AddComponent<CanvasGroup>();
            }

            canvasGroup.alpha = 1f;
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;

            astarStatsText.transform.parent.gameObject.SetActive(showAStar);
            bfsStatsText.transform.parent.gameObject.SetActive(showBFS);
            SetStatsBlockEnabled(astarStatsText, showAStar);
            SetStatsBlockEnabled(bfsStatsText, showBFS);
            Debug.Log("Visualization stats root active = true");
            Debug.Log($"A* stats panel active = {showAStar}");
            Debug.Log($"BFS stats panel active = {showBFS}");
        }

        private void EnsureStatsUi()
        {
            if (statsRoot != null && astarStatsText != null && bfsStatsText != null)
            {
                return;
            }

            Canvas canvas = FindHudCanvas();
            if (canvas == null)
            {
                GameObject canvasObject = new GameObject("HUDCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
                canvas = canvasObject.GetComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            }

            canvas.sortingOrder = Mathf.Max(canvas.sortingOrder, 120);
            Transform existingRoot = canvas.transform.Find("PathfindingVisualizationStats");
            bool createdRuntime = existingRoot == null;
            statsRoot = existingRoot != null ? existingRoot.gameObject : new GameObject("PathfindingVisualizationStats", typeof(RectTransform), typeof(Image), typeof(VerticalLayoutGroup), typeof(CanvasGroup));
            statsRoot.transform.SetParent(canvas.transform, false);
            statsRoot.transform.localScale = Vector3.one;

            RectTransform rootRect = statsRoot.GetComponent<RectTransform>();
            rootRect.anchorMin = new Vector2(0f, 1f);
            rootRect.anchorMax = new Vector2(0f, 1f);
            rootRect.pivot = new Vector2(0f, 1f);
            rootRect.anchoredPosition = new Vector2(20f, -120f);
            rootRect.sizeDelta = new Vector2(320f, 180f);

            Image rootImage = statsRoot.GetComponent<Image>();
            if (rootImage == null)
            {
                rootImage = statsRoot.AddComponent<Image>();
            }

            rootImage.color = new Color(0f, 0f, 0f, 0.65f);
            rootImage.enabled = true;
            rootImage.raycastTarget = false;

            VerticalLayoutGroup rootLayout = statsRoot.GetComponent<VerticalLayoutGroup>();
            rootLayout.childAlignment = TextAnchor.UpperLeft;
            rootLayout.padding = new RectOffset(10, 10, 10, 10);
            rootLayout.spacing = 6f;
            rootLayout.childControlWidth = false;
            rootLayout.childControlHeight = false;
            rootLayout.childForceExpandWidth = false;
            rootLayout.childForceExpandHeight = false;

            astarStatsText = EnsureStatsBlock("AStarStatsPanel", "A* Search\nTime: calculating...\nNodes: 0\nPath Length: 0", astarVisitedColor);
            bfsStatsText = EnsureStatsBlock("BFSStatsPanel", "BFS Search\nTime: calculating...\nNodes: 0\nPath Length: 0", bfsVisitedColor);
            statsRoot.SetActive(false);
            if (createdRuntime)
            {
                Debug.Log("Visualization stats UI created runtime");
            }
        }

        private Text EnsureStatsBlock(string objectName, string initialText, Color textColor)
        {
            Transform existing = statsRoot.transform.Find(objectName);
            GameObject panelObject = existing != null ? existing.gameObject : new GameObject(objectName, typeof(RectTransform), typeof(Image));
            panelObject.transform.SetParent(statsRoot.transform, false);

            RectTransform panelRect = panelObject.GetComponent<RectTransform>();
            panelRect.localScale = Vector3.one;
            panelRect.sizeDelta = new Vector2(300f, 78f);

            LayoutElement layoutElement = panelObject.GetComponent<LayoutElement>();
            if (layoutElement == null)
            {
                layoutElement = panelObject.AddComponent<LayoutElement>();
            }

            layoutElement.preferredWidth = 300f;
            layoutElement.preferredHeight = 78f;
            layoutElement.minWidth = 300f;
            layoutElement.minHeight = 78f;

            Image image = panelObject.GetComponent<Image>();
            image.color = new Color(0f, 0f, 0f, 0.65f);
            image.enabled = true;
            image.raycastTarget = false;

            Transform textTransform = panelObject.transform.Find("Text");
            GameObject textObject = textTransform != null ? textTransform.gameObject : new GameObject("Text", typeof(RectTransform), typeof(Text));
            textObject.transform.SetParent(panelObject.transform, false);

            RectTransform textRect = textObject.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = new Vector2(12f, 8f);
            textRect.offsetMax = new Vector2(-12f, -8f);

            Text text = textObject.GetComponent<Text>();
            text.text = initialText;
            text.font = GetStatsFont();
            text.fontSize = 22;
            text.alignment = TextAnchor.UpperLeft;
            text.color = new Color(textColor.r, textColor.g, textColor.b, 1f);
            text.enabled = true;
            text.raycastTarget = false;
            return text;
        }

        private void UpdateStatsText(VisualizationMode mode, PathfindingStepData step, VisualizationRunData runData, int currentStep, int totalSteps)
        {
            EnsureStatsUi();
            Text text = mode == VisualizationMode.AStar ? astarStatsText : bfsStatsText;
            if (text == null)
            {
                return;
            }

            string title = mode == VisualizationMode.AStar ? "A* Search" : "BFS Search";
            int nodes = step != null ? step.visitedCells.Count : 0;
            bool finished = step != null && step.isFinished;
            bool failed = finished && !step.pathFound || finished && runData.stats.lastPathFailed;
            string timeLine = finished ? $"Time: {runData.stats.lastPathTimeMs:0.###} ms" : "Time: calculating...";
            string pathLine = finished ? $"Path Length: {runData.stats.pathLength}" : "Path Length: calculating...";

            if (failed)
            {
                text.text = $"{title}\nPATH FAILED\nNodes: {nodes}\nPath Length: 0";
                Debug.Log($"Stats text updated: {text.text}");
                return;
            }

            text.text = $"{title}\n{timeLine}\nNodes: {nodes}\n{pathLine}";
            Debug.Log($"Stats text updated: {text.text}");
        }

        private void SetStatsBlockEnabled(Text text, bool enabled)
        {
            if (text == null)
            {
                return;
            }

            text.enabled = enabled;
            Image image = text.transform.parent.GetComponent<Image>();
            if (image != null)
            {
                image.enabled = enabled;
            }
        }

        private Canvas FindHudCanvas()
        {
            GameObject hudCanvasObject = GameObject.Find("HUDCanvas");
            if (hudCanvasObject != null && hudCanvasObject.TryGetComponent(out Canvas hudCanvas))
            {
                return hudCanvas;
            }

            Canvas[] canvases = FindObjectsByType<Canvas>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (Canvas canvas in canvases)
            {
                if (canvas.name == "HUDCanvas")
                {
                    return canvas;
                }
            }

            return canvases.Length > 0 ? canvases[0] : null;
        }

        private PathfindingStats CopyStats(PathfindingStats source)
        {
            return new PathfindingStats
            {
                lastPathTimeMs = source.lastPathTimeMs,
                visitedNodes = source.visitedNodes,
                pathLength = source.pathLength,
                recalculationCount = source.recalculationCount,
                lastPathFailed = source.lastPathFailed,
                visitedPositions = new List<Vector3>(source.visitedPositions)
            };
        }

        private Font GetStatsFont()
        {
            if (statsFont != null)
            {
                return statsFont;
            }

            statsFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (statsFont == null)
            {
                statsFont = Resources.GetBuiltinResource<Font>("Arial.ttf");
            }

            return statsFont;
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

        private class VisualizationRunData
        {
            public List<PathfindingStepData> steps = new List<PathfindingStepData>();
            public PathfindingStats stats = new PathfindingStats();
        }
    }
}
