using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using DungeonCrawler.Pathfinding;
using DungeonCrawler.AI;
using DungeonCrawler.Enemy;
using DungeonCrawler.Dungeon;
using DungeonCrawler.Stats;
using UnityEngine.Tilemaps;
using DungeonCrawler.UI;

namespace DungeonCrawler.AI
{
    public class PerformanceTestManager : MonoBehaviour
    {
        [Header("Grids & Maps")]
        [SerializeField] private PathfindingGrid bfsGrid;
        [SerializeField] private PathfindingGrid astarGrid;
        [SerializeField] private Tilemap bfsFloor;
        [SerializeField] private Tilemap bfsWall;
        [SerializeField] private Tilemap astarFloor;
        [SerializeField] private Tilemap astarWall;

        [Header("Prefabs")]
        [SerializeField] private GameObject astarSlimePrefab;
        [SerializeField] private GameObject bfsSlimePrefab;
        [SerializeField] private GameObject targetPrefab;

        [Header("Settings")]
        [SerializeField] private int mapWidth = 150;
        [SerializeField] private int mapHeight = 150;
        [SerializeField] private int roomCount = 50;
        [SerializeField] private int branchCount = 25;
        [SerializeField] private float slimeSpeed = 12f;

        [Header("UI")]
        [SerializeField] private PathfindingBenchmarkUI benchmarkUI;
        [SerializeField] private UnityEngine.UI.Button runButton;
        [SerializeField] private UnityEngine.UI.Button backButton;

        private GameObject astarSlime;
        private GameObject bfsSlime;
        private GameObject astarTarget;
        private GameObject bfsTarget;

        private void Start()
        {
            if (runButton != null) runButton.onClick.AddListener(GenerateAndRace);
            if (backButton != null) backButton.onClick.AddListener(GoBack);
            
            // Clear any main menu UI that might be active in this scene
            GameUIManager ui = GameUIManager.Instance;
            if (ui != null)
            {
                ui.HideMainMenu();
                // Optionally show some test-specific HUD if needed, 
                // but usually the test manager has its own UI.
            }

            var mainMenu = Object.FindAnyObjectByType<DungeonCrawler.UI.MainMenuUI>(FindObjectsInactive.Include);
            if (mainMenu != null) mainMenu.gameObject.SetActive(false);
        }

        private void Update()
        {
            if (Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame)
            {
                GenerateAndRace();
            }
        }

        public void GenerateAndRace()
        {
            ClearExisting();
            if (benchmarkUI != null) benchmarkUI.ClearMarkers();

            DungeonMapGenerator generator = FindAnyObjectByType<DungeonMapGenerator>();
            if (generator == null) generator = DungeonMapGenerator.Instance;
            
            // Configuration for high-complexity maps
            generator.mapWidth = mapWidth;
            generator.mapHeight = mapHeight;
            generator.roomCountMin = roomCount;
            generator.roomCountMax = roomCount;
            generator.roomSizeMin = 4;
            generator.roomSizeMax = 10;
            generator.branchCount = branchCount;
            generator.useRandomSeed = true;

            // 1. Generate Master Map on BFS Tilemaps
            generator.floorTilemap = bfsFloor;
            generator.wallTilemap = bfsWall;
            DungeonData data = generator.GenerateMap(1, false);
            
            if (data == null)
            {
                Debug.LogError("Map generation failed.");
                return;
            }

            // 2. Clone to A* Tilemaps
            CopyTilemap(bfsFloor, astarFloor);
            CopyTilemap(bfsWall, astarWall);

            // 3. Configure Grids with their specific tilemaps
            if (bfsGrid != null) bfsGrid.Configure(bfsFloor, bfsWall);
            if (astarGrid != null) astarGrid.Configure(astarFloor, astarWall);

            // 4. Calculate relative spawn/exit
            Vector3 relativeSpawn = data.PlayerSpawn - bfsFloor.transform.position;
            Vector3 relativeExit = data.ExitPortal - bfsFloor.transform.position;
            
            // Snap to valid nodes for reliability
            if (bfsGrid.TryGetNodeFromWorld(bfsGrid.transform.position + relativeSpawn, out GridNode sn))
                relativeSpawn = sn.WorldPosition - bfsGrid.transform.position;
            if (bfsGrid.TryGetNodeFromWorld(bfsGrid.transform.position + relativeExit, out GridNode en))
                relativeExit = en.WorldPosition - bfsGrid.transform.position;

            // 5. Spawn Targets
            bfsTarget = Instantiate(targetPrefab, bfsGrid.transform.position + relativeExit, Quaternion.identity);
            bfsTarget.name = "Target_BFS";
            bfsTarget.transform.localScale = Vector3.one * 6.0f;
            AddDummyHealth(bfsTarget);
            
            astarTarget = Instantiate(targetPrefab, astarGrid.transform.position + relativeExit, Quaternion.identity);
            astarTarget.name = "Target_AStar";
            astarTarget.transform.localScale = Vector3.one * 6.0f;
            AddDummyHealth(astarTarget);

            // 6. Spawn Slimes
            bfsSlime = new GameObject("Slime_BFS");
            bfsSlime.transform.position = bfsGrid.transform.position + relativeSpawn;
            bfsSlime.transform.localScale = Vector3.one * 1.0f; // Scale 1.0 fits in 1-tile corridors
            EnemySpawner.SetupEnemy(bfsSlime, 1, EnemyController.PathfindingMode.BFS);
            
            astarSlime = new GameObject("Slime_AStar");
            astarSlime.transform.position = astarGrid.transform.position + relativeSpawn;
            astarSlime.transform.localScale = Vector3.one * 1.0f;
            EnemySpawner.SetupEnemy(astarSlime, 1, EnemyController.PathfindingMode.AStar);

            var bfsCtrl = bfsSlime.GetComponent<EnemyController>();
            var astarCtrl = astarSlime.GetComponent<EnemyController>();

            if (bfsCtrl != null && astarCtrl != null)
            {
                // Force immediate initialization and path calculation
                bfsCtrl.Initialize(bfsTarget.transform, bfsGrid);
                astarCtrl.Initialize(astarTarget.transform, astarGrid);
                
                bfsCtrl.MoveSpeed = slimeSpeed;
                astarCtrl.MoveSpeed = slimeSpeed;
                bfsCtrl.DetectionRange = 5000f;
                astarCtrl.DetectionRange = 5000f;
                
                // 7. Draw Search Areas (the "loang" and "phóng" effects)
                if (benchmarkUI != null)
                {
                    // AI calculates path on Initialize, so stats should be ready
                    benchmarkUI.DrawSearchArea();
                }
            }

            UpdateCameras();
        }

        private bool EnsureReferences()
        {
            if (bfsGrid == null || astarGrid == null)
            {
                PathfindingGrid[] grids = FindObjectsByType<PathfindingGrid>(FindObjectsInactive.Include, FindObjectsSortMode.None);
                foreach (PathfindingGrid grid in grids)
                {
                    string gridName = grid.name.ToLowerInvariant();
                    if (bfsGrid == null && gridName.Contains("bfs")) bfsGrid = grid;
                    if (astarGrid == null && (gridName.Contains("astar") || gridName.Contains("a*"))) astarGrid = grid;
                }
            }

            bfsFloor = ResolveTilemap(bfsFloor, bfsGrid, "BFS_Floor");
            bfsWall = ResolveTilemap(bfsWall, bfsGrid, "BFS_Wall");
            astarFloor = ResolveTilemap(astarFloor, astarGrid, "AStar_Floor");
            astarWall = ResolveTilemap(astarWall, astarGrid, "AStar_Wall");

            if (bfsGrid == null || astarGrid == null || bfsFloor == null || bfsWall == null || astarFloor == null || astarWall == null)
            {
                Debug.LogError("PerformanceTestManager requires bfsGrid, astarGrid, bfsFloor, bfsWall, astarFloor, and astarWall references.");
                return false;
            }

            return true;
        }

        private Tilemap ResolveTilemap(Tilemap current, PathfindingGrid grid, string fallbackName)
        {
            if (current != null) return current;

            if (grid != null)
            {
                Tilemap[] childTilemaps = grid.GetComponentsInChildren<Tilemap>(true);
                bool wantsFloor = fallbackName.ToLowerInvariant().Contains("floor");
                foreach (Tilemap tilemap in childTilemaps)
                {
                    string tilemapName = tilemap.name.ToLowerInvariant();
                    if (wantsFloor && tilemapName.Contains("floor")) return tilemap;
                    if (!wantsFloor && tilemapName.Contains("wall")) return tilemap;
                }

                if (childTilemaps.Length > 0) return childTilemaps[0];
            }

            GameObject tilemapObject = new GameObject(fallbackName, typeof(Tilemap), typeof(TilemapRenderer));
            if (grid != null) tilemapObject.transform.SetParent(grid.transform, false);
            return tilemapObject.GetComponent<Tilemap>();
        }

        private void CopyTilemap(Tilemap source, Tilemap target)
        {
            if (source == null || target == null) return;
            target.ClearAllTiles();
            foreach (var pos in source.cellBounds.allPositionsWithin)
            {
                TileBase tile = source.GetTile(pos);
                if (tile != null) target.SetTile(pos, tile);
            }
        }

        private void AddDummyHealth(GameObject targetObj)
        {
            var h = targetObj.AddComponent<DungeonCrawler.Combat.Health>();
            if (h != null) h.Configure(100, 100);
        }

        private void UpdateCameras()
        {
            Camera camBFS = GameObject.Find("Camera_BFS")?.GetComponent<Camera>();
            if (camBFS != null)
            {
                camBFS.transform.position = bfsGrid.transform.position + new Vector3(mapWidth / 2f, mapHeight / 2f, -10);
                camBFS.orthographicSize = 90;
            }

            Camera camAStar = GameObject.Find("Camera_AStar")?.GetComponent<Camera>();
            if (camAStar != null)
            {
                camAStar.transform.position = astarGrid.transform.position + new Vector3(mapWidth / 2f, mapHeight / 2f, -10);
                camAStar.orthographicSize = 90;
            }
        }

        private void ClearExisting()
        {
            if (bfsSlime != null) Object.Destroy(bfsSlime);
            if (astarSlime != null) Object.Destroy(astarSlime);
            if (bfsTarget != null) Object.Destroy(bfsTarget);
            if (astarTarget != null) Object.Destroy(astarTarget);
        }

        private void GoBack()
        {
            UnityEngine.SceneManagement.SceneManager.LoadScene("DungeonPrototype");
        }
    }
}
