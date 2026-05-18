using DungeonCrawler.Enemy;
using DungeonCrawler.Combat;
using DungeonCrawler.Items;
using DungeonCrawler.Player;
using DungeonCrawler.UI;
using DungeonCrawler.Village;
using System.Collections.Generic;
using UnityEngine;

namespace DungeonCrawler.Dungeon
{
    public class FloorManager : MonoBehaviour
    {
        private static FloorManager _instance;
        public static FloorManager Instance
        {
            get
            {
                if (_instance == null) _instance = FindAnyObjectByType<FloorManager>(FindObjectsInactive.Include);
                return _instance;
            }
        }

        [SerializeField] private int currentFloor = 1;
        [SerializeField] private int highestUnlockedFloor = 1;
        [SerializeField] private int checkpointFloor = 1;
        [SerializeField] private bool villageMode = true;

        private DungeonData currentDungeonData;
        private ExitPortal activePortal;
        private bool hadEnemies;
        private bool floorClearHandled;
        private readonly List<int> unlockedCheckpointFloors = new List<int> { 1 };

        public int CurrentFloor => currentFloor;
        public int HighestUnlockedFloor => highestUnlockedFloor;
        public int CheckpointFloor => checkpointFloor;
        public bool IsVillageMode => villageMode;
        public IReadOnlyList<int> UnlockedCheckpointFloors => unlockedCheckpointFloors;
        public bool IsBossFloor => currentFloor % 10 == 0;

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                if (Application.isPlaying) Destroy(gameObject);
                else DestroyImmediate(gameObject);
                return;
            }
            _instance = this;
        }

        private void Update()
        {
            if (villageMode)
            {
                return;
            }

            EnemyController[] enemies = FindObjectsByType<EnemyController>(FindObjectsInactive.Exclude);
            if (enemies.Length > 0)
            {
                hadEnemies = true;
                floorClearHandled = false;
                return;
            }

            if (hadEnemies && activePortal == null)
            {
                HandleFloorCleared();
                SpawnExitPortal();
            }
        }

        public void AdvanceFloor()
        {
            highestUnlockedFloor = Mathf.Max(highestUnlockedFloor, currentFloor + 1);

            currentFloor++;
            ResetRoomForCurrentFloor();
        }

        public void ResetForGeneratedRoom()
        {
            currentFloor = 1;
            highestUnlockedFloor = 1;
            checkpointFloor = 1;
            hadEnemies = false;
            floorClearHandled = false;
            activePortal = null;
            villageMode = true;
            unlockedCheckpointFloors.Clear();
            unlockedCheckpointFloors.Add(1);
        }

        public void RespawnAtCheckpoint()
        {
            villageMode = false;
            currentFloor = Mathf.Max(1, checkpointFloor);
            ClearRoomRuntimeObjects();
            GenerateMap();

            GameObject player = GameObject.Find("Player");
            if (player != null)
            {
                player.transform.position = currentDungeonData != null ? currentDungeonData.PlayerSpawn : new Vector3(4.5f, 5.5f, 0f);
                Health health = player.GetComponent<Health>();
                if (health != null)
                {
                    health.ReviveFull();
                }

                PlayerMovement movement = player.GetComponent<PlayerMovement>();
                if (movement != null)
                {
                    movement.enabled = true;
                }
            }

            hadEnemies = false;
            floorClearHandled = false;
            SpawnEntities();

            GameUIManager ui = GameUIManager.Instance;
            if (ui != null)
            {
                ui.HideMainMenu();
                ui.ShowGameplayHUD();
            }

            Debug.Log($"Respawned at checkpoint floor {currentFloor}", this);
        }

        public void EnterDungeonAtFloor(int floor)
        {
            villageMode = false;
            currentFloor = Mathf.Max(1, floor);
            checkpointFloor = Mathf.Max(1, checkpointFloor);
            highestUnlockedFloor = Mathf.Max(highestUnlockedFloor, currentFloor);
            ClearRoomRuntimeObjects();
            GenerateMap();
            
            ResetPlayerForDungeon(true);
            hadEnemies = false;
            floorClearHandled = false;
            
            SpawnEntities();
            
            GameUIManager ui = GameUIManager.Instance;
            if (ui != null)
            {
                ui.HideMainMenu();
                ui.ShowGameplayHUD();
                ui.RefreshAllHUD();
            }

            VillageManager villageManager = VillageManager.Instance;
            if (villageManager != null)
            {
                villageManager.SetVillageVisualsVisible(false);
            }
        }

        public void ReturnToVillage()
        {
            villageMode = true;
            ClearRoomRuntimeObjects();
            
            if (DungeonMapGenerator.Instance != null)
            {
                DungeonMapGenerator.Instance.ClearTilemaps();
            }

            ResetPlayerForVillage();
            hadEnemies = false;
            floorClearHandled = false;

            GameUIManager ui = GameUIManager.Instance;
            if (ui != null)
            {
                ui.HideGameplayHUD();
                ui.ShowMainMenu();
            }

            VillageManager villageManager = VillageManager.Instance;
            if (villageManager != null)
            {
                villageManager.EnterVillage();
            }
        }

        public void ClearRoomRuntimeObjectsPublic()
        {
            ClearRoomRuntimeObjects();
        }

        public void ConfigureProgression(int newHighestUnlockedFloor, int newCheckpointFloor, List<int> checkpoints)
        {
            highestUnlockedFloor = Mathf.Max(1, newHighestUnlockedFloor);
            checkpointFloor = Mathf.Max(1, newCheckpointFloor);
            unlockedCheckpointFloors.Clear();

            if (checkpoints != null)
            {
                foreach (int floor in checkpoints)
                {
                    if (floor > 0 && !unlockedCheckpointFloors.Contains(floor))
                    {
                        unlockedCheckpointFloors.Add(floor);
                    }
                }
            }

            if (!unlockedCheckpointFloors.Contains(1))
            {
                unlockedCheckpointFloors.Add(1);
            }

            if (!unlockedCheckpointFloors.Contains(checkpointFloor))
            {
                unlockedCheckpointFloors.Add(checkpointFloor);
            }

            unlockedCheckpointFloors.Sort();

            FloorSelectUI floorSelectUI = Object.FindAnyObjectByType<FloorSelectUI>(FindObjectsInactive.Include);
            if (floorSelectUI != null)
            {
                floorSelectUI.Refresh();
            }
        }

        private void ResetRoomForCurrentFloor()
        {
            ClearRoomRuntimeObjects();
            GenerateMap();
            ResetPlayerForDungeon(false);

            hadEnemies = false;
            floorClearHandled = false;
            SpawnEntities();
        }

        private void GenerateMap()
        {
            if (DungeonMapGenerator.Instance != null)
            {
                currentDungeonData = DungeonMapGenerator.Instance.GenerateMap(currentFloor, IsBossFloor);
                
                // Rebuild Minimap if it exists
                if (MinimapUI.Instance != null)
                {
                    MinimapUI.Instance.Rebuild();
                }

                // Rebuild Tilemap Visuals if they exist
                TilemapDebugVisuals visuals = Object.FindAnyObjectByType<TilemapDebugVisuals>();
                if (visuals != null)
                {
                    visuals.Rebuild();
                }
}
            else
            {
                Debug.LogWarning("DungeonMapGenerator.Instance is missing! Fallback to default positions.");
                currentDungeonData = null;
            }
        }

        private void SpawnEntities()
        {
            if (villageMode) return;

            if (currentFloor == 1)
            {
                Vector3 spawnPos = (currentDungeonData != null && currentDungeonData.EnemySpawns.Count > 0) 
                    ? currentDungeonData.EnemySpawns[0] 
                    : new Vector3(16.5f, 5.5f, 0f);
                
                EnemySpawner.SpawnTestPair(spawnPos, currentFloor);

                if (currentDungeonData != null)
                {
                    foreach (var pos in currentDungeonData.ChestSpawns)
                    {
                        ChestSpawner.SpawnChest(pos);
                    }
                }
                return;
            }

            if (currentDungeonData != null)
{
                foreach (var pos in currentDungeonData.EnemySpawns)
                {
                    EnemySpawner.SpawnAt(pos, currentFloor);
                }

                foreach (var pos in currentDungeonData.ChestSpawns)
                {
                    ChestSpawner.SpawnChest(pos);
                }
            }
            else
            {
                EnemySpawner.SpawnFloorEnemy(currentFloor);
                ChestSpawner.SpawnChestsForFloor(currentFloor);
            }
        }

        private void HandleFloorCleared()
        {
            if (floorClearHandled)
            {
                return;
            }

            floorClearHandled = true;
            if (IsBossFloor)
            {
                UnlockCheckpoint(currentFloor);
            }
        }

        private void UnlockCheckpoint(int floor)
        {
            checkpointFloor = floor;
            highestUnlockedFloor = Mathf.Max(highestUnlockedFloor, floor + 1);
            if (!unlockedCheckpointFloors.Contains(floor))
            {
                unlockedCheckpointFloors.Add(floor);
                unlockedCheckpointFloors.Sort();
            }

            Debug.Log($"Unlocked checkpoint floor {floor}", this);

            FloorSelectUI floorSelectUI = Object.FindAnyObjectByType<FloorSelectUI>(FindObjectsInactive.Include);
            if (floorSelectUI != null)
            {
                floorSelectUI.Refresh();
            }
        }

        private void ResetPlayerForDungeon(bool reviveFull)
        {
            Vector3 spawnPos = currentDungeonData != null ? currentDungeonData.PlayerSpawn : new Vector3(4.5f, 5.5f, 0f);
            ResetPlayer(spawnPos, reviveFull);
        }

        private void ResetPlayerForVillage()
        {
            ResetPlayer(new Vector3(4.5f, 5.5f, 0f), true);
        }

        private static void ResetPlayer(Vector3 position, bool reviveFull)
        {
            GameObject player = GameObject.Find("Player");
            if (player == null)
            {
                return;
            }

            player.transform.position = position;
            Rigidbody2D rb = player.GetComponent<Rigidbody2D>();
            if (rb != null)
            {
                rb.linearVelocity = Vector2.zero;
            }

            Health health = player.GetComponent<Health>();
if (health != null && reviveFull)
            {
                health.ReviveFull();
            }

            PlayerMovement movement = player.GetComponent<PlayerMovement>();
            if (movement != null)
            {
                movement.enabled = true;
            }
        }

        private void ClearRoomRuntimeObjects()
        {
            if (activePortal != null)
            {
                if (Application.isPlaying) Destroy(activePortal.gameObject);
                else DestroyImmediate(activePortal.gameObject);
                activePortal = null;
            }

            // Must include inactive objects because GameUIManager hides them at Main Menu
            foreach (EnemyController enemy in FindObjectsByType<EnemyController>(FindObjectsInactive.Include))
            {
                if (Application.isPlaying) Destroy(enemy.gameObject);
                else DestroyImmediate(enemy.gameObject);
            }

            foreach (ItemPickup itemPickup in FindObjectsByType<ItemPickup>(FindObjectsInactive.Include))
            {
                if (Application.isPlaying) Destroy(itemPickup.gameObject);
                else DestroyImmediate(itemPickup.gameObject);
            }

            foreach (Chest chest in FindObjectsByType<Chest>(FindObjectsInactive.Include))
            {
                if (Application.isPlaying) Destroy(chest.gameObject);
                else DestroyImmediate(chest.gameObject);
            }
        }

        private void SpawnExitPortal()
        {
            GameObject portalObject = new GameObject("ExitPortal");
            Vector3 portalPos = currentDungeonData != null ? currentDungeonData.ExitPortal : new Vector3(16.5f, 5.5f, 0f);
            portalObject.transform.position = portalPos;

            activePortal = portalObject.AddComponent<ExitPortal>();
SpriteRenderer spriteRenderer = portalObject.AddComponent<SpriteRenderer>();
            spriteRenderer.sprite = CreateSquareSprite(new Color(0.2f, 0.9f, 1f));
            spriteRenderer.sortingOrder = 8;
            portalObject.transform.localScale = new Vector3(0.75f, 0.75f, 1f);

            CircleCollider2D portalCollider = portalObject.AddComponent<CircleCollider2D>();
            portalCollider.isTrigger = true;
            portalCollider.radius = 0.45f;
        }

        private static Sprite CreateSquareSprite(Color color)
        {
            Texture2D texture = new Texture2D(1, 1)
            {
                filterMode = FilterMode.Point,
                hideFlags = HideFlags.HideAndDontSave
            };

            texture.SetPixel(0, 0, color);
            texture.Apply();
            return Sprite.Create(texture, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f), 1f);
        }
    }

}
