using DungeonCrawler.Combat;
using DungeonCrawler.Dungeon;
using DungeonCrawler.Items;
using DungeonCrawler.Pathfinding;
using DungeonCrawler.Stats;
using DungeonCrawler.UI;
using UnityEngine;
using UnityEngine.UI;

namespace DungeonCrawler.Enemy
{
    /// <summary>
    /// Small helper for creating and validating a test enemy in the dungeon room.
    /// </summary>
    public class EnemySpawner : MonoBehaviour
    {
        public const string EnemyName = "Enemy";
        private static readonly Vector3 DungeonEnemySpawnPosition = new Vector3(16.5f, 5.5f, 0f);

        [SerializeField] private GameObject normalSlimePrefab;
        [SerializeField] private GameObject bfsSlimePrefab;
        [SerializeField] private GameObject bossSlimePrefab;
        [SerializeField] private GameObject bossFallbackSlimePrefab;

        private static EnemySpawner instance;

        [Header("Performance Testing")]
        public bool spawnAlgorithmTestPair = true;
        public int normalEnemyCount = 2;

        private void Awake()
        {
            instance = this;
        }

        public static EnemySpawner Instance => instance != null ? instance : Object.FindAnyObjectByType<EnemySpawner>();

        public void BindSlimePrefabs(GameObject normalPrefab, GameObject bfsPrefab, GameObject bossPrefab, GameObject fallbackBossPrefab)
        {
            normalSlimePrefab = normalPrefab;
            bfsSlimePrefab = bfsPrefab;
            bossSlimePrefab = bossPrefab;
            bossFallbackSlimePrefab = fallbackBossPrefab;
            instance = this;
        }

        public void SpawnTestEnemyFromButton()
        {
            SpawnTestEnemy();
        }

        public static void SpawnTestPair(Vector3 position, int floor)
        {
            Vector3 astarPos = position + new Vector3(-0.25f, 0, 0);
            Vector3 bfsPos = position + new Vector3(0.25f, 0, 0);

            GameObject astar = SpawnAt(astarPos, floor, EnemyController.PathfindingMode.AStar);
            if (astar != null)
            {
                astar.name = "Enemy_AStar_Test";
                ConfigureBenchmarkDetection(astar);
                Debug.Log($"Spawned A* test enemy at {astarPos}");
            }

            GameObject bfs = SpawnAt(bfsPos, floor, EnemyController.PathfindingMode.BFS);
            if (bfs != null)
            {
                bfs.name = "Enemy_BFS_Test";
                ConfigureBenchmarkDetection(bfs);
                Debug.Log($"Spawned BFS test enemy at {bfsPos}");
            }
        }

        public static GameObject SpawnAt(Vector3 position, int floor)
        {
            // If in test mode, this should generally be handled by SpawnTestPair,
            // but if called directly, we'll keep the random logic.
            
            EnemyController.PathfindingMode mode = Random.value > 0.5f ? 
                EnemyController.PathfindingMode.AStar : 
                EnemyController.PathfindingMode.BFS;

            return SpawnAt(position, floor, mode);
        }

        public static GameObject SpawnAt(Vector3 position, int floor, EnemyController.PathfindingMode mode)
        {
            EnemyVisualController.CleanOrphanSlimeVisuals();
            string typePrefix = mode == EnemyController.PathfindingMode.BFS ? "BFS_Slime" : EnemyName;
            GameObject enemy = new GameObject(IsBossFloor(floor) ? $"Boss_Floor_{floor}" : $"{typePrefix}_Floor_{floor}");
            enemy.transform.position = position;
            enemy.transform.rotation = Quaternion.identity;

            SetupEnemy(enemy, floor, mode);
            
            if (mode == EnemyController.PathfindingMode.BFS)
            {
                Debug.Log("BFS Enemy spawned", enemy);
            }
            
            return enemy;
        }

        public static GameObject GetOrCreateEnemy(Vector3 spawnPosition)
        {
            EnemyVisualController.CleanOrphanSlimeVisuals();

            GameObject enemy = GameObject.Find(EnemyName);
            if (enemy == null)
            {
                enemy = new GameObject(EnemyName);
            }

            enemy.transform.position = spawnPosition;
            enemy.transform.rotation = Quaternion.identity;

            SetupEnemy(enemy);
            return enemy;
        }

        public static GameObject SpawnFloorEnemy(int floor)
        {
            EnemyVisualController.CleanOrphanSlimeVisuals();

            Vector3 spawnPosition = FindTestSpawnPosition();
            
            // Randomly decide mode
            EnemyController.PathfindingMode mode = Random.value > 0.5f ? 
                EnemyController.PathfindingMode.AStar : 
                EnemyController.PathfindingMode.BFS;

            return SpawnAt(spawnPosition, floor, mode);
        }

        public static GameObject SpawnTestEnemy()
        {
            EnemyVisualController.CleanOrphanSlimeVisuals();

            Vector3 spawnPosition = FindTestSpawnPosition();
            GameObject enemy = new GameObject($"{EnemyName}_{Time.frameCount}");
            enemy.transform.position = spawnPosition;
            enemy.transform.rotation = Quaternion.identity;

            SetupEnemy(enemy);
            Debug.Log("Spawned test enemy", enemy);
            return enemy;
        }

        public static void SetupEnemy(GameObject enemy)
        {
            int floor = FloorManager.Instance != null ? FloorManager.Instance.CurrentFloor : 1;
            SetupEnemy(enemy, floor, EnemyController.PathfindingMode.AStar);
        }

        public static void SetupEnemy(GameObject enemy, int floor)
        {
            SetupEnemy(enemy, floor, EnemyController.PathfindingMode.AStar);
        }

        public static void SetupEnemy(GameObject enemy, int floor, EnemyController.PathfindingMode mode)
        {
            if (enemy == null)
            {
                Debug.LogWarning("Enemy setup skipped: Enemy GameObject is missing.");
                return;
            }

            EnemyVisualController.CleanOrphanSlimeVisuals();
            Remove3DRenderComponents(enemy);
            Remove3DColliders(enemy);

            Rigidbody2D rb = enemy.GetComponent<Rigidbody2D>();
            if (rb == null)
            {
                rb = enemy.AddComponent<Rigidbody2D>();
            }

            if (rb != null)
            {
                rb.bodyType = RigidbodyType2D.Dynamic;
                rb.gravityScale = 0f;
                rb.freezeRotation = true;
                rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
                rb.interpolation = RigidbodyInterpolation2D.Interpolate;
            }

            CircleCollider2D circleCollider = enemy.GetComponent<CircleCollider2D>();
            if (circleCollider == null)
            {
                circleCollider = enemy.AddComponent<CircleCollider2D>();
            }

            if (circleCollider != null)
            {
                circleCollider.radius = 0.38f;
            }

            SpriteRenderer spriteRenderer = enemy.GetComponent<SpriteRenderer>();
            if (spriteRenderer == null)
            {
                spriteRenderer = enemy.AddComponent<SpriteRenderer>();
            }

            if (spriteRenderer != null)
            {
                spriteRenderer.sprite = CreateSquareSprite(IsBossFloor(floor) ? new Color(0.55f, 0.15f, 1f) : new Color(1f, 0.12f, 0.08f));
                spriteRenderer.color = Color.white;
                spriteRenderer.sortingOrder = IsBossFloor(floor) ? 22 : 21;
            }

            CharacterStats stats = enemy.GetComponent<CharacterStats>();
            if (stats == null)
            {
                stats = enemy.AddComponent<CharacterStats>();
            }

            if (stats != null)
            {
                int maxHP = IsBossFloor(floor) ? 80 + floor * 6 : 10 + (floor - 1) * 3;
                int attack = IsBossFloor(floor) ? 18 + floor : 8 + Mathf.Max(0, floor - 1);
                stats.Configure(maxHP, attack, 0, 0.05f, 1.5f, 0f, IsBossFloor(floor) ? 2.5f : 3f);
            }

            Health health = enemy.GetComponent<Health>();
            if (health == null)
            {
                health = enemy.AddComponent<Health>();
            }

            if (health != null)
            {
                health.Configure(stats != null ? stats.MaxHP : 10, stats != null ? stats.MaxHP : 10);
            }

            WorldHealthBar worldHealthBar = enemy.GetComponent<WorldHealthBar>();
            if (worldHealthBar == null)
            {
                worldHealthBar = enemy.AddComponent<WorldHealthBar>();
            }

            if (worldHealthBar != null)
            {
                worldHealthBar.Bind(health);
                worldHealthBar.SetLocalOffset(IsBossFloor(floor) ? new Vector3(0f, 1.6f, 0f) : new Vector3(0f, 1.0f, 0f));
            }

            DamageDealer damageDealer = enemy.GetComponent<DamageDealer>();
            if (damageDealer == null)
            {
                damageDealer = enemy.AddComponent<DamageDealer>();
            }

            if (damageDealer != null)
            {
                damageDealer.Damage = stats != null ? stats.Attack : 8;
                damageDealer.Cooldown = 1f;
            }

            if (enemy.GetComponent<LootDropper>() == null)
            {
                enemy.AddComponent<LootDropper>();
                Debug.Log("LootDropper attached to Enemy.", enemy);
            }

            EnemyController enemyController = enemy.GetComponent<EnemyController>();
            if (enemyController == null)
            {
                enemyController = enemy.AddComponent<EnemyController>();
            }

            if (enemyController != null)
            {
                enemyController.Mode = mode;
                enemyController.EnemyDetectionMode = EnemyController.DetectionMode.Range;
                enemyController.MoveSpeed = stats != null ? stats.MoveSpeed : 3f;
                enemyController.DetectionRange = 20f;
                enemyController.AttackRange = 1.5f;
                enemyController.AttackDamage = stats != null ? stats.Attack : 8;
                enemyController.AttackCooldown = 1f;
            }

            bool createdVisualController = false;
            EnemyVisualController visualController = enemy.GetComponent<EnemyVisualController>();
            if (visualController == null)
            {
                visualController = enemy.AddComponent<EnemyVisualController>();
                createdVisualController = true;
            }

            if (visualController != null)
            {
                EnemySpawner spawner = instance != null ? instance : Object.FindAnyObjectByType<EnemySpawner>();
                if (spawner != null)
                {
                    visualController.Configure(IsBossFloor(floor), spawner.normalSlimePrefab, spawner.bfsSlimePrefab, spawner.bossSlimePrefab, spawner.bossFallbackSlimePrefab, createdVisualController);
                    Debug.Log($"{(mode == EnemyController.PathfindingMode.BFS ? "BFS" : "A*")} enemy visual assigned: {(mode == EnemyController.PathfindingMode.BFS ? (spawner.bfsSlimePrefab != null ? spawner.bfsSlimePrefab.name : "Blue_Slime fallback") : (spawner.normalSlimePrefab != null ? spawner.normalSlimePrefab.name : "Green_Slime fallback"))}");
                }
                else
                {
                    visualController.Configure(IsBossFloor(floor), createdVisualController);
                }
            }

            CreateEnemyLabel(enemy, mode);
        }

        private static void ConfigureBenchmarkDetection(GameObject enemy)
        {
            EnemyController enemyController = enemy != null ? enemy.GetComponent<EnemyController>() : null;
            if (enemyController == null)
            {
                return;
            }

            enemyController.EnemyDetectionMode = EnemyController.DetectionMode.WholeMap;
            enemyController.DetectionRange = float.MaxValue;
            Debug.Log($"{enemy.name} detection mode set to WholeMap", enemy);
        }

            private static void CreateEnemyLabel(GameObject enemy, EnemyController.PathfindingMode mode)
            {
            GameObject labelObj = new GameObject("AlgorithmLabel");
            labelObj.transform.SetParent(enemy.transform, false);
            labelObj.transform.localPosition = new Vector3(0, 1.3f, 0); // Position above head

            Canvas canvas = labelObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            labelObj.AddComponent<UnityEngine.UI.CanvasScaler>();

            RectTransform rect = labelObj.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(200, 50);
            rect.localScale = new Vector3(0.01f, 0.01f, 1f);

            GameObject textObj = new GameObject("LabelText");
            textObj.transform.SetParent(labelObj.transform, false);
            UnityEngine.UI.Text text = textObj.AddComponent<UnityEngine.UI.Text>();
            text.text = mode == EnemyController.PathfindingMode.BFS ? "BFS" : "A*";
            text.fontSize = 40;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = mode == EnemyController.PathfindingMode.BFS ? new Color(0.6f, 0.4f, 1f) : Color.green;
            
            Font legacyFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (legacyFont != null) text.font = legacyFont;

            RectTransform textRect = textObj.GetComponent<RectTransform>();
            textRect.sizeDelta = new Vector2(200, 50);
            textRect.anchoredPosition = Vector2.zero;

            // Ensure label is visible and above health bar
            UnityEngine.UI.Image bg = null; // No background requested but can be added if hard to see
            canvas.sortingOrder = 40; // High enough to be over health bar (usually 30-35)

            Debug.Log($"Enemy label created: {text.text}", enemy);
            }

        private static void Remove3DRenderComponents(GameObject enemy)
        {
            foreach (MeshRenderer meshRenderer in enemy.GetComponents<MeshRenderer>())
            {
                Object.DestroyImmediate(meshRenderer);
            }

            foreach (MeshFilter meshFilter in enemy.GetComponents<MeshFilter>())
            {
                Object.DestroyImmediate(meshFilter);
            }
        }

        private static void Remove3DColliders(GameObject enemy)
        {
            foreach (Collider collider3D in enemy.GetComponents<Collider>())
            {
                Object.DestroyImmediate(collider3D);
            }
        }

        private static Vector3 FindTestSpawnPosition()
        {
            FloorManager floorManager = FloorManager.Instance;
            if (floorManager != null && !floorManager.IsVillageMode)
            {
                return DungeonEnemySpawnPosition;
            }

            GameObject player = GameObject.Find("Player");
            Vector3 fallback = player != null ? player.transform.position + new Vector3(5f, 0f, 0f) : DungeonEnemySpawnPosition;
            PathfindingGrid grid = PathfindingGrid.Instance;
            if (player == null || grid == null)
            {
                return fallback;
            }

            foreach (GridNode node in grid.GetAllNodes())
            {
                if (node == null || !node.IsWalkable)
                {
                    continue;
                }

                float distance = Vector2.Distance(player.transform.position, node.WorldPosition);
                if (distance >= 4f && distance <= 6f)
                {
                    return node.WorldPosition;
                }
            }

            return fallback;
        }

        private static bool IsBossFloor(int floor)
        {
            return floor > 0 && floor % 10 == 0;
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
